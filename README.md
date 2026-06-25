# Forms Data Management API

A RESTful ASP.NET Core 8 Web API for creating, reading, updating, and soft-deleting form records. Built with JWT authentication, FluentValidation, Entity Framework Core (SQL Server), and Swagger/OpenAPI.

---

## Table of Contents

- [Features](#features)
- [Tech Stack](#tech-stack)
- [Project Structure](#project-structure)
- [Getting Started](#getting-started)
  - [Prerequisites](#prerequisites)
  - [Configuration](#configuration)
  - [Run the API](#run-the-api)
- [API Reference](#api-reference)
  - [Authentication](#authentication)
  - [Endpoints](#endpoints)
  - [Request & Response Schemas](#request--response-schemas)
- [Validation Rules](#validation-rules)
- [Error Handling](#error-handling)
- [Running Tests](#running-tests)

---

## Features

- Full CRUD with **soft delete** (records are never permanently removed)
- **JWT Bearer authentication** — all endpoints require a valid token
- **Optimistic concurrency** via SQL Server `rowversion`
- **Paged, filtered listing** by subject, priority, and criticality
- **FluentValidation** with structured `ValidationProblemDetails` responses
- **Swagger UI** with Bearer token support (Development only)
- Global exception middleware — no unhandled 500s leak stack traces

---

## Tech Stack

| Layer | Technology |
|---|---|
| Framework | ASP.NET Core 8 |
| ORM | Entity Framework Core 8 (SQL Server) |
| Validation | FluentValidation 11 |
| Auth | JWT Bearer (`Microsoft.AspNetCore.Authentication.JwtBearer`) |
| Docs | Swashbuckle / Swagger UI |
| Tests | xUnit · Moq · FluentAssertions · `Microsoft.AspNetCore.Mvc.Testing` |

---

## Project Structure

```
FormsDataManagementAPI/
├── Authorization/          # IFormAuthorizationService + stub implementation
├── Controllers/            # FormsController (REST endpoints)
├── Data/                   # ApplicationDbContext (EF Core config + indexes)
├── DTOs/                   # Request/response records + FormListQuery
├── Extensions/             # ValidationExtensions (FluentValidation → ProblemDetails)
├── Middleware/             # ExceptionHandlingMiddleware
├── Models/                 # FormData entity
├── Repositories/           # IFormDataRepository + FormDataRepository
├── Services/               # IFormService + FormService
├── Validators/             # CreateFormRequestValidator, UpdateFormRequestValidator
├── appsettings.json
└── Program.cs

FormsDataManagementAPI.Tests/
├── Controllers/            # FormsControllerTests (integration tests via WebApplicationFactory)
├── Repositories/           # FormDataRepositoryTests
├── Services/               # FormServiceTests
└── Validators/             # CreateFormRequestValidatorTests

TokenGenerator/             # Console utility for generating local development JWT tokens
```

---

## Getting Started

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- SQL Server or SQL Server LocalDB (`(localdb)\mssqllocaldb` is the default)

### Configuration

Copy `appsettings.json` values and set your secrets (use `dotnet user-secrets` in development):

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=FormsDataManagement;Trusted_Connection=True;"
  },
  "Jwt": {
    "Key": "REPLACE_WITH_A_LONG_SECRET_KEY_AT_LEAST_32_CHARS",
    "Issuer": "FormsDataManagementAPI",
    "Audience": "FormsDataManagementAPI"
  }
}
```

> **Important:** The JWT key must be at least 32 characters. Never commit a real key to source control.

Set secrets locally:

```bash
dotnet user-secrets set "Jwt:Key" "your-super-secret-key-here"
```

### Run the API

```bash
# Apply EF Core migrations (first run)
dotnet ef database update --project FormsDataManagementAPI

# Start the API
dotnet run --project FormsDataManagementAPI
```

Swagger UI is available at `https://localhost:{port}/swagger` when running in Development mode.

---

## API Reference

### Authentication

All endpoints require a JWT Bearer token in the `Authorization` header:

```
Authorization: Bearer <your-token>
```

The user identity (`sub` or `NameIdentifier` claim) is used to scope create/update/delete operations and for authorization checks.

### Endpoints

| Method | Route | Description |
|--------|-------|-------------|
| `POST` | `/api/forms` | Create a new form |
| `GET` | `/api/forms/{id}` | Get a form by ID |
| `GET` | `/api/forms` | List forms (paged + filtered) |
| `PUT` | `/api/forms/{id}` | Update an existing form |
| `DELETE` | `/api/forms/{id}` | Soft-delete a form |

### Request & Response Schemas

#### `CreateFormRequest` / `UpdateFormRequest`

```json
{
  "subject": "string (required, max 200 chars)",
  "description": "string? (max 4000 chars)",
  "dueDate": "datetime? (must be future)",
  "priority": "int? (1–10)",
  "critical": "bool?"
}
```

#### `FormDataResponse`

```json
{
  "id": "guid",
  "subject": "string",
  "description": "string?",
  "dueDate": "datetime?",
  "priority": "int?",
  "critical": "bool?",
  "createdAt": "datetime",
  "updatedAt": "datetime?",
  "createdBy": "string"
}
```

#### `GET /api/forms` Query Parameters

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `page` | int | `1` | Page number (≥ 1) |
| `pageSize` | int | `20` | Items per page (1–100) |
| `subjectFilter` | string? | — | Partial match on subject |
| `criticalFilter` | bool? | — | Filter by critical flag |
| `priorityFilter` | int? | — | Filter by exact priority |

Response is a `PagedResult<FormDataResponse>`:

```json
{
  "items": [...],
  "totalCount": 42,
  "page": 1,
  "pageSize": 20
}
```

---

## Validation Rules

| Field | Rule |
|-------|------|
| `Subject` | Required, max 200 characters |
| `Priority` | 1–10 (inclusive), when provided |
| `DueDate` | Must be a future date/time, when provided |

Validation failures return `400 Bad Request` with a standard `ValidationProblemDetails` body:

```json
{
  "title": "One or more validation errors occurred.",
  "errors": {
    "Subject": ["Subject is required."],
    "Priority": ["Priority must be between 1 and 10."]
  }
}
```

---

## Error Handling

The `ExceptionHandlingMiddleware` catches all unhandled exceptions and returns a consistent JSON error body — no stack traces are exposed.

| Scenario | HTTP Status | Message |
|----------|-------------|---------|
| Concurrency conflict (`RowVersion` mismatch) | `409 Conflict` | Fetch latest version and retry |
| Database error | `503 Service Unavailable` | Try again later |
| Unexpected exception | `500 Internal Server Error` | Generic message |
| Client disconnects | — | Silently ignored |

---

## Running Tests

```bash
dotnet test FormsDataManagementAPI.Tests
```

53 tests across four layers:

| Suite | Count | Approach |
|-------|------:|----------|
| `FormsControllerTests` | 14 | Integration — `WebApplicationFactory` spins up the full ASP.NET Core pipeline in-memory; `IFormService` is mocked, JWT is replaced with a test auth handler |
| `FormServiceTests` | 12 | Unit — service logic and authorization checks with mocked repository |
| `FormDataRepositoryTests` | 13 | Integration — EF Core InMemory database |
| `CreateFormRequestValidatorTests` | 14 | Unit — FluentValidation rule correctness |

> **Controller integration tests** cover all five endpoints (Create, GetById, List, Update, Delete) including validation rejection, 404 paths, and a 401 unauthenticated request.
