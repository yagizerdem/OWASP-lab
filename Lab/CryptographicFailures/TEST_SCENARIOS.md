# CryptographicFailures – Test Scenarios

Every checklist item is exercised twice: against the **vulnerable** endpoints (`lab/vulnerable/crypto/*`, the attack must work)
and against the **secure** endpoints (`lab/secure/crypto/*`, the same attack must fail).
The pre-existing `VulnerablePasswordController` (`POST /VulnerablePassword`, unsalted MD5) is kept as the baseline of item 9.

## Environment

- `H` = `http://localhost:5020`, `S` = `https://localhost:5021` (launchSettings profiles `http` / `https`, use `curl -k`).
- Vulnerable endpoints work over plain HTTP on purpose (HTTPS redirection is skipped for `/lab/vulnerable`).
  Secure endpoints answer **400 `HTTPS required`** over HTTP, so always use `S` for them.
- Migrations and seeders run on startup. Seed data (all fake) lives in `Context/Data`:
  `dummy.weakhashes.json` (100 users, unsalted MD5, plaintext column included as the lab answer key),
  `dummy.securecredentials.json` (same first 20 users, PBKDF2), `dummy.legacycards.json` / `dummy.protectedcards.json` (20 Luhn valid fake cards),
  `dummy.resettokens.json` (3 sample tokens). Regenerate with `dotnet run` in `Scripts/DummyData` (`--regen-users` for new users).
- Keys: the secure endpoints use a key ring. In `Development` the keys (`k1`, `k2`) and an admin token are generated once into
  `Backend/App_Data/dev-keys.json` (git-ignored). Outside Development set `Crypto__Keys__k1`, `Crypto__Keys__k2` (base64, 32 bytes),
  `Crypto__ActiveKeyId` and `Crypto__AdminToken` as environment variables, otherwise the app refuses to start.
- `USER` / `PASS` = any row of `dummy.weakhashes.json` (`UserName` / `PlaintextPassword`). `ADMIN` = `AdminToken` from `dev-keys.json`.
- Seeded rows are never regenerated while a table is non-empty. To start from scratch drop the `CryptographicFailures_LAB` database.

Checklist (OWASP A04 Cryptographic Failures):
(1) old/weak algorithms, (2) default/weak/re-used keys, no rotation, (3) keys in source control, (4) encryption/headers not enforced,
(5) certificate validation, (6) IV reuse, ECB, no authenticated encryption, (7) password as key without KDF, (8) non-cryptographic randomness,
(9) deprecated or non-cryptographic hash functions, (10) padding oracle / error side channels, (11) algorithm downgrade or bypass.

---

## Item 1 / 9 / 11 – Algorithms, hashes, downgrade (`algorithms`)

| # | Vulnerable | Secure |
|---|---|---|
| 1.1 | `GET {H}/lab/vulnerable/crypto/algorithms/hash?text=password&algorithm=md5` → `5f4dcc3b5aa765d61d8327deb882cf99`; `sha1` and `fnv` also work | `GET {S}/lab/secure/crypto/algorithms/hash?text=a&algorithm=md5` (also `sha1`, `fnv`) → 400; `sha256/384/512` → 200 |
| 1.2 | `POST /algorithms/verify {"message":"amount=1000","algorithm":"none"}` → `valid:true` with **no signature** (bypass) | same body → 400 `The algorithm is chosen by the server` |
| 1.3 | `POST /algorithms/sign {"message":"m","algorithm":"md5"}` → `MD5(secret‖message)` (length extension possible, secret = committed `Crypto:LegacyKey`) | `POST /algorithms/sign {"message":"m"}` → `HMAC-SHA256`, signature `k1.<hex>`; HKDF sub-key, not the raw key |
| 1.4 | Verify uses `==` on strings (timing side channel) | `POST /algorithms/verify` with a changed message or signature → 401 `Invalid signature` (constant-time compare) |
| 1.5 | `algorithm=fnv` signature is a 32-bit non-cryptographic checksum: forge by brute force in seconds | `algorithm` other than empty/`HMAC-SHA256` → 400 |
| 1.6 | `POST /VulnerablePassword` accepts MD5 hashes; every row of `WeakHashes` has an unsalted MD5; identical passwords give identical hashes (`SELECT HashedPassword, COUNT(*) FROM WeakHashes GROUP BY HashedPassword`) | `SELECT Algorithm, Iterations, LEN(Salt) FROM SecureCredentials` → `PBKDF2-SHA256`, 600000, per-user salt, equal passwords give different hashes |

## Item 9 / 7 – Password storage and login (`password`)

