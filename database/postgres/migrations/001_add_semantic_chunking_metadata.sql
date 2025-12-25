-- Migration: Add semantic chunking metadata to chunks table
-- Date: 2025-12-25
-- Purpose: Support article-aware, paragraph-aware, and generic chunking

-- Add new columns for semantic metadata
ALTER TABLE chunks ADD COLUMN IF NOT EXISTS article_number VARCHAR(50);
ALTER TABLE chunks ADD COLUMN IF NOT EXISTS article_title TEXT;
ALTER TABLE chunks ADD COLUMN IF NOT EXISTS chunk_type VARCHAR(20) DEFAULT 'generic';

-- Add index for article queries
CREATE INDEX IF NOT EXISTS idx_chunks_article_number ON chunks(article_number) WHERE article_number IS NOT NULL;
CREATE INDEX IF NOT EXISTS idx_chunks_chunk_type ON chunks(chunk_type);

-- Add comment
COMMENT ON COLUMN chunks.article_number IS 'Legal document article number (e.g., "1", "2", "3a")';
COMMENT ON COLUMN chunks.article_title IS 'Article title or heading';
COMMENT ON COLUMN chunks.chunk_type IS 'Chunk type: article, paragraph, or generic';

