---
title: A03 Software Supply Chain Failures
sidebar_position: 3
---

## Overview

Software Supply Chain Failures happen when the code you ship is not the code you think you are shipping: a dependency is swapped for a malicious look-alike, or a component with a known, published vulnerability goes to production because nobody checked. It is number three in the OWASP Top 10 (2025).

This lab simulates a company's build and deploy pipeline. It contains two supply-chain flaws, both written on purpose:

| Flaw                                    | What goes wrong                                                                                                                       | Endpoints                                   |
| --------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------- | ------------------------------------------- |
| **Dependency confusion**                | The resolver picks the highest version of a package from any registry, so a public `acme-logger` 99.0.0 beats the internal 1.3.0.     | `/lab/.../supplychain/packages/resolve`     |
| **Components with known vulnerabilities** | The deploy step ships every component in the bill of materials without checking it against security advisories.                    | `/lab/.../supplychain/deploy`               |

Every flaw has a vulnerable endpoint under `/lab/vulnerable/...` and a secure endpoint under `/lab/secure/...` that does the same job correctly. Two read-only endpoints under `/lab/supplychain/...` show the data both sides work with.

## Access the lab

Start the labs as described in the Installation section of the Introduction page. The Software Supply Chain Failures service then listens on:

| What             | Address                                            |
| ---------------- | -------------------------------------------------- |
| **Swagger UI**   | `http://localhost:5090/swagger`                    |
| API base URL     | `http://localhost:5090`                            |
| Second HTTP port | `5091` (same service, also plain HTTP)             |
| Database         | `SoftwareSupplyChainFailures_LAB` on `sqlserver:1433` |

Open `http://localhost:5090/swagger` in your browser. Swagger is only enabled when `ASPNETCORE_ENVIRONMENT` is `Development`, which the Docker setup already does.

If you run the project from your IDE or with `dotnet run` instead of Docker, use the launch profile: `http` serves `http://localhost:5090`, while `https` serves `https://localhost:5091` and `http://localhost:5090`.

## Seeded data

On first start the service applies its migrations and seeds the database from the JSON files in `src/Context/Data`. There are no real registries here; the tables stand in for them:

- **`dummy.packages.json`**: 7 packages spread over two registries, `internal` (the company's own feed) and `public` (anyone can publish there).
- **`dummy.lock.json`**: the lockfile, which pins the exact version and SHA-256 hash of 3 packages.
- **`dummy.components.json`**: the bill of materials (SBOM), the 5 components that are about to be deployed.
- **`dummy.advisories.json`**: 4 security advisories, each saying which version of a package fixes a problem.
- **`dummy.users.json`**: 100 users. No endpoint in this lab uses them.

The packages that matter most:

| Package       | Version  | Registry   | In the lockfile?           |
| ------------- | -------- | ---------- | -------------------------- |
| `acme-logger` | `1.3.0`  | `internal` | Yes, this is the pinned one |
| `acme-logger` | `99.0.0` | `public`   | No, this is the attacker's  |
| `json-parser` | `3.1.0`  | `public`   | Yes                         |
| `json-parser` | `3.2.0`  | `public`   | No                          |

To find more data, visit the [seed data](https://github.com/yagizerdem/OWASP-lab/tree/main/Lab/SoftwareSupplyChainFailures/src/Context/Data) in the repository.

## Response format

Every successful response uses the same JSON envelope:

```json
{
  "message": "Success",
  "data": {},
  "status": 200
}
```

Errors thrown by the service are written by the shared error handler, which uses capitalised keys:

```json
{
  "Message": "Package is not in the lockfile",
  "Data": null,
  "Status": 400
}
```

The one exception is the blocked deployment in step 5, which is built by the controller itself and keeps the lowercase keys.

## Endpoints

All paths start with `http://localhost:5090`. None of them expect any credentials.

| #   | Method | Path                                           | Purpose                                                      |
| --- | ------ | ---------------------------------------------- | ------------------------------------------------------------ |
| 1   | `GET`  | `/lab/supplychain/packages`                    | List both registries and the lockfile                        |
| 2   | `GET`  | `/lab/vulnerable/supplychain/packages/resolve` | Resolve a package by name, highest version wins (query `name`) |
| 3   | `GET`  | `/lab/secure/supplychain/packages/resolve`     | Resolve a package from the lockfile only (query `name`)      |
| 4   | `GET`  | `/lab/supplychain/sbom`                        | The SBOM and the advisories that affect it                   |
| 5   | `POST` | `/lab/vulnerable/supplychain/deploy`           | Deploy the SBOM without checking advisories                  |
| 6   | `POST` | `/lab/secure/supplychain/deploy`               | Deploy only if no Critical or High advisory applies          |

## Walkthrough in Swagger

In this walkthrough you are first an attacker who wants your code inside ACME's build, and then the engineer who has to stop that. You don't need an account.

### How to send a request in Swagger

Open `http://localhost:5090/swagger`. Every step below follows the same pattern:

1. Find the endpoint in the list and click it to expand it.
2. Click **Try it out**.
3. Paste the value from the step into the `name` field, if the endpoint has one.
4. Click **Execute** and read the status code and body under **Server response**.

You never need the **Authorize** button in this lab.

### Step 1: Look at the registries and the lockfile

Endpoint: **`GET /lab/supplychain/packages`**

This endpoint has no parameters. Click **Execute**. You get two lists: `packages`, everything available in both registries, and `lockfile`, what the build is supposed to use.

Look at `acme-logger`. ACME publishes it on its internal registry, and the lockfile pins version `1.3.0`. But somebody has also published a package with the same name on the public registry, with the version number `99.0.0`:

```json
{
  "name": "acme-logger",
  "version": "99.0.0",
  "source": "public",
  "sha256": "1bc573d9076eed03ebd84feb853d310be4b0fc8ccdeae8ce18ababb6c30ca918"
}
```

Public registries let anyone publish any name that is free there. Internal package names often leak through error messages, job ads or public repositories, so this is all an attacker needs.

### Step 2: Resolve the package the vulnerable way

Endpoint: **`GET /lab/vulnerable/supplychain/packages/resolve`**

Paste this into `name`:

```text
acme-logger
```

The resolver looks in every registry and takes the highest version number it finds:

```json
{
  "message": "Success",
  "data": {
    "name": "acme-logger",
    "version": "99.0.0",
    "source": "public",
    "sha256": "1bc573d9076eed03ebd84feb853d310be4b0fc8ccdeae8ce18ababb6c30ca918"
  },
  "status": 200
}
```

The build would now run the attacker's code. This is **dependency confusion**: nothing checks where the package came from, whether it matches the lockfile, or whether its hash is the expected one.

Try two more names to see that the lockfile is ignored in general:

| `name`        | Result                                                                        |
| ------------- | ----------------------------------------------------------------------------- |
| `json-parser` | Version `3.2.0`, although the lockfile pins `3.1.0`.                          |
| `acme-auth`   | Version `2.0.1`, from whichever registry the database returns first. The internal and the public copy have different hashes, and nothing decides between them. |

A name that doesn't exist in any registry, such as `left-pad`, returns `404` with `Package not found`.

### Step 3: Resolve the same package the secure way

Endpoint: **`GET /lab/secure/supplychain/packages/resolve`**

Paste the same name into `name`:

```text
acme-logger
```

You get the internal, pinned version:

```json
{
  "message": "Success",
  "data": {
    "name": "acme-logger",
    "version": "1.3.0",
    "source": "internal",
    "sha256": "e89689168029e21dbcffef9e75ef4ad7f4102345260a36e8f84ebec23d4cfb11"
  },
  "status": 200
}
```

The secure resolver only uses packages listed in the lockfile, takes exactly the pinned version, only accepts `acme-*` packages from the internal registry, and checks that the hash matches the lockfile. Try the other names:

| `name`         | Result                                                                    |
| -------------- | ------------------------------------------------------------------------- |
| `json-parser`  | `200`, version `3.1.0`, as pinned.                                        |
| `acme-auth`    | `200`, the `internal` copy with hash `4c82f8c2...`.                       |
| `image-resize` | `400` with `Package is not in the lockfile`.                              |

If a package's hash ever differed from the lockfile, the secure resolver would answer `403` with `Package hash does not match the lockfile`. The seed data has no such case, because every pinned version in the allowed registry matches its hash.

### Step 4: Check what is about to be deployed

Endpoint: **`GET /lab/supplychain/sbom`**

This endpoint has no parameters. Click **Execute**. The `components` list is the SBOM, the 5 components that make up the release. The `findings` list matches them against the advisories:

```json
"findings": [
  {
    "component": "http-client",
    "version": "4.0.0",
    "advisoryId": "LAB-2026-0003",
    "severity": "Low",
    "fixedVersion": "4.0.1",
    "summary": "Verbose error messages include request headers"
  },
  {
    "component": "image-resize",
    "version": "0.9.2",
    "advisoryId": "LAB-2026-0001",
    "severity": "Critical",
    "fixedVersion": "1.0.0",
    "summary": "Crafted image file executes code during decoding"
  },
  {
    "component": "yaml-reader",
    "version": "1.4.0",
    "advisoryId": "LAB-2026-0002",
    "severity": "High",
    "fixedVersion": "1.5.0",
    "summary": "Unsafe type loading when reading YAML documents"
  }
]
```

There is also an advisory for `json-parser`, but it was fixed in `3.0.0` and the SBOM already uses `3.1.0`, so it doesn't show up.

### Step 5: Deploy without looking

Endpoint: **`POST /lab/vulnerable/supplychain/deploy`**

This endpoint has no parameters or body. Click **Execute**:

```json
{
  "message": "deployed",
  "data": {
    "components": [
      { "name": "acme-logger", "version": "1.3.0" },
      { "name": "http-client", "version": "4.0.0" },
      { "name": "image-resize", "version": "0.9.2" },
      { "name": "json-parser", "version": "3.1.0" },
      { "name": "yaml-reader", "version": "1.4.0" }
    ]
  },
  "status": 200
}
```

The release went out with `image-resize` 0.9.2, which has a Critical, publicly known code execution bug. The information was available in step 4; the pipeline just never asked for it.

### Step 6: Deploy with the advisory check

Endpoint: **`POST /lab/secure/supplychain/deploy`**

Click **Execute**. This time the deployment is refused with `409`:

```json
{
  "message": "Deployment blocked: components with Critical/High advisories",
  "data": [
    {
      "component": "image-resize",
      "version": "0.9.2",
      "advisoryId": "LAB-2026-0001",
      "severity": "Critical",
      "fixedVersion": "1.0.0",
      "summary": "Crafted image file executes code during decoding"
    },
    {
      "component": "yaml-reader",
      "version": "1.4.0",
      "advisoryId": "LAB-2026-0002",
      "severity": "High",
      "fixedVersion": "1.5.0",
      "summary": "Unsafe type loading when reading YAML documents"
    }
  ],
  "status": 409
}
```

Only Critical and High findings block the release. The Low finding for `http-client` would be returned as a warning if the deployment went through. With the seed data the secure deploy always stops here, because there is no endpoint to upgrade the components.

## What the secure version does differently

- **Dependency confusion (steps 1 to 3):** the build uses a lockfile with exact versions and hashes, internal package names are only ever fetched from the internal registry, and the downloaded package must match the pinned hash. A higher version number somewhere else changes nothing.
- **Known vulnerabilities (steps 4 to 6):** every deployment is checked against advisories, and Critical or High findings stop it. The fix is then to upgrade the component to at least the `fixedVersion` shown in the finding.