| # | Vulnerable (`POST /VulnerablePassword`) | Secure (`POST {S}/lab/secure/crypto/password/login`) |
|---|---|---|
| 2.1 | Correct login → 200 and the **whole row** (hash and plaintext password) is returned | Correct login → 200 with `user`, `algorithm`, `iterations` only |
| 2.2 | Unknown user → 404, wrong password → 401 (enumeration) | Unknown user and wrong password → identical 401 `Invalid username or password`, similar duration (dummy hash) |
| 2.3 | Crack: `md5` of the hash from the DB is found in any rainbow table instantly (`hashcat -m 0`) | `hashcat -m 10900` against PBKDF2 is ~600000× slower per guess, and every user needs a separate attack (salt) |
| 2.4 | DB column `PlaintextPassword` is stored in clear (lab answer key) | No plaintext column exists in `SecureCredentials` |

## Item 1 / 2 / 3 / 6 / 10 – Encryption (`encryption`)

| # | Vulnerable (`{H}/lab/vulnerable/crypto/encryption`) | Secure (`{S}/lab/secure/crypto/encryption`) |
|---|---|---|
| 3.1 | `POST ecb/encrypt {"text":"AAAAAAAAAAAAAAAA…"}` (48 × `A`) → three **identical** 32 hex char blocks (pattern leak) | `POST encrypt` with the same text → no repeated blocks; two calls return different tokens (random nonce) |
| 3.2 | `POST des/encrypt` → 56-bit DES key (deprecated) | Only AES-256-GCM is offered |
| 3.3 | `POST cbc/encrypt` twice with the same text → **identical** `cipherHex` (static IV) | See 3.1: nonce is random per call |
| 3.4 | `POST session/issue?uid=7` → token. Flip byte 6 of the token (`XOR 0x01`, hex chars 13-14) and call `GET session/whoami?token=…` → `isAdmin:true`, payload `admin=1;uid=0007` (**bit flipping**, no integrity) | `POST session/issue?uid=7`, change any character of the token, `GET session/whoami?token=…` → 401 `Invalid session` (GCM tag) |
| 3.5 | Padding oracle: flip the last byte of the token → 400 `Padding is invalid and cannot be removed.`; flip byte 0 (IV) → 422 `Malformed session payload`. Two different answers = oracle (`padbuster`-style tools decrypt any block) | Every failure (bad tag, bad length, unknown key id, garbage) → the same 401 `Invalid session` |
| 3.6 | Key is `legacy-key-1234!`, hard-coded in code and committed in `appsettings.json` (`git grep legacy-key-1234`); the same key and IV are used for everything | Keys come from the key ring: `git grep` finds no key material; `App_Data/` is git-ignored; outside Development, startup fails without `Crypto__Keys__k1` |
| 3.7 | `POST ecb/decrypt` with garbage → raw exception text (information leak) | `POST decrypt` with garbage → `Invalid token` |

## Item 7 – Password used as a key (`password-key`)

| # | Vulnerable | Secure |
|---|---|---|
| 4.1 | `POST password-key/encrypt {"password":"pass1","text":"secret"}` → key is the password padded with zeros (no KDF, no salt, static IV): same input gives the same output | Same call → `v1.600000.<salt>.<nonce>.<cipher>`; output differs every time (random salt and nonce), ~70 ms of PBKDF2 work |
| 4.2 | `POST password-key/decrypt` with a wrong password → 400 `Padding is invalid…`; right password → plaintext. A dictionary loop over 10 000 passwords finds the key in milliseconds | Wrong password and damaged token → the same 400 `Invalid password or token`; a token with a low iteration count (downgrade) or absurdly high count (DoS) is rejected the same way |

## Item 8 / 9 – Randomness and reset tokens (`reset`)

| # | Vulnerable (`{H}/lab/vulnerable/crypto/reset`) | Secure (`{S}/lab/secure/crypto/reset`) |
|---|---|---|
| 5.1 | `POST request {"userName":USER}` twice within one second → **identical** 6 digit `code` and `issuedAtUnix` (`new Random(seconds)`); in .NET `new Random((int)issuedAtUnix).Next(100000,1000000)` reproduces the code of anybody | `POST request` → 202 and a 43 char base64url token (returned as `devMailbox` in Development only); differs every time |
| 5.2 | Unknown user → 404 (enumeration) | Unknown user → the same 202 message, `devMailbox:null` |
| 5.3 | `POST confirm` with the code → 200; the **same code works again** (never consumed, never expires) | The token works once; the second `confirm` → 400 `Invalid or expired token` |
| 5.4 | Wrong code can be guessed forever (900000 possibilities, no rate limit) | 256-bit token, wrong/expired/used token all → the same 400 |
| 5.5 | `SELECT TokenValue FROM ResetTokens WHERE IssuedSecurely = 0` → readable plaintext codes (seed row `123456`) | `SELECT TokenValue FROM ResetTokens WHERE IssuedSecurely = 1` → 64 char SHA-256 hex, never the token; `ExpiresAt` = +15 minutes |
| 5.6 | New password stored as unsalted MD5 in `WeakHashes` | New password stored as PBKDF2 in `SecureCredentials`; password shorter than 8 characters → 400 |

## Item 2 / 3 – Sensitive data at rest, key management and rotation (`cards`)

