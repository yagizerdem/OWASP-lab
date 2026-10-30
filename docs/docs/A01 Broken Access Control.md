---
title: A01 Broken Access Control
sidebar_position: 1
---

## Overview

Broken Access Control happens when a service lets a caller do something their identity should not allow: reading another customer's data, calling an admin function, or changing records they do not own. It is number one in the OWASP Top 10 (2025).

This lab is a small banking API. It contains two kinds of access-control flaws, both written on purpose:

| Flaw                                        | What goes wrong                                                                                  | Endpoints                                   |
| ------------------------------------------- | ------------------------------------------------------------------------------------------------ | ------------------------------------------- |
| **IDOR** (insecure direct object reference) | The server returns whatever user ID or account number you send, without checking you own it.     | `/lab/vulnerable/idor/...`                  |
| **Missing function-level authorization**    | Admin-style endpoints (list users, edit balances, delete risk profiles) can be called by anyone. | `/lab/vulnerable/missing-authorization/...` |

:::note

Only the vulnerable side exists for this lab so far. There are no `/lab/secure/...` endpoints yet.

:::

## Access the lab

Start the labs as described in the Installation section of the Introduction page. The Broken Access Control service then listens on:

| What             | Address                                       |
| ---------------- | --------------------------------------------- |
| **Swagger UI**   | `http://localhost:5010/swagger`               |
| API base URL     | `http://localhost:5010`                       |
| Second HTTP port | `5011` (same service, also plain HTTP)        |
| Database         | `BrokenAccessControl_LAB` on `sqlserver:1433` |

Open `http://localhost:5010/swagger` in your browser. Swagger is only enabled when `ASPNETCORE_ENVIRONMENT` is `Development`, which the Docker setup already does.

If you run the project from your IDE or with `dotnet run` instead of Docker, use the launch profile: `https` serves `https://localhost:5010` and `http://localhost:5011`, while `http` serves only `http://localhost:5011`. The Swagger UI is at `/swagger` on whichever address you use.

## Seeded data

On first start the service applies its migrations and seeds the database from the JSON files in `src/Context/Data`:

- **100 users**, each with a username, e-mail and plain-text password.
- **100 bank accounts**, each owned by one user and holding a balance.
- **100 customer risk profiles**, one per user, with a risk score, KYC status, internal staff note and a flag (29 profiles are flagged).

The seed files are committed, so the IDs below are the same on every fresh start:

| Item                    | Value                                  |
| ----------------------- | -------------------------------------- |
| User `juliana` ID       | `d8bdb799-3030-4882-a74e-dfca61771004` |
| User `juliana` e-mail   | `patience_cummerata@borer.us`          |
| User `juliana` password | `pass335598`                           |
| Her bank account no.    | `1719307719708282` (balance `23907`)   |
| Her risk profile ID     | `7d4720ab-373c-40d9-9960-19a51d19bea0` |
| Second user ID          | `84722c98-d9ce-4263-b5da-f0e4bba12173` |
| Second account no.      | `5518487640519250` (balance `3574`)    |

