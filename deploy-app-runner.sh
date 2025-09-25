#!/bin/bash

# App Runner deployment script - builds and triggers new deployment
# Usage: ./deploy-app-runner.sh [tag]

set -e

# Configuration
AWS_REGION="us-east-1"
SERVICE_ARN="arn:aws:apprunner:us-east-1:108367188859:service/wagl-backend-api/5984749a10a5464da789955c600899f9"

# Get tag or use timestamp
TAG=${1:-$(date +%Y%m%d%H%M%S)}
COMMIT_HASH=$(git rev-parse --short HEAD)

echo "🚀 App Runner deployment starting..."
echo "   Service: wagl-backend-api"
echo "   Tag: ${TAG}-${COMMIT_HASH}"

# Build Docker image locally for validation
echo "🔨 Building and validating Docker image..."
docker build -t wagl-backend:latest .

# Test the image locally (optional)
echo "🧪 Quick local test..."
if docker run --rm wagl-backend:latest dotnet --version > /dev/null 2>&1; then
    echo "✅ Image validation passed"
else
    echo "❌ Image validation failed"
    exit 1
fi

# Trigger App Runner deployment
echo "🔄 Triggering App Runner deployment..."
OPERATION_ID=$(aws apprunner start-deployment \
    --service-arn $SERVICE_ARN \
    --region $AWS_REGION \
    --query 'OperationId' \
    --output text)

echo "✅ App Runner deployment initiated!"
echo "   Operation ID: $OPERATION_ID"
echo "   Monitor status: aws apprunner describe-service --service-arn $SERVICE_ARN --query 'Service.Status'"
echo "   Service URL: https://v6uwnty3vi.us-east-1.awsapprunner.com"
echo "   Custom Domain: https://api.wagl.ai"

echo ""
echo "⏱️  App Runner deployments typically take 2-3 minutes"
echo "🔍 Check logs in AWS Console: App Runner → wagl-backend-api → Logs"