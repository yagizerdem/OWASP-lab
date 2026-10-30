---
title: A09 Security Logging and Alerting Failures
sidebar_position: 9
---

## Overview

Security Logging and Alerting Failures are not about an attack succeeding; they are about nobody noticing. If logins are not recorded, a brute-force run leaves no trace. If a sensitive change writes no audit record, you cannot tell afterwards who did it or undo it. And a log that copies raw user input can be forged. It is number nine in the OWASP Top 10 (2025).

This lab contains two logging flaws, both written on purpose:

| Flaw                              | What goes wrong                                                                                                        | Endpoints                               |
| --------------------------------- | ---------------------------------------------------------------------------------------------------------------------- | --------------------------------------- |
| **Bad login logging**             | The password is written to the log, user input is logged raw (so a newline forges a log line), and failures are not recorded or alerted on. | `/lab/.../logging/login`                |
| **Sensitive change with no audit**| Changing an account's e-mail leaves no record of who did it or what the old value was.                                 | `/lab/.../logging/accounts/email`       |

Every flaw has a vulnerable endpoint under `/lab/vulnerable/...` and a secure endpoint under `/lab/secure/...` that does the same job correctly. The secure side also has read endpoints to inspect the records it keeps.

## Access the lab

Start the labs as described in the Installation section of the Introduction page. The Security Logging and Alerting Failures service then listens on:

| What             | Address                              |
| ---------------- | ------------------------------------ |
| **Swagger UI**   | `http://localhost:5060/swagger`      |
| API base URL     | `http://localhost:5060`              |
| Second HTTP port | `5061` (same service, also plain HTTP) |
| Database         | `SecurityLoggingAndAlertingFailures_LAB` on `sqlserver:1433` |

Open `http://localhost:5060/swagger` in your browser. Swagger is only enabled when `ASPNETCORE_ENVIRONMENT` is `Development`, which the Docker setup already does.

To see the log messages, keep the service's console output in view. With Docker that is the `securityloggingandalertingfailures` container's **Logs** tab in Docker Desktop (or `docker compose logs -f securityloggingandalertingfailures`).

If you run the project from your IDE or with `dotnet run` instead of Docker, use the launch profile: `http` serves `http://localhost:5060`, while `https` serves `https://localhost:5061` and `http://localhost:5060`.

## Seeded data

On first start the service applies its migrations and seeds **100 users** from `dummy.users.json`. The security-event and audit tables start empty; you fill them during the walkthrough.

The users used below:

| Username  | E-mail                   | Password     | User ID                                |
| --------- | ------------------------ | ------------ | -------------------------------------- |
| `eugenia` | `joseph@johnsschaden.uk` | `pass909538` | `ea68f224-0272-44a9-8409-437613ca570f` |
| `myrtle`  | `marcelle@haag.com`      | `pass289170` | `81ac763e-5ea9-4cff-b761-eea7ac776921` |

To find more data, visit the [seed data](https://github.com/yagizerdem/OWASP-lab/tree/main/Lab/SecurityLoggingAndAlertingFailures/src/Context/Data) in the repository.

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
  "Message": "Invalid credentials",
  "Data": null,
  "Status": 401
}
```

Most of what matters in this lab is not in the HTTP response, though; it is in the log output and in the two read endpoints.

## Endpoints

All paths start with `http://localhost:5060`. None of them expect credentials in a header.

| #   | Method | Path                                         | Purpose                                              |
| --- | ------ | -------------------------------------------- | ---------------------------------------------------- |
| 1   | `POST` | `/lab/vulnerable/logging/login`              | Login that logs the password and raw input           |
| 2   | `POST` | `/lab/secure/logging/login`                  | Login that records events and alerts on brute force  |
| 3   | `GET`  | `/lab/secure/logging/events`                 | The security-event log (query `take`)                |
| 4   | `PUT`  | `/lab/vulnerable/logging/accounts/email`     | Change an e-mail, no record kept                     |
| 5   | `PUT`  | `/lab/secure/logging/accounts/email`         | Change an e-mail and write an audit record           |
| 6   | `GET`  | `/lab/secure/logging/audit/{targetUserId}`   | The audit history of one account                     |

## Walkthrough in Swagger

In this walkthrough you watch what the service does and does not record. Keep the container's log output open next to Swagger.

### How to send a request in Swagger

Open `http://localhost:5060/swagger`. Every step below follows the same pattern:

1. Find the endpoint in the list and click it to expand it.
2. Click **Try it out**.
3. Paste the values from the step into the JSON **Request body** (or the path field for step 6).
4. Click **Execute**, then read the response and, where the step says so, the log output.

### Step 1: Log in and leak the password into the log

Endpoint: **`POST /lab/vulnerable/logging/login`**

Send a correct login:

```json
{
  "email": "joseph@johnsschaden.uk",
  "password": "pass909538"
}
```

You get `200`, but look at the log line it produced:

```text
Login attempt email=joseph@johnsschaden.uk password=pass909538
```

The plaintext password is now in the log. Logs are copied to files, shipped to other systems and read by many people, so a password must never appear there.

