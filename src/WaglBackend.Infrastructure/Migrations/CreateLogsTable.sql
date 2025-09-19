-- Create Logs table for Serilog database logging
-- This table will store all Warning and Error level logs from the application

CREATE TABLE IF NOT EXISTS public."Logs" (
    "Id" SERIAL PRIMARY KEY,
    "Timestamp" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    "Level" VARCHAR(50) NOT NULL,
    "Message" TEXT NOT NULL,
    "MessageTemplate" TEXT,
    "Exception" TEXT,
    "Properties" JSONB,
    "MachineName" VARCHAR(255),
    "ProcessId" INTEGER,
    "ThreadId" INTEGER,
    "Environment" VARCHAR(100),
    "Application" VARCHAR(100) DEFAULT 'WaglBackend',
    "RequestId" VARCHAR(100),
    "RequestPath" VARCHAR(500),
    "SourceContext" VARCHAR(500),
    "ActionId" VARCHAR(100),
    "ActionName" VARCHAR(255),
    "ConnectionId" VARCHAR(100)
);

-- Create indexes for better query performance
CREATE INDEX IF NOT EXISTS "IX_Logs_Timestamp" ON public."Logs" ("Timestamp");
CREATE INDEX IF NOT EXISTS "IX_Logs_Level" ON public."Logs" ("Level");
CREATE INDEX IF NOT EXISTS "IX_Logs_Application" ON public."Logs" ("Application");
CREATE INDEX IF NOT EXISTS "IX_Logs_SourceContext" ON public."Logs" ("SourceContext");
CREATE INDEX IF NOT EXISTS "IX_Logs_RequestId" ON public."Logs" ("RequestId");
CREATE INDEX IF NOT EXISTS "IX_Logs_Properties" ON public."Logs" USING GIN ("Properties");

-- Create a partial index for errors and warnings only
CREATE INDEX IF NOT EXISTS "IX_Logs_Errors_Warnings" ON public."Logs" ("Timestamp", "Level")
WHERE "Level" IN ('Warning', 'Error', 'Fatal');

COMMENT ON TABLE public."Logs" IS 'Serilog structured logging table for warnings and errors';
COMMENT ON COLUMN public."Logs"."Properties" IS 'Structured log properties in JSONB format';
COMMENT ON COLUMN public."Logs"."MessageTemplate" IS 'Serilog message template with placeholders';