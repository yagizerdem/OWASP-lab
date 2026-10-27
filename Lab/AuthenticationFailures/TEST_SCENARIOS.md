# AuthenticationFailures – Test Scenarios

Every rule is exercised twice: against the **vulnerable** endpoints (`lab/vulnerable/*`, the attack must work) and
against the **secure** endpoints (`lab/secure/*`, the same attack must fail). The legacy `lab/Auth/*` endpoints are
untouched weak baselines (only marked `[AllowAnonymous]` so the global deny-by-default policy does not block them).

## Environment

- `BASE` = `http://localhost:5001` (launchSettings `http` profile). With the `https` profile use `https://localhost:5000` and `curl -k`.
- Migrations (`mig2`, `mig3`) and seeders run automatically on startup.
- Seed users (from `Data/dummy.*.json`):

| Role | E-mail | Password | Secure notes at start |
|---|---|---|---|
| Admin (A) | `sophia@schamberger.info` | `pass390325` | 5 (at the limit) |
| User (B) | `florine.hodkiewicz@ohara.name` | `pass716578` | 2 |
| User (C) | `antonio.brakus@conn.com` | `pass577783` | 2 |

- Lockout, rate limit and alert scenarios accumulate state per IP/account. To reset: `DELETE FROM SecurityEvents` (Adminer at `localhost:8080`) and wait one minute.
  Thresholds live in `appsettings.json` (`Security`, `JWT`).
- `$TA`, `$TB` = access tokens of A and B, `$RB` = refresh token of B.

```bash
login() { curl -s -X POST $BASE/lab/secure/auth/login -H 'Content-Type: application/json' -d "{\"email\":\"$1\",\"password\":\"$2\"}"; }
```

Rules: (1) deny by default, (2) reusable access control + minimal CORS, (3) record ownership, (4) domain-enforced business limits,
(5) no directory listing / metadata / backup files, (6) log failures + alert, (7) rate limits, (8) session/JWT lifetime + revocation,
(9) declarative, well-established toolkits.

---

## Rule 1 – Deny by default

| # | Vulnerable (`lab/vulnerable`) – attack works | Secure (`lab/secure`) – attack fails |
|---|---|---|
| 1.1 | `GET /notes` without token → 200, **all users' notes** | `GET /notes` without token → 401 `Authentication required`, `WWW-Authenticate: Bearer` |
| 1.2 | `GET /admin/users` without token → 200 with every user **and plaintext password** | `GET /security-events` without token → 401; as normal user → 403 |
| 1.3 | `POST /notes` without token → 201 | `POST /notes` without token → 401 (all methods are protected, not only GET) |
| 1.4 | – | Add a controller action with no attribute and call it anonymously → 401 (fallback policy) |
| 1.5 | – | `GET /public/health` → 200 (the only explicit `[AllowAnonymous]` resource) |
| 1.6 | – | Expired token, `alg:none` token, token re-signed with another key → all 401 |
| 1.7 | – | `ASPNETCORE_ENVIRONMENT=Production`: `/swagger/v1/swagger.json` → 404 |

## Rule 2 – Access control implemented once, minimal CORS

| # | Vulnerable | Secure |
|---|---|---|
| 2.1 | Code review: `VulnerableNotesController.RequireAnyLogin` is a hand written check copied per controller; `/auth/me` parses the token by hand | Single mechanism: fallback policy, `[Authorize]`, `AdminOnly`, `User.GetUserId()` |
| 2.2 | `OPTIONS /notes` with `Origin: https://evil.com` → `Access-Control-Allow-Origin: https://evil.com` and `Allow-Credentials: true` | Same preflight on `/lab/secure/notes` → no CORS headers (`Cors:AllowedOrigins` empty) |
| 2.3 | – | Set `Cors:AllowedOrigins` to `["https://app.example"]`: that origin is echoed, `https://evil.com` is not, `*` never appears |
| 2.4 | `GET /auth/me?token=<jwt>` works (token in URL → logs/referrers) | Only the `Authorization: Bearer` header is read |

## Rule 3 – Record ownership (IDOR / BOLA / mass assignment)

