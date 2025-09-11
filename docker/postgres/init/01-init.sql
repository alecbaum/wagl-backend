-- Initialize the Wagl database
-- This script runs when the PostgreSQL container is first created

-- Create extensions
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";
CREATE EXTENSION IF NOT EXISTS "pgcrypto";

-- Set default timezone
SET timezone = 'UTC';

-- Create a schema for application data (optional)
-- CREATE SCHEMA IF NOT EXISTS wagl;

-- Grant permissions to the application user
GRANT ALL PRIVILEGES ON DATABASE wagldb TO waglmin;

-- Log the initialization
DO $$
BEGIN
    RAISE NOTICE 'Wagl database initialized successfully';
END $$;