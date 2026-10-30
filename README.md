## Introduction

OWASP Lab is a hands-on training project for learning web application security by breaking real code. It covers all ten categories of the [OWASP Top 10 (2025)](https://owasp.org/Top10/), and each category has its own lab: a small ASP.NET Core Web API with its own SQL Server database, filled with dummy data so every attack has something to find.

The vulnerabilities are written into the code on purpose. You send real requests to a running service, watch the attack work, and then open the controller to see exactly which line made it possible.

> The services are intentionally insecure. Run them only on your own machine and never expose them to the internet.

## Overview

- **Ten labs, one per category.** Broken Access Control, Security Misconfiguration, Software Supply Chain Failures, Cryptographic Failures, Injection, Insecure Design, Authentication Failures, Software or Data Integrity Failures, Security Logging and Alerting Failures, and Mishandling of Exceptional Conditions.
- **Vulnerable and secure side by side.** Most labs pair each vulnerable endpoint (`/lab/vulnerable/...`) with a fixed one (`/lab/secure/...`), so the same payload can be sent to both and the results compared. The Injection and Broken Access Control labs only have the vulnerable side for now.
- **One command to run everything.** `docker compose up -d` starts SQL Server, Adminer and all ten labs. Each lab has its own database and seeded dummy data.
- **Explore with the tools you like.** Every lab ships a Swagger UI, and you can also use curl or any REST client. Adminer lets you inspect the databases.
- **Written guides.** A Docusaurus site in `docs/` explains each lab: its endpoints, how to test them from Swagger, why the flaw works and how to fix it.

Ports start at `5000` and go up by ten per lab (`5000` Authentication Failures, `5010` Broken Access Control, and so on up to `5090`). Swagger is at `http://localhost:<port>/swagger`, and Adminer is at `http://localhost:8080`.

To get started, clone the repository and run:

```bash
docker compose up -d
```

## Folder Structure

```text
OWASP-lab/
├── Lab/
│   ├── AuthenticationFailures/
│   ├── BrokenAccessControl/
│   ├── CryptographicFailures/
│   ├── Injection/
│   │   ├── Dockerfile
│   │   ├── Injection.slnx
│   │   └── src/
│   │       ├── Backend/        # ASP.NET Core Web API: controllers and Program.cs
│   │       ├── Context/        # EF Core DbContext, migrations, seeders, dummy data
│   │       ├── Models/         # Entities and DTOs
│   │       └── Scripts/
│   │           └── DummyData/  # Generates the dummy JSON files
│   ├── InsecureDesign/
│   ├── MishandlingOfExceptionalConditions/
│   ├── SecurityLoggingAndAlertingFailures/
│   ├── SecurityMisconfiguration/
│   ├── SoftwareOrDataIntegrityFailures/
│   ├── SoftwareSupplyChainFailures/
│   └── Shared/
│       └── OwaspLab.Common/    # Code shared by all labs: ApiResponse, exceptions, JWT helper
├── docs/                       # Docusaurus documentation site (React, Tailwind CSS)
├── docker-compose.yml          # SQL Server, Adminer and the ten lab services
├── init-db.sh                  # Creates the lab databases in SQL Server
├── DESIGN.light.md             # Design tokens for the docs site, light theme
├── DESIGN.dark.md              # Design tokens for the docs site, dark theme
├── LICENSE.txt
└── README.md
```

Every lab follows the same layout as `Injection` above: `Backend`, `Context`, `Models` and `Scripts/DummyData` inside `src/`, plus its own `Dockerfile` and solution file.

## Author

**yagiz erdem**

- GitHub: [@yagizerdem](https://github.com/yagizerdem)
- Project: [github.com/yagizerdem/OWASP-lab](https://github.com/yagizerdem/OWASP-lab)

## License

Distributed under the MIT License. See [`LICENSE.txt`](./LICENSE.txt) for more information.
