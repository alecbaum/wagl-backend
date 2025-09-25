# Wagl Backend Infrastructure Documentation

This document contains comprehensive information about all AWS infrastructure components for the Wagl Backend project.

## 📋 Infrastructure Overview

**AWS Account**: 108367188859
**Region**: us-east-1 (US East - N. Virginia)
**Project**: wagl-backend
**Environment**: Production

## 🌐 Networking Infrastructure

### VPC Configuration
- **VPC ID**: `vpc-037729e4703616c6e`
- **Name**: wagl-backend-vpc
- **CIDR Block**: 10.0.0.0/16
- **DNS Hostnames**: Enabled
- **DNS Resolution**: Enabled

### Internet Gateway
- **IGW ID**: `igw-0565d47a409f2260e`
- **Name**: wagl-backend-igw
- **Attached to VPC**: vpc-037729e4703616c6e

### Subnets

#### Public Subnets
| Name | Subnet ID | AZ | CIDR | Route Table |
|------|-----------|----|----|-------------|
| wagl-backend-public-1a | `subnet-096357f99cfbd9066` | us-east-1a | 10.0.1.0/24 | rtb-0dc0a411a2cb92840 |
| wagl-backend-public-1b | `subnet-09e4045416a2dfe89` | us-east-1b | 10.0.2.0/24 | rtb-0dc0a411a2cb92840 |

#### Private Subnets
| Name | Subnet ID | AZ | CIDR | Route Table |
|------|-----------|----|----|-------------|
| wagl-backend-private-1a | `subnet-00f9ba3449886f8f3` | us-east-1a | 10.0.3.0/24 | rtb-08797fbeff2cb1c5d |
| wagl-backend-private-1b | `subnet-02963cc40eb5866a3` | us-east-1b | 10.0.4.0/24 | rtb-08797fbeff2cb1c5d |

### NAT Gateway
- **NAT Gateway ID**: `nat-0a8643be527bd5e7d`
- **Name**: wagl-backend-nat
- **Elastic IP**: `34.235.175.22` (eipalloc-0d789d3d27abea30b)
- **Subnet**: subnet-096357f99cfbd9066 (public-1a)
- **State**: available

### Route Tables
- **Public Route Table ID**: `rtb-0dc0a411a2cb92840`
- **Name**: wagl-backend-public-rt
- **Routes**:
  - 10.0.0.0/16 → local
  - 0.0.0.0/0 → igw-0565d47a409f2260e

- **Private Route Table ID**: `rtb-08797fbeff2cb1c5d`
- **Name**: wagl-backend-private-rt
- **Routes**:
  - 10.0.0.0/16 → local
  - 0.0.0.0/0 → nat-0a8643be527bd5e7d

### Security Groups

#### Application Load Balancer Security Group
- **Security Group ID**: `sg-0cf4fe6a0ef17bcd3`
- **Name**: wagl-backend-alb-sg
- **VPC**: vpc-037729e4703616c6e
- **Inbound Rules**:
  - Port 80 (HTTP) from 0.0.0.0/0
  - Port 443 (HTTPS) from 0.0.0.0/0
- **Outbound Rules**: All traffic allowed

#### ECS Fargate Security Group
- **Security Group ID**: `sg-0ed7714fe63508850`
- **Name**: wagl-backend-ecs-sg
- **VPC**: vpc-037729e4703616c6e
- **Inbound Rules**:
  - Port 80 from sg-0cf4fe6a0ef17bcd3 (ALB)
- **Outbound Rules**: All traffic allowed

#### Database/Cache Security Group
- **Security Group ID**: `sg-09f0cb6c27a3e83f9`
- **Name**: wagl-backend-db-sg
- **VPC**: vpc-037729e4703616c6e
- **Inbound Rules**:
  - Port 5432 (PostgreSQL) from sg-0ed7714fe63508850 (ECS)
  - Port 6379 (Redis) from sg-0ed7714fe63508850 (ECS)
  - Port 5432 (PostgreSQL) from 47.190.70.197/32 (Office IP)
  - Port 6379 (Redis) from 47.190.70.197/32 (Office IP)
- **Outbound Rules**: All traffic allowed

## ⚖️ Load Balancing

