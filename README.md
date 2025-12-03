Here is your **complete, consolidated, production-ready **README**.md** with all sections fully merged:

- Project introduction
- Completed Phase 1 features
- Architecture diagram
- Tech stack
- Full run instructions
- Migration commands
- `appsettings.example.json`
- User Secrets usage
- Azure deployment guidance

You can paste this entire file into your repository.

---

# URL Shortener – .NET 9 MVC

A lightweight, modular, and extensible **URL** Shortening platform built using ****ASP**.**NET** Core **MVC** (.**NET** 9)**. This repository covers **Phase 1**, establishing a clean foundation with backend components, initial UI, database setup, and Redis integration.

---

## 📌 Current Phase Completed: Phase 1 (MVP Foundation)

### What’s done so far

- Modular **MVC** folder structure:

  * Controllers, Views, Models, Services, Middleware
- Implemented:

    * **URL** Shortening Service (Short code generator, **CRUD** support)
    * Redis caching integration (`RedisCacheService`)
    * PostgreSQL + EF Core 9 configuration
- DTOs and ViewModels integrated
- Redirect Middleware scaffold
- Razor Views created for:

    * Home
    * Login
    * **URL** Creation
    * Dashboard
- Prepared structure for OAuth login (Google & Microsoft)
- Ready for Phase 2 enhancements (analytics, redirect pipeline, logging, OAuth flows)

---

## 📁 Project Structure

``` UrlShortener/ │── Controllers/ │── Models/ │── Services/ │── Middleware/ │── Views/ │── wwwroot/ │── appsettings.json │── Program.cs ```

This structure ensures clarity, separation of concerns, and future scalability.

---

# 🏗️ System Architecture Overview

This **URL** Shortener follows a scalable, cloud-ready architecture optimized for low latency and horizontal scaling.

```
                                ┌───────────────────────┐
                                │      Client/User      │
                                └─────────────┬─────────┘
                                              │
                                              ▼
                                ┌────────────────────────────┐
                                │ Load Balancer / **API** GW │
                                └────────────┬───────────────┘
                                             │
                                ┌────────────┴───────────────────┐
                                │       App Servers              │
                                │  (**ASP**.**NET** Core **MVC**)│
                                └────────────┬───────────────────┘
                                             │
                    ┌────────────────────────┼────────────────────────┐
                    │                        │                        │
                    ▼                        ▼                        ▼
            ┌──────────────┐        ┌────────────────┐       ┌─────────────────────┐
            │    Cache     │        │   Database     │       │   ID Generator      │
            │   (Redis)    │        │ (PostgreSQL)   │       │ (Counter / Random)  │
            └──────────────┘        └────────────────┘       └─────────────────────┘
                    │
                    ▼
        ┌─────────────────────────────┐
        │     Background Workers      │
        │  (Cleanup, Analytics, Jobs) │
        └─────────────────────────────┘
```

---

## 🔍 Architecture Breakdown

### Application Layer (.NET 9 MVC)

- Controllers, Services, Middleware, Razor UI
- Stateless, horizontally scalable

### Redis Cache Layer

- Fast short-**URL** lookup
- Reduces DB load
- **TTL**-based cleanup support

### PostgreSQL Database

- Stores **URL** mappings, users, logs, analytics

### ID Generation Layer

- Random/Base62 or counter-based IDs
- Ensures uniqueness & scalability

### Background Workers

Used for:

- Expired **URL** cleanup
- Analytics
- Logging pipelines
- High-volume batch jobs (future phases)

---

# 🧰 Tech Stack

- ****ASP**.**NET** Core **MVC** (.**NET** 9)**
- **EF Core 9** (PostgreSQL / **SQL** Server compatible)
- **Redis Cache** (StackExchange.Redis)
- **Razor Views**
- **Serilog** (Console + PostgreSQL sink)

---

# 🚀 Features Implemented in Phase 1

- Short **URL** creation
- Database + Redis integration
- Caching abstraction
- **MVC** controllers and UI views
- Dashboard layout
- Redirect middleware skeleton

---

# 📄 appsettings.example.json (Safe for GitHub)

Create this file as `appsettings.example.json` and instruct users to copy it to `appsettings.json`.

