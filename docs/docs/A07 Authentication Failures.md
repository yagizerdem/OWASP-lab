---
title: A07 Authentication Failures
sidebar_position: 7
---

## Overview

Authentication Failures are the weaknesses that let an attacker log in as someone else: being able to tell which accounts exist, guessing passwords with no limit, or sessions that keep working long after the user logged out. It is number seven in the OWASP Top 10 (2025).

This lab shows three authentication problems, each as a pair of login endpoints: a vulnerable one and a secure one that does the same job correctly.

| Flaw                       | What goes wrong                                                                                   | Endpoints                           |
| -------------------------- | ------------------------------------------------------------------------------------------------- | ----------------------------------- |
| **User enumeration**       | The error tells you whether it was the e-mail or the password that was wrong.                      | `/lab/.../enumeration/login`        |
| **Brute force**            | Failed logins are not counted, so you can try passwords forever.                                   | `/lab/.../bruteforce/login`         |
| **Session never ends**     | The token lasts 30 days and logout does nothing on the server, so a stolen token keeps working.    | `/lab/.../session/...`              |

## Access the lab

Start the labs as described in the Installation section of the Introduction page. The Authentication Failures service then listens on:

| What             | Address                              |
| ---------------- | ------------------------------------ |
| **Swagger UI**   | `http://localhost:5000/swagger`      |
| API base URL     | `http://localhost:5000`              |
| Second HTTP port | `5001` (same service, also plain HTTP) |
| Database         | `AuthenticationFailures_LAB` on `sqlserver:1433` |

Open `http://localhost:5000/swagger` in your browser. Swagger is only enabled when `ASPNETCORE_ENVIRONMENT` is `Development`, which the Docker setup already does.

If you run the project from your IDE or with `dotnet run` instead of Docker, the ports are swapped: the `http` profile serves `http://localhost:5001` and `https` serves `https://localhost:5000`.

## Seeded data

On first start the service applies its migrations and seeds **100 users** from `dummy.users.json`. Each user has a username, e-mail and plain-text password.

The user used below:

| Username | E-mail                    | Password     |
| -------- | ------------------------- | ------------ |
| `kallie` | `sophia@schamberger.info` | `pass390325` |

