#!/bin/bash

# Wagl Frontend Demo Startup Script

echo "🎯 Starting Wagl Frontend Demo..."
echo ""

# Check if Docker is running
if ! docker info > /dev/null 2>&1; then
    echo "❌ Docker is not running. Please start Docker first."
    exit 1
fi

# Check if port 3000 is in use
if lsof -Pi :3000 -sTCP:LISTEN -t >/dev/null 2>&1; then
    echo "⚠️  Port 3000 is already in use."
    echo "   Stop the service using port 3000 or choose a different port."
    echo "   To find what's using port 3000: lsof -i :3000"
    exit 1
fi

# Change to the demo directory
cd "$(dirname "$0")"

echo "🐳 Building and starting Docker container..."
docker-compose up --build -d

# Wait a moment for the container to start
sleep 3

# Check if container is running
if docker-compose ps | grep -q "wagl-frontend-demo.*Up"; then
    echo ""
    echo "✅ Wagl Frontend Demo is running!"
    echo ""
    echo "🌐 Open your browser to: http://localhost:3000"
    echo ""
    echo "📋 Available commands:"
    echo "   • View logs:    docker-compose logs -f"
    echo "   • Stop demo:    docker-compose down"
    echo "   • Restart:      docker-compose restart"
    echo ""
    echo "🔧 API Endpoints (switchable in UI):"
    echo "   • Primary:      https://api.wagl.ai"
    echo "   • Direct:       https://v6uwnty3vi.us-east-1.awsapprunner.com"
    echo ""
    echo "🎮 Test Scenarios:"
    echo "   • Register/Login as admin user"
    echo "   • Create sessions and chat rooms"
    echo "   • Generate invite codes for anonymous users"
    echo "   • Test real-time messaging"
    echo "   • Monitor dashboard statistics"
    echo ""
else
    echo "❌ Failed to start the demo container"
    echo "   Check the logs: docker-compose logs"
    exit 1
fi