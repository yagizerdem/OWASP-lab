---
title: A06 Insecure Design
sidebar_position: 6
---

## Overview

Insecure Design is not a bug in the code; the code does exactly what it was written to do. The problem is that the design itself is unsafe: a password reset that hands the code straight back to the caller, or a signup bonus that nobody stops you from claiming twice. You cannot patch these away with a bug fix; the flow has to be rethought. It is number six in the OWASP Top 10 (2025).

This lab is a small accounts API with two design flaws, both written on purpose:

| Flaw                              | What goes wrong                                                                                                   | Endpoints                                        |
| --------------------------------- | ----------------------------------------------------------------------------------------------------------------- | ------------------------------------------------ |
| **Weak password-reset flow**      | The reset code is returned in the API response, never expires and can be replayed any number of times.           | `/lab/vulnerable/request-password-reset`, `/lab/vulnerable/reset-password-with-otp` |
| **Business rule never enforced**  | "One signup bonus per account" was only ever an idea. Nothing stops the same account from claiming it over and over. | `/lab/vulnerable/claim-signup-bonus`             |

:::note

This lab shows the insecure design on purpose. The fixes are about changing the flow, not adding a line of validation, so there are no matching `/lab/secure/...` endpoints. There is one secure login endpoint used only as a starting point.

:::

## Access the lab

Start the labs as described in the Installation section of the Introduction page. The Insecure Design service then listens on:

| What             | Address                              |
| ---------------- | ------------------------------------ |
| **Swagger UI**   | `http://localhost:5040/swagger`      |
| API base URL     | `http://localhost:5040`              |
| Second HTTP port | `5041` (same service, also plain HTTP) |
| Database         | `InsecureDesign_LAB` on `sqlserver:1433` |

Open `http://localhost:5040/swagger` in your browser. Swagger is only enabled when `ASPNETCORE_ENVIRONMENT` is `Development`, which the Docker setup already does.

If you run the project from your IDE or with `dotnet run` instead of Docker, use the launch profile: `http` serves `http://localhost:5040`, while `https` serves `https://localhost:5041` and `http://localhost:5040`.

## Seeded data

On first start the service applies its migrations and seeds **100 users** from `dummy.users.json`. Each user has a username, e-mail, plain-text password, age and a security answer. The password-reset and bonus tables start empty; you fill them during the walkthrough.

The user used below:

| Username | E-mail                             | Password    | Security answer |
| -------- | ---------------------------------- | ----------- | --------------- |
| `price`  | `schuyler.blick@waelchibednar.name`| `pass77545` | `Texas`         |

