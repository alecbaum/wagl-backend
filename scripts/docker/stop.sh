#!/bin/bash

# Wagl Backend - Docker Stop Script

set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Default values
ENVIRONMENT="development"
REMOVE_VOLUMES=false
REMOVE_IMAGES=false
FORCE_STOP=false

# Function to display usage
usage() {
    echo "Usage: $0 [OPTIONS]"
    echo "Options:"
    echo "  -e, --environment    Environment (development|production) [default: development]"
    echo "  -v, --volumes        Remove volumes (will delete all data)"
    echo "  -i, --images         Remove images after stopping"
    echo "  -f, --force          Force stop (kill containers)"
    echo "  -h, --help           Display this help message"
    echo ""
    echo "Examples:"
    echo "  $0                              # Stop development environment"
    echo "  $0 -e production               # Stop production environment"
    echo "  $0 -v                          # Stop and remove volumes"
    echo "  $0 -f -v -i                    # Force stop, remove volumes and images"
}

# Parse command line arguments
while [[ $# -gt 0 ]]; do
    case $1 in
        -e|--environment)
            ENVIRONMENT="$2"
            shift 2
            ;;
        -v|--volumes)
            REMOVE_VOLUMES=true
            shift
            ;;
        -i|--images)
            REMOVE_IMAGES=true
            shift
            ;;
        -f|--force)
            FORCE_STOP=true
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

# Print stop information
echo -e "${BLUE}=== Wagl Backend Docker Stop ===${NC}"
echo -e "${YELLOW}Environment:${NC} $ENVIRONMENT"
echo -e "${YELLOW}Compose File:${NC} $COMPOSE_FILE"
echo -e "${YELLOW}Remove Volumes:${NC} $REMOVE_VOLUMES"
echo -e "${YELLOW}Remove Images:${NC} $REMOVE_IMAGES"
echo -e "${YELLOW}Force Stop:${NC} $FORCE_STOP"
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

# Prepare docker-compose command
COMPOSE_CMD="docker-compose -f $COMPOSE_FILE --env-file $ENV_FILE"

# Stop services
echo -e "${BLUE}Stopping services...${NC}"

if [[ "$FORCE_STOP" == true ]]; then
    $COMPOSE_CMD kill
    echo -e "${GREEN}✓ Services killed${NC}"
else
    $COMPOSE_CMD stop
    echo -e "${GREEN}✓ Services stopped${NC}"
fi

# Remove containers and optionally volumes
REMOVE_ARGS="--remove-orphans"
if [[ "$REMOVE_VOLUMES" == true ]]; then
    REMOVE_ARGS="$REMOVE_ARGS -v"
    echo -e "${YELLOW}Warning: This will remove all data volumes${NC}"
    read -p "Are you sure you want to continue? (y/N): " -r
    if [[ ! $REPLY =~ ^[Yy]$ ]]; then
        echo -e "${BLUE}Cancelled${NC}"
        exit 0
    fi
fi

$COMPOSE_CMD down $REMOVE_ARGS
echo -e "${GREEN}✓ Containers removed${NC}"

if [[ "$REMOVE_VOLUMES" == true ]]; then
    echo -e "${GREEN}✓ Volumes removed${NC}"
fi

# Remove images if requested
if [[ "$REMOVE_IMAGES" == true ]]; then
    echo -e "${BLUE}Removing images...${NC}"
    
    # Get image names from compose file
    IMAGES=$(docker-compose -f "$COMPOSE_FILE" config --services | while read service; do
        docker-compose -f "$COMPOSE_FILE" images -q "$service"
    done | sort | uniq)
    
    if [[ -n "$IMAGES" ]]; then
        echo "$IMAGES" | xargs docker rmi -f 2>/dev/null || true
        echo -e "${GREEN}✓ Images removed${NC}"
    fi
fi

# Clean up dangling resources
echo -e "${BLUE}Cleaning up dangling resources...${NC}"
docker system prune -f >/dev/null 2>&1
echo -e "${GREEN}✓ Cleanup completed${NC}"

echo -e "${GREEN}=== Stop completed successfully ===${NC}"