### Step 2: Forge a log line with a newline

Same endpoint. The e-mail is pasted into the log message as-is, so you can smuggle a line break into it. In the `email` field put a value that contains a newline. In Swagger's JSON body, a newline inside a string is written as `\n`:

```json
{
  "email": "attacker@evil.test\nLogin success email=admin@lab.local",
  "password": "x"
}
```

The login fails (`401`), but the log now reads as two lines:

```text
Login attempt email=attacker@evil.test
Login success email=admin@lab.local password=x
```

Anyone reading the log, or a tool parsing it, now sees a "Login success" line that never happened. This is **log forging**, and it works because the raw input was trusted.

### Step 3: Run a brute force against the vulnerable login

Same endpoint. Send a few wrong passwords for a real account:

```json
{
  "email": "joseph@johnsschaden.uk",
  "password": "wrong"
}
```

Each returns `401`. Now look at the log: there is nothing. Failed logins are not recorded at all, so an attacker can guess passwords all day and leave no trace anyone could alert on.

### Step 4: Do the same against the secure login

Endpoint: **`POST /lab/secure/logging/login`**

Send the same wrong password five times:

```json
{
  "email": "joseph@johnsschaden.uk",
  "password": "wrong"
}
```

Each attempt is `401`, but each one also writes a warning line to the log and stores a `LOGIN_FAILURE` event. On the fifth failure within five minutes, the service also logs a critical line and stores a `BRUTE_FORCE_ALERT`:

```text
warn: Login failure for joseph@johnsschaden.uk from ::1
crit: ALERT brute force suspected: 5 failed logins for joseph@johnsschaden.uk from ::1
```

Try step 2's newline trick here too: the forged line break is gone, replaced by `_`, because the secure login strips CR/LF before logging. And a successful login logs only `Login success for ... from ...`, never the password.

### Step 5: Read the security-event log

Endpoint: **`GET /lab/secure/logging/events`**

Click **Execute** (you can set `take` to limit how many rows). You see the structured record the secure login kept: a `LOGIN_FAILURE` per attempt and the `BRUTE_FORCE_ALERT` from step 4:

```json
{
  "eventType": "BRUTE_FORCE_ALERT",
  "username": "joseph@johnsschaden.uk",
  "ipAddress": "::1",
  "success": false,
  "details": "5 failed logins in 5 minutes",
  ...
}
```

This is what makes an attack visible: someone, or an automated monitor, can see the failures piling up and respond.

### Step 6: Change an e-mail with no record

Endpoint: **`PUT /lab/vulnerable/logging/accounts/email`**

Change eugenia's e-mail. The vulnerable endpoint only needs the target:

```json
{
  "actorUserId": "00000000-0000-0000-0000-000000000000",
  "targetUserId": "ea68f224-0272-44a9-8409-437613ca570f",
  "newEmail": "attacker@evil.test"
}
```

You get `200` with `email changed`. Changing the e-mail on an account is often the first move in a takeover, yet nothing was recorded: no who, no when, no old value. You cannot tell this happened, and you cannot restore the old address because it was overwritten with no copy.

### Step 7: Change an e-mail and leave an audit trail

Endpoint: **`PUT /lab/secure/logging/accounts/email`**

Here the actor has to be a real user. Have `myrtle` change `eugenia`'s e-mail:

```json
{
  "actorUserId": "81ac763e-5ea9-4cff-b761-eea7ac776921",
  "targetUserId": "ea68f224-0272-44a9-8409-437613ca570f",
  "newEmail": "changed@example.com"
}
```

You get `200`, and because the actor and target differ, the log also carries a warning that one user changed another user's e-mail. Now read the record:

Endpoint: **`GET /lab/secure/logging/audit/{targetUserId}`**

Put eugenia's ID into `targetUserId`:

```text
ea68f224-0272-44a9-8409-437613ca570f
```

You get the audit history, newest first:

```json
{
  "actorUserId": "81ac763e-5ea9-4cff-b761-eea7ac776921",
  "action": "EMAIL_CHANGED",
  "targetUserId": "ea68f224-0272-44a9-8409-437613ca570f",
  "oldValue": "joseph@johnsschaden.uk",
  "newValue": "changed@example.com",
  "ipAddress": "::1",
  ...
}
```

Now you know who changed it, from which IP, and what the address used to be, so the change can be investigated and reversed. The change and its audit record are saved together, and there is deliberately no endpoint that edits or deletes audit rows.

:::warning

Steps 6 and 7 change data. To get the original seed data back, run `docker compose down -v` and then `docker compose up -d`. This resets the database of every lab, not just this one.

:::

## What the secure version does differently

- **Login logging (steps 1 to 5):** the password is never logged, user input is stripped of CR/LF so it cannot forge log lines, every attempt is stored as a security event, and repeated failures raise an alert so an attack is visible.
- **Audit trail (steps 6 and 7):** every sensitive change writes an append-only record with the actor, the old and new values and the IP, saved in the same transaction as the change, so nothing happens without a trace and cross-user changes are flagged.