```json
{
  "AppSettings": {
    "BaseUrl": "https://your-app-base-url"
  },

  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },

  "ConnectionStrings": {
    "DefaultConnection": "Server=YOUR_POSTGRES_SERVER;Database=YOUR_DB_NAME;Port=5432;User Id=YOUR_DB_USER;Password=YOUR_DB_PASSWORD;Ssl Mode=Require;",
    "Redis": "YOUR_REDIS_HOST:6380,password=YOUR_REDIS_KEY,ssl=True,abortConnect=False"
  },

  "Authentication": {
    "Google": {
      "ClientId": "YOUR_GOOGLE_CLIENT_ID",
      "ClientSecret": "YOUR_GOOGLE_CLIENT_SECRET"
    },
    "Microsoft": {
      "ClientId": "YOUR_MICROSOFT_CLIENT_ID",
      "ClientSecret": "YOUR_MICROSOFT_CLIENT_SECRET"
    }
  },

  "Jwt": {
    "Issuer": "UrlShortener",
    "Audience": "UrlShortenerUsers",
    "Key": "YOUR_SUPER_SECRET_KEY_MINIMUM_32_CHARACTERS"
  },

  "AllowedHosts": "*",

  "Serilog": {
    "Using": [ "Serilog.Sinks.Console", "Serilog.Sinks.PostgreSQL" ],
    "MinimumLevel": "Information",
    "WriteTo": [
      {
        "Name": "Console"
      },
      {
        "Name": "PostgreSQL",
        "Args": {
          "connectionString": "Host=YOUR_POSTGRES_SERVER;Port=5432;Database=YOUR_DB_NAME;Username=YOUR_DB_USER;Password=YOUR_DB_PASSWORD;Ssl Mode=Require;Trust Server Certificate=true;",
          "tableName": "Logs",
          "needAutoCreateTable": true,
          "columnOptions": {
            "additionalColumns": [
              { "ColumnName": "UserId", "DataType": "text" },
              { "ColumnName": "RequestPath", "DataType": "text" }
            ]
          }
        }
      }
    ]
  }
}
```

---

# ▶️ Running the Application (Local)

## 1. Prerequisites

| Tool           | Version        |
| -------------- | -------------- |
| **.NET SDK**   | 9.0+           |
| **PostgreSQL** | 14+            |
| **Redis**      | Local or Azure |

Verify installation:

``` dotnet --version ```

---

## 2. Restore Packages

``` dotnet restore ```

---

## 3. Setup Database & Run Migrations

Ensure your connection string is configured.

### Create migration

``` dotnet ef migrations add InitialCreate ```

### Apply migration

``` dotnet ef database update ```

---

## 4. Run the Application

``` dotnet run ```

App runs at:

``` [https://localhost:**5001**](https://localhost:**5001**) [http://localhost:**5000**](http://localhost:**5000**) ```

---

# 🔐 Using User Secrets (Highly Recommended)

Instead of hardcoding secrets:

``` dotnet user-secrets init ```

Set secrets:

``` dotnet user-secrets set *ConnectionStrings:DefaultConnection* *YOUR_CONNECTION_STRING* dotnet user-secrets set *ConnectionStrings:Redis* *YOUR_REDIS* dotnet user-secrets set *Jwt:Key* *YOUR_32_CHAR_SECRET* dotnet user-secrets set *Authentication:Google:ClientId* *XXXXX* dotnet user-secrets set *Authentication:Google:ClientSecret* *XXXXX* dotnet user-secrets set *Authentication:Microsoft:ClientId* *XXXXX* dotnet user-secrets set *Authentication:Microsoft:ClientSecret* *XXXXX* ```

---

# ☁️ Deploying to Azure (High-Level)

## Publish the project:

    ```
    dotnet publish -c Release
    ```
## Deploy to Azure App Service
## Add environment variables in **Configuration**
## Attach Azure PostgreSQL & Redis resources
## Restart the application

---

If you'd like, I can also generate:

- A polished badge section
- A development roadmap for Phase 2
- A Contribution Guide
- A Dockerfile + container instructions

Just let me know and I’ll prepare it.