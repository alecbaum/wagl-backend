#!/bin/bash

# Wagl Backend - Docker Logs Script

set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Default values
ENVIRONMENT="development"
SERVICE=""
FOLLOW=false
TAIL_LINES=100
TIMESTAMPS=false

# Function to display usage
usage() {
    echo "Usage: $0 [OPTIONS] [SERVICE]"
    echo "Options:"
    echo "  -e, --environment    Environment (development|production) [default: development]"
    echo "  -f, --follow         Follow log output"
    echo "  -n, --tail           Number of lines to show from end [default: 100]"
    echo "  -t, --timestamps     Show timestamps"
    echo "  -h, --help           Display this help message"
    echo ""
    echo "Services:"
    echo "  api                  Backend API service"
    echo "  postgres             PostgreSQL database (development only)"
    echo "  redis                Redis cache"
    echo "  pgadmin              pgAdmin tool (development only)"
    echo "  redis-commander      Redis Commander tool (development only)"
    echo ""
    echo "Examples:"
    echo "  $0                              # Show all service logs"
    echo "  $0 api                         # Show API logs only"
    echo "  $0 -f api                      # Follow API logs"
    echo "  $0 -n 50 -t postgres          # Show last 50 lines with timestamps"
}

# Parse command line arguments
while [[ $# -gt 0 ]]; do
    case $1 in
        -e|--environment)
            ENVIRONMENT="$2"
            shift 2
            ;;
        -f|--follow)
            FOLLOW=true
            shift
            ;;
        -n|--tail)
            TAIL_LINES="$2"
            shift 2
            ;;
        -t|--timestamps)
            TIMESTAMPS=true
            shift
            ;;
        -h|--help)
            usage
            exit 0
            ;;
        -*)
            echo -e "${RED}Unknown option: $1${NC}"
            usage
            exit 1
            ;;
        *)
            SERVICE="$1"
            shift
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

# Prepare docker-compose command
COMPOSE_CMD="docker-compose -f $COMPOSE_FILE --env-file $ENV_FILE"

# Build logs command arguments
LOGS_ARGS=""

if [[ "$FOLLOW" == true ]]; then
    LOGS_ARGS="$LOGS_ARGS -f"
fi

if [[ "$TIMESTAMPS" == true ]]; then
    LOGS_ARGS="$LOGS_ARGS -t"
fi

LOGS_ARGS="$LOGS_ARGS --tail=$TAIL_LINES"

# Print logs information
echo -e "${BLUE}=== Wagl Backend Docker Logs ===${NC}"
echo -e "${YELLOW}Environment:${NC} $ENVIRONMENT"
if [[ -n "$SERVICE" ]]; then
    echo -e "${YELLOW}Service:${NC} $SERVICE"
else
    echo -e "${YELLOW}Service:${NC} All services"
fi
echo -e "${YELLOW}Follow:${NC} $FOLLOW"
echo -e "${YELLOW}Tail Lines:${NC} $TAIL_LINES"
echo -e "${YELLOW}Timestamps:${NC} $TIMESTAMPS"
echo ""

# Check if services are running
RUNNING_SERVICES=$($COMPOSE_CMD ps --services --filter status=running 2>/dev/null || true)

if [[ -z "$RUNNING_SERVICES" ]]; then
    echo -e "${YELLOW}Warning: No services are currently running${NC}"
    echo -e "${BLUE}To start services, run: ./scripts/docker/run.sh${NC}"
    exit 0
fi

# Validate service if specified
if [[ -n "$SERVICE" ]]; then
    if ! echo "$RUNNING_SERVICES" | grep -q "^$SERVICE$"; then
        echo -e "${RED}Error: Service '$SERVICE' is not running${NC}"
        echo -e "${YELLOW}Running services:${NC}"
        echo "$RUNNING_SERVICES" | sed 's/^/  /'
        exit 1
    fi
fi

# Show logs
if [[ "$FOLLOW" == true ]]; then
    echo -e "${BLUE}Following logs... (Press Ctrl+C to stop)${NC}"
    echo ""
fi

$COMPOSE_CMD logs $LOGS_ARGS $SERVICE