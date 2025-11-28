**URL** Shortener (TinyURL-Style) – .**NET** 9 + Postgres + Redis

A minimal, high-performance, cloud-ready **URL** Shortener built using:

.**NET** 9 Minimal APIs

PostgreSQL (persistent storage)

Redis (read-side cache)

Base62 ID generation via Postgres sequence

Background cleanup worker for expired URLs

Health checks (liveness/readiness)

This **MVP** is designed for extensibility toward large-scale deployments, distributed caching, rate limiting, and multi-region failover.

Features

Create short URLs (random Base62 or custom alias).

Redirect instantly with Redis-backed caching.

Optional expiration time.

Click count incremented asynchronously.

Expired links auto-cleaned in a hosted background worker.

Health endpoints for Kubernetes/containers.

### Project Structure

UrlShortener/
 ├── UrlShortener.Api/          → Minimal **API** host
 ├── UrlShortener.Application/  → Services, DTOs, ID generator interface
 ├── UrlShortener.Domain/       → Domain models
 ├── UrlShortener.Infrastructure/→ EF Core repo, Redis cache, Base62, DB context
 ├── UrlShortener.Workers/      → Cleanup background service
 └── deploy/
    ├── Dockerfile
    └── init-db.sql

Prerequisites

.**NET** 9 **SDK**

Docker (for Postgres + Redis)

Postgres client (optional for debugging)

## Run Postgres + Redis Locally

Create a simple docker-compose.yml:

version: '3.9'
services:
    postgres:
    image: postgres:16
    environment:
    POSTGRES_USER: urlshort
    POSTGRES_PASSWORD: urlshort
    POSTGRES_DB: urlshortdb
    ports:
    - ***5432**:**5432***
    volumes:
    - ./deploy/init-db.sql:/docker-entrypoint-initdb.d/init.sql

    redis:
    image: redis:7
    ports:
    - ***6379**:**6379***

Start services:

docker compose up -d

## Configure the App

Create appsettings.Development.json inside UrlShortener.Api:

{
    *ConnectionStrings*: {
    *DefaultConnection*: "Host=localhost;Port=**5432**;Database=urlshortdb;Username=urlshort;Password=urlshort*
    },
    *Redis*: {
    *Connection*: *localhost:**6379***
    },
    *DefaultHost*: *localhost:**5173**"
}

## Run the API

Navigate to the **API** project:

cd UrlShortener.Api dotnet run

By default it runs at:

[http://localhost:**5173**](http://localhost:**5173**)

## API Usage

Create a Short **URL** **POST** /api/v1/shorten Content-Type: application/json

{
    *originalUrl*: *[https://example.com*,](https://example.com*,)
    *customAlias*: null,
    *expireAt*: null
}

Response:

{
    *code*: *abc123*,
    *shortUrl*: *[http://localhost:**5173**/abc123*](http://localhost:**5173**/abc123*)
}

Redirect **GET** /{code}

Redirects to the original **URL**.

### Health Checks

Liveness:

**GET** /health/live

Readiness:

**GET** /health/ready

## Build & Run in Docker

Go to the repository root:

docker build -f deploy/Dockerfile -t urlshortener .
docker run -p **8080**:80 \
    -e ConnectionStrings__DefaultConnection=*Host=postgres;...* \
    -e Redis__Connection=*redis:**6379*** \
    urlshortener

For local networks, use docker-compose networking.