To find more data, visit the [seed data](https://github.com/yagizerdem/OWASP-lab/tree/main/Lab/AuthenticationFailures/src/Context/Data) in the repository.

## Response format

Every endpoint answers with the same JSON envelope:

```json
{
  "message": "Login successful",
  "data": { "id": "...", "username": "kallie" },
  "status": 200
}
```

Errors are written by the shared error handler, which uses capitalised keys:

```json
{
  "Message": "Invalid password or email",
  "Data": null,
  "Status": 401
}
```

## Endpoints

All paths start with `http://localhost:5000`. The login endpoints take a JSON body; the session `me` endpoints read an `Authorization: Bearer <token>` header.

| #   | Method | Path                                 | Purpose                                             |
| --- | ------ | ------------------------------------ | --------------------------------------------------- |
| 1   | `POST` | `/lab/vulnerable/enumeration/login`  | Login that reveals which field was wrong            |
| 2   | `POST` | `/lab/secure/enumeration/login`      | Login with one identical error for every failure    |
| 3   | `POST` | `/lab/vulnerable/bruteforce/login`   | Login with no attempt limit                         |
| 4   | `POST` | `/lab/secure/bruteforce/login`       | Login that locks the account after 5 failures       |
| 5   | `POST` | `/lab/vulnerable/session/login`      | Login that returns a 30-day token                   |
| 6   | `POST` | `/lab/vulnerable/session/logout`     | "Logout" that does nothing on the server            |
| 7   | `GET`  | `/lab/vulnerable/session/me`         | Identity from a bearer token, even after logout     |
| 8   | `POST` | `/lab/secure/session/login`          | Login that returns a 15-minute token                |
| 9   | `POST` | `/lab/secure/session/logout`         | Logout that revokes the token on the server         |
| 10  | `GET`  | `/lab/secure/session/me`             | Identity from a bearer token, 401 once logged out   |

## Walkthrough in Swagger

In this walkthrough you are an attacker who wants into `kallie`'s account. You work up from "does this e-mail exist?" to a password to a stolen session.

### How to send a request in Swagger

Open `http://localhost:5000/swagger`. Every step below follows the same pattern:

1. Find the endpoint in the list and click it to expand it.
2. Click **Try it out**.
3. Paste the values from the step into the JSON **Request body**. For the `me` endpoints, paste the token into the field described in the step.
4. Click **Execute** and read the status code and body under **Server response**.

### Step 1: Find out which e-mails exist

Endpoint: **`POST /lab/vulnerable/enumeration/login`**

Try an e-mail you made up, with any password:

```json
{
  "email": "nobody@example.com",
  "password": "x"
}
```

You get `404` with `No account exists for this e-mail`. Now try a real one:

```json
{
  "email": "sophia@schamberger.info",
  "password": "x"
}
```

This time it is `401` with `Wrong password for this account`. The two answers are different, so the response just told you that `sophia@schamberger.info` is a registered account and `nobody@example.com` is not. An attacker runs a list of e-mails through this and keeps the ones that answer `401`.

### Step 2: See how the secure login answers

Endpoint: **`POST /lab/secure/enumeration/login`**

Send the same two requests again:

| `email`                   | `password` | Result                                       |
| ------------------------- | ---------- | -------------------------------------------- |
| `nobody@example.com`      | `x`        | `401` `Invalid password or email`            |
| `sophia@schamberger.info` | `x`        | `401` `Invalid password or email`            |

Both are identical now, so the response tells you nothing about which e-mails are real. The secure login also compares the password in constant time and checks a dummy value when the e-mail is unknown, so the two cases take about the same time, which closes a subtler version of the same leak.

### Step 3: Guess the password with no limit

Endpoint: **`POST /lab/vulnerable/bruteforce/login`**

Knowing a real e-mail from step 1, try passwords. First a few wrong ones:

```json
{
  "email": "sophia@schamberger.info",
  "password": "password1"
}
```

Each returns `401` with `Invalid password or email`, and you can send this as many times as you like; nothing counts or slows down the attempts. A real attacker would run a password list here. When you finally send the right one:

```json
{
  "email": "sophia@schamberger.info",
  "password": "pass390325"
}
```

you get `200` with `Login successful`. The point is that the 1000 wrong guesses before it cost you nothing.

### Step 4: Run into the lockout

Endpoint: **`POST /lab/secure/bruteforce/login`**

Send a wrong password five times:

```json
{
  "email": "sophia@schamberger.info",
  "password": "wrong"
}
```

The first five return `401`. On the sixth attempt, even with the *correct* password `pass390325`, you get `429` with `Too many failed attempts. Try again later.` The account is locked for 15 minutes after the last failed attempt, so guessing is no longer practical. A successful login before the limit resets the counter. The lock is keyed on the e-mail whether or not it exists, so it does not reveal which accounts are real either.

### Step 5: Log in and get a long-lived token

Endpoint: **`POST /lab/vulnerable/session/login`**

Log in as kallie with the real password:

```json
{
  "email": "sophia@schamberger.info",
  "password": "pass390325"
}
```

You get a token:

```json
{
  "message": "Login successful",
  "data": {
    "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
  },
  "status": 200
}
```

Copy the whole token string. This token is valid for 30 days. Imagine it gets stolen, from a log, a browser, or a shared computer.

### Step 6: Confirm the token works

Endpoint: **`GET /lab/vulnerable/session/me`**

This endpoint reads the token from the `Authorization` header. In Swagger, after **Try it out**, look for the parameter or header field and enter:

```text
Bearer <paste the token here>
```

You get kallie's identity back: her user ID, e-mail, username and the token's expiry 30 days out.

### Step 7: Log out, and watch the token keep working

Endpoint: **`POST /lab/vulnerable/session/logout`**

This endpoint takes no body. Click **Execute**; you get `200` with `Logged out`. Now go back to **`GET /lab/vulnerable/session/me`** and send the *same* token from step 5 again. It still returns kallie's identity. Logout did nothing but answer `200`: the token was never invalidated, so whoever holds it is still logged in.

### Step 8: See how the secure session behaves

First log in on the secure endpoint to get a fresh token:

**`POST /lab/secure/session/login`**

```json
{
  "email": "sophia@schamberger.info",
  "password": "pass390325"
}
```

The token this returns is valid for 15 minutes, not 30 days. Send it to **`GET /lab/secure/session/me`** as `Bearer <token>`; you get the identity. Now call **`POST /lab/secure/session/logout`** with that same token set as the `Bearer` header, then call **`GET /lab/secure/session/me`** with it again. This time you get `401` with `Invalid token`. Logout put the token on a server-side deny-list, so it stops working the moment the user logs out.

## What the secure version does differently

- **Enumeration (steps 1 and 2):** every failed login returns the same status and message, and takes about the same time, so responses never reveal which e-mails exist.
- **Brute force (steps 3 and 4):** failures are counted per e-mail, and the account is locked for a while after too many, so password guessing stops being practical.
- **Sessions (steps 5 to 8):** tokens are short-lived, and logout records the token on a server-side deny-list so it cannot be reused afterwards.