### Application Load Balancer
- **ARN**: `arn:aws:elasticloadbalancing:us-east-1:108367188859:loadbalancer/app/wagl-backend-alb/3124c2064a300bb9`
- **Name**: wagl-backend-alb
- **DNS Name**: `wagl-backend-alb-2094314021.us-east-1.elb.amazonaws.com`
- **Hosted Zone ID**: Z35SXDOTRQ7X7K
- **Scheme**: Internet-facing
- **Type**: Application
- **Subnets**: subnet-096357f99cfbd9066, subnet-09e4045416a2dfe89
- **Security Groups**: sg-0cf4fe6a0ef17bcd3

### Target Group
- **ARN**: `arn:aws:elasticloadbalancing:us-east-1:108367188859:targetgroup/wagl-backend-tg/8c49a7cb03cc12c4`
- **Name**: wagl-backend-tg
- **Protocol**: HTTP
- **Port**: 80
- **Target Type**: IP
- **Health Check Path**: /health
- **Health Check Interval**: 30 seconds
- **Health Check Timeout**: 5 seconds
- **Healthy Threshold**: 2

### Listener
- **ARN**: `arn:aws:elasticloadbalancing:us-east-1:108367188859:listener/app/wagl-backend-alb/3124c2064a300bb9/56104950a30bc926`
- **Port**: 80
- **Protocol**: HTTP
- **Default Action**: Forward to wagl-backend-tg

## 🚀 Container Platform (AWS App Runner)

### App Runner Service
- **ARN**: `arn:aws:apprunner:us-east-1:108367188859:service/wagl-backend-api/5984749a10a5464da789955c600899f9`
- **Name**: wagl-backend-api
- **Service ID**: 5984749a10a5464da789955c600899f9
- **Status**: RUNNING
- **Service URL**: `v6uwnty3vi.us-east-1.awsapprunner.com`
- **Custom Domain**: `api.wagl.ai` (Associated)
- **Platform**: Managed Platform
- **Runtime**: Docker

### App Runner Configuration
- **CPU**: 0.25 vCPU
- **Memory**: 0.5 GB
- **Port**: 80
- **Environment Variables**:
  - Database connection: Aurora PostgreSQL
  - Redis connection: ElastiCache Serverless with TLS
  - JWT configuration: Production keys
  - SignalR: Dual-port Redis backplane
- **Health Check**: /health endpoint
- **Auto Scaling**: Automatic (App Runner managed)

### VPC Connector Configuration
- **VPC Connector**: Configured for database and cache access
- **Subnets**: subnet-00f9ba3449886f8f3, subnet-02963cc40eb5866a3 (Private)
- **Security Groups**: sg-09f0cb6c27a3e83f9 (Database/Cache access)
- **Outbound Traffic**: VPC only (for database/cache)

### Key Migration Benefits
- **Simplified Deployment**: No task definitions or clusters to manage
- **Automatic Scaling**: Built-in horizontal and vertical scaling
- **Zero Downtime**: Automatic blue-green deployments
- **Cost Optimization**: Pay-per-use model vs. always-running Fargate
- **SSL/TLS**: Automatic certificate management
- **Faster Deployments**: ~2 minutes vs 3-5 minutes with ECS

## 🏺 Legacy Infrastructure (Removed)

### Former ECS Setup (Migrated to App Runner - 2025-09-24)
- **ECS Cluster**: wagl-backend-cluster (Removed)
- **ECS Service**: wagl-backend-service (Deleted 2025-09-24)
- **Task Definition**: wagl-backend-api:1-5 (Legacy)
- **Application Load Balancer**: Still exists but unused
- **Target Groups**: No longer routing traffic
- **Auto Scaling**: Replaced with App Runner auto-scaling

## 🏪 Container Registry (ECR)

### ECR Repository
- **Repository ARN**: `arn:aws:ecr:us-east-1:108367188859:repository/waglswarm-web2`
- **Repository Name**: waglswarm-web2
- **Repository URI**: `108367188859.dkr.ecr.us-east-1.amazonaws.com/waglswarm-web2`
- **Registry ID**: 108367188859
- **Image Tag Mutability**: MUTABLE
- **Image Scanning**: Disabled
- **Encryption**: AES256

## 🗄️ Database (Aurora Serverless v2)

### Aurora Cluster
- **ARN**: `arn:aws:rds:us-east-1:108367188859:cluster:wagl-backend-aurora`
- **Cluster Identifier**: wagl-backend-aurora
- **Engine**: aurora-postgresql
- **Engine Version**: 15.8
- **Master Username**: postgres
- **Endpoint**: `wagl-backend-aurora.cluster-cexeows4418s.us-east-1.rds.amazonaws.com`
- **Reader Endpoint**: `wagl-backend-aurora.cluster-ro-cexeows4418s.us-east-1.rds.amazonaws.com`
- **Port**: 5432
- **VPC Security Groups**: sg-09f0cb6c27a3e83f9
- **DB Subnet Group**: wagl-backend-db-subnet-group
- **Hosted Zone ID**: Z2R2ITUGPM61AM

