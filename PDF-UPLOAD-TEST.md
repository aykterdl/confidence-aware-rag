# 📄 TC ANAYASASI PDF YÜKLEME KILAVUZU

## ✅ SİSTEM DURUMU

```
✅ Backend API: http://localhost:8080 (Timeout: 5 dakika)
✅ Frontend UI: http://localhost:3000
✅ PostgreSQL + pgvector: Running
✅ Ollama: Running
```

---

## 🎯 TEST ADIMLARI

### 1. Tarayıcıyı Yenile
```
http://localhost:3000 - F5 veya Ctrl+R
```

### 2. Developer Console'u Aç
```
Chrome/Edge: F12 → Console tab
Firefox: F12 → Console tab
```

### 3. PDF Upload
1. **"📄 Upload PDF" butonuna tıkla** (sağ üstte)
2. **"Select PDF File"** → TC Anayasası PDF'ini seç
3. **"Upload & Process PDF"** butonuna tıkla
4. **CONSOLE'U İZLE** - Şu log'ları göreceksin:

```javascript
📤 [PDF Upload] Starting upload... {filename: "tc_anayasasi.pdf", size: 1234567, title: "TC Anayasası"}
🔄 [PDF Upload] Sending to backend...
⏱️ [PDF Upload] Request completed in 45.3s
✅ [PDF Upload] Success! {documentId: "...", chunkCount: 145, ...}
```

### 4. Backend Log'larını İzle (Opsiyonel)
Ayrı bir terminal/PowerShell'de:
```powershell
docker logs -f rag-demo-rag-api-1
```

Şu log'ları göreceksin:
```
info: RAG.Api.Services.ChunkIngestionService[0]
      📄 INGESTION STARTED - Document: TC Anayasası (Text: 58432 chars)
info: RAG.Api.Services.TextChunkingService[0]
      Step 1: Chunking text (58432 characters, maxSize: 500, overlap: 50)
info: RAG.Api.Services.TextChunkingService[0]
      Step 1 COMPLETED: 145 chunks created
info: RAG.Api.Services.ChunkIngestionService[0]
      Step 3.1: Processing chunk 1/145 (ID: ..., Length: 498)
info: RAG.Api.Services.ChunkIngestionService[0]
      Embedding generated successfully for chunk 1 (Dimensions: 768)
info: RAG.Api.Services.ChunkIngestionService[0]
      Chunk entity created and added to list: 1/145
info: RAG.Api.Services.ChunkIngestionService[0]
      Step 3.2: Processing chunk 2/145 (ID: ..., Length: 495)
...
info: RAG.Api.Services.ChunkIngestionService[0]
      Step 4: Saving 145 chunks to database...
info: RAG.Api.Services.ChunkIngestionService[0]
      ✅ INGESTION COMPLETED - Document: TC Anayasası (145 chunks in 45.2s)
```

---

## 🐛 SORUN GİDERME

### Hata: "Request completed in 45.3s" → 500 Error

**Sebep**: Ollama çok yavaş veya embedding üretemiyor.

**Çözüm**:
```powershell
# Ollama durumunu kontrol et
docker logs rag-demo-ollama-1 --tail 50

# Ollama'yı yeniden başlat
docker restart rag-demo-ollama-1

# Model'in yüklendiğini kontrol et
docker exec rag-demo-ollama-1 ollama list
```

Çıktı şu olmalı:
```
NAME                  ID              SIZE      MODIFIED
nomic-embed-text:latest  ...          274 MB    2 days ago
llama3.2:1b:latest       ...          1.3 GB    2 days ago
```

Eğer model yoksa:
```powershell
docker exec rag-demo-ollama-1 ollama pull nomic-embed-text
```

---

### Hata: "Failed to load resource: net::ERR_CONNECTION_REFUSED"

**Sebep**: Backend çalışmıyor.

**Çözüm**:
```powershell
docker-compose up -d rag-api
```

---

### Hata: Frontend'de değişiklikler görünmüyor

**Çözüm**:
```powershell
# Tarayıcıda Hard Refresh
Ctrl + Shift + R (veya Ctrl + F5)
```

---

## 📊 BAŞARI KRİTERLERİ

Upload başarılı ise:

### Frontend Console:
```javascript
✅ [PDF Upload] Success! {
  documentId: "a91e4a1a-d431-4853-baee-c21f3a149a5b",
  documentTitle: "TC Anayasası",
  chunkCount: 145,
  extractedTextLength: 58432
}
```

### Frontend UI:
```
✅ Upload Successful!
Document: TC Anayasası
Chunks Created: 145
Text Extracted: 58,432 characters
```

### Backend Log:
```
✅ INGESTION COMPLETED - Document: TC Anayasası (145 chunks in 45.2s)
```

---

## 🎬 SONRA NE YAPILACAK?

1. **Upload panelini kapat**: "Hide Upload" butonuna tıkla

2. **Soru sor**:
   ```
   TC Anayasasının 1. maddesi nedir?
   ```

3. **Cevabı kontrol et**:
   - ✅ **High Confidence** (yeşil badge)
   - ✅ Sources: "TC Anayasası" görünmeli
   - ✅ Cevap Türkçe olmalı
   - ✅ Anayasa'dan doğru alıntı yapmalı

---

## ⏱️ BEKLENEN SÜRELER

| PDF Boyutu | Chunk Sayısı | İşlem Süresi |
|------------|--------------|--------------|
| 10 sayfa   | ~30 chunk    | ~15 saniye   |
| 50 sayfa   | ~150 chunk   | ~45 saniye   |
| 200 sayfa  | ~600 chunk   | ~3 dakika    |

**Not**: Her chunk için ~300ms embedding süresi var (Ollama + CPU)

---

## 🔧 DEĞİŞEN AYARLAR

### Backend (✅ Uygulandı):
- `TimeoutSeconds`: 30 → **300 saniye** (5 dakika)
- HttpClient timeout artırıldı
- Detaylı log'lar eklendi

### Frontend (✅ Uygulandı):
- Console log'ları eklendi
- Error mesajları detaylandırıldı
- Upload progress tracking

---

**🚀 HAZIR! Şimdi test edebilirsin.**

