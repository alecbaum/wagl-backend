# Wagl Backend API

A .NET Core 8.0 Web API implementing a hybrid authentication system with tiered user access and provider API key authentication, following the Atomic Design Pattern.

## 🏗️ Architecture

This project follows the **Atomic Design Pattern** for clean architecture:

- **Atoms**: Core entities, value objects, enums, and constants
- **Molecules**: DTOs, interfaces, configurations, and exceptions  
- **Organisms**: Services, repositories, handlers, and validators
- **Templates**: Controllers, middleware, and filters
- **Pages**: Feature modules and service registration

## 🛠️ Technology Stack

- **Framework**: .NET Core 8.0 Web API
- **Database**: PostgreSQL (Digital Ocean)
- **Cache**: Redis
- **Authentication**: Hybrid (.NET Identity + Custom API Keys)
- **ORM**: Entity Framework Core 8.0
- **Documentation**: Swagger/OpenAPI

## 🚀 Quick Start

### Prerequisites

- .NET 8.0 SDK
- PostgreSQL database
- Redis (optional, falls back to in-memory cache)

### Local Development

1. **Clone and setup**:
   ```bash
   git clone <repository-url>
   cd Wagl-Backend
   cp .env.example .env.local
   ```

2. **Configure environment**:
   Edit `.env.local` with your local database settings:
   ```env
   DATABASE_HOST=localhost
   DATABASE_PORT=5432
   DATABASE_NAME=wagldb_dev
   DATABASE_USER=wagl_dev_user
   DATABASE_PASSWORD=dev_password
   ```

3. **Run the application**:
   ```bash
   ./scripts/deploy.sh
   ```

### Production Deployment

1. **Configure production environment**:
   ```bash
   cp .env.example .env.production
   # Edit .env.production with production values
   ```

2. **Deploy to production**:
   ```bash
   ENVIRONMENT=production ./scripts/deploy.sh
   ```

## 🔐 Authentication System

### Hybrid Authentication

The API supports two authentication methods:

#### 1. JWT Authentication (Users)
- **Tiers**: Tier1 (100 req/hr), Tier2 (500 req/hr), Tier3 (2000 req/hr)
- **Endpoints**: `/api/v1/auth/login`, `/api/v1/auth/register`
- **Header**: `Authorization: Bearer <jwt_token>`

#### 2. API Key Authentication (Providers)
- **Rate Limit**: 10,000 requests/hour
- **Header**: `Authorization: Bearer <api_key>`
- **Format**: `wagl_<32_character_key>`

### Demo Accounts

After seeding, the following demo accounts are available:

```
Tier1 User: tier1@wagl.com / Tier1Pass123!
Tier2 User: tier2@wagl.com / Tier2Pass123!  
Tier3 User: tier3@wagl.com / Tier3Pass123!
Admin User: admin@wagl.com / AdminPass123!
```

## 🏢 Production Configuration

### Digital Ocean Database
```env
DATABASE_HOST=telfin-db-do-user-17957093-0.d.db.ondigitalocean.com
DATABASE_PORT=25060
DATABASE_NAME=wagldb
DATABASE_USER=waglmin
DATABASE_SSL_MODE=require
```

### Environment Variables

See `.env.example` for all available configuration options.

## 📊 API Endpoints

### Authentication
- `POST /api/v1/auth/login` - User login
- `POST /api/v1/auth/register` - User registration  
- `POST /api/v1/auth/refresh` - Refresh JWT token
- `POST /api/v1/auth/logout` - User logout

### Health Checks
- `GET /health` - Overall health
- `GET /health/ready` - Readiness probe
- `GET /health/live` - Liveness probe

### Documentation
- `GET /` - Swagger UI (development only)
- `GET /swagger/v1/swagger.json` - OpenAPI specification

## 🔧 Development Commands

### Building
```bash
dotnet build                          # Build solution
dotnet run --project src/WaglBackend.Api  # Run API
dotnet test                          # Run tests
```

### Database
```bash
./scripts/create-migration.sh       # Create EF migration
dotnet ef database update \         # Apply migrations
  --project src/WaglBackend.Infrastructure \
  --startup-project src/WaglBackend.Api
```

### Code Quality
```bash
dotnet format                        # Format code
```

## 📁 Project Structure

```
src/
├── WaglBackend.Api/                # API entry point
├── WaglBackend.Core/               # Atoms & Molecules
│   ├── Atoms/                      # Entities, ValueObjects, Enums, Constants
│   └── Molecules/                  # DTOs, Interfaces, Configurations
├── WaglBackend.Domain/             # Organisms  
│   └── Organisms/                  # Services, Repositories, Handlers
├── WaglBackend.Infrastructure/     # Templates & Pages
│   ├── Templates/                  # Controllers, Middleware, Filters
│   ├── Pages/                      # Feature Modules, Extensions
│   ├── Services/                   # Service implementations
│   └── Persistence/                # Database context, configurations
└── tests/
    └── WaglBackend.Tests/          # Unit & integration tests
```

## 🎯 Features

### Tier-Based Access Control
- **Tier1**: Basic API access, standard support
- **Tier2**: Advanced features, analytics, webhooks  
- **Tier3**: Premium features, 24/7 support, custom integrations

### Rate Limiting
- Redis-backed distributed rate limiting
- Tier-specific limits with graceful degradation
- Real-time limit headers in responses

### Caching Strategy
- Redis for distributed caching
- API key validation caching (5-minute TTL)
- User profile caching with auto-refresh

### Security Features
- BCrypt password hashing
- Secure API key generation
- JWT token validation and refresh
- IP address whitelisting for providers

### Monitoring
- Health checks for PostgreSQL and Redis
- Comprehensive error handling and logging
- Usage analytics and audit trails

## 🚦 Status Codes

- `200` - Success
- `201` - Created
- `400` - Bad Request (validation errors)
- `401` - Unauthorized (invalid credentials)
- `403` - Forbidden (insufficient permissions)
- `429` - Too Many Requests (rate limited)
- `500` - Internal Server Error

## 🧪 Testing

### Unit Tests
```bash
dotnet test --filter Category=Unit
```

### Integration Tests
```bash
dotnet test --filter Category=Integration
```

### API Testing
Use the included Swagger UI or tools like Postman with the OpenAPI specification.

## 🐳 Docker Support

*Coming soon*

## 📝 Contributing

1. Follow the Atomic Design Pattern
2. Write tests for new features
3. Update documentation
4. Use conventional commit messages

## 📄 License

*To be determined*

## 🆘 Support

For issues and questions:
1. Check the logs in `/logs/` directory
2. Review health check endpoints
3. Verify environment configuration
4. Check database connectivity

---

**Built with ❤️ using .NET Core 8.0 and the Atomic Design Pattern**