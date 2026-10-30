---
title: A04 Cryptographic Failures
sidebar_position: 4
---

## Overview

Cryptographic Failures happen when sensitive data is not protected the way it should be: passwords hashed with a fast, broken algorithm, secrets generated with a random number generator that can be predicted, or card numbers stored in plain text. It is number four in the OWASP Top 10 (2025).

This lab contains three cryptographic flaws, all written on purpose:

| Flaw                              | What goes wrong                                                                                                    | Endpoints                                           |
| --------------------------------- | ------------------------------------------------------------------------------------------------------------------ | --------------------------------------------------- |
| **Weak password storage**         | Passwords are stored as unsalted MD5, so equal passwords get equal hashes and every hash can be cracked in seconds. | `/lab/.../crypto/password-hashes`, `/lab/.../crypto/login` |
| **Predictable reset codes**       | Reset codes come from `System.Random` seeded with the clock, never expire and can be used again and again.         | `/lab/.../crypto/reset/...`                         |
| **Card numbers in plain text**    | Full card numbers are stored unencrypted, returned in responses and written to the log.                            | `/lab/.../crypto/cards`                             |

Every flaw has a vulnerable endpoint under `/lab/vulnerable/...` and a secure endpoint under `/lab/secure/...` that does the same job correctly, so you can send the same request to both and compare.

## Access the lab

Start the labs as described in the Installation section of the Introduction page. The Cryptographic Failures service then listens on:

| What             | Address                                         |
| ---------------- | ----------------------------------------------- |
| **Swagger UI**   | `http://localhost:5020/swagger`                 |
| API base URL     | `http://localhost:5020`                         |
| Second HTTP port | `5021` (same service, also plain HTTP)          |
| Database         | `CryptographicFailures_LAB` on `sqlserver:1433` |

Open `http://localhost:5020/swagger` in your browser. Swagger is only enabled when `ASPNETCORE_ENVIRONMENT` is `Development`, which the Docker setup already does.

If you run the project from your IDE or with `dotnet run` instead of Docker, use the launch profile: `http` serves `http://localhost:5020`, while `https` serves `https://localhost:5021` and `http://localhost:5020`.

## Seeded data

On first start the service applies its migrations and seeds the database from the JSON files in `src/Context/Data`:

- **`dummy.weakhashes.json`**: 100 users whose passwords are stored as unsalted MD5. The file also contains each plain-text password, so you have the answers.
- **`dummy.securecredentials.json`**: the first 20 of those users, with the same passwords, stored with PBKDF2 and a salt per user.
- **`dummy.legacycards.json`**: one card per user for the same 20 users, stored in plain text.
- **`dummy.protectedcards.json`**: the same 20 card numbers. They are encrypted with AES-256-GCM when the service seeds them, so the database never holds them in clear.

The encryption key for the cards is generated once into `src/Backend/App_Data/card-key.txt`, a file git ignores. Outside `Development` the service refuses to start unless the key is set in the `Crypto__CardKey` environment variable.

The users used below:

| Username    | Password     | In both password tables? | Card number        |
| ----------- | ------------ | ------------------------ | ------------------ |
| `alphonso`  | `pass806674` | Yes                      | `4278049029986100` |
| `nathanial` | `pass222867` | Yes                      | `4680386832126595` |
| `madie`     | `pass222867` | Yes                      |                    |

To find more data, visit the [seed data](https://github.com/yagizerdem/OWASP-lab/tree/main/Lab/CryptographicFailures/src/Context/Data) in the repository.

## Response format

Every successful response uses the same JSON envelope:

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
  "Message": "Invalid username or password",
  "Data": null,
  "Status": 401
}
```

## Endpoints

All paths start with `http://localhost:5020`. None of them expect any credentials in a header.

| #   | Method | Path                                  | Purpose                                                    |
| --- | ------ | ------------------------------------- | ---------------------------------------------------------- |
| 1   | `GET`  | `/lab/vulnerable/crypto/password-hashes` | The MD5 password table, as a leaked database dump would show it |
| 2   | `POST` | `/lab/vulnerable/crypto/login`        | Log in against the MD5 table                               |
| 3   | `GET`  | `/lab/secure/crypto/password-hashes`  | The PBKDF2 password table                                  |
| 4   | `POST` | `/lab/secure/crypto/login`            | Log in against the PBKDF2 table                            |
| 5   | `POST` | `/lab/vulnerable/crypto/reset/request`| Ask for a 6-digit reset code made with `System.Random`     |
| 6   | `POST` | `/lab/vulnerable/crypto/reset/confirm`| Set a new password with that code                          |
| 7   | `POST` | `/lab/secure/crypto/reset/request`    | Ask for a random, single-use reset token                   |
| 8   | `POST` | `/lab/secure/crypto/reset/confirm`    | Set a new password with that token                         |
| 9   | `GET`  | `/lab/vulnerable/crypto/cards`        | List cards with their full numbers                         |
| 10  | `POST` | `/lab/vulnerable/crypto/cards`        | Store a card in plain text                                 |
| 11  | `GET`  | `/lab/secure/crypto/cards`            | List cards with masked numbers                             |
| 12  | `POST` | `/lab/secure/crypto/cards`            | Store a card encrypted                                     |

