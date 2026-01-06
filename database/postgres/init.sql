-- ============================================================================
-- PostgreSQL Initialization Script
-- ============================================================================
-- PURPOSE: Set up PostgreSQL extensions required by the application
-- IMPORTANT: This file is ONLY for database-level prerequisites
-- 
-- Schema (tables, columns, indexes) is managed by EF Core migrations
-- DO NOT add schema definitions here
-- ============================================================================

-- Enable pgvector extension for vector similarity search
CREATE EXTENSION IF NOT EXISTS vector;

-- Enable UUID extension for GUID support
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";