Get note ids first: `NA1` = a note of A, `NB1` = a note of B.

| # | Vulnerable | Secure |
|---|---|---|
| 3.1 | `GET /notes/{NA1}` anonymously → 200 | B: `GET /notes/NA1` → 404; a random guid → identical 404 body |
| 3.2 | `GET /notes/by-owner?ownerId=<A id>` → A's notes (owner chosen by the client) | `GET /notes` returns only the caller's rows (`OwnerId == token.sub`) |
| 3.3 | B: `PUT /notes/{NA1}` → 200, A's note changed | B: `PUT /notes/NA1` → 404, A's note unchanged |
| 3.4 | B: `DELETE /notes/{NA1}` → 200, note gone | B: `DELETE /notes/NA1` → 404, note intact |
| 3.5 | `POST /notes` with `{"ownerId":"<A id>",...}` → note created **for A** (mass assignment), id/timestamps also client-chosen | B: `POST /notes` with `ownerId`/`id` in the body → 201 but the note belongs to **B**, extra fields ignored |
| 3.6 | `PUT` with another `ownerId` steals the note | – |
| 3.7 | – | Admin A: `GET /notes/NB1` → 404 (admin role does not bypass ownership) |
| 3.8 | – | Edit `sub` in B's JWT → 401 (bad signature) |

## Rule 4 – Business limits enforced by the domain model

| # | Vulnerable | Secure |
|---|---|---|
| 4.1 | `POST /notes` 20 times → 20 rows, no limit | A (5 notes): `POST /notes` → 409 `Note limit reached (5 per user)` |
| 4.2 | Title/content of any size accepted | B (2 notes): 3× `POST` → 201, 4th → 409; delete one, `POST` again → 201 |
| 4.3 | – | 10 parallel `POST` as C → never more than 5 rows (Serializable transaction); no 500 |
| 4.4 | – | Title > 100 or content > 2000 chars, or empty → 400 |
| 4.5 | – | Code review: the rule lives in `NoteEntity.Create`; seeder also goes through it |

## Rule 5 – No directory listing, no metadata / backup files

| # | Vulnerable | Secure |
|---|---|---|
| 5.1 | `GET /vuln-public/` → HTML directory listing | `GET /`, `/static/`, `/wwwroot/` → 404 |
| 5.2 | `GET /vuln-public/.git/config`, `/.git/HEAD` → repository metadata | `GET /.git/config`, `/.env`, `/.DS_Store` → 404 + `BlockedPath` event |
| 5.3 | `GET /vuln-public/appsettings.json.bak`, `/db-backup.sql.old` → backup content | `GET /appsettings.json.bak`, `/x.sql`, `/backup.zip`, `/x.old`, `/file~` → 404 + `BlockedPath` |
| 5.4 | – | `/%2e%67it/config` (encoded) → 404 + `BlockedPath` |
| 5.5 | – | Responses carry `X-Content-Type-Options: nosniff`, `X-Frame-Options: DENY`, `Cache-Control: no-store` |
| 5.6 | The `vuln-public` files are generated at startup (never committed) | `docker exec <ctn> find /app -name ".git*" -o -name "*.bak"` → only the generated `vuln-public` content |

## Rule 6 – Log access control failures, alert on repeats

Inspect with Admin A: `GET /lab/secure/security-events?take=20`.

| # | Vulnerable | Secure |
|---|---|---|
| 6.1 | Any attack above (IDOR, brute force, admin list) → **nothing** in `SecurityEvents`, no log line | Anonymous `GET /notes` → `Unauthenticated` (Warning) |
| 6.2 | 15 wrong passwords → no event, no alert | B: `GET /security-events` → 403 + `Forbidden` |
| 6.3 | – | B: `GET /notes/NA1` → `OwnershipViolation` (userId = B, note id in detail) |
| 6.4 | – | Wrong password → `LoginFailed`; login/logout/logout-all → `LoginSucceeded`/`Logout`/`LogoutAll` (Info) |
| 6.5 | – | 5 failures from one IP within 10 min → one `RepeatedFailures` event, severity **Alert**, and a `SECURITY ALERT` Critical console line |
| 6.6 | – | `GET /security-events?severity=Alert` (admin) lists only alerts |
| 6.7 | – | E-mail `a@b.com\r\nSECURITY ALERT fake` → stored with control chars replaced; 10 KB e-mail truncated to 256 chars, no 500 |

