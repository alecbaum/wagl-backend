# Wagl Backend - Docker Guide

This guide explains how to run the Wagl Backend using Docker containers for both development and production environments.

## 🏗️ Architecture Overview

The dockerized solution includes:

- **API Service**: .NET 9 Web API with hybrid authentication
- **PostgreSQL**: Database service (development) or external Digital Ocean (production)
- **Redis**: Caching service
- **Development Tools** (optional):
  - pgAdmin: Database management
  - Redis Commander: Redis management

## 🚀 Quick Start

### Development Environment

```bash
# Start all services in background
./scripts/docker/run.sh -d

# Start with development tools (pgAdmin, Redis Commander)
./scripts/docker/run.sh -d -t

# Build and start with clean volumes
./scripts/docker/run.sh -c -b
```

### Production Environment

```bash
# Start production environment
./scripts/docker/run.sh -e production -d

# Build and start production with external Redis
./scripts/docker/run.sh -e production -b -d
```

## 📋 Available Scripts

### Build Script
```bash
./scripts/docker/build.sh [OPTIONS]

Options:
  -e, --environment    Environment (development|production)
  -c, --config         Build configuration (Debug|Release)
  -t, --tag-suffix     Additional tag suffix
  -p, --push           Push to container registry
  -r, --registry       Container registry URL
```

### Run Script
```bash
./scripts/docker/run.sh [OPTIONS]

Options:
  -e, --environment    Environment (development|production)
  -d, --detach         Run in detached mode
  -t, --tools          Include development tools
  -c, --clean          Clean start (remove volumes)
  -b, --build          Build images before running
```

### Stop Script
```bash
./scripts/docker/stop.sh [OPTIONS]

Options:
  -e, --environment    Environment (development|production)
  -v, --volumes        Remove volumes (deletes data)
  -i, --images         Remove images
  -f, --force          Force stop (kill containers)
```

### Logs Script
```bash
./scripts/docker/logs.sh [OPTIONS] [SERVICE]

Options:
  -e, --environment    Environment (development|production)
  -f, --follow         Follow log output
  -n, --tail           Number of lines to show
  -t, --timestamps     Show timestamps

Services: api, postgres, redis, pgadmin, redis-commander
```

## 🌍 Environment Configuration

### Development (.env.docker)
- Local PostgreSQL container
- Local Redis container
- Development JWT secrets
- Debug logging enabled
- Optional development tools

### Production (.env.docker.production)
- External Digital Ocean PostgreSQL
- External/Local Redis
- Production-grade JWT secrets
- Optimized logging
- No development tools

## 🔌 Service URLs

### Development
- **API**: http://localhost:8080
- **Swagger UI**: http://localhost:8080
- **Health Check**: http://localhost:8080/health
- **pgAdmin**: http://localhost:5050 (admin@wagl.com / admin123)
- **Redis Commander**: http://localhost:8081

### Production
- **API**: http://localhost:80
- **Health Check**: http://localhost/health

## 📊 Health Checks

All services include health checks:

```bash
# Check API health
curl http://localhost:8080/health

# Check service status
docker-compose ps
```

## 💾 Data Persistence

### Development Volumes
- `postgres_data`: PostgreSQL data
- `redis_data`: Redis data
- `./logs`: Application logs

### Production Volumes
- `redis_prod_data`: Redis data (if using local Redis)
- `./logs`: Application logs

## 🔧 Common Operations

### View Running Services
```bash
docker-compose ps
```

### Scale API Service
```bash
docker-compose up -d --scale api=3
```

### Backup Database (Development)
```bash
docker-compose exec postgres pg_dump -U waglmin wagldb > backup.sql
```

### Access Container Shell
```bash
# API container
docker-compose exec api bash

# PostgreSQL container
docker-compose exec postgres psql -U waglmin wagldb
```

### Monitor Resources
```bash
docker stats
```

## 🐛 Troubleshooting

### Port Conflicts
If ports are already in use:
```bash
# Check what's using the port
lsof -i :8080

# Stop conflicting services or change ports in docker-compose.yml
```

### Database Connection Issues
```bash
# Check PostgreSQL logs
./scripts/docker/logs.sh postgres

# Verify connection string in environment files
```

### Redis Connection Issues
```bash
# Check Redis logs
./scripts/docker/logs.sh redis

# Test Redis connectivity
docker-compose exec redis redis-cli ping
```

### Build Issues
```bash
# Clean everything and rebuild
./scripts/docker/stop.sh -v -i
./scripts/docker/run.sh -c -b
```

### Memory Issues
```bash
# Check resource usage
docker stats

# Adjust memory limits in docker-compose files
# Restart Docker Desktop if needed
```

## 🔒 Security Notes

### Development
- Uses default passwords (change for production)
- Exposes management tools
- Debug logging enabled

### Production
- Uses environment variables for secrets
- No debug tools exposed
- Optimized resource limits
- SSL required for database connections

## 📈 Production Deployment

### Prerequisites
1. Docker and docker-compose installed
2. Environment variables configured
3. External services accessible (Database, Redis)

### Deployment Steps
```bash
# 1. Pull latest code
git pull origin main

# 2. Build production image
./scripts/docker/build.sh -e production -t $(git rev-parse --short HEAD)

# 3. Start production services
./scripts/docker/run.sh -e production -d

# 4. Verify deployment
curl http://localhost/health
```

### Zero-Downtime Updates
```bash
# Build new image
./scripts/docker/build.sh -e production -t v2.0.0

# Update image tag in docker-compose.production.yml
# Rolling restart
docker-compose -f docker-compose.production.yml up -d --no-deps api
```

## 📝 Development Workflow

### Local Development
```bash
# Start development environment
./scripts/docker/run.sh -t -d

# Make code changes
# Rebuild and restart
./scripts/docker/build.sh
docker-compose restart api

# View logs
./scripts/docker/logs.sh -f api
```

### Testing Changes
```bash
# Run with clean state
./scripts/docker/run.sh -c -b

# Run tests
docker-compose exec api dotnet test

# Check health
curl http://localhost:8080/health
```

## 🔗 Integration with CI/CD

### GitHub Actions Example
```yaml
- name: Build Docker Image
  run: ./scripts/docker/build.sh -e production -t ${{ github.sha }}

- name: Push to Registry
  run: ./scripts/docker/build.sh -e production -t ${{ github.sha }} -p -r your-registry.com
```

### Deployment Pipeline
1. Code push triggers build
2. Tests run in containers
3. Production image built and tagged
4. Health checks validate deployment
5. Traffic switched to new containers

This Docker setup provides a complete containerized development and production environment for the Wagl Backend API.