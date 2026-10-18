-- HMS Database Initialization Script
-- This runs on first container start

-- Create database if not exists (handled by EF Core migrations, this is a fallback)
IF NOT EXISTS (SELECT * FROM sys.databases WHERE name = 'HmsDb')
BEGIN
    CREATE DATABASE HmsDb
    COLLATE SQL_Latin1_General_CP1_CI_AS;
END
GO

USE HmsDb;
GO

-- Enable snapshot isolation for better concurrency
ALTER DATABASE HmsDb SET ALLOW_SNAPSHOT_ISOLATION ON;
ALTER DATABASE HmsDb SET READ_COMMITTED_SNAPSHOT ON;
GO

PRINT 'HMS Database initialized successfully.';
GO
