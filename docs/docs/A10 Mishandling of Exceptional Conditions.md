---
title: A10 Mishandling of Exceptional Conditions
sidebar_position: 10
---

## Overview

Mishandling of Exceptional Conditions is about what a service does when something goes wrong. An error in the wrong place can open a door (a failed check that is treated as "allowed") or corrupt data (an operation that stops halfway and leaves money gone). It is a new entry, number ten, in the OWASP Top 10 (2025).

This lab contains two error-handling flaws, both written on purpose:

| Flaw                              | What goes wrong                                                                                              | Endpoints                                  |
| --------------------------------- | ------------------------------------------------------------------------------------------------------------ | ------------------------------------------ |
| **Fail-open on error**            | When an authorization check throws, the `catch` block decides to *allow* the request, so bad input gets in.  | `/lab/.../exceptions/documents/{id}`       |
| **Half-finished operation**       | A wallet transfer debits the sender, then the next step throws, and nothing undoes the debit.                | `/lab/.../exceptions/wallets/transfer`     |

Every flaw has a vulnerable endpoint under `/lab/vulnerable/...` and a secure endpoint under `/lab/secure/...` that does the same job correctly. Two read endpoints under `/lab/exceptions/...` show the data both sides work with.

## Access the lab

Start the labs as described in the Installation section of the Introduction page. The Mishandling of Exceptional Conditions service then listens on:

| What             | Address                              |
| ---------------- | ------------------------------------ |
| **Swagger UI**   | `http://localhost:5050/swagger`      |
| API base URL     | `http://localhost:5050`              |
| Second HTTP port | `5051` (same service, also plain HTTP) |
| Database         | `MishandlingOfExceptionalConditions_LAB` on `sqlserver:1433` |

Open `http://localhost:5050/swagger` in your browser. Swagger is only enabled when `ASPNETCORE_ENVIRONMENT` is `Development`, which the Docker setup already does.

If you run the project from your IDE or with `dotnet run` instead of Docker, use the launch profile: `http` serves `http://localhost:5050`, while `https` serves `https://localhost:5051` and `http://localhost:5050`.

## Seeded data

On first start the service applies its migrations and seeds the database from the JSON files in `src/Context/Data`:

- **`dummy.documents.json`**: 60 documents, some marked private. A private document should only be readable by its owner.
- **`dummy.wallets.json`**: one wallet per user, each with a balance.
- **`dummy.users.json`**: 100 users.

Document IDs are generated fresh on every start, so this walkthrough has you copy a real one from the documents endpoint in step 1. One stable user is useful as a wallet owner:

| User ID                                | Wallet balance |
| -------------------------------------- | -------------- |
| `660314ca-bf75-4640-912d-675592259f70` | `1000`         |

This user also owns at least one private document.

