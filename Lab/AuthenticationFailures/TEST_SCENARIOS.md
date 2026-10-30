# AuthenticationFailures – Test Scenarios

Three examples, each with a **vulnerable** endpoint (`lab/vulnerable/*`, the attack works) and a **secure** endpoint
(`lab/secure/*`, the same attack fails).

## Environment

- `BASE` = `http://localhost:5001/lab` (launchSettings `http` profile).
- Migrations and the user seeder run automatically on startup.
- Seed user: `sophia@schamberger.info` / `pass390325` (any user from `Data/dummy.users.json` works).
- Lockout counters and the logout deny-list live in memory: restarting the app resets them.

```bash
login() { curl -s -X POST $BASE/$1/login -H 'Content-Type: application/json' -d "{\"email\":\"$2\",\"password\":\"$3\"}"; }
```

---

## 1 – User enumeration (`/enumeration/login`)

| Step                         | Vulnerable                                | Secure                                 |
| ---------------------------- | ----------------------------------------- | -------------------------------------- |
| Unknown e-mail               | **404** `No account exists for this e-mail` | 401 `Invalid password or email`        |
| Known e-mail, wrong password | **401** `Wrong password for this account`   | 401 `Invalid password or email`        |
| Correct credentials          | 200                                       | 200                                    |

The vulnerable endpoint tells the attacker which e-mails are registered. The secure one answers identically and
compares the password in constant time (also against a dummy value for unknown e-mails).

```bash
login vulnerable/enumeration nobody@x.com a          # 404 -> e-mail does not exist
login vulnerable/enumeration sophia@schamberger.info a  # 401 -> e-mail exists
```

## 2 – Brute force (`/bruteforce/login`)

| Step                                     | Vulnerable              | Secure                                                  |
| ---------------------------------------- | ----------------------- | ------------------------------------------------------- |
| 5 wrong passwords                        | 5× 401                  | 5× 401                                                  |
| 6th attempt **with the correct password** | **200** (no limit at all) | **429** `Too many failed attempts` (locked for 15 min) |

Secure details: failures are counted per e-mail (`Security:MaxFailedLogins` = 5, `Security:LockoutMinutes` = 15),
a successful login resets the counter, and unknown e-mails are locked the same way (no enumeration through lockout).

```bash
for i in 1 2 3 4 5; do login secure/bruteforce sophia@schamberger.info wrong$i; done
login secure/bruteforce sophia@schamberger.info pass390325   # 429
```

## 3 – Session that does not end (`/session/login`, `/session/logout`, `/session/me`)

| Step                               | Vulnerable                         | Secure                         |
| ---------------------------------- | ---------------------------------- | ------------------------------ |
| Login → token lifetime             | **30 days**                        | 15 minutes                     |
| `GET /me` with the token           | 200                                | 200                            |
| `POST /logout`, then `GET /me` again | **200** – the old token still works | **401** – the token is revoked |

The secure logout puts the token's `jti` on a deny-list until the token expires, and `/me` checks that list.

```bash
T=$(login secure/session sophia@schamberger.info pass390325 | grep -o '"token":"[^"]*' | cut -d'"' -f4)
curl -s -X POST $BASE/secure/session/logout -H "Authorization: Bearer $T"
curl -s $BASE/secure/session/me -H "Authorization: Bearer $T"      # 401
```

## Known limits (not bugs)

- Passwords are stored in plaintext to keep the lab small; a real system stores Argon2/bcrypt hashes.
- Per-account lockout lets an attacker lock a known account (DoS trade-off); real systems add per-IP rate limits and CAPTCHA.
- In-memory state works for a single instance only; production would use Redis or the database.
