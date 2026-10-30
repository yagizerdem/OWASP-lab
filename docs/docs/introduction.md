## About The Project

OWASP Lab is a hands-on training project for learning web application security by breaking real code. It covers all ten categories of the [OWASP Top 10 (2025)](https://owasp.org/Top10/), and each category has its own lab: a small ASP.NET Core Web API with its own SQL Server database, filled with dummy data so every attack has something to find.

The vulnerabilities are written into the code on purpose. Instead of reading about SQL injection or broken access control in the abstract, you send real requests to a running service, watch the attack work, and then open the controller to see exactly which line made it possible.

Most labs pair each vulnerable endpoint with a secure one that does the same job correctly. Vulnerable routes live under `/lab/vulnerable/...` and their fixed counterparts under `/lab/secure/...`, so you can send the same payload to both and compare the results. The Injection and Broken Access Control labs only have the vulnerable side for now.

Everything runs locally with a single `docker compose up -d`. You can explore each lab through its Swagger UI, use curl or any REST client, and inspect the databases with Adminer while you work.

:::warning

These services are intentionally insecure. Run them only on your own machine, never expose them to the internet, and don't copy code from a vulnerable controller into a real project.

:::