| # | Vulnerable (`{H}/lab/vulnerable/crypto/cards`) | Secure (`{S}/lab/secure/crypto/cards`) |
|---|---|---|
| 6.1 | `GET ?owner=USER` → full 16 digit card number; `SELECT CardNumber FROM LegacyCards` shows plaintext | `GET ?owner=USER` → `**** **** **** 1234` and `keyId`; `SELECT CipherText, Nonce, KeyId FROM ProtectedCards` shows ciphertext only |
| 6.2 | `POST {"ownerName":"x","cardNumber":"4111111111111111"}` → stored as is; the console logs the **full number** | Invalid number (length/Luhn) → 400; valid → 201 masked; the log contains only the last four digits |
| 6.3 | – | `POST rotation/activate?keyId=k2` without header → 401; with `X-Admin-Token: ADMIN` → 200, `activeKeyId:k2`; new cards get `KeyId = k2`, old ones stay readable |
| 6.4 | – | `POST rotation/reencrypt` (admin) → `reencrypted: N`; afterwards `SELECT DISTINCT KeyId FROM ProtectedCards` → only `k2` |
| 6.5 | – | Two rows with the same card number have different `Nonce` and `CipherText` |

## Item 4 – Encryption in transit, cookies and headers (`transport`)

| # | Vulnerable (`{H}/lab/vulnerable/crypto/transport`) | Secure (`{S}/lab/secure/crypto/transport`) |
|---|---|---|
| 7.1 | `POST {H}/…/session {"userName":"bob"}` over plain **HTTP** → works; `Set-Cookie: sid=<md5(bob)>; path=/` (no `Secure`, `HttpOnly`, `SameSite`); the value is predictable | `POST {S}/…/session` → `Set-Cookie: sid=<43 random chars>; max-age=900; secure; samesite=strict; httponly` |
| 7.2 | Response has no `Strict-Transport-Security`, no `Cache-Control`, no `X-Content-Type-Options` | `GET {S}/…/info` → `Strict-Transport-Security`, `X-Content-Type-Options: nosniff`, `X-Frame-Options: DENY`, `Content-Security-Policy`, `Referrer-Policy`, `Cache-Control: no-store` |
| 7.3 | `GET {H}/…/profile?apiKey=legacy-key-1234!` → 200: the secret is in the URL (logs, history, `Referer`) | Secrets travel only in headers or bodies; no endpoint reads a key from the query string |
| 7.4 | – | `GET {H}/lab/secure/crypto/transport/info` (plain HTTP) → 400 `HTTPS required` |

## Item 5 – Certificate validation (`tls`)

Needs internet access for the `badssl.com` targets (not reachable from some corporate/proxy networks – the vulnerable endpoint then answers 502 `Upstream unreachable`, which is a network problem, not a result). The `local-dev-cert` target calls this app's own `https://localhost:5021` and only works with the `https` profile.

| # | Vulnerable (`{H}/lab/vulnerable/crypto/tls/fetch?target=…`) | Secure (`{S}/lab/secure/crypto/tls/fetch?target=…`) |
|---|---|---|
| 8.1 | `target=self-signed`, `expired`, `wrong-host`, `untrusted-root` → **200** (`validated:false`): any man in the middle is accepted | Same targets → **502** `The upstream TLS certificate was rejected`; the reason is only in the server log |
| 8.2 | `target=valid` → 200 | `target=valid` → 200 (validation passes for a good certificate) |
| 8.3 | `target=local-dev-cert` → 200 even if the ASP.NET dev certificate is untrusted | `target=local-dev-cert` → 502 when the dev certificate is untrusted on the machine, 200 after `dotnet dev-certs https --trust` |
| 8.4 | Code review: `Program.cs` client `insecure-tls` uses `DangerousAcceptAnyServerCertificateValidator` | Client `default-tls` uses the default validation |
| 8.5 | `target=http://evil` or any other URL → 400 `Unknown target` (allow-list, no SSRF in either controller) | same |

## Known limits (not bugs)

- Dummy passwords are stored in plaintext in `WeakHashes.PlaintextPassword` and in the seed JSON on purpose: they are the lab answer key.
- Card numbers in `dummy.protectedcards.json` are fake test numbers in plaintext (the secure seeder encrypts them at startup, so no ciphertext or key is ever committed).
- The dev key file protects nothing outside a developer machine; production-grade key custody (HSM/KMS, envelope encryption) is out of scope.
- The admin token for key rotation is a lab-level simplification; a real system would use the authentication from the AuthenticationFailures lab.
- A real length-extension or padding-oracle exploit needs a script; the scenarios show the observable difference (different answers, flipped flag), not the full attack tool.
- `BruteForce` style endpoints are not rate limited here; rate limiting is covered in the AuthenticationFailures lab.
- Not verified automatically in this repository: the `badssl.com` and `local-dev-cert` TLS targets (network dependent). All other rows were executed against a throw-away database.
