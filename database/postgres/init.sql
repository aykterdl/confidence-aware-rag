-- pgvector extension'ı etkinleştir
CREATE EXTENSION IF NOT EXISTS vector;

-- UUID extension (id'ler için)
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";

-- 1. Dökümanlar tablosu (PDF'ler)
CREATE TABLE documents (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    filename VARCHAR(500) NOT NULL,
    file_path TEXT,
    upload_date TIMESTAMP DEFAULT NOW(),
    total_chunks INTEGER DEFAULT 0,
    metadata JSONB,
    created_at TIMESTAMP DEFAULT NOW()
);

-- 2. Chunks tablosu (Bölünmüş metinler + embeddings)
CREATE TABLE chunks (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    document_id UUID NOT NULL REFERENCES documents(id) ON DELETE CASCADE,
    chunk_index INTEGER NOT NULL,
    content TEXT NOT NULL,
    embedding VECTOR(768),  -- Ollama nomic-embed-text → 768 boyut
    token_count INTEGER,
    created_at TIMESTAMP DEFAULT NOW(),
    CONSTRAINT unique_document_chunk UNIQUE (document_id, chunk_index)
);

-- Index'ler (performans için)
CREATE INDEX idx_chunks_document_id ON chunks(document_id);
CREATE INDEX idx_chunks_embedding ON chunks USING ivfflat (embedding vector_cosine_ops);

-- 3. Query log tablosu (opsiyonel - sorgu geçmişi)
CREATE TABLE query_logs (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    query_text TEXT NOT NULL,
    result_text TEXT,
    source_chunk_ids UUID[],
    created_at TIMESTAMP DEFAULT NOW()
);

-- Test verisi (opsiyonel - silinen satırlar)
-- INSERT INTO documents (filename, file_path) VALUES ('test.pdf', '/uploads/test.pdf');





