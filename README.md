# Chatty.BE

Chatty.BE is a layered ASP.NET Core 10 backend for a chat application. It provides JWT authentication with refresh tokens, private and group conversations, messaging with attachments, Cloudinary-based file uploads, SignalR notifications, and Redis-ready distributed caching, backed by SQL Server and Entity Framework Core.

## Table of Contents
- [Chatty.BE](#chattybe)
  - [Table of Contents](#table-of-contents)
  - [Key Features](#key-features)
  - [System Architecture](#system-architecture)
  - [Project Structure](#project-structure)
  - [Tech Stack](#tech-stack)
  - [Getting Started](#getting-started)
  - [Database \& Migrations](#database--migrations)
  - [Configuration](#configuration)
  - [Running \& Building](#running--building)
  - [CI Pipeline](#ci-pipeline)
  - [API Overview](#api-overview)
    - [Auth (`/api/auth`)](#auth-apiauth)
    - [Users (`/api/users`)](#users-apiusers)
    - [Conversations (`/api/conversations`)](#conversations-apiconversations)
    - [Messages (`/api/conversations/{conversationId}/messages`)](#messages-apiconversationsconversationidmessages)
    - [Files (`/api/files`)](#files-apifiles)
    - [SignalR Hub (`/hubs/chat`)](#signalr-hub-hubschat)
  - [Usage Examples](#usage-examples)
  - [Tests](#tests)

## Key Features
- JWT bearer auth with hashed refresh tokens, BCrypt password hashing, and session tracking.
- Private and group conversations with participant add/remove flows.
- Messaging with attachments, unread counts, and per-user receipt tracking.
- User profile lookup and updates plus keyword search.
- Cloudinary-backed file upload endpoint that returns secure URLs for reuse in messages.
- Real-time notifications over authenticated SignalR hub `/hubs/chat` for messages, read receipts, and participant changes.
- Distributed caching for read-heavy user, conversation, and message queries (Redis when enabled, in-memory fallback otherwise).
- Structured error handling middleware returning RFC7807 `ProblemDetails`.
- Automated tests (xUnit) covering application services and API controllers.

## System Architecture
- **API layer (`Chatty.BE.API`)**: ASP.NET Core controllers expose REST endpoints; Swagger configured for interactive docs; JWT authentication middleware; custom exception middleware; SignalR hub registration.
- **Application layer (`Chatty.BE.Application`)**: DTOs, service interfaces, and service implementations for auth, conversations, messages, users; cross-cutting helpers (date/time, file validation).
- **Domain layer (`Chatty.BE.Domain`)**: Entity models (`User`, `Conversation`, `Message`, `MessageAttachment`, `MessageReceipt`, `RefreshToken`) and enums (`MessageType`, `MessageStatus`).
- **Infrastructure layer (`Chatty.BE.Infrastructure`)**: EF Core persistence (SQL Server), repository implementations, Unit of Work, AutoMapper profiles, JWT token provider, password hashing, Cloudinary file storage, SignalR notification service, distributed cache decorators, dependency injection registration.
- **Tests (`Tests`)**: xUnit projects for application services, API integration (in-memory EF Core), domain, and infrastructure components.

## Project Structure
- `Chatty.BE.API/` - Program/bootstrap, controllers, request contracts, middleware, Swagger setup, SignalR hub mapping.
- `Chatty.BE.Application/` - DTOs, service interfaces, service implementations, helpers, and custom exceptions.
- `Chatty.BE.Domain/` - Core entities and enums shared across layers.
- `Chatty.BE.Infrastructure/` - EF Core DbContext/configurations, repositories, dependency injection, security (JWT, hashing), cache decorators, Cloudinary service, SignalR hub/client contracts, AutoMapper profile.
- `Tests/` - `Chatty.BE.API.IntegrationTests`, `Chatty.BE.Application.Test`, `Chatty.BE.Domain.Tests`, `Chatty.BE.Infrastructure.Tests`.
- `.github/workflows/dotnet-auto-unit-test.yml` - CI workflow for automated tests.

## Tech Stack
- .NET 10, ASP.NET Core Web API, SignalR.
- Entity Framework Core (SQL Server, InMemory for tests), AutoMapper.
- JWT authentication (`Microsoft.AspNetCore.Authentication.JwtBearer`, custom `JwtTokenProvider`).
- Distributed caching (`Microsoft.Extensions.Caching.StackExchangeRedis` with in-memory fallback).
- BCrypt password hashing (`BCrypt.Net-Next`).
- Cloudinary file uploads (`CloudinaryDotNet`).
- Swagger / OpenAPI (`Swashbuckle.AspNetCore`).
- Testing: xUnit, `Microsoft.AspNetCore.Mvc.Testing`, `Microsoft.NET.Test.Sdk`, coverlet.

## Docker Development
For a streamlined local development experience, you can use Docker Compose to run the API and all its dependencies (SQL Server, Redis).

### Prerequisites
- [Docker Desktop](https://www.docker.com/products/docker-desktop) installed and running.

### Setup & Run
1. **Configure Environment**: A root `.env` file is used by Docker Compose. You can customize it if needed.
2. **Start Services**:
   ```bash
   # Using Git Bash / Linux / macOS
   ./dev.sh up

   # Using PowerShell (Windows)
   .\dev.ps1 up
   
   # Or using Docker Compose directly
   docker-compose -f docker-compose.dev.yml up -d
   ```
3. **Run Migrations**: Since the database is now running in a container, apply migrations to it from your host:
   ```bash
   # Using Git Bash / Linux / macOS
   ./dev.sh migrate

   # Using PowerShell (Windows)
   .\dev.ps1 migrate
   
   # Or manually
   dotnet ef database update --project Chatty.BE.Infrastructure --startup-project Chatty.BE.API
   ```
4. **Access the API**: The API will be available at `http://localhost:8080/swagger`.

### Common Commands
All commands below can be run with `./dev.sh <action>` or `.\dev.ps1 <action>`:
- `logs`: Follow logs from all containers.
- `build`: Rebuild the API container.
- `down`: Stop and remove all containers.
- `ps`: List running containers.

## Getting Started
1. Install .NET 10 SDK and access to SQL Server.
2. From the repository root, restore packages:
   ```bash
   dotnet restore
   ```
3. Create a `.env` in `Chatty.BE.API/` or set environment variables (see Configuration).
4. Apply EF Core migrations to your database:
   ```bash
   dotnet ef database update --project Chatty.BE.Infrastructure --startup-project Chatty.BE.API
   ```
   (Install the `dotnet-ef` tool if needed: `dotnet tool install --global dotnet-ef`.)
5. Run the API:
   ```bash
   dotnet run --project Chatty.BE.API
   ```
6. Open Swagger UI at `https://localhost:5001/swagger` (or the port shown on startup).

## Database & Migrations
- Ensure SQL Server is reachable and `DEFAULT_CONNECTION`/`ConnectionStrings:DefaultConnection` is set.
- Create/update database with EF Core tools:
  ```bash
  dotnet ef database update \
    --project Chatty.BE.Infrastructure \
    --startup-project Chatty.BE.API
  ```
  If `dotnet ef` is not installed, add it once: `dotnet tool install --global dotnet-ef`.
- To add a migration (example):
  ```bash
  dotnet ef migrations add AddMyChange \
    --project Chatty.BE.Infrastructure \
    --startup-project Chatty.BE.API \
    --output-dir Persistence/Migrations
  ```

## Configuration
Configuration is read from environment variables (preferred) or `appsettings.*`. Key settings:

| Setting | Description |
| --- | --- |
| `DEFAULT_CONNECTION` or `ConnectionStrings__DefaultConnection` | SQL Server connection string used by EF Core. |
| `JWT_PRIVATE_KEY` / `JWT_PUBLIC_KEY` | PEM-encoded RSA key pair. Signing uses `JWT_PRIVATE_KEY`; validation prefers `JWT_PUBLIC_KEY` when set. |
| `JWT_SECRET` | Symmetric key for JWT signing/validation fallback when RSA public key is not provided. |
| `JWT_ISSUER` / `JWT_AUDIENCE` | Token issuer/audience; defaults to `Chatty.BE` / `Chatty.BE.Clients`. |
| `JWT_EXPIRATION_MINUTES` | Access token lifetime in minutes (default 15). |
| `JWT_REFRESH_DAYS` | Refresh token lifetime in days (default 30). |
| `RedisCache__Enabled` | Enable Redis-backed distributed caching (`true`/`false`). |
| `RedisCache__Configuration` | Redis connection string (for example `localhost:6379`). |
| `RedisCache__InstanceName` | Key prefix namespace for cache entries. |
| `RedisCache__UserCacheSeconds` | TTL for user query cache entries. |
| `RedisCache__ConversationCacheSeconds` | TTL for conversation query cache entries. |
| `RedisCache__MessageCacheSeconds` | TTL for message/unread cache entries. |
| `CLOUDINARY_CLOUD_NAME`, `CLOUDINARY_API_KEY`, `CLOUDINARY_API_SECRET` | Cloudinary credentials for uploads. |
| `CLOUDINARY_FOLDER` | Optional Cloudinary folder/prefix. |

Example `.env` for local development (use your own secrets):
```env
DEFAULT_CONNECTION=Server=localhost;Database=ChattyDb;User Id=sa;Password=Pass@word1;TrustServerCertificate=True;
JWT_SECRET=your-256-bit-secret
JWT_ISSUER=Chatty.BE
JWT_AUDIENCE=Chatty.BE.Clients
JWT_EXPIRATION_MINUTES=15
JWT_REFRESH_DAYS=30
RedisCache__Enabled=false
RedisCache__Configuration=localhost:6379
RedisCache__InstanceName=chatty:be:dev:
CLOUDINARY_CLOUD_NAME=your-cloud
CLOUDINARY_API_KEY=your-key
CLOUDINARY_API_SECRET=your-secret
```

## Running & Building
- Build solution: `dotnet build Chatty.BE.sln`
- Run API: `dotnet run --project Chatty.BE.API`
- Run all tests: `dotnet test`

## CI Pipeline
- Workflow: `.github/workflows/dotnet-auto-unit-test.yml`
- Trigger: runs on every pull request.
- Quality gates (in order):
  1. `dotnet restore Chatty.BE.sln`
  2. `dotnet format Chatty.BE.sln --verify-no-changes --severity warn --no-restore`
  3. `dotnet build Chatty.BE.sln --configuration Release --no-restore`
  4. `dotnet test Chatty.BE.sln --configuration Release --no-build --collect:"XPlat Code Coverage"`
  5. `dotnet list Chatty.BE.sln package --vulnerable --include-transitive`
- The workflow uses read-only token permissions and dependency caching via `actions/setup-dotnet`.

## API Overview
Authentication uses Bearer JWT. Endpoints marked [auth] require an access token in `Authorization: Bearer <token>`.

### Auth (`/api/auth`)
- `POST /register` - Register user with `userName`, `email`, `password`; returns user identity data.
- `POST /login` - Returns `accessToken`, `refreshToken`, and expiration windows.
- `POST /change-password` - Update password for a given `userId`; forbids when caller is not that user. *[No `[Authorize]` attribute applied; relies on caller-provided `UserId`]*.
- `POST /logout` - Revokes a specific refresh token for `userId`.
- `POST /refresh` - Exchanges refresh token for new access/refresh pair; revokes reused/expired tokens.
- [auth] `POST /logout-all-sessions` - Revokes all refresh tokens for current user.
- [auth] `GET /sessions` - Lists active (non-expired) refresh tokens for current user.

### Users (`/api/users`)
- [auth] `GET /{id}` - Get user by id.
- [auth] `GET /by-username/{userName}` - Get user by username.
- [auth] `GET /search?keyword=...` - Search by username/email/display name (case-insensitive LIKE).
- [auth] `PUT /{id}` - Update current user profile (display name, avatar URL, bio); forbids updating others.

### Conversations (`/api/conversations`)
- [auth] `POST /private` - Create or return an existing private conversation between two users.
- [auth] `POST /group` - Create group conversation with owner and participant ids.
- [auth] `POST /{id}/participants` - Add participant to conversation.
- [auth] `DELETE /{id}/participants/{userId}` - Remove participant.
- [auth] `GET` - List conversations for `userId` (ordered by last activity).
- [auth] `GET /{id}` - Get conversation with participants.

### Messages (`/api/conversations/{conversationId}/messages`)
- [auth] `POST /` - Send message; body includes `content`, `type` (`Text|Image|File`), optional attachments.
- [auth] `GET /` - Paginated messages (`page`, `pageSize`).
- [auth] `PUT /read` - Mark all conversation messages as read for current user.
- [auth] `GET /unread-count` - Count unread messages for current user in the conversation.

### Files (`/api/files`)
- [auth] `POST /upload` - Multipart upload (`file` form field); returns `{ "fileUrl": "..." }` from Cloudinary.

### SignalR Hub (`/hubs/chat`)
- Authentication: hub connections require a valid JWT access token (`[Authorize]` + endpoint authorization policy).
- Groups: connection joins a group keyed by user id; server also broadcasts to conversation id groups.
- Client methods exposed by `IChatClient`: `ReceiveMessage`, `MessagesRead(conversationId, readerUserId, messageIds)`, `UserJoinedConversation`, `UserLeftConversation`.
- Scale-out: when `RedisCache__Enabled=true`, SignalR uses Redis backplane (`Microsoft.AspNetCore.SignalR.StackExchangeRedis`) so events propagate across multiple API instances.

## Usage Examples
Login:
```bash
curl -X POST http://localhost:5000/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"demo@example.com","password":"P@ssw0rd!"}'
```

Send a text message:
```bash
curl -X POST http://localhost:5000/api/conversations/{conversationId}/messages \
  -H "Authorization: Bearer <access_token>" \
  -H "Content-Type: application/json" \
  -d '{"senderId":"<current-user-id>","content":"Hello","type":0,"attachments":[]}'
```

Mark conversation as read:
```bash
curl -X PUT http://localhost:5000/api/conversations/{conversationId}/messages/read \
  -H "Authorization: Bearer <access_token>"
```

Upload file:
```bash
curl -X POST http://localhost:5000/api/files/upload \
  -H "Authorization: Bearer <access_token>" \
  -F "file=@/path/to/image.jpg"
```

## Tests
- Integration tests: `Tests/Chatty.BE.API.IntegrationTests` spin up the API with in-memory EF Core and exercise auth, conversations, and messaging controllers.
- Application service tests: `Tests/Chatty.BE.Application.Test` cover AuthService, ConversationService, MessageService, UserService behavior.
- Domain and infrastructure tests validate entities and repository behavior.