To find more data, visit the [seed data](https://github.com/yagizerdem/OWASP-lab/tree/main/Lab/InsecureDesign/src/Context/Data) in the repository.

## Response format

Every endpoint answers with the same JSON envelope:

```json
{
  "message": "Success",
  "data": {},
  "status": 200
}
```

Errors are written by the shared error handler, which uses capitalised keys:

```json
{
  "Message": "User not found",
  "Data": null,
  "Status": 404
}
```

## Endpoints

All paths start with `http://localhost:5040`. None of them expect any credentials in a header.

| #   | Method | Path                                      | Purpose                                                    |
| --- | ------ | ----------------------------------------- | ---------------------------------------------------------- |
| 1   | `POST` | `/lab/secure/login`                       | Log in with e-mail and password (the one solid endpoint)   |
| 2   | `POST` | `/lab/vulnerable/request-password-reset`  | Ask for a reset code, which comes back in the response     |
| 3   | `POST` | `/lab/vulnerable/reset-password-with-otp` | Set a new password with that code                          |
| 4   | `POST` | `/lab/vulnerable/change-password`         | Change a password using the security answer                |
| 5   | `POST` | `/lab/vulnerable/claim-signup-bonus`      | Claim a 50 credit signup bonus                             |
| 6   | `GET`  | `/lab/vulnerable/wallet-balance`          | Read an account's bonus balance (query `email`)            |

## Walkthrough in Swagger

In this walkthrough you are an attacker who only knows a victim's e-mail address, `price`'s, and nothing else. The goal is to take over the account and then mint free balance, using only the way the flows are designed.

### How to send a request in Swagger

Open `http://localhost:5040/swagger`. Every step below follows the same pattern:

1. Find the endpoint in the list and click it to expand it.
2. Click **Try it out**.
3. Paste the values from the step into the JSON **Request body**.
4. Click **Execute** and read the status code and body under **Server response**.

You never need the **Authorize** button in this lab.

### Step 1: Confirm you cannot log in yet

Endpoint: **`POST /lab/secure/login`**

You know the e-mail but not the password. Paste this:

```json
{
  "email": "schuyler.blick@waelchibednar.name",
  "password": "guess"
}
```

You get `401` with `Invalid password`. The login itself is fine; it is the reset flow around it that is not.

### Step 2: Ask for a reset code and read it from the response

Endpoint: **`POST /lab/vulnerable/request-password-reset`**

Paste just the victim's e-mail:

```json
{
  "email": "schuyler.blick@waelchibednar.name"
}
```

A reset code would normally be sent to the account owner by e-mail or SMS, where only they can read it. Here it comes straight back to you:

```json
{
  "message": "OTP generated (would normally be delivered out-of-band)",
  "data": {
    "otp": "0473"
  },
  "status": 200
}
```

Your value will differ. You now hold the reset code for an account you do not own. This is the design flaw: the secret channel (out-of-band delivery) is the whole point of a reset code, and this design skips it.

Note also that the code is only four digits (`0000`–`9999`). Even without reading it from the response, there are only 10000 of them, and nothing slows down guessing.

### Step 3: Take over the account

Endpoint: **`POST /lab/vulnerable/reset-password-with-otp`**

Use the code from step 2 to set a password you choose:

```json
{
  "email": "schuyler.blick@waelchibednar.name",
  "otp": "0473",
  "newPassword": "attacker-owns-this"
}
```

You get `200` with `password reset successfully`. Go back to **`POST /lab/secure/login`** and log in with the new password; it works. The account is yours.

Now show the second half of the flaw: run this same request a second time, unchanged. It still returns `200`. The code has no expiry and is never marked as used, so a single leaked code works forever. A code that was never issued returns `401` with `Invalid OTP`.

### Step 4: The security-answer reset has the same weakness

Endpoint: **`POST /lab/vulnerable/change-password`**

This endpoint resets a password if you know the user's security answer. Security answers are often guessable facts (a city, a pet, a school):

```json
{
  "email": "schuyler.blick@waelchibednar.name",
  "securityAnswer": "Texas",
  "newPassword": "another-new-password"
}
```

You get `200` with `password changed successfully`. A single guessable answer, with no limit on attempts, is enough to reset the password. A wrong answer returns `401` with `Invalid security answer`, but nothing stops you from trying again and again.

### Step 5: Claim the signup bonus once

Endpoint: **`POST /lab/vulnerable/claim-signup-bonus`**

The intended rule is "each account gets one 50 credit bonus." Claim it:

```json
{
  "email": "schuyler.blick@waelchibednar.name"
}
```

You get `200` and the balance so far:

```json
{
  "message": "signup bonus claimed",
  "data": {
    "balance": 50
  },
  "status": 200
}
```

### Step 6: Claim it again, and again

Same endpoint, exactly the same body. Click **Execute** a few more times. The balance climbs every time: `100`, `150`, `200`. Confirm it with **`GET /lab/vulnerable/wallet-balance`**, putting the e-mail in the `email` field:

```json
{
  "data": {
    "balance": 200
  }
}
```

"One bonus per account" was never turned into something the server enforces: there is no unique constraint, and nothing checks for an existing claim before adding another. The rule lived only in the developers' heads, which is what makes this a design flaw rather than a bug.

:::warning

Steps 3 to 6 change data. To get the original seed data back, run `docker compose down -v` and then `docker compose up -d`. This resets the database of every lab, not just this one.

:::

## How these should be designed

There are no secure endpoints for these flows in the lab. The fixes are changes to the design:

- **Password reset (steps 2 and 3):** send the code out-of-band (e-mail or SMS) and never return it in the response. Make it long enough not to be guessed, give it a short expiry, allow it to be used once, and rate-limit requests per account.
- **Security-answer reset (step 4):** security questions are weak on their own. At most treat them as one factor among several, limit the number of attempts, and prefer a proper out-of-band reset instead.
- **Signup bonus (steps 5 and 6):** turn the rule into something the database enforces, such as a unique constraint on the user, and check for an existing claim inside a transaction before granting a new one.
