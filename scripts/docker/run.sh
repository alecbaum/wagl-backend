#!/bin/bash

# Wagl Backend - Docker Run Script

set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Default values
ENVIRONMENT="development"
DETACH=false
INCLUDE_TOOLS=false
CLEAN_START=false
BUILD_FIRST=false

# Function to display usage
usage() {
    echo "Usage: $0 [OPTIONS]"
    echo "Options:"
    echo "  -e, --environment    Environment (development|production) [default: development]"
    echo "  -d, --detach         Run in detached mode"
    echo "  -t, --tools          Include development tools (pgAdmin, Redis Commander)"
    echo "  -c, --clean          Clean start (remove volumes and containers)"
    echo "  -b, --build          Build images before running"
    echo "  -h, --help           Display this help message"
    echo ""
    echo "Examples:"
    echo "  $0                              # Run development environment"
    echo "  $0 -d                          # Run in background"
    echo "  $0 -e production               # Run production environment"
    echo "  $0 -t -d                       # Run with tools in background"
    echo "  $0 -c -b                       # Clean start with fresh build"
}

# Parse command line arguments
while [[ $# -gt 0 ]]; do
    case $1 in
        -e|--environment)
            ENVIRONMENT="$2"
            shift 2
            ;;
        -d|--detach)
            DETACH=true
            shift
            ;;
        -t|--tools)
            INCLUDE_TOOLS=true
            shift
            ;;
        -c|--clean)
            CLEAN_START=true
            shift
            ;;
        -b|--build)
            BUILD_FIRST=true
            shift
            ;;
        -h|--help)
            usage
            exit 0
            ;;
        *)
            echo -e "${RED}Unknown option: $1${NC}"
            usage
            exit 1
            ;;
    esac
done

# Validate environment
if [[ "$ENVIRONMENT" != "development" && "$ENVIRONMENT" != "production" ]]; then
    echo -e "${RED}Error: Environment must be 'development' or 'production'${NC}"
    exit 1
fi

# Set docker-compose file based on environment
if [[ "$ENVIRONMENT" == "production" ]]; then
    COMPOSE_FILE="docker-compose.production.yml"
    ENV_FILE=".env.docker.production"
else
    COMPOSE_FILE="docker-compose.yml"
    ENV_FILE=".env.docker"
fi

# Print run information
echo -e "${BLUE}=== Wagl Backend Docker Run ===${NC}"
echo -e "${YELLOW}Environment:${NC} $ENVIRONMENT"
echo -e "${YELLOW}Compose File:${NC} $COMPOSE_FILE"
echo -e "${YELLOW}Env File:${NC} $ENV_FILE"
echo -e "${YELLOW}Detached Mode:${NC} $DETACH"
echo -e "${YELLOW}Include Tools:${NC} $INCLUDE_TOOLS"
echo -e "${YELLOW}Clean Start:${NC} $CLEAN_START"
echo ""

# Check if Docker is running
if ! docker info >/dev/null 2>&1; then
    echo -e "${RED}Error: Docker is not running${NC}"
    exit 1
fi

# Check if compose file exists
if [[ ! -f "$COMPOSE_FILE" ]]; then
    echo -e "${RED}Error: Docker compose file '$COMPOSE_FILE' not found${NC}"
    exit 1
fi

# Clean start if requested
if [[ "$CLEAN_START" == true ]]; then
    echo -e "${BLUE}Cleaning up existing containers and volumes...${NC}"
    docker-compose -f "$COMPOSE_FILE" --env-file "$ENV_FILE" down -v --remove-orphans 2>/dev/null || true
    docker system prune -f >/dev/null 2>&1
    echo -e "${GREEN}✓ Cleanup completed${NC}"
fi

# Build images if requested
if [[ "$BUILD_FIRST" == true ]]; then
    echo -e "${BLUE}Building images...${NC}"
    docker-compose -f "$COMPOSE_FILE" --env-file "$ENV_FILE" build
    echo -e "${GREEN}✓ Images built successfully${NC}"
fi

# Prepare docker-compose command
COMPOSE_CMD="docker-compose -f $COMPOSE_FILE --env-file $ENV_FILE"

# Add profiles for tools if requested
PROFILES=""
if [[ "$INCLUDE_TOOLS" == true && "$ENVIRONMENT" != "production" ]]; then
    PROFILES="--profile tools"
fi

# Add Redis profile for production if using local Redis
if [[ "$ENVIRONMENT" == "production" ]]; then
    PROFILES="$PROFILES --profile with-redis"
fi

# Run the services
echo -e "${BLUE}Starting services...${NC}"
if [[ "$DETACH" == true ]]; then
    $COMPOSE_CMD up -d $PROFILES
    
    if [[ $? -eq 0 ]]; then
        echo -e "${GREEN}✓ Services started successfully in detached mode${NC}"
        echo ""
        echo -e "${YELLOW}Service URLs:${NC}"
        echo -e "  API: http://localhost:8080"
        echo -e "  Health Check: http://localhost:8080/health"
        echo -e "  Swagger UI: http://localhost:8080"
        
        if [[ "$INCLUDE_TOOLS" == true && "$ENVIRONMENT" != "production" ]]; then
            echo -e "  pgAdmin: http://localhost:5050"
            echo -e "  Redis Commander: http://localhost:8081"
        fi
        
        echo ""
        echo -e "${BLUE}To view logs:${NC} docker-compose -f $COMPOSE_FILE logs -f"
        echo -e "${BLUE}To stop services:${NC} docker-compose -f $COMPOSE_FILE down"
    else
        echo -e "${RED}✗ Failed to start services${NC}"
        exit 1
    fi
else
    $COMPOSE_CMD up $PROFILES
fi