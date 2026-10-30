---
title: A02 Security Misconfiguration
sidebar_position: 2
---

## Overview

Security Misconfiguration covers everything that is insecure not because of a bug in the code, but because of how the service was set up: debug features left switched on, default accounts that were never changed, too much information in responses. It is number two in the OWASP Top 10 (2025).

This lab is a small back-office API with two misconfigurations, both written on purpose:

| Flaw                                | What goes wrong                                                                                                          | Endpoints                                  |
| ----------------------------------- | ------------------------------------------------------------------------------------------------------------------------ | ------------------------------------------ |
| **Diagnostics endpoint left on**    | A debug endpoint that was useful during development still answers anyone, and returns the database password with it.    | `/lab/.../misconfig/diagnostics`           |
| **Default credentials**             | Back-office accounts still use the vendor's factory passwords, such as `admin` / `admin`, and the login accepts them.     | `/lab/.../misconfig/admin/...`             |

Every flaw has a vulnerable endpoint under `/lab/vulnerable/...` and a secure endpoint under `/lab/secure/...` that does the same job correctly, so you can send the same request to both and compare.

## Access the lab

Start the labs as described in the Installation section of the Introduction page. The Security Misconfiguration service then listens on:

| What             | Address                                         |
| ---------------- | ----------------------------------------------- |
| **Swagger UI**   | `http://localhost:5070/swagger`                 |
| API base URL     | `http://localhost:5070`                         |
| Second HTTP port | `5071` (same service, also plain HTTP)          |
| Database         | `SecurityMisconfiguration_LAB` on `sqlserver:1433` |

Open `http://localhost:5070/swagger` in your browser. Swagger is only enabled when `ASPNETCORE_ENVIRONMENT` is `Development`, which the Docker setup already does.

If you run the project from your IDE or with `dotnet run` instead of Docker, use the launch profile: `http` serves `http://localhost:5070`, while `https` serves `https://localhost:5071` and `http://localhost:5070`.

## Seeded data

On first start the service applies its migrations and seeds the database from the JSON files in `src/Context/Data`:

- **3 back-office accounts** in `dummy.admins.json`. Two of them still have their factory password.
- **100 users** in `dummy.users.json`. No endpoint in this lab uses them.

The back-office accounts are the ones you need:

| Username  | Password                               | Role      | Still on the default password? |
| --------- | -------------------------------------- | --------- | ------------------------------ |
| `admin`   | `admin`                                | `Admin`   | Yes                            |
| `support` | `support123`                           | `Support` | Yes                            |
| `ops`     | `Ops-a4e983b3d9bc4be79f039b2581b6445c` | `Admin`   | No                             |

To find more data, visit the [seed data](https://github.com/yagizerdem/OWASP-lab/tree/main/Lab/SecurityMisconfiguration/src/Context/Data) in the repository.

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
  "Message": "Invalid credentials",
  "Data": null,
  "Status": 401
}
```

## Endpoints

All paths start with `http://localhost:5070`. None of them expect any credentials in a header.

| #   | Method | Path                                         | Purpose                                                    |
| --- | ------ | -------------------------------------------- | ---------------------------------------------------------- |
| 1   | `GET`  | `/lab/vulnerable/misconfig/diagnostics`      | Debug information, including the connection string         |
| 2   | `GET`  | `/lab/secure/misconfig/diagnostics`          | A health check that reveals nothing useful                 |
| 3   | `POST` | `/lab/vulnerable/misconfig/admin/login`      | Back-office login that accepts factory passwords           |
| 4   | `POST` | `/lab/secure/misconfig/admin/login`          | Back-office login that refuses accounts on a default password |
| 5   | `POST` | `/lab/secure/misconfig/admin/change-password`| Replace a default password with a strong one               |

## Walkthrough in Swagger

In this walkthrough you are an outsider who has just found the service. You have no account. The goal is to see how far you get using only what the service hands out and what any vendor manual would tell you.

### How to send a request in Swagger

Open `http://localhost:5070/swagger`. Every step below follows the same pattern:

1. Find the endpoint in the list and click it to expand it.
2. Click **Try it out**.
3. Paste the values from the step into the JSON **Request body**, if the endpoint has one.
4. Click **Execute** and read the status code and body under **Server response**.

You never need the **Authorize** button in this lab.

### Step 1: Ask the diagnostics endpoint about the server

