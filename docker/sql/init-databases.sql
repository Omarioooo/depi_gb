USE [master];
GO

IF DB_ID(N'$(AuthDb)') IS NULL
    EXEC(N'CREATE DATABASE [$(AuthDb)]');
GO

IF DB_ID(N'$(KeycloakDb)') IS NULL
    EXEC(N'CREATE DATABASE [$(KeycloakDb)] COLLATE Latin1_General_100_CI_AS_SC_UTF8');
GO

IF DB_ID(N'$(InventoryDb)') IS NULL
    EXEC(N'CREATE DATABASE [$(InventoryDb)]');
GO

IF EXISTS (SELECT 1 FROM sys.databases WHERE name = N'$(KeycloakDb)' AND is_read_committed_snapshot_on = 0)
    ALTER DATABASE [$(KeycloakDb)] SET READ_COMMITTED_SNAPSHOT ON WITH ROLLBACK IMMEDIATE;
GO