### Serverless v2 Scaling Configuration
- **Min Capacity**: 0.5 ACU
- **Max Capacity**: 16 ACU
- **Platform Version**: 3

### Aurora Instance
- **ARN**: `arn:aws:rds:us-east-1:108367188859:db:wagl-backend-aurora-instance`
- **Instance Identifier**: wagl-backend-aurora-instance
- **Instance Class**: db.serverless
- **Engine**: aurora-postgresql
- **Cluster**: wagl-backend-aurora

### DB Subnet Group
- **ARN**: `arn:aws:rds:us-east-1:108367188859:subgrp:wagl-backend-db-subnet-group`
- **Name**: wagl-backend-db-subnet-group
- **VPC**: vpc-037729e4703616c6e
- **Subnets**: subnet-00f9ba3449886f8f3, subnet-02963cc40eb5866a3

## 🚀 Cache (ElastiCache Serverless)

### ElastiCache Serverless Cache
- **ARN**: `arn:aws:elasticache:us-east-1:108367188859:serverlesscache:wagl-backend-cache`
- **Name**: wagl-backend-cache
- **Engine**: valkey
- **Major Engine Version**: 8
- **Endpoint**: `wagl-backend-cache-ggfeqp.serverless.use1.cache.amazonaws.com:6379`
- **Security Groups**: sg-09f0cb6c27a3e83f9
- **Subnets**: subnet-00f9ba3449886f8f3, subnet-02963cc40eb5866a3
- **TLS Encryption**: MANDATORY (ElastiCache Serverless requirement)
- **Connection Status**: Healthy (TLS properly configured)
- **SignalR Backplane**: Configured with TLS support

### Cache Usage Limits
- **Data Storage**: 1 GB Maximum
- **ECPU Per Second**: 5000 Maximum
- **Snapshot Retention**: 0 days
- **Daily Snapshot Time**: 05:30 UTC

### Cache Subnet Group
- **ARN**: `arn:aws:elasticache:us-east-1:108367188859:subnetgroup:wagl-backend-cache-subnet-group`
- **Name**: wagl-backend-cache-subnet-group
- **VPC**: vpc-037729e4703616c6e
- **Subnets**: subnet-00f9ba3449886f8f3, subnet-02963cc40eb5866a3

## 🔐 IAM Roles and Policies

### ECS Task Execution Role
- **ARN**: `arn:aws:iam::108367188859:role/wagl-backend-ecs-execution-role`
- **Role Name**: wagl-backend-ecs-execution-role
- **Attached Policies**:
  - arn:aws:iam::aws:policy/service-role/AmazonECSTaskExecutionRolePolicy
- **Trust Policy**: ecs-tasks.amazonaws.com

## 📊 Monitoring and Logging

### CloudWatch Log Groups
- **Log Group**: `/ecs/wagl-backend`
- **Purpose**: ECS container logs
- **Retention**: Not set (indefinite)

### CloudWatch Dashboard
- **Dashboard Name**: wagl-backend-monitoring
- **Widgets**:
  - ECS Service Metrics (CPU/Memory)
  - Load Balancer Metrics (Request Count, Response Time, Status Codes)
  - Aurora Serverless Metrics (Connections, CPU, Capacity)
  - Recent Error Logs

### CloudWatch Alarms

#### Application Alarms
1. **Wagl-Backend-High-Error-Rate**
   - **Metric**: HTTPCode_Target_5XX_Count
   - **Threshold**: > 10 errors in 5 minutes
   - **Evaluation Periods**: 2

2. **Wagl-Backend-Aurora-High-CPU**
   - **Metric**: CPUUtilization (Aurora)
   - **Threshold**: > 80% for 5 minutes
   - **Evaluation Periods**: 2

#### Auto Scaling Alarms
Auto-scaling alarms are automatically created by Application Auto Scaling:

