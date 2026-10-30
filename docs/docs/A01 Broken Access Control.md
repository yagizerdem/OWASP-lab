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

To find more IDs, call the `users` endpoint below.

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

### 1. Login

`POST /lab/login` checks an e-mail and password and returns a signed JWT. The token is also set as an HTTP-only `token` cookie.

Request body:

```json
{
  "email": "patience_cummerata@borer.us",
  "password": "pass335598"
}
```

| Result                     | Status | Message                               |
| -------------------------- | ------ | ------------------------------------- |
| Correct credentials        | `200`  | `Login successful`, with `data.token` |
| Wrong password             | `401`  | `Invalid password or email`           |
| E-mail that does not exist | `404`  | `Invalid password or email`           |

The token is **not required** by any other endpoint. Logging in only shows that a valid identity exists and that the service never uses it.

### 2. IDOR: accounts by user ID

`GET /lab/vulnerable/idor/user/{userId}` returns every bank account whose `OwnerId` matches the ID in the URL. It never asks who is calling.

### 3. IDOR: account by number

`GET /lab/vulnerable/idor/account/{accountNo}` returns the account with that number. Account numbers are 16-digit values, so they can be guessed or collected.

### 4 to 6. Customer risk profiles (read)

The risk profile holds internal staff data that should only be visible to administrators: risk score, KYC status, an internal note and the flag.

- `GET .../customer-risk-profiles` returns all of them.
- `GET .../customer-risk-profiles/{id}` returns one. An unknown ID still answers `200` with `data: null`.
- `GET .../customer-risk-profiles/flagged` returns only the profiles with `isFlagged` set to `true`.

### 7. Change a flag

`PUT .../customer-risk-profiles/{id}/flag?isFlagged=true` sets the flag. An unknown ID returns `404` with `Customer risk profile not found`.

### 8. Delete a risk profile

`DELETE .../customer-risk-profiles/{id}` removes the profile permanently. A second call with the same ID returns `404`.

### 9. List all users

`GET .../users` returns every user with `username`, `email` and the **plain-text `password`**. This is the quickest way to collect IDs and credentials for the other requests.

### 10. Overwrite a balance

`PUT .../bank-accounts/{accountNo}/balance?balance=1000000` replaces the balance of any account and returns the updated account. An unknown number returns `404` with `Bank account not found`.

## Test it from Swagger

1. Open `http://localhost:5010/swagger`.
2. Expand an endpoint and click **Try it out**.
3. Fill in the parameters, then click **Execute**.
4. Read the response body and status code under **Server response**.

You do **not** need to log in or click **Authorize**. If an endpoint answers without any credentials, access control is missing.

### Scenario A: read another user's accounts (IDOR)

1. Call **`GET /lab/vulnerable/missing-authorization/users`** and copy the `id` of any user.
2. Call **`GET /lab/vulnerable/idor/user/{userId}`** and paste that ID into `userId`.
3. The response lists that user's accounts and balances, although you never proved who you are.
4. Copy an `accountNumber` from the response and call **`GET /lab/vulnerable/idor/account/{accountNo}`** to read the same account directly.

Expected with the seed data: `userId` = `d8bdb799-3030-4882-a74e-dfca61771004` returns account `1719307719708282` with balance `23907`.

### Scenario B: read internal data (missing authorization)

1. Call **`GET .../customer-risk-profiles`**. You receive all 100 profiles with scores, KYC status and staff notes.
2. Call **`GET .../customer-risk-profiles/flagged`** to list only the customers flagged for review.
3. Call **`GET .../users`** and notice that passwords come back in clear text.

### Scenario C: change data you should not touch

1. Call **`PUT .../bank-accounts/{accountNo}/balance`** with `accountNo` = `1719307719708282` and `balance` = `1000000`. The response shows the new balance.
2. Call **`GET /lab/vulnerable/idor/account/1719307719708282`** to confirm the change stuck.
3. Call **`PUT .../customer-risk-profiles/{id}/flag`** with `id` = `7d4720ab-373c-40d9-9960-19a51d19bea0` and `isFlagged` = `false` to clear a flag.
4. Call **`DELETE .../customer-risk-profiles/{id}`** with the same ID to remove the profile.

:::warning

Steps in scenario C change or delete data. To get the original data back, run `docker compose down -v` and then `docker compose up -d`.

:::

## How to fix it

There is no secure implementation in the repository yet. A fix would normally:

1. Validate the JWT on every request with ASP.NET Core authentication and mark controllers `[Authorize]`.
2. Take the user ID from the token claims (`sub`), never from the URL or body.
3. Check ownership before returning or changing a record, and answer `403` or `404` when it does not match.
4. Restrict admin functions (risk profiles, listing users, changing balances) to an administrator role.
5. Return DTOs that leave out sensitive fields, and store passwords as salted hashes.

:::warning

This service is intentionally insecure. Run it only on your own machine and never expose it to the internet.

:::
