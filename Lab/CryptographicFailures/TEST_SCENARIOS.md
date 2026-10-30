# CryptographicFailures – Test Scenarios

Three cases, each with a vulnerable route (`/lab/vulnerable/crypto/...`, the attack works) and a secure route
(`/lab/secure/crypto/...`, the same attack fails).

## Environment

- `H` = `http://localhost:5020`. Swagger UI: `H/swagger`.
- Migrations and seeders run on startup. Seed data (all fake) lives in `src/Context/Data`:
  - `dummy.weakhashes.json`: 100 users with unsalted MD5 hashes. The `PlaintextPassword` column is the answer key.
  - `dummy.securecredentials.json`: the first 20 of those users, same passwords, stored with PBKDF2.
  - `dummy.legacycards.json` / `dummy.protectedcards.json`: one fake card per user for the same 20 users.
- Example user: `alphonso` / `pass806674` (exists in both password tables and owns a card).
- The card key is generated once into `src/Backend/App_Data/card-key.txt` (git-ignored) in Development.
  Outside Development set `Crypto__CardKey` (base64, 32 bytes) or the app refuses to start.
- Seeders only insert into empty tables. To start from scratch, drop the `CryptographicFailures_LAB` database.

---

## Case 1 – Weak password storage (`PasswordController`)

| # | Vulnerable | Secure |
|---|---|---|
| 1.1 | `GET H/lab/vulnerable/crypto/password-hashes` → user names with unsalted MD5 hashes. 14 users share `f83beca10dc074c0663afd1362893df1`. | `GET H/lab/secure/crypto/password-hashes` → every row has its own salt, so users with the same password still get different hashes. |
| 1.2 | Look the hash up in any MD5 table or run `hashcat -m 0` → `pass806674` in seconds, and it unlocks all 14 accounts at once. | `hashcat -m 10900` against PBKDF2 (600000 iterations) is far slower per guess, and each user needs a separate attack because of the salt. |
| 1.3 | `POST H/lab/vulnerable/crypto/login` `{"userName":"alphonso","password":"pass806674"}` → 200. | `POST H/lab/secure/crypto/login` with the same body → 200. Unknown user and wrong password give the same 401 in about the same time. |

## Case 2 – Predictable reset code (`ResetController`)

| # | Vulnerable | Secure |
|---|---|---|
| 2.1 | `POST H/lab/vulnerable/crypto/reset/request` `{"userName":"alphonso"}` → `code` and `issuedAtUnix`. | `POST H/lab/secure/crypto/reset/request` → 202 with the same message whether the user exists or not; `devMailbox` holds a 43 character random token (Development only). |
| 2.2 | Predict the code yourself: `new Random(issuedAtUnix).Next(100000, 1000000)` gives the same number. Two requests in the same second get the same code. | The token comes from `RandomNumberGenerator`, so there is no seed to guess. Only its SHA-256 is stored. |
| 2.3 | `POST .../reset/confirm` `{"userName":"alphonso","token":"<code>","newPassword":"x"}` → 200. Send it again → 200 again (the code never expires and is never used up). | `POST H/lab/secure/crypto/reset/confirm` with the token → 200. Same request again → 400 `Invalid or expired token`. Tokens also expire after 15 minutes. |

## Case 3 – Card numbers in plaintext (`CardController`)

| # | Vulnerable | Secure |
|---|---|---|
| 3.1 | `GET H/lab/vulnerable/crypto/cards` → full 16 digit card numbers. | `GET H/lab/secure/crypto/cards` → only `**** **** **** 1234`. |
| 3.2 | In Adminer: `SELECT CardNumber FROM LegacyCards` → readable numbers. A database dump exposes every card. | `SELECT CipherText, Nonce FROM ProtectedCards` → base64 AES-256-GCM ciphertext. Without the key, which is not in the database or the repository, the dump is useless. |
| 3.3 | `POST H/lab/vulnerable/crypto/cards` `{"ownerName":"alphonso","cardNumber":"4111111111111111"}` → the log shows `New card for alphonso: 4111111111111111`. | Same request to `H/lab/secure/crypto/cards` → the log only shows `ending 1111`, and the response is masked. Inputs that aren't 13 to 19 digits get 400. |
