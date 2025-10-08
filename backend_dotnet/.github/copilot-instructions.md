# Linqyard API - Copilot Instructions

## Architecture Overview
Linqyard is a .NET 9 Web API following a clean layered architecture:
- **Linqyard.Api**: Main web API project with controllers, middleware, configuration
- **Linqyard.Contracts**: Shared contracts (DTOs, responses) referenced by API layer

## Key Patterns & Conventions

### Controller Architecture
- All controllers inherit from `BaseApiController` which provides:
  - Standard header access (`RequestId`, `CorrelationId`, `BearerToken`, `UserId`)
  - RFC 7807 ProblemDetails helpers (`NotFoundProblem()`, `BadRequestProblem()`, etc.)
  - Response envelope helpers (`OkEnvelope<T>()`, `PagedOk<T>()`)
- Controllers use `[Route("")]` for root endpoints and standard HTTP verbs
- Always include `[ProducesResponseType]` attributes for proper OpenAPI documentation

### Response Patterns
- **Success responses**: Wrapped in `ApiResponse<T>` envelope via `OkEnvelope()` helper
- **Error responses**: Use ProblemDetails via `NotFoundProblem()`, `BadRequestProblem()`, etc.
- **Health endpoints**: Keep lightweight, avoid DB calls for liveness probes

### Logging & Observability
- **Serilog** is configured with structured logging and enrichment
- **Correlation ID**: Every request gets X-Correlation-Id header (auto-generated if missing)
- Log configuration in `appsettings.json` with console + file sinks
- Request logging automatically captures HTTP details, user context, and correlation IDs

### Middleware Pipeline Order
```csharp
app.UseSerilogRequestLogging(); // First for full request tracking
app.UseMiddleware<CorrelationIdMiddleware>(); // Ensure correlation ID exists
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
```

### Configuration Management
- Environment-specific settings: `appsettings.json`, `appsettings.Development.json`, `appsettings.Production.json`
- Serilog configuration is fully externalized to appsettings
- User secrets configured for development (`UserSecretsId` in .csproj)

## Development Commands

### Build & Run
```powershell
# Build solution
dotnet build

# Run API (Development profile)
dotnet run --project Linqyard.Api

# Run with specific profile
dotnet run --project Linqyard.Api --launch-profile https
```

### Docker
```powershell
# Build image
docker build -t Linqyard-api .

# Run container
docker run -p 8080:8080 -p 8081:8081 Linqyard-api
```

## Project Structure Conventions
- Controllers in `Controllers/` folder, inherit from `BaseApiController`
- Middleware in `Middleware/` folder with standard naming pattern
- Responses/DTOs in `Linqyard.Contracts` project for reusability
- Logs written to `logs/` directory with daily rolling

## Adding New Features
1. **New endpoints**: Create controller inheriting `BaseApiController`, use envelope helpers
2. **New contracts**: Add to `Linqyard.Contracts` project, use records for immutability  
3. **New middleware**: Follow `CorrelationIdMiddleware` pattern, register in `Program.cs`
4. **Error handling**: Use ProblemDetails helpers, ensure correlation IDs are included

## Key Files to Reference
- `BaseApiController.cs`: Base patterns for all controllers
- `CorrelationIdMiddleware.cs`: Middleware implementation example
- `Program.cs`: Startup configuration and middleware pipeline
- `appsettings.json`: Serilog and application configuration