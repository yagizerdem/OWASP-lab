## About The Project

OWASP Lab is a hands-on training project for learning web application security by breaking real code. It covers all ten categories of the [OWASP Top 10 (2025)](https://owasp.org/Top10/), and each category has its own lab: a small ASP.NET Core Web API with its own SQL Server database, filled with dummy data so every attack has something to find.

The vulnerabilities are written into the code on purpose. Instead of reading about SQL injection or broken access control in the abstract, you send real requests to a running service, watch the attack work, and then open the controller to see exactly which line made it possible.

Most labs pair each vulnerable endpoint with a secure one that does the same job correctly. Vulnerable routes live under `/lab/vulnerable/...` and their fixed counterparts under `/lab/secure/...`, so you can send the same payload to both and compare the results. The Injection and Broken Access Control labs only have the vulnerable side for now.

Everything runs locally with a single `docker compose up -d`. You can explore each lab through its Swagger UI, use curl or any REST client, and inspect the databases with Adminer while you work.

:::warning

These services are intentionally insecure. Run them only on your own machine, never expose them to the internet, and don't copy code from a vulnerable controller into a real project.

:::

## Built With

<table>
  <tr>
    <td align="center" width="150">
      <img src="./img/tech/dotnet.svg" width="64" height="64" alt=".NET" />
      <br /><br />
      <a href="https://dotnet.microsoft.com/apps/aspnet"><strong>ASP.NET Core</strong></a>
      <br />.NET 10
    </td>
    <td align="center" width="150">
      <img src="./img/tech/efcore.svg" width="64" height="64" alt="Entity Framework Core" />
      <br /><br />
      <a href="https://learn.microsoft.com/ef/core/"><strong>EF Core</strong></a>
      <br />10
    </td>
    <td align="center" width="150">
      <img src="./img/tech/sqlserver.svg" width="64" height="64" alt="SQL Server" />
      <br /><br />
      <a href="https://www.microsoft.com/sql-server"><strong>SQL Server</strong></a>
      <br />2025
    </td>
    <td align="center" width="150">
      <img src="./img/tech/swagger.svg" width="64" height="64" alt="Swagger" />
      <br /><br />
      <a href="https://github.com/domaindrivendev/Swashbuckle.AspNetCore"><strong>Swashbuckle</strong></a>
      <br />Swagger UI
    </td>
    <td align="center" width="150">
      <img src="./img/tech/docker.svg" width="64" height="64" alt="Docker" />
      <br /><br />
      <a href="https://www.docker.com/"><strong>Docker</strong></a>
      <br />Docker Compose
    </td>
  </tr>
</table>

## Installation

### Prerequisites

- [Docker Desktop](https://www.docker.com/products/docker-desktop/) (includes Docker Compose). Make sure it is running before you start.
- [Git](https://git-scm.com/downloads) to clone the repository.
- A few GB of free disk space and these ports free: `1433`, `8080` and `5000` to `5091`.

### Run the labs

1. Clone the repository and move into it:

   ```bash
   git clone https://github.com/yagizerdem/OWASP-lab.git
   cd OWASP-lab
   ```

2. Build and start everything:

   ```bash
   docker compose up -d
   ```

   The first run builds ten images and starts SQL Server, so it can take a few minutes. Each lab creates and seeds its own database when it starts.

3. Check that all containers are up:

   ```bash
   docker compose ps
   ```

### Open a lab

Every lab listens on its own port. Open its Swagger UI at `http://localhost:<port>/swagger`.

| Lab                                    | Port   |
| -------------------------------------- | ------ |
| Authentication Failures                | `5000` |
| Broken Access Control                  | `5010` |
| Cryptographic Failures                 | `5020` |
| Injection                              | `5030` |
| Insecure Design                        | `5040` |
| Mishandling of Exceptional Conditions  | `5050` |
| Security Logging and Alerting Failures | `5060` |
| Security Misconfiguration              | `5070` |
| Software or Data Integrity Failures    | `5080` |
| Software Supply Chain Failures         | `5090` |

To look at the data behind a lab, open Adminer at `http://localhost:8080` and sign in with system **MS SQL**, server `sqlserver`, username `sa` and password `12345aA!`. Each lab uses its own database, named after the lab, for example `Injection_LAB`.

### Stop and reset

```bash
docker compose down        # stop the labs, keep the data
docker compose down -v     # stop the labs and delete all databases
```

:::tip

If a lab's data gets messy after a few attacks, `docker compose down -v` followed by `docker compose up -d` gives you a fresh, re-seeded database.

:::
