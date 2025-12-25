-- Migration: Change documents.metadata from JSONB to TEXT
-- Date: 2025-12-25
-- Purpose: Support both JSON and plain string metadata

-- Change column type
ALTER TABLE documents ALTER COLUMN metadata TYPE TEXT USING metadata::TEXT;

-- Add comment
COMMENT ON COLUMN documents.metadata IS 'Document metadata as text (supports both JSON and plain strings)';