1. **CPU High Alarm**: `TargetTracking-service/wagl-backend-cluster/wagl-backend-service-AlarmHigh-70ecd6cc-0672-404e-afca-b4bada4fb97d`
2. **CPU Low Alarm**: `TargetTracking-service/wagl-backend-cluster/wagl-backend-service-AlarmLow-bb655d92-c70a-4991-809b-aad4d0932da7`
3. **Memory High Alarm**: `TargetTracking-service/wagl-backend-cluster/wagl-backend-service-AlarmHigh-fe64dd5e-a1e6-4ec7-8fb0-c6a5fada0502`
4. **Memory Low Alarm**: `TargetTracking-service/wagl-backend-cluster/wagl-backend-service-AlarmLow-17a81060-b41f-45bb-8b5d-5f47be7ed506`

## 🔗 Connection Strings and Environment Variables

### Database Connection
```
Host=wagl-backend-aurora.cluster-cexeows4418s.us-east-1.rds.amazonaws.com;Database=waglbackend;Username=postgres;Password=WaglBackend2024!
```

### Redis Connection (TLS Required)
```
wagl-backend-cache-ggfeqp.serverless.use1.cache.amazonaws.com:6379,ssl=true,abortConnect=false
```

**CRITICAL**: ElastiCache Serverless (ValKey) requires TLS encryption for all connections. The application automatically detects ElastiCache Serverless endpoints and enables TLS with SslProtocols.Tls12.

### Office Access
- **Office IP**: 47.190.70.197 (Whitelisted for PostgreSQL and Redis access)
- **PostgreSQL Port**: 5432
- **Redis Port**: 6379

### VPN Access (OpenVPN)
- **VPN Server**: 54.87.224.34:1194 (UDP)
- **Instance**: i-0763635c74cbebae7 (wagl-backend-openvpn)
- **Instance Type**: t3.nano (~$3-6/month)
- **VPN Network**: 10.8.0.0/24
- **Accessible Subnets**: 10.0.3.0/24 (private-1a), 10.0.4.0/24 (private-1b)
- **Active Users**: bash, alec
- **Client Files**: bash.ovpn, alec.ovpn (available for download)

### Application URLs
- **Production Domain**: `https://api.wagl.ai` (Primary - App Runner)
- **Direct App Runner URL**: `https://v6uwnty3vi.us-east-1.awsapprunner.com`
- **Load Balancer**: `http://wagl-backend-alb-2094314021.us-east-1.elb.amazonaws.com` (Legacy - Unused)
- **Health Check**: `https://api.wagl.ai/health`

## 💰 Cost Optimization Features

### Auto Scaling
- **ECS Service**: Scales from 1-10 instances based on CPU/Memory
- **Aurora Serverless v2**: Scales from 0.5-16 ACU based on demand
- **ElastiCache Serverless**: Automatic capacity management

### Serverless Components
- Aurora Serverless v2: Pay per second usage
- ElastiCache Serverless: Pay for actual consumption
- Fargate: Pay per second of container runtime

## 🏷️ Resource Tags

All resources are tagged with:
- **Name**: [Resource-specific name]
- **Project**: wagl-backend

## 🌐 DNS and SSL Configuration

### Route 53 Hosted Zone
- **Hosted Zone ID**: `Z04843342UGS95V394GIH`
- **Domain**: wagl.ai
- **DNS Records**:
  - CNAME record: `api.wagl.ai` → App Runner (v6uwnty3vi.us-east-1.awsapprunner.com)
  - DNS validation records: SSL certificate validation (ACM)

### SSL Certificate
- **Certificate ARN**: `arn:aws:acm:us-east-1:108367188859:certificate/071eb2b0-e1b0-41b4-9a7b-0ab1af16ad61`
- **Domains Covered**: api.wagl.ai, *.wagl.ai
- **Validation Method**: DNS validation
- **Status**: ISSUED (Valid until 2026-10-16)
- **Usage**: Associated with App Runner service

## 📝 Deployment Notes