To find more data, visit the [seed data](https://github.com/yagizerdem/OWASP-lab/tree/main/Lab/MishandlingOfExceptionalConditions/src/Context/Data) in the repository.

## Response format

Every successful response uses the same JSON envelope:

```json
{
  "message": "Success",
  "data": {},
  "status": 200
}
```

Errors are written by the shared error handler, which uses capitalised keys. An unexpected crash (not a planned error) becomes a generic `500`:

```json
{
  "Message": "Internal server error",
  "Data": null,
  "Status": 500
}
```

## Endpoints

All paths start with `http://localhost:5050`. None of them expect credentials in a header.

| #   | Method | Path                                           | Purpose                                                   |
| --- | ------ | ---------------------------------------------- | --------------------------------------------------------- |
| 1   | `GET`  | `/lab/exceptions/documents`                    | List documents with their IDs, owners and privacy flags   |
| 2   | `GET`  | `/lab/vulnerable/exceptions/documents/{id}`    | Read a document; fails open on a bad `userId`             |
| 3   | `GET`  | `/lab/secure/exceptions/documents/{id}`        | Read a document; fails closed                             |
| 4   | `GET`  | `/lab/exceptions/wallets/{userId}`             | Read a user's wallet balance                              |
| 5   | `POST` | `/lab/vulnerable/exceptions/wallets/transfer`  | Transfer money; debits first, no rollback                 |
| 6   | `POST` | `/lab/secure/exceptions/wallets/transfer`      | Transfer money; all-or-nothing in a transaction           |

## Walkthrough in Swagger

In this walkthrough you first read a document you should not be allowed to see, then make money disappear, by sending input that makes the code take its error path.

### How to send a request in Swagger

Open `http://localhost:5050/swagger`. Every step below follows the same pattern:

1. Find the endpoint in the list and click it to expand it.
2. Click **Try it out**.
3. Paste the values from the step into the fields: the path parameter (`id` or `userId`), the query parameter (`userId`), or the JSON **Request body**.
4. Click **Execute** and read the status code and body under **Server response**.

You never need the **Authorize** button in this lab.

### Step 1: Find a private document

Endpoint: **`GET /lab/exceptions/documents`**

This endpoint has no parameters. Click **Execute** and find a document with `"isPrivate": true`. Copy its `id`:

```json
{
  "id": "copy-this-guid",
  "ownerId": "660314ca-bf75-4640-912d-675592259f70",
  "title": "Minus sit dolore molestiae soluta",
  "isPrivate": true
}
```

Use that document's ID below wherever it says `<private doc id>`. You are **not** its owner; you have no account at all.

### Step 2: Read it as the owner would (and get refused)

Endpoint: **`GET /lab/vulnerable/exceptions/documents/{id}`**

First behave normally. Put the private document's ID into `id`, and in the `userId` query field put an ID that is not the owner, for example all zeros:

| Field    | Value                                  |
| -------- | -------------------------------------- |
| `id`     | `<private doc id>`                     |
| `userId` | `11111111-1111-1111-1111-111111111111` |

The ownership check runs: your ID is a valid GUID but not the owner, so you get `403` with `You cannot read this document`. So far the access control works.

### Step 3: Break the check and fail open

Same endpoint. Now put something that is **not** a GUID into `userId`:

| Field    | Value              |
| -------- | ------------------ |
| `id`     | `<private doc id>` |
| `userId` | `abc`              |

You get `200` and the full private document, content and all:

```json
{
  "message": "Success",
  "data": {
    "title": "Minus sit dolore molestiae soluta",
    "content": "Qui illum excepturi accusantium...",
    "isPrivate": true,
    ...
  },
  "status": 200
}
```

Inside, the code parses `userId` into a GUID to compare it with the owner. `abc` is not a GUID, so the parse throws, and the `catch` block sets "can read" to `true`. The error path decided to *allow* the request. Sending deliberately broken input was enough to read a private document.

### Step 4: The secure endpoint fails closed

Endpoint: **`GET /lab/secure/exceptions/documents/{id}`**

Send the same broken input:

| Field    | Value              |
| -------- | ------------------ |
| `id`     | `<private doc id>` |
| `userId` | `abc`              |

This time you get `401` with `A valid userId is required`. The secure endpoint validates the input first and refuses what it cannot make sense of, instead of guessing. Access is granted only when the check positively succeeds; anything else is denied. (With a valid non-owner ID it returns `403`, exactly like step 2.)

### Step 5: Check the balances before a transfer

Endpoint: **`GET /lab/exceptions/wallets/{userId}`**

Put the known user's ID into `userId`:

```text
660314ca-bf75-4640-912d-675592259f70
```

You get the balance, `1000`. Note it down so you can compare after the next step.

### Step 6: Send money to a wallet that does not exist

Endpoint: **`POST /lab/vulnerable/exceptions/wallets/transfer`**

Transfer from the known user to a `toUserId` that has no wallet. Any random GUID works, because it belongs to no user:

```json
{
  "fromUserId": "660314ca-bf75-4640-912d-675592259f70",
  "toUserId": "00000000-0000-0000-0000-000000000000",
  "amount": 100
}
```

You get `500` with `Internal server error`. Now look at the sender's balance again with **`GET /lab/exceptions/wallets/660314ca-bf75-4640-912d-675592259f70`**: it is `900`, not `1000`.

The transfer debited the sender and saved that change, then tried to load the receiver's wallet, which does not exist, and threw. Nothing undid the debit. The money left the sender and reached nobody: 100 simply vanished.

### Step 7: The secure transfer is all-or-nothing

Endpoint: **`POST /lab/secure/exceptions/wallets/transfer`**

Send the same request:

```json
{
  "fromUserId": "660314ca-bf75-4640-912d-675592259f70",
  "toUserId": "00000000-0000-0000-0000-000000000000",
  "amount": 100
}
```

You get `404` with `Receiver wallet not found`. Check the sender's balance again: it is unchanged (`900` if you ran step 6, otherwise `1000`). The secure endpoint checks everything that can fail *before* it changes anything, and makes both balance updates inside one database transaction, so an error leaves both wallets exactly as they were.

:::warning

Step 6 changes data (and does not give it back). To restore the original balances and documents, run `docker compose down -v` and then `docker compose up -d`. This resets the database of every lab, not just this one.

:::

## What the secure version does differently

- **Fail-open (steps 2 to 4):** the secure endpoint validates input before using it and only grants access when the check positively succeeds. Input it cannot understand ends the request with an error; the error path never defaults to "allow".
- **Half-finished operation (steps 5 to 7):** the secure transfer checks every precondition before making any change, and wraps the debit and credit in a single transaction, so either both happen or neither does.
