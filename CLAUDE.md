# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Repository Information

- **GitHub Repository**: https://github.com/alecbaum/wagl-backend
- **Owner**: nution101
- **Status**: Currently empty, ready for initial implementation

## Project Overview

This is a .NET Core 9 Web API project implementing a hybrid authentication system with tiered user access and provider API key authentication. The project follows the Atomic Design Pattern for clear architectural separation.

## Technology Stack

- **Framework**: .NET Core 9 Web API
- **Database**: PostgreSQL
- **Cache**: AWS ElastiCache Serverless (ValKey)
- **Authentication**: Hybrid (.NET Identity + Custom API Key)
- **Architecture**: Atomic Design Pattern

## Commands

Based on the project specifications, common .NET commands would be:

### Development
```bash
# Build the solution
dotnet build

# Run the API (typically in Api project)
dotnet run --project src/ProjectName.Api

# Run tests
dotnet test

# Restore packages
dotnet restore

# Watch for changes (development)
dotnet watch run --project src/ProjectName.Api

# Entity Framework migrations (when database layer is implemented)
dotnet ef migrations add InitialCreate --project src/ProjectName.Infrastructure
dotnet ef database update --project src/ProjectName.Infrastructure
```

### Code Quality
```bash
# Format code
dotnet format

# Analyze code
dotnet analyze
```

## Architecture: Atomic Design Pattern

The project follows a strict 5-layer Atomic Design hierarchy:

### 1. Atoms (src/ProjectName.Core/Atoms/)
- **Entities/**: Core domain entities (User, Provider, ApiUsageLog, TierFeature)
- **ValueObjects/**: Immutable value objects (ApiKey, TierLevel, Email, UserId)
- **Enums/**: System enumerations (AccountTier, AccountType, FeatureFlags)
- **Constants/**: Application constants (CacheKeys, PolicyNames, ClaimTypes, ErrorCodes)

### 2. Molecules (src/ProjectName.Core/Molecules/)
- **DTOs/Request/**: Input data transfer objects
- **DTOs/Response/**: Output data transfer objects
- **Interfaces/**: Basic contracts (IAuthenticatable, ICacheable, IAuditable, IRateLimited)
- **Configurations/**: Settings objects (JwtConfiguration, RedisConfiguration)
- **Exceptions/**: Custom exception types

### 3. Organisms (src/ProjectName.Domain/Organisms/)
- **Services/**: Complex business logic services
  - Authentication/: User and provider auth services, API key management, JWT handling
  - Authorization/: Tier-based authorization, permission management
  - Caching/: Redis caching services
  - RateLimiting/: Tier-based and provider rate limiting
  - Business/: Core business services
- **Repositories/**: Data access layer with base repository pattern
- **Handlers/**: Authentication handlers and command handlers
- **Validators/**: Input validation logic

### 4. Templates (src/ProjectName.Infrastructure/Templates/)
- **Controllers/**: API controllers with base controller inheritance
- **Middleware/**: Request pipeline middleware (auth, rate limiting, error handling, logging, usage tracking)
- **Filters/**: Action filters (authorization, validation, caching, audit)

### 5. Pages (src/ProjectName.Infrastructure/Pages/)
- **Features/**: Complete feature modules
  - UserManagement/: User-related endpoints and DI
  - ProviderManagement/: Provider-related endpoints and DI
  - Authentication/: Auth endpoints and configuration
  - Analytics/: Usage analytics and reporting
- **Extensions/**: Service registration and app configuration extensions

## Authentication Architecture

### Dual Authentication System
- **User Authentication**: .NET Identity with JWT tokens, tiered access (Tier1, Tier2, Tier3)
- **Provider Authentication**: Custom API key authentication with bearer tokens

### Authentication Flow
1. MultiAuth policy scheme automatically routes between JWT and API Key handlers
2. JWT tokens (longer, contain dots) → JwtAuthenticationHandler
3. API keys (shorter strings) → ApiKeyAuthenticationHandler
4. Both create ClaimsPrincipal with role-based claims

### Authorization Policies
- `Tier1Access`: Requires Tier1+ role
- `Tier2Access`: Requires Tier2+ role  
- `Tier3Access`: Requires Tier3 role
- `ProviderAccess`: Requires Provider role
- `Tier1OrProvider`: Allows either user tiers or providers

## Rate Limiting Strategy

Tier-based rate limiting with different limits per account type:
- **Tier1**: 100 requests/hour
- **Tier2**: 500 requests/hour
- **Tier3**: 2000 requests/hour
- **Provider**: 100000000 requests/hour

Rate limiting implemented via middleware with Redis backing for distributed scenarios.

## Key Design Patterns

### Repository Pattern
- Base repository with generic CRUD operations
- Specialized repositories inherit from BaseRepository
- Async/await throughout data access layer

### Service Layer Pattern
- Business logic encapsulated in service classes
- Services depend on repository abstractions
- Caching integrated at service level

### API Key Management
- ApiKey value object with secure generation and verification
- BCrypt hashing for stored API keys
- Cache-first validation with database fallback

## Database Design

### Entity Relationships
- User entity managed by .NET Identity
- Provider entity with owned ApiKey value object
- ApiUsageLog for tracking API calls
- TierFeature for feature flag management

### Value Objects
- ApiKey: Secure key generation and verification
- TierLevel: Tier-based access control
- Email: Email validation and formatting
- UserId: Strongly-typed user identification

## Caching Strategy

- Redis for distributed caching
- API key validation caching (5-minute TTL)
- User profile caching
- Rate limit counter storage

## Development Workflow

### Adding New Features
1. **Define Atoms**: Create entities, value objects, enums as needed
2. **Build Molecules**: Create request/response DTOs and interfaces
3. **Implement Organisms**: Build services, repositories, handlers, validators
4. **Setup Templates**: Create controllers, middleware, filters
5. **Compose Pages**: Wire everything together in feature modules

### Project Structure Guidelines
- Keep Atoms dependency-free
- Molecules should be simple combinations
- Business logic belongs in Organisms
- Templates handle HTTP concerns
- Pages compose complete features

### Testing Structure
- Unit tests organized by atomic design layers
- Test services and repositories in isolation
- Integration tests for complete feature flows
- Follow AAA pattern (Arrange, Act, Assert)

## Configuration

### Required Settings
- ConnectionStrings: PostgreSQL and Redis
- Authentication: JWT settings and API key configuration
- RateLimiting: Per-tier limits and window settings
- Redis: Cache configuration

### Environment-Specific Settings
- Development: Swagger UI, detailed logging
- Production: Security headers, minimal logging

## AWS ElastiCache Serverless Troubleshooting Rules

### Critical Requirements for AWS ElastiCache Serverless (ValKey/Redis)

**MANDATORY RULE**: AWS ElastiCache Serverless **REQUIRES** TLS encryption by default. This is non-negotiable and must be included in all connection strings.

#### Connection String Requirements

1. **TLS is Mandatory**: All ElastiCache Serverless connections must use `ssl=true`
2. **Dual Port Architecture**:
   - Port 6379: Write operations
   - Port 6380: Read operations (used by SignalR)
3. **AbortConnect=false**: Recommended for better connection resilience

**Correct Connection String Format**:
```
wagl-backend-cache-ggfeqp.serverless.use1.cache.amazonaws.com:6379,ssl=true,abortConnect=false
```

#### StackExchange.Redis Configuration

For .NET applications using StackExchange.Redis:
```csharp
var configurationOptions = new ConfigurationOptions
{
    EndPoints = { { "cache-endpoint.amazonaws.com", 6379 } },
    Ssl = true,
    AbortOnConnectFail = false // Required for serverless
};
```

#### SignalR Configuration

SignalR requires additional configuration for dual-port architecture. SignalR uses port 6380 for subscriptions and may need special handling.

### Troubleshooting Decision Tree

1. **Connection Timeouts**:
   - ✅ First check: Is `ssl=true` in connection string?
   - ✅ Second check: Is `abortConnect=false` set?
   - ❌ Don't assume network/security group issues first

2. **"UnableToConnect" Errors**:
   - ✅ Check if error mentions port 6380 (SignalR subscriptions)
   - ✅ Verify TLS certificate validation
   - ❌ Don't repeatedly try different security group configurations

3. **Health Check Failures**:
   - ✅ Verify TLS is enabled in health check configuration
   - ✅ Check if main Redis connection works but SignalR subscription fails
   - ❌ Don't assume the entire Redis connection is broken

### Key Lessons Learned

1. **Research First**: Always understand AWS service requirements before troubleshooting
2. **AWS Serverless = TLS Mandatory**: ElastiCache Serverless enforces TLS by default
3. **Read Error Messages Carefully**: Port numbers in errors provide critical clues
4. **Stop Repeating Failed Approaches**: When the same fix doesn't work, step back and research
5. **Trust Official Documentation**: AWS docs clearly state TLS requirements for Serverless
6. **Dual Port Awareness**: SignalR may fail on port 6380 even when main connection on 6379 works

### Entity Framework Troubleshooting Rules

#### LINQ Translation Errors

When encountering "Translation of method failed" errors:

1. **DateTime.Add() Operations**: Cannot be translated to SQL
   ```csharp
   // ❌ This will fail in LINQ queries
   .Where(x => x.StartedAt.Value.Add(x.Duration) < cutoffTime)

   // ✅ Use client-side evaluation instead
   var activeSessions = await Query.Where(x => x.Status == Status.Active).ToListAsync();
   var filtered = activeSessions.Where(x => x.StartedAt.Value.Add(x.Duration) < cutoffTime);
   ```

2. **Complex Calculations**: Move to client-side when EF Core cannot translate

#### PostgreSQL JSON Serialization

For JSONB columns with complex types:
```csharp
// ✅ Explicit JSON conversion required
builder.Property(p => p.AllowedIpAddresses)
    .HasColumnType("jsonb")
    .HasConversion(
        v => v == null ? null : JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
        v => v == null ? null : JsonSerializer.Deserialize<string[]>(v, (JsonSerializerOptions?)null));
```

## Multi-Agent System

This project uses a comprehensive multi-agent system for rapid development within 6-day sprint cycles:

### Core Orchestration Agents
- **master-orchestrator**: PROACTIVELY coordinates all complex, multi-step tasks and delegates to specialized agents
- **full-stack-builder**: Handles complete web application development coordinating frontend, backend, and deployment
- **project-orchestrator**: Manages project-specific workflows with custom rules and agent preferences
- **bug-fix-coordinator**: Specializes in debugging, testing, and validation workflows

### Engineering Agents
- **backend-architect**: Designs .NET Core APIs, database architecture, and server infrastructure
- **frontend-developer**: Builds UI components with access to patterns and component knowledge
- **rapid-prototyper**: Creates MVPs and proof-of-concepts for quick validation
- **ai-engineer**: Implements AI/ML features, language model integration, and intelligent automation
- **mobile-app-builder**: Develops native iOS/Android applications with platform-specific optimizations
- **devops-automator**: Sets up CI/CD pipelines, cloud infrastructure, and deployment automation
- **test-writer-fixer**: Writes comprehensive tests, analyzes failures, and maintains test integrity

### Specialized Domain Agents
- **design/**: ui-designer, ux-researcher, brand-guardian, visual-storyteller, whimsy-injector
- **product/**: feedback-synthesizer, sprint-prioritizer, trend-researcher  
- **marketing/**: app-store-optimizer, tiktok-strategist, content-creator
- **studio-operations/**: analytics-reporter, finance-tracker, infrastructure-maintainer, legal-compliance-checker, support-responder
- **testing/**: api-tester, performance-benchmarker, test-results-analyzer, workflow-optimizer, tool-evaluator

### Proactive Agent Activation
Agents automatically trigger based on task context:
- **Complex projects**: master-orchestrator coordinates multi-agent workflows
- **Code changes**: test-writer-fixer ensures comprehensive testing
- **UI/UX updates**: whimsy-injector adds delightful user experiences  
- **Feature flags**: experiment-tracker manages A/B testing and validation
- **Launch activities**: project-shipper coordinates releases and go-to-market

### Manual Agent Coordination
For explicit agent control use `@agent-name` or request specific agents:
- "Use @master-orchestrator to coordinate building the authentication system"
- "Have @backend-architect design the API structure first"
- "Get @rapid-prototyper to create a quick MVP for user testing"

Agents are configured for the .NET Core 9 Web API architecture and follow the Atomic Design Pattern organizational structure.

## Infrastructure Management

### AWS CLI Operations
- **Primary Tool**: Use AWS CLI exclusively for all infrastructure interactions
- **Automated Operations**: Perform AWS operations directly rather than asking user to do them manually
- **Exception**: Only ask user for manual operations when technically impossible via CLI
- **Resource Creation**: For significant changes or new AWS resources, request user approval before creation
- **Commands**: Execute AWS CLI commands to manage services, deployments, and configurations

### Infrastructure Guidelines
- Automate infrastructure tasks wherever possible
- Validate AWS credentials and permissions before operations
- Use appropriate AWS CLI profiles and regions
- Document infrastructure changes in commit messages

### Infrastructure Documentation
- **Complete Infrastructure Reference**: See `INFRASTRUCTURE.md` for comprehensive details of all AWS resources
- **Live Infrastructure Status**: All resources are documented with IDs, ARNs, endpoints, and configurations
- **Reference Requirements**: Always reference INFRASTRUCTURE.md when needing specific AWS resource information
- **Update Requirements**: When making infrastructure changes via AWS CLI, immediately update INFRASTRUCTURE.md with new resource details
- **Connection Strings**: Database and cache connection strings are documented in INFRASTRUCTURE.md
- **Monitoring**: CloudWatch dashboards and alarms are documented with specific metric configurations

**Key Infrastructure Endpoints (Always reference INFRASTRUCTURE.md for current values):**
- **Application URL**: `http://wagl-backend-alb-2094314021.us-east-1.elb.amazonaws.com`
- **Database Endpoint**: `wagl-backend-aurora.cluster-cexeows4418s.us-east-1.rds.amazonaws.com:5432`
- **Cache Endpoint**: `wagl-backend-cache-ggfeqp.serverless.use1.cache.amazonaws.com:6379` (TLS required)