## Rule 7 – Rate limits

| # | Vulnerable | Secure |
|---|---|---|
| 7.1 | 200 × `POST /auth/login` in a minute → never 429 (brute force possible) | 11 × `POST /auth/login` in a minute (different e-mails) → 10× 401, then **429** + `Retry-After` |
| 7.2 | 200 × `GET /notes` → always 200 (scraping) | 101 × `GET /public/health` in a minute → 101st is 429 (global limit) |
| 7.3 | – | 11 × `POST /auth/refresh` per minute → 429 |
| 7.4 | – | Correct password while limited → still 429 |
| 7.5 | – | 429s write a single `RateLimited` event per IP per 30 s (DB is not flooded) |
| 7.6 | Legacy `lab/Auth/ratelimit/login` → 5th request in 120 s is 429 (4 per window) | – |

## Rule 8 – Session / JWT lifetime and revocation

| # | Vulnerable | Secure |
|---|---|---|
| 8.1 | `POST /auth/login` → token valid **30 days**; also set as a cookie without `HttpOnly`/`Secure` | Token valid **15 min** (`exp − nbf = 900 s`), no cookie, no roles inside the JWT; refresh token valid 7 days |
| 8.2 | `POST /auth/logout` → 200 but the **same token still works** on `/auth/me` | `POST /auth/logout` → the same access token gets 401 on `/auth/me` (+ `RevokedTokenUsed` event) |
| 8.3 | No refresh token exists | After logout the revoked refresh token → 401 |
| 8.4 | – | Refresh rotation: `RB1 → RB2`; replaying `RB1` → 401, `RB2` is revoked too, `RefreshTokenReuse` Alert |
| 8.5 | – | `logout-all` with two logins → both refresh tokens dead; other device's access token dies within 15 min (documented limit) |
| 8.6 | – | DB: `RefreshTokens.TokenHash` is 64 hex chars, no raw token |
| 8.7 | – | Delete the user in DB → existing access token gets 401 |
| 8.8 | – | Promote B to Admin in DB → B's existing token reaches `/security-events` without re-login; demote → 403 immediately |

## Authentication-failure extras

| # | Vulnerable | Secure |
|---|---|---|
| A.1 | Unknown e-mail → **404** `No account exists…`; wrong password → **401** `Wrong password…` (user enumeration) | Both → 401 `Invalid password or email`, identical body |
| A.2 | 100 wrong passwords, then the right one still works (no lockout) | 5 wrong passwords for one account → 6th attempt (even with the right password) is **429** + `LoginBlocked` |
| A.3 | – | Lockout also applies to unknown e-mails (no enumeration through lockout) |
| A.4 | – | `Security:LockoutMinutes` = 1, wait → login works again |
| A.5 | – | 4 wrong + 1 right + 4 wrong → no lockout (counter resets on success) |
| A.6 | Plain `!=` string compare (timing), plaintext passwords exposed by `/admin/users` | Constant-time compare on hashes; unknown e-mail costs the same |
| A.7 | – | Missing body fields → 400; `' OR 1=1 --` as e-mail → 401 |

## Known limits (not bugs)

- `UserEntity.Password` is stored in plaintext because the User model must not change. A real system needs Argon2/bcrypt.
- Lockout is per account: an attacker can lock a known e-mail for 15 min (DoS vs brute force trade-off). The per-IP rate limit softens this.
- "Alert admins" is implemented as a `RepeatedFailures` Alert row plus a Critical log line; mail/SIEM integration plugs into `SecurityEventService.RaiseAlertIfNeededAsync`.
- Rate limit counters are in memory (single instance).
- `/vuln-public` content is fake and generated at startup; it is exempt from the hardening middleware on purpose.
- A `.dockerignore` would live in `Lab/` (outside this lab) and was not added; check 5.6 instead.
