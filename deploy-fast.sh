#!/bin/bash

# Fast deployment script - builds locally and pushes directly to ECR
# Usage: ./deploy-fast.sh [tag]

set -e

# Configuration
AWS_REGION="us-east-1"
AWS_ACCOUNT_ID="108367188859"
IMAGE_REPO_NAME="waglswarm-web2"
REPOSITORY_URI="$AWS_ACCOUNT_ID.dkr.ecr.$AWS_REGION.amazonaws.com/$IMAGE_REPO_NAME"

# Get tag or use timestamp
TAG=${1:-$(date +%Y%m%d%H%M%S)}
COMMIT_HASH=$(git rev-parse --short HEAD)
FULL_TAG="fast-${TAG}-${COMMIT_HASH}"

echo "🚀 Fast deployment starting..."
echo "   Repository: $REPOSITORY_URI"
echo "   Tag: $FULL_TAG"

# Login to ECR
echo "🔐 Logging into ECR..."
aws ecr get-login-password --region $AWS_REGION | docker login --username AWS --password-stdin $REPOSITORY_URI

# Pull latest image for caching
echo "📦 Pulling latest image for layer caching..."
docker pull $REPOSITORY_URI:latest 2>/dev/null || echo "No previous image found"

# Build with cache
echo "🔨 Building image with layer caching..."
docker build \
  --cache-from $REPOSITORY_URI:latest \
  -f Dockerfile.fast \
  -t $IMAGE_REPO_NAME:latest \
  -t $REPOSITORY_URI:latest \
  -t $REPOSITORY_URI:$FULL_TAG \
  .

# Push images in parallel for speed
echo "⬆️  Pushing images..."
docker push $REPOSITORY_URI:latest &
docker push $REPOSITORY_URI:$FULL_TAG &
wait

# Force ECS update
echo "🔄 Forcing ECS deployment..."
aws ecs update-service \
  --cluster wagl-backend-cluster \
  --service wagl-backend-service \
  --force-new-deployment \
  --no-cli-pager

echo "✅ Fast deployment initiated!"
echo "   Monitor status: aws ecs describe-services --cluster wagl-backend-cluster --services wagl-backend-service --query 'services[0].deployments[0].rolloutState'"
echo "   Logs: aws logs tail /ecs/wagl-backend --follow"