1. **Container Image**: Currently pointing to `waglswarm-web2:latest` in ECR
2. **Database**: Aurora cluster with PostgreSQL-compatible migrations successfully applied
3. **SSL/TLS**: SSL certificate requested and validation records added
4. **Domain**: Production domain `api.wagl.ai` configured with Route 53
5. **Environment**: All resources are configured for production workloads
6. **Networking**: NAT Gateway added for private subnet internet access
7. **Database Access**: Office IP (47.190.70.197) whitelisted for PostgreSQL and Redis access
8. **Application Status**: .NET Core 9 application running with proper environment configuration
9. **Migrations**: PostgreSQL syntax issues resolved in task definition revision 3
10. **Authentication**: Aurora master password reset and database connectivity verified
11. **VPN Access**: OpenVPN server deployed for secure database/cache access from anywhere
12. **User Management**: Automated certificate management and user provisioning scripts installed
13. **TLS Configuration**: ElastiCache Serverless TLS properly configured (Task Definition v5)
14. **JSON Serialization**: PostgreSQL dynamic JSON serialization issues resolved
15. **SignalR**: Redis backplane configured with mandatory TLS for ElastiCache Serverless
16. **Health Checks**: All endpoints healthy - /health returning 200 OK
17. **LINQ Translation**: Entity Framework DateTime.Add issues resolved with client-side evaluation
18. **Production Status**: Fully operational as of 2025-09-19
19. **App Runner Migration**: Successfully migrated from ECS to App Runner (2025-09-24)
20. **SignalR Port Fix**: ElastiCache Serverless dual-port architecture properly configured (ports 6379/6380)
21. **JWT Authentication**: Verified working after App Runner deployment
22. **ECS Cleanup**: Legacy ECS service and resources removed

## 🔄 Maintenance Windows

- **Aurora**: Saturday 10:18-10:48 UTC
- **Aurora Instance**: Thursday 09:35-10:05 UTC
- **ElastiCache**: Daily snapshot at 05:30 UTC

---

## 🛠️ Technical Implementation Details

### Recent Critical Fixes and Migrations

#### App Runner Migration (2025-09-24)
- **Issue**: ECS deployments were slow (3-5 minutes) and complex to manage
- **Solution**: Migrated to AWS App Runner for simplified container deployment
- **Benefits**: Faster deployments (~2 minutes), automatic scaling, zero-downtime updates
- **Status**: Migration completed successfully with full functionality

#### SignalR ValKey Dual-Port Architecture Fix (2025-09-24)
- **Issue**: SignalR Redis backplane failing with AWS ElastiCache Serverless port configuration
- **Root Cause**: ElastiCache Serverless uses dual ports: 6379 (writes) and 6380 (reads/subscriptions)
- **Solution**: Updated SignalR configuration to connect to both ports with proper TLS
- **Code Location**: `src/WaglBackend.Api/Startup.cs:145-189`
- **Result**: SignalR real-time functionality now working correctly

#### Previous Fixes (2025-09-19)

#### PostgreSQL JSON Serialization Resolution
- **Issue**: `Type 'String[]' required dynamic JSON serialization, which requires an explicit opt-in`
- **Root Cause**: Provider entity's AllowedIpAddresses (string array) couldn't serialize to JSONB
- **Solution**: Added explicit JSON conversion in ProviderConfiguration.cs
- **Code Location**: `src/WaglBackend.Infrastructure/Persistence/Configurations/ProviderConfiguration.cs`

#### ElastiCache Serverless TLS Configuration
- **Issue**: SignalR Redis backplane failing on TLS connections
- **Root Cause**: ElastiCache Serverless requires mandatory TLS encryption
- **Solution**: Enhanced SignalR configuration to auto-detect ElastiCache Serverless and enable TLS
- **Code Location**: `src/WaglBackend.Api/Startup.cs:131-160`
- **Connection String Format**: `wagl-backend-cache-ggfeqp.serverless.use1.cache.amazonaws.com:6379,ssl=true,abortConnect=false`

#### LINQ Translation Fixes
- **Issue**: `Translation of method 'System.DateTime.Add' failed`
- **Root Cause**: Entity Framework couldn't translate DateTime.Add(TimeSpan) to SQL
- **Solution**: Modified repository methods for client-side evaluation of duration calculations
- **Code Location**: `src/WaglBackend.Infrastructure/Persistence/Repositories/ChatSessionRepository.cs`

### Key Lessons Learned
1. **ElastiCache Serverless Requirement**: ALL connections must use TLS (ssl=true)
2. **PostgreSQL Arrays**: Require explicit JSON conversion for EF Core JSONB mapping
3. **SignalR Configuration**: Must detect AWS ElastiCache endpoints and configure TLS automatically
4. **Startup Configuration**: Service registration order matters for dependency injection
5. **Task Definition Management**: Environment variables must include proper TLS connection strings

---

**Last Updated**: 2025-09-24
**Infrastructure Version**: 2.0 (App Runner Migration)
**Deployment Region**: us-east-1
**Current Platform**: AWS App Runner
**Application Status**: Fully Operational with SignalR Fix Applied