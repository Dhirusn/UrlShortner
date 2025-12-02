**URL** Shortener – .**NET** 9 **MVC** 

A lightweight, modular, and extensible **URL** Shortening platform built using **ASP**.**NET** Core **MVC** (.**NET** 9). This repository contains the first development phase, focused on creating a clean foundation with essential backend components, initial frontend views, database configuration, and Redis integration for caching.

📌 Current Phase Completed: Phase 1 (Core **MVP** Foundation) What’s done so far

Implemented modular **MVC** folder structure for controllers, views, models, and services.

Added Home, **URL**, Auth, and Dashboard controllers with initial actions.

Implemented **URL** Shortening Service with:

Short code generation

Basic **CRUD** support

Integrated Redis Caching Layer (StackExchange.Redis).

Added RedisCacheService and connected Redis multiplexer.

Added EF Core **SQL** Database configuration (DbContext + Migrations ready).

Implemented DTOs and ViewModels for early UI workflows.

Added Redirect Middleware skeleton for handling short **URL** redirects.

Created Razor Views for:

Home

Login

**URL** Create

Dashboard

Prepared project structure for OAuth login (implementation to follow in Phase 2).

📁 Project Structure (Current) UrlShortener/ │── Controllers/ │── Models/ │── Services/ │── Middleware/ │── Views/ │── wwwroot/ │── appsettings.json │── Program.cs

A clean structure focusing on clarity, separation of concerns, and scalability for upcoming phases.

🧰 Tech Stack

**ASP**.**NET** Core **MVC** (.**NET** 9)

EF Core 9 (**SQL** Server / SQLite ready)

Redis Cache (StackExchange.Redis)

### Razor Views

Middleware-based redirect pipeline

🚀 Features Implemented in Phase 1

Short **URL** creation (basic logic)

Database + Redis configuration

Caching layer abstraction

**MVC** controllers + views

Basic dashboard UI scaffolding

Redirect middleware setup (logic coming)
