#!/bin/bash

# Wagl Backend - Docker Build Script

set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Default values
ENVIRONMENT="development"
BUILD_CONFIG="Release"
TAG_SUFFIX=""
PUSH_TO_REGISTRY=false
REGISTRY_URL=""

# Function to display usage
usage() {
    echo "Usage: $0 [OPTIONS]"
    echo "Options:"
    echo "  -e, --environment    Environment (development|production) [default: development]"
    echo "  -c, --config         Build configuration (Debug|Release) [default: Release]"
    echo "  -t, --tag-suffix     Additional tag suffix"
    echo "  -p, --push           Push to container registry"
    echo "  -r, --registry       Container registry URL"
    echo "  -h, --help           Display this help message"
    echo ""
    echo "Examples:"
    echo "  $0                                    # Build for development"
    echo "  $0 -e production                     # Build for production"
    echo "  $0 -e production -t v1.0.0 -p       # Build, tag and push to registry"
}

# Parse command line arguments
while [[ $# -gt 0 ]]; do
    case $1 in
        -e|--environment)
            ENVIRONMENT="$2"
            shift 2
            ;;
        -c|--config)
            BUILD_CONFIG="$2"
            shift 2
            ;;
        -t|--tag-suffix)
            TAG_SUFFIX="$2"
            shift 2
            ;;
        -p|--push)
            PUSH_TO_REGISTRY=true
            shift
            ;;
        -r|--registry)
            REGISTRY_URL="$2"
            shift 2
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

# Set image name and tag
IMAGE_NAME="wagl-backend-api"
IMAGE_TAG="${ENVIRONMENT}"

if [[ -n "$TAG_SUFFIX" ]]; then
    IMAGE_TAG="${IMAGE_TAG}-${TAG_SUFFIX}"
fi

if [[ -n "$REGISTRY_URL" ]]; then
    FULL_IMAGE_NAME="${REGISTRY_URL}/${IMAGE_NAME}:${IMAGE_TAG}"
else
    FULL_IMAGE_NAME="${IMAGE_NAME}:${IMAGE_TAG}"
fi

# Print build information
echo -e "${BLUE}=== Wagl Backend Docker Build ===${NC}"
echo -e "${YELLOW}Environment:${NC} $ENVIRONMENT"
echo -e "${YELLOW}Build Config:${NC} $BUILD_CONFIG"
echo -e "${YELLOW}Image Name:${NC} $FULL_IMAGE_NAME"
echo -e "${YELLOW}Registry Push:${NC} $PUSH_TO_REGISTRY"
echo ""

# Check if Docker is running
if ! docker info >/dev/null 2>&1; then
    echo -e "${RED}Error: Docker is not running${NC}"
    exit 1
fi

# Build the Docker image
echo -e "${BLUE}Building Docker image...${NC}"
docker build \
    --build-arg BUILD_CONFIGURATION="$BUILD_CONFIG" \
    --tag "$FULL_IMAGE_NAME" \
    --file src/WaglBackend.Api/Dockerfile \
    .

if [[ $? -eq 0 ]]; then
    echo -e "${GREEN}✓ Docker image built successfully${NC}"
else
    echo -e "${RED}✗ Docker image build failed${NC}"
    exit 1
fi

# Tag additional versions
if [[ "$ENVIRONMENT" == "production" && -n "$TAG_SUFFIX" ]]; then
    # Also tag as latest for production
    LATEST_TAG="${REGISTRY_URL:+$REGISTRY_URL/}${IMAGE_NAME}:latest"
    docker tag "$FULL_IMAGE_NAME" "$LATEST_TAG"
    echo -e "${GREEN}✓ Tagged as latest: $LATEST_TAG${NC}"
fi

# Push to registry if requested
if [[ "$PUSH_TO_REGISTRY" == true ]]; then
    if [[ -z "$REGISTRY_URL" ]]; then
        echo -e "${RED}Error: Registry URL is required for pushing${NC}"
        exit 1
    fi
    
    echo -e "${BLUE}Pushing to container registry...${NC}"
    docker push "$FULL_IMAGE_NAME"
    
    if [[ $? -eq 0 ]]; then
        echo -e "${GREEN}✓ Image pushed successfully${NC}"
        
        # Push latest tag for production
        if [[ "$ENVIRONMENT" == "production" && -n "$TAG_SUFFIX" ]]; then
            docker push "$LATEST_TAG"
            echo -e "${GREEN}✓ Latest tag pushed successfully${NC}"
        fi
    else
        echo -e "${RED}✗ Image push failed${NC}"
        exit 1
    fi
fi

# Clean up dangling images
echo -e "${BLUE}Cleaning up dangling images...${NC}"
docker image prune -f >/dev/null 2>&1

echo -e "${GREEN}=== Build completed successfully ===${NC}"
echo -e "${YELLOW}Image:${NC} $FULL_IMAGE_NAME"

# Display image size
IMAGE_SIZE=$(docker images --format "table {{.Repository}}:{{.Tag}}\t{{.Size}}" | grep "$IMAGE_NAME:$IMAGE_TAG" | awk '{print $2}')
echo -e "${YELLOW}Size:${NC} $IMAGE_SIZE"