## Walkthrough in Swagger

In this walkthrough you are an attacker who got hold of the database, or at least some of its tables, and wants to turn that into working passwords and card numbers.

### How to send a request in Swagger

Open `http://localhost:5020/swagger`. Every step below follows the same pattern:

1. Find the endpoint in the list and click it to expand it.
2. Click **Try it out**.
3. Paste the values from the step into the JSON **Request body**, if the endpoint has one.
4. Click **Execute** and read the status code and body under **Server response**.

You never need the **Authorize** button in this lab.

### Step 1: Look at the leaked password hashes

Endpoint: **`GET /lab/vulnerable/crypto/password-hashes`**

This endpoint has no parameters. It plays the part of a stolen copy of the user table. Click **Execute** and look for `nathanial`, `madie` and `foster`:

```json
{ "userName": "nathanial", "hashedPassword": "fe75aae24cc18c191a78d231ffa03b31" },
{ "userName": "madie",     "hashedPassword": "fe75aae24cc18c191a78d231ffa03b31" },
{ "userName": "foster",    "hashedPassword": "fe75aae24cc18c191a78d231ffa03b31" }
```

Three users, one hash. MD5 without a salt always turns the same password into the same hash, so the dump already tells you who shares a password. The biggest group is 14 users with the hash `f83beca10dc074c0663afd1362893df1`, one of them `alphonso`.

MD5 is also very fast, which makes guessing cheap. Hashes of common passwords are listed in public lookup tables, and a normal graphics card tries billions of MD5 guesses per second. Cracking one hash from a group unlocks every account in it.

### Step 2: Log in with a cracked password

Endpoint: **`POST /lab/vulnerable/crypto/login`**

Say you cracked `fe75aae24cc18c191a78d231ffa03b31` and got `pass222867`. Paste this into the request body:

```json
{
  "userName": "madie",
  "password": "pass222867"
}
```

You get `200` with `Logged in`. The same password works for `nathanial` and `foster`. A wrong password returns `401` with `Invalid username or password`.

### Step 3: Compare with the secure password table

Endpoint: **`GET /lab/secure/crypto/password-hashes`**

Click **Execute** and find the same users:

```json
{
  "userName": "nathanial",
  "passwordHash": "6gWDka/jpKa8UjfdsVP12xmSIaFPFAaT0k7CXCpJTVE=",
  "salt": "QnK26lCj6CilarHaUZ2QWw==",
  "iterations": 600000
},
{
  "userName": "madie",
  "passwordHash": "znfj+x83KMffk9q8YniB4Pcuq7JMkFkmNSi0NRcR6qU=",
  "salt": "f5qMr1uVioV3wGmDdUno8w==",
  "iterations": 600000
}
```

Same password, different hashes. Each user has a random salt that is mixed into the hash, so the dump no longer shows who shares a password, and a lookup table is useless. The hash is PBKDF2 with 600000 iterations, which makes every single guess about 600000 times more expensive than with MD5. And because of the salt, the attacker has to start over for every user.

### Step 4: Log in against the secure table

Endpoint: **`POST /lab/secure/crypto/login`**

Paste the same body as in step 2:

```json
{
  "userName": "madie",
  "password": "pass222867"
}
```

You get `200` with `Logged in`, so for a legitimate user nothing changes. Now try a user that doesn't exist:

```json
{
  "userName": "nobody",
  "password": "pass222867"
}
```

You get `401` with `Invalid username or password`, the same answer as for a wrong password, and it takes about as long. The service runs one PBKDF2 check even when the user doesn't exist, so the response time doesn't reveal which usernames are real.

### Step 5: Request a reset code the vulnerable way

Endpoint: **`POST /lab/vulnerable/crypto/reset/request`**

Paste this into the request body:

```json
{
  "userName": "alphonso"
}
```

Normally the code would be sent by e-mail. Here the response shows it directly, together with the moment it was created:

```json
{
  "message": "Code sent (shown here instead of an e-mail)",
  "data": {
    "code": "482913",
    "issuedAtUnix": 1793321400
  },
  "status": 200
}
```

Your numbers will differ. The code is made with `new Random(issuedAtUnix).Next(100000, 1000000)`. `System.Random` is not built for secrets: started with the same seed, it always produces the same number, and the seed here is just the current second. Anyone who knows roughly when a reset was requested can compute the code without ever seeing the e-mail.

You can see this without writing any code: click **Execute** twice within the same second. Both answers have the same `issuedAtUnix` and the same `code`.

### Step 6: Use the code, twice

Endpoint: **`POST /lab/vulnerable/crypto/reset/confirm`**

Put the code from step 5 into `token`:

```json
{
  "userName": "alphonso",
  "token": "482913",
  "newPassword": "hacked123"
}
```

You get `200` with `Password changed`. Click **Execute** again with the same body: it works again. The code never expires and is never marked as used, so a code that leaks once works forever. A code that was never issued returns `401` with `Wrong code`.

Check the result in **`POST /lab/vulnerable/crypto/login`** with `{ "userName": "alphonso", "password": "hacked123" }`: you get `200`.

### Step 7: Request a reset token the secure way

Endpoint: **`POST /lab/secure/crypto/reset/request`**

Paste this into the request body:

```json
{
  "userName": "alphonso"
}
```

You get `202`:

```json
{
  "message": "If the account exists, a reset message was sent",
  "data": {
    "devMailbox": "q3V0p8eZ1X2..."
  },
  "status": 202
}
```

The token is 32 random bytes from `RandomNumberGenerator`, the cryptographic random number generator, so there is no seed to guess. The database only stores its SHA-256 hash. `devMailbox` only exists because there is no mail server in the lab; it is filled in the `Development` environment only.

Try the same request with `"userName": "nobody"`. The answer is the same `202` and the same message, only `devMailbox` is `null`, so the endpoint doesn't reveal whether an account exists.

### Step 8: Use the token, twice

Endpoint: **`POST /lab/secure/crypto/reset/confirm`**

Copy `devMailbox` from step 7 into `token`:

```json
{
  "userName": "alphonso",
  "token": "<paste devMailbox here>",
  "newPassword": "a-much-better-password"
}
```

You get `200` with `Password changed`. Click **Execute** again: now you get `400` with `Invalid or expired token`. Each token works once, and only for 15 minutes. A new password shorter than 8 characters is refused with `400` and `Password must be at least 8 characters`.

### Step 9: Read the card numbers

Endpoint: **`GET /lab/vulnerable/crypto/cards`**

This endpoint has no parameters. Click **Execute**:

```json
{
  "ownerName": "alphonso",
  "cardNumber": "4278049029986100",
  "id": "219aa487-9c5a-422a-b3d7-ec34ada0fdc8",
  ...
}
```

Every full card number is in the response, and the database table `LegacyCards` holds them the same way. Anyone with a database backup, a read-only SQL account or an SQL injection gets all of them.

### Step 10: Store a card and check the log

Endpoint: **`POST /lab/vulnerable/crypto/cards`**

Paste this into the request body:

```json
{
  "ownerName": "alphonso",
  "cardNumber": "4111111111111111"
}
```

You get `201` with the full card in the response. Now look at the service's console output, for example in Docker Desktop under the `cryptographicfailures` container's **Logs** tab. You will find a line like:

```text
New card for alphonso: 4111111111111111
```

Logs are copied to many places and read by many people, so card numbers must never end up there.

### Step 11: Do the same on the secure endpoints

Endpoint: **`GET /lab/secure/crypto/cards`**

Click **Execute**. The same cards come back masked:

```json
{
  "id": "...",
  "ownerName": "alphonso",
  "masked": "**** **** **** 6100"
}
```

Endpoint: **`POST /lab/secure/crypto/cards`**

Paste the same body as in step 10:

```json
{
  "ownerName": "alphonso",
  "cardNumber": "4111111111111111"
}
```

You get `201`, but the response only shows `**** **** **** 1111`, and the log only says `New card for alphonso ending 1111`. In the database the table `ProtectedCards` holds only the last four digits, a random nonce and the AES-256-GCM ciphertext. Without the key, which is neither in the database nor in the repository, a stolen copy of the table is useless.

The secure endpoint also checks the input: `"cardNumber": "1234"` returns `400` with `Invalid card number`.

:::warning

Steps 6, 8, 10 and 11 change data. To get the original seed data back, run `docker compose down -v` and then `docker compose up -d`. This resets the database of every lab, not just this one.

:::

## What the secure version does differently

- **Password storage (steps 1 to 4):** passwords are hashed with PBKDF2-HMAC-SHA256, 600000 iterations and a random salt per user, and compared in constant time. An unknown user costs the same time as a wrong password.
- **Reset codes (steps 5 to 8):** tokens come from `RandomNumberGenerator`, only their hash is stored, they expire after 15 minutes and work once. The request endpoint gives the same answer whether or not the user exists.
- **Card numbers (steps 9 to 11):** numbers are encrypted with AES-256-GCM before they are stored, responses and logs only show the last four digits, and the key lives outside the code and the database.