Endpoint: **`GET /lab/vulnerable/misconfig/diagnostics`**

This endpoint has no parameters. Developers add endpoints like this to check that a deployment works, and then forget to remove them. Click **Execute**:

```json
{
  "message": "Success",
  "data": {
    "environment": "Development",
    "connectionString": "Server=sqlserver,1433;Database=SecurityMisconfiguration_LAB;User Id=sa;Password=12345aA!;TrustServerCertificate=True",
    "allowedHosts": "*",
    "machineName": "<container id>",
    "os": "<operating system of the container>",
    "runtime": ".NET 10.0.x",
    "contentRoot": "<folder the app runs from>"
  },
  "status": 200
}
```

The machine name, OS and path depend on your setup. The important line is `connectionString`: it contains the database user `sa`, the most powerful SQL Server account, and its password `12345aA!`. Anyone who can reach the database with that can read and change every table, without going through the API at all. The rest of the response tells an attacker which OS, runtime and environment to look for exploits against.

### Step 2: Ask the secure diagnostics endpoint the same thing

Endpoint: **`GET /lab/secure/misconfig/diagnostics`**

Click **Execute**:

```json
{
  "message": "Success",
  "data": {
    "status": "ok",
    "environment": "Development",
    "databaseConfigured": true
  },
  "status": 200
}
```

It still tells you the service is up and has a database, but not how to reach it. Outside the `Development` environment it returns only `{ "status": "ok" }`.

### Step 3: Log in with the factory password

Endpoint: **`POST /lab/vulnerable/misconfig/admin/login`**

Back-office products often ship with an `admin` / `admin` account, and the default is printed in the vendor manual. Paste this into the request body:

```json
{
  "username": "admin",
  "password": "admin"
}
```

You are in, with the `Admin` role:

```json
{
  "message": "login successful",
  "data": {
    "username": "admin",
    "role": "Admin"
  },
  "status": 200
}
```

The second default account works too:

```json
{
  "username": "support",
  "password": "support123"
}
```

A wrong password, for example `"password": "guess"`, returns `401` with `Invalid credentials`. So the login itself works; the problem is that nobody changed the passwords after installation.

### Step 4: Try the same login on the secure endpoint

Endpoint: **`POST /lab/secure/misconfig/admin/login`**

Paste the same body:

```json
{
  "username": "admin",
  "password": "admin"
}
```

This time you get `403`:

```json
{
  "Message": "The default password must be changed before this account can be used",
  "Data": null,
  "Status": 403
}
```

The password is correct, but the account is still flagged as using its factory password, so the secure login refuses it until the password is changed. The `ops` account, which already has a strong password, logs in normally:

```json
{
  "username": "ops",
  "password": "Ops-a4e983b3d9bc4be79f039b2581b6445c"
}
```

### Step 5: Change the default password

Endpoint: **`POST /lab/secure/misconfig/admin/change-password`**

The new password must be at least 12 characters, different from the current one and not a well-known default (`admin`, `password`, `123456`, `support123`, `changeme`). First try a weak one:

```json
{
  "username": "admin",
  "currentPassword": "admin",
  "newPassword": "changeme"
}
```

You get `400` with `New password must be at least 12 characters, new, and not a known default`. Now a strong one:

```json
{
  "username": "admin",
  "currentPassword": "admin",
  "newPassword": "Correct-Horse-Battery-42"
}
```

The answer is `200` with `"data": "password changed"`.

### Step 6: Log in again

Go back to **`POST /lab/secure/misconfig/admin/login`** and use the new password:

```json
{
  "username": "admin",
  "password": "Correct-Horse-Battery-42"
}
```

You get `200` with `login successful`. The old `admin` / `admin` now fails on both login endpoints with `401`, because the password really changed.

:::warning

Step 5 changes data. To get the original seed data back, run `docker compose down -v` and then `docker compose up -d`. This resets the database of every lab, not just this one.

:::

## What the secure version does differently

- **Diagnostics (steps 1 and 2):** the secure endpoint never returns secrets such as connection strings, and outside `Development` it only says whether the service is up. In a real deployment, debug endpoints should not be deployed at all, or should sit behind authentication.
- **Default credentials (steps 3 to 6):** every account that still has its factory password is marked, and the secure login refuses it until the password is replaced with a strong one. Better still, installations should not ship with shared default passwords in the first place.