To find more data, visit the [seed data](https://github.com/yagizerdem/OWASP-lab/tree/main/Lab/BrokenAccessControl/src/Context/Data) in the repository.

## Response format

Every endpoint answers with the same JSON envelope:

```json
{
  "message": "Success",
  "data": {},
  "status": 200
}
```

Errors use the same shape, with a different `status` and a `message` that names the problem.

## Endpoints

All paths start with `http://localhost:5010`. None of the endpoints except `login` expect any credentials, and that is the point of the lab.

| #   | Method   | Path                                                                      | Purpose                                        |
| --- | -------- | ------------------------------------------------------------------------- | ---------------------------------------------- |
| 1   | `POST`   | `/lab/login`                                                              | Log in and receive a JWT                       |
| 2   | `GET`    | `/lab/vulnerable/idor/user/{userId}`                                      | List the bank accounts of any user             |
| 3   | `GET`    | `/lab/vulnerable/idor/account/{accountNo}`                                | Read any bank account by its number            |
| 4   | `GET`    | `/lab/vulnerable/missing-authorization/customer-risk-profiles`            | List every customer risk profile               |
| 5   | `GET`    | `/lab/vulnerable/missing-authorization/customer-risk-profiles/{id}`       | Read one risk profile                          |
| 6   | `GET`    | `/lab/vulnerable/missing-authorization/customer-risk-profiles/flagged`    | List only the flagged profiles                 |
| 7   | `PUT`    | `/lab/vulnerable/missing-authorization/customer-risk-profiles/{id}/flag`  | Set or clear the flag (query `isFlagged`)      |
| 8   | `DELETE` | `/lab/vulnerable/missing-authorization/customer-risk-profiles/{id}`       | Delete a risk profile                          |
| 9   | `GET`    | `/lab/vulnerable/missing-authorization/users`                             | List every user, including passwords           |
| 10  | `PUT`    | `/lab/vulnerable/missing-authorization/bank-accounts/{accountNo}/balance` | Overwrite an account balance (query `balance`) |

:::tip

Swagger may also list the same IDOR actions under a short alias such as `/IDOR/GetUserAccountsByUserId/{userId}`. They reach the same code, so use whichever you prefer.

:::

## Walkthrough in Swagger

In this walkthrough you play **juliana**, an ordinary customer of the bank. Her login details are in the seed data, so you can log in as her. The goal is to see how much you can read and change with nothing but her account, and in the end without logging in at all.

You will also need a second customer to attack. That is **damien_dach**:

| Who         | User ID                                | E-mail                           | Password     | Account no.        | Balance |
| ----------- | -------------------------------------- | -------------------------------- | ------------ | ------------------ | ------- |
| juliana     | `d8bdb799-3030-4882-a74e-dfca61771004` | `patience_cummerata@borer.us`    | `pass335598` | `1719307719708282` | `23907` |
| damien_dach | `84722c98-d9ce-4263-b5da-f0e4bba12173` | `clovis_satterfield@mcclure.biz` | `pass375855` | `5518487640519250` | `3574`  |

### How to send a request in Swagger

Open `http://localhost:5010/swagger`. Every step below follows the same pattern:

1. Find the endpoint in the list and click it to expand it.
2. Click **Try it out**.
3. Paste the values from the step into the fields Swagger shows: path parameters such as `userId`, query parameters such as `balance`, or the JSON **Request body**.
4. Click **Execute** and read the status code and body under **Server response**.

You never need the **Authorize** button. Every endpoint except login answers without any credentials, and that is the whole problem this lab is about.

### Step 1: Log in as juliana

Endpoint: **`POST /lab/login`**

Login is the only endpoint that checks who you are. Paste this into the request body:

```json
{
  "email": "patience_cummerata@borer.us",
  "password": "pass335598"
}
```

You get `200` with `Login successful` and a JWT in `data.token`. The same token is also set as an HTTP-only `token` cookie.

```json
{
  "message": "Login successful",
  "data": {
    "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
  },
  "status": 200
}
```

You don't need to copy the token. None of the other endpoints read it, so being logged in or not makes no difference to them. Keep that in mind for the rest of the walkthrough.

While you are here, try two wrong logins and compare the answers:

| Request body                                                      | Status | What it tells an attacker  |
| ----------------------------------------------------------------- | ------ | -------------------------- |
| `{ "email": "patience_cummerata@borer.us", "password": "wrong" }` | `401`  | This e-mail is registered. |
| `{ "email": "nobody@example.com", "password": "wrong" }`          | `404`  | This e-mail is not.        |

The message is the same, but the status code gives away which e-mails exist.

### Step 2: Look at your own accounts

Endpoint: **`GET /lab/vulnerable/idor/user/{userId}`**

This is the request a banking app would send to show juliana her accounts. Paste juliana's ID into `userId`:

```text
d8bdb799-3030-4882-a74e-dfca61771004
```

The response lists her one account:

```json
{
  "message": "Success",
  "data": [
    {
      "accountNumber": "1719307719708282",
      "balance": 23907.00,
      "owner": null,
      "ownerId": "d8bdb799-3030-4882-a74e-dfca61771004",
      "id": "07801581-dc9d-4e00-88c8-5574688c0c98",
      ...
    }
  ],
  "status": 200
}
```

Nothing wrong yet. The problem is where the user ID comes from: the URL, which the caller controls.

### Step 3: Read someone else's accounts (IDOR)

Same endpoint: **`GET /lab/vulnerable/idor/user/{userId}`**

Now replace the ID with damien_dach's:

```text
84722c98-d9ce-4263-b5da-f0e4bba12173
```

You get damien's account and balance, even though you are juliana, or nobody at all:

```json
"accountNumber": "5518487640519250",
"balance": 3574.00,
"ownerId": "84722c98-d9ce-4263-b5da-f0e4bba12173"
```

This is an **IDOR** (insecure direct object reference). The server looks up whatever ID it receives and never compares it with the person making the request. A correct version would ignore the ID in the URL and take the user ID from the verified token (the `sub` claim), or at least check that `ownerId` matches the caller and answer `403` otherwise.

### Step 4: Read an account by its number

Endpoint: **`GET /lab/vulnerable/idor/account/{accountNo}`**

The same flaw, keyed by account number instead of user ID. Paste damien's account number into `accountNo`:

```text
5518487640519250
```

The response is a list with that one account, including its balance and owner ID. Account numbers are 16-digit values that end up on statements and transfer receipts, so they are easy to collect or guess.

### Step 5: List every user, including passwords

Endpoint: **`GET /lab/vulnerable/missing-authorization/users`**

This one has no parameters. Click **Execute** and you get all 100 users. Search the response for `damien_dach`:

```json
{
  "username": "damien_dach",
  "password": "pass375855",
  "email": "clovis_satterfield@mcclure.biz",
  "bankAccounts": [],
  "id": "84722c98-d9ce-4263-b5da-f0e4bba12173",
  ...
}
```

This is **missing function-level authorization**. A user list is an admin function, but the endpoint never checks for an admin role, or for any login at all. And because passwords are stored in plain text, it hands over every account. To prove it, go back to step 1 and log in as damien:

```json
{
  "email": "clovis_satterfield@mcclure.biz",
  "password": "pass375855"
}
```

### Step 6: Read internal risk profiles

Risk profiles are notes that bank staff keep about customers: a risk score, the KYC status, an internal comment and whether the customer is flagged for review. Customers should never see them. Three endpoints hand them out to anyone.

**`GET /lab/vulnerable/missing-authorization/customer-risk-profiles/flagged`** has no parameters. It returns the 29 flagged customers. juliana is one of them:

```json
{
  "userId": "d8bdb799-3030-4882-a74e-dfca61771004",
  "riskScore": 4,
  "kycStatus": "Pending",
  "internalNote": "Ipsa quia iste officia non dolorum odio.",
  "isFlagged": true,
  "id": "7d4720ab-373c-40d9-9960-19a51d19bea0",
  ...
}
```

**`GET /lab/vulnerable/missing-authorization/customer-risk-profiles/{id}`** returns one profile. Paste damien's profile ID into `id`:

```text
952abe75-148e-42bd-8f14-de9bccdcf581
```

You get his risk score `5`, KYC status `Pending` and his internal note. Then try an ID that doesn't exist:

```text
00000000-0000-0000-0000-000000000000
```

The answer is still `200`, with `data: null`.

**`GET /lab/vulnerable/missing-authorization/customer-risk-profiles`** returns all 100 profiles at once.

### Step 7: Change someone's balance

Endpoint: **`PUT /lab/vulnerable/missing-authorization/bank-accounts/{accountNo}/balance`**

Give damien a million. Fill in the two fields:

| Field       | Value              |
| ----------- | ------------------ |
| `accountNo` | `5518487640519250` |
| `balance`   | `1000000`          |

The response shows the updated account with `"balance": 1000000` and the message `Bank account balance updated successfully`. Run step 4 again with the same account number to confirm the new balance is stored in the database.

An account number that doesn't exist, such as `0000000000000000`, returns `404` with `Bank account not found`.

### Step 8: Clear a flag and delete a profile

Endpoint: **`PUT /lab/vulnerable/missing-authorization/customer-risk-profiles/{id}/flag`**

Take juliana off the review list. Fill in:

| Field       | Value                                  |
| ----------- | -------------------------------------- |
| `id`        | `7d4720ab-373c-40d9-9960-19a51d19bea0` |
| `isFlagged` | `false`                                |

The response shows her profile with `"isFlagged": false`. Run the `flagged` request from step 6 again: it now returns 28 profiles, and juliana is gone.

Endpoint: **`DELETE /lab/vulnerable/missing-authorization/customer-risk-profiles/{id}`**

Now remove her profile completely. Paste the same ID into `id`:

```text
7d4720ab-373c-40d9-9960-19a51d19bea0
```

You get `Customer risk profile deleted successfully`. Click **Execute** again and the answer becomes `404` with `Customer risk profile not found`, because the profile no longer exists.

:::warning

Steps 7 and 8 change and delete data. To get the original seed data back, run `docker compose down -v` and then `docker compose up -d`. This resets the database of every lab, not just this one.

:::

## What a fix would look like

There is no secure version of these endpoints in the lab yet, but the fixes are short:

- **IDOR (steps 2 to 4):** don't trust IDs from the URL for ownership. Read the user ID from the verified JWT and only return accounts whose `ownerId` matches it. Anything else gets `403`.
- **Missing authorization (steps 5 to 8):** require a valid token on every endpoint (`[Authorize]`) and an admin role on the admin ones (`[Authorize(Roles = "Admin")]`).
- **Plain-text passwords (step 5):** store only a salted password hash, and never include the password field in any response.
