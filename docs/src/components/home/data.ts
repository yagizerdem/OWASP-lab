export const REPO_URL = "https://github.com/yagizerdem/OWASP-lab";
export const CLONE_COMMAND = `git clone ${REPO_URL}.git && cd OWASP-lab && docker compose up -d`;

export type Level = "Beginner" | "Intermediate" | "Advanced";

export type Lab = {
  id: string;
  title: string;
  description: string;
  level: Level;
  folder: string;
  port: number;
};

export const LABS: Lab[] = [
  {
    id: "A01",
    title: "Broken Access Control",
    description:
      "Access other users' data by tampering with IDs, tenant tokens, and elevated roles.",
    level: "Beginner",
    folder: "BrokenAccessControl",
    port: 5010,
  },
  {
    id: "A02",
    title: "Security Misconfiguration",
    description:
      "Exploit verbose stack traces, default seed credentials, and unprotected debug endpoints.",
    level: "Beginner",
    folder: "SecurityMisconfiguration",
    port: 5070,
  },
  {
    id: "A03",
    title: "Software Supply Chain Failures",
    description:
      "Abuse vulnerable and untrusted third-party dependencies, malicious NuGet feeds, and untracked CVEs.",
    level: "Intermediate",
    folder: "SoftwareSupplyChainFailures",
    port: 5090,
  },
  {
    id: "A04",
    title: "Cryptographic Failures",
    description:
      "Crack weak hashing algorithms, decrypt hardcoded secrets, and recover exposed sensitive tokens.",
    level: "Intermediate",
    folder: "CryptographicFailures",
    port: 5020,
  },
  {
    id: "A05",
    title: "Injection",
    description:
      "SQL injection, blind timing-based SQLi, and unsafe dynamic raw queries bypassing ORM protections.",
    level: "Beginner",
    folder: "Injection",
    port: 5030,
  },
  {
    id: "A06",
    title: "Insecure Design",
    description:
      "Abuse weak OTP resets, race condition balances, and unenforced business domain validation rules.",
    level: "Advanced",
    folder: "InsecureDesign",
    port: 5040,
  },
  {
    id: "A07",
    title: "Authentication Failures",
    description:
      "User enumeration through login errors, brute force without lockout, and tokens that survive logout.",
    level: "Beginner",
    folder: "AuthenticationFailures",
    port: 5000,
  },
  {
    id: "A08",
    title: "Software or Data Integrity Failures",
    description:
      "Tamper with unsigned serialized JSON/binary payloads and forge metadata in update pipelines.",
    level: "Intermediate",
    folder: "SoftwareOrDataIntegrityFailures",
    port: 5080,
  },
  {
    id: "A09",
    title: "Security Logging & Alerting Failures",
    description:
      "Execute stealth reconnaissance and exfiltration attacks without leaving observable audit trails.",
    level: "Advanced",
    folder: "SecurityLoggingAndAlertingFailures",
    port: 5060,
  },
  {
    id: "A10",
    title: "Mishandling Exceptional Conditions",
    description:
      "Turn unhandled core exceptions, resource starvation, and edge crash states into exploit primitives.",
    level: "Intermediate",
    folder: "MishandlingOfExceptionalConditions",
    port: 5050,
  },
];

export const labUrl = (folder: string) => `${REPO_URL}/tree/main/Lab/${folder}`;
