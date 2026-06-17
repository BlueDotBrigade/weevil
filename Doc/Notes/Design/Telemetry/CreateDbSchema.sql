/* =============================================================================
   Weevil Telemetry - Database Schema (v1.0)
   -----------------------------------------------------------------------------
   Target : Azure SQL
            server   = weevil-db.database.windows.net
            database = weevil-db-catalog
   Run as : a database administrator (NOT the runtime producer user below).

   This script is idempotent: it can be run repeatedly to (re)create the schema
   and the locked-down runtime user. It is the single source of truth for the
   telemetry DDL; keep TelemetryDbContext.OnModelCreating in lock-step with it.

   Design notes:
     * session_id is a client-generated GUID and is the sole primary key.
       It is also the de-duplication key: re-uploading an already-stored
       session violates the PK (error 2627), which the client treats as a
       successful "duplicate" and deletes the local outbox file. This keeps
       uploads idempotent even if the application is killed mid-upload.
     * Usage counters live in the child table telemetry_session_metric as
       key/count rows, so adding a new metric never requires a schema change.
     * Nothing is server-generated, so the runtime user needs no SELECT rights.
   ============================================================================= */

/* ----------------------------------------------------------------------------
   1. Tables
   ---------------------------------------------------------------------------- */

IF OBJECT_ID(N'dbo.telemetry_session', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.telemetry_session
    (
        session_id             uniqueidentifier NOT NULL,

        application            nvarchar(256)    NOT NULL,
        source                 nvarchar(256)    NOT NULL,
        version                nvarchar(32)     NOT NULL,

        is_debugging           bit              NOT NULL,

        session_start_utc      datetime2        NOT NULL,
        session_end_utc        datetime2        NOT NULL,

        session_active_minutes float            NOT NULL,

        log_file_size_bytes    bigint           NOT NULL,
        installed_ram_mb       bigint           NOT NULL,
        installed_cpu          nvarchar(256)    NOT NULL,

        schema_version         nvarchar(16)     NOT NULL,

        CONSTRAINT pk_telemetry_session PRIMARY KEY (session_id)
    );
END;
GO

IF OBJECT_ID(N'dbo.telemetry_session_metric', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.telemetry_session_metric
    (
        session_id   uniqueidentifier NOT NULL,
        metric_key   nvarchar(128)    NOT NULL,
        metric_count int              NOT NULL,

        CONSTRAINT pk_telemetry_session_metric
            PRIMARY KEY (session_id, metric_key),

        CONSTRAINT fk_telemetry_session_metric_telemetry_session
            FOREIGN KEY (session_id)
            REFERENCES dbo.telemetry_session (session_id)
    );
END;
GO

/* ----------------------------------------------------------------------------
   2. Indexes
   ---------------------------------------------------------------------------- */

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'ix_telemetry_session_metric_metric_key')
    CREATE INDEX ix_telemetry_session_metric_metric_key
        ON dbo.telemetry_session_metric (metric_key);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'ix_telemetry_session_session_start_utc')
    CREATE INDEX ix_telemetry_session_session_start_utc
        ON dbo.telemetry_session (session_start_utc);
GO

/* ----------------------------------------------------------------------------
   3. Runtime producer user (least privilege)
   -----------------------------------------------------------------------------
   Contained database user with a password (no server login). Replace the
   password before running. The runtime user may only INSERT; it cannot read,
   update, delete, or inspect schema definitions.
   ---------------------------------------------------------------------------- */

IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = N'weevil_telemetry_producer')
BEGIN
    CREATE USER weevil_telemetry_producer WITH PASSWORD = N'REPLACE_WITH_A_STRONG_PASSWORD';
END;
GO

GRANT CONNECT TO weevil_telemetry_producer;

GRANT INSERT ON OBJECT::dbo.telemetry_session         TO weevil_telemetry_producer;
GRANT INSERT ON OBJECT::dbo.telemetry_session_metric  TO weevil_telemetry_producer;

DENY UPDATE ON OBJECT::dbo.telemetry_session          TO weevil_telemetry_producer;
DENY DELETE ON OBJECT::dbo.telemetry_session          TO weevil_telemetry_producer;
DENY UPDATE ON OBJECT::dbo.telemetry_session_metric   TO weevil_telemetry_producer;
DENY DELETE ON OBJECT::dbo.telemetry_session_metric   TO weevil_telemetry_producer;

-- Limit visibility: no schema-wide reads, and no schema introspection.
DENY SELECT ON SCHEMA::dbo TO weevil_telemetry_producer;
DENY VIEW DEFINITION       TO weevil_telemetry_producer;
GO

/* ----------------------------------------------------------------------------
   4. Teardown (commented) - run only if the user must be removed.
   ----------------------------------------------------------------------------
   DROP USER IF EXISTS weevil_telemetry_producer;
   GO
   ---------------------------------------------------------------------------- */
