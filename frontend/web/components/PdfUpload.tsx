'use client';

import { useState } from 'react';

interface UploadResult {
  success: boolean;
  documentId: string;
  title: string;
  chunkCount: number;
  characterCount: number;
  pageCount?: number;
  message: string;
}

export function PdfUpload() {
  const [file, setFile] = useState<File | null>(null);
  const [title, setTitle] = useState('');
  const [isUploading, setIsUploading] = useState(false);
  const [result, setResult] = useState<UploadResult | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [progress, setProgress] = useState(0);
  const [progressMessage, setProgressMessage] = useState('');

  const handleFileChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const selectedFile = e.target.files?.[0];
    if (selectedFile) {
      setFile(selectedFile);
      // Dosya adından başlık oluştur (uzantıyı kaldır)
      const fileName = selectedFile.name.replace('.pdf', '');
      setTitle(fileName);
      setResult(null);
      setError(null);
    }
  };

  const handleUpload = async () => {
    if (!file) return;

    setIsUploading(true);
    setError(null);
    setResult(null);
    setProgress(0);
    setProgressMessage('Preparing upload...');

    console.log('📤 [PDF Upload] Starting upload...', {
      filename: file.name,
      size: file.size,
      title: title || file.name.replace('.pdf', '')
    });

    // Simulate progress (since backend doesn't stream)
    const progressInterval = setInterval(() => {
      setProgress((prev) => {
        if (prev >= 90) return prev; // Stop at 90% until backend responds
        return prev + 1;
      });
    }, 1000); // Update every second

    try {
      const formData = new FormData();
      formData.append('file', file);
      if (title) {
        formData.append('title', title);
      }

      console.log('🔄 [PDF Upload] Sending to backend...');
      setProgress(5);
      setProgressMessage('Uploading PDF to backend...');
      
      const startTime = Date.now();

      setTimeout(() => setProgressMessage('Processing PDF (extracting text)...'), 2000);
      setTimeout(() => setProgressMessage('Creating chunks...'), 5000);
      setTimeout(() => setProgressMessage('Generating embeddings (this may take several minutes)...'), 10000);

      // Direct backend call to Clean Architecture ingestion endpoint
      const response = await fetch('http://localhost:8080/api/documents/ingest', {
        method: 'POST',
        body: formData,
        mode: 'cors', // Enable CORS
      });

      clearInterval(progressInterval);

      const duration = ((Date.now() - startTime) / 1000).toFixed(1);
      console.log(`⏱️ [PDF Upload] Request completed in ${duration}s`);

      if (!response.ok) {
        const errorData = await response.json().catch(() => ({ error: `HTTP ${response.status}` }));
        console.error('❌ [PDF Upload] Server error:', errorData);
        throw new Error(errorData.error || errorData.message || `Server error: ${response.status}`);
      }

      const data = await response.json();
      
      console.log('✅ [PDF Upload] Success!', {
        documentId: data.documentId,
        title: data.title,
        chunkCount: data.chunkCount,
        characterCount: data.characterCount,
        pageCount: data.pageCount
      });

      setProgress(100);
      setProgressMessage('Complete!');
      setResult(data);
      setFile(null);
      setTitle('');
      
      // Reset file input
      const fileInput = document.getElementById('pdf-file-input') as HTMLInputElement;
      if (fileInput) fileInput.value = '';
    } catch (err) {
      clearInterval(progressInterval);
      const errorMessage = err instanceof Error ? err.message : 'Upload failed';
      console.error('❌ [PDF Upload] Failed:', errorMessage, err);
      setError(errorMessage);
      setProgress(0);
      setProgressMessage('');
    } finally {
      setIsUploading(false);
    }
  };

  return (
    <div className="bg-white border border-slate-200 rounded-lg p-6 shadow-sm">
      <div className="flex items-center space-x-2 mb-4">
        <svg
          className="w-5 h-5 text-blue-600"
          fill="none"
          stroke="currentColor"
          viewBox="0 0 24 24"
        >
          <path
            strokeLinecap="round"
            strokeLinejoin="round"
            strokeWidth={2}
            d="M7 16a4 4 0 01-.88-7.903A5 5 0 1115.9 6L16 6a5 5 0 011 9.9M15 13l-3-3m0 0l-3 3m3-3v12"
          />
        </svg>
        <h3 className="text-lg font-semibold text-slate-900">
          Upload PDF Document
        </h3>
      </div>

      <div className="space-y-4">
        {/* File Input */}
        <div>
          <label
            htmlFor="pdf-file-input"
            className="block text-sm font-medium text-slate-700 mb-2"
          >
            Select PDF File
          </label>
          <input
            id="pdf-file-input"
            type="file"
            accept=".pdf,application/pdf"
            onChange={handleFileChange}
            disabled={isUploading}
            className="block w-full text-sm text-slate-500
              file:mr-4 file:py-2 file:px-4
              file:rounded-lg file:border-0
              file:text-sm file:font-semibold
              file:bg-blue-50 file:text-blue-700
              hover:file:bg-blue-100
              disabled:opacity-50 disabled:cursor-not-allowed"
          />
        </div>

        {/* Title Input */}
        {file && (
          <div>
            <label
              htmlFor="doc-title"
              className="block text-sm font-medium text-slate-700 mb-2"
            >
              Document Title (Optional)
            </label>
            <input
              id="doc-title"
              type="text"
              value={title}
              onChange={(e) => setTitle(e.target.value)}
              disabled={isUploading}
              placeholder="e.g., TC Anayasası"
              className="w-full px-3 py-2 border border-slate-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent disabled:bg-slate-100 disabled:cursor-not-allowed text-sm text-slate-900 placeholder:text-slate-400"
            />
          </div>
        )}

        {/* Progress Bar */}
        {isUploading && (
          <div className="space-y-2">
            <div className="flex items-center justify-between text-sm">
              <span className="text-slate-700 font-medium">{progressMessage}</span>
              <span className="text-slate-600">{progress}%</span>
            </div>
            <div className="w-full bg-slate-200 rounded-full h-2.5 overflow-hidden">
              <div
                className="bg-blue-600 h-2.5 rounded-full transition-all duration-300 ease-out"
                style={{ width: `${progress}%` }}
              />
            </div>
            <p className="text-xs text-slate-500 text-center">
              ⏱️ Large PDFs may take several minutes to process...
            </p>
          </div>
        )}

        {/* Upload Button */}
        {file && !result && (
          <button
            onClick={handleUpload}
            disabled={isUploading}
            className="w-full px-4 py-2 bg-blue-600 text-white font-medium rounded-lg hover:bg-blue-700 focus:outline-none focus:ring-2 focus:ring-blue-500 focus:ring-offset-2 disabled:bg-slate-300 disabled:cursor-not-allowed transition-colors flex items-center justify-center space-x-2"
          >
            {isUploading ? (
              <>
                <svg
                  className="animate-spin h-5 w-5"
                  xmlns="http://www.w3.org/2000/svg"
                  fill="none"
                  viewBox="0 0 24 24"
                >
                  <circle
                    className="opacity-25"
                    cx="12"
                    cy="12"
                    r="10"
                    stroke="currentColor"
                    strokeWidth="4"
                  />
                  <path
                    className="opacity-75"
                    fill="currentColor"
                    d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"
                  />
                </svg>
                <span>Uploading & Processing...</span>
              </>
            ) : (
              <>
                <svg
                  className="w-5 h-5"
                  fill="none"
                  stroke="currentColor"
                  viewBox="0 0 24 24"
                >
                  <path
                    strokeLinecap="round"
                    strokeLinejoin="round"
                    strokeWidth={2}
                    d="M4 16v1a3 3 0 003 3h10a3 3 0 003-3v-1m-4-8l-4-4m0 0L8 8m4-4v12"
                  />
                </svg>
                <span>Upload & Process PDF</span>
              </>
            )}
          </button>
        )}

        {/* Success Result */}
        {result && (
          <div className="bg-green-50 border border-green-200 rounded-lg p-4">
            <div className="flex items-start space-x-2">
              <svg
                className="w-5 h-5 text-green-600 mt-0.5 flex-shrink-0"
                fill="none"
                stroke="currentColor"
                viewBox="0 0 24 24"
              >
                <path
                  strokeLinecap="round"
                  strokeLinejoin="round"
                  strokeWidth={2}
                  d="M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z"
                />
              </svg>
              <div className="flex-1">
                <p className="text-sm font-semibold text-green-900 mb-1">
                  Upload Successful!
                </p>
                <div className="text-xs text-green-800 space-y-1">
                  <p>
                    <span className="font-medium">Document:</span>{' '}
                    {result.title}
                  </p>
                  <p>
                    <span className="font-medium">Chunks Created:</span>{' '}
                    {result.chunkCount}
                  </p>
                  <p>
                    <span className="font-medium">Text Extracted:</span>{' '}
                    {result.characterCount.toLocaleString()} characters
                  </p>
                  {result.pageCount && (
                    <p>
                      <span className="font-medium">Pages:</span>{' '}
                      {result.pageCount}
                    </p>
                  )}
                  <p className="text-green-700 font-medium mt-2">
                    {result.message}
                  </p>
                </div>
              </div>
            </div>
          </div>
        )}

        {/* Error */}
        {error && (
          <div className="bg-red-50 border border-red-200 rounded-lg p-4">
            <div className="flex items-start space-x-2">
              <svg
                className="w-5 h-5 text-red-600 mt-0.5 flex-shrink-0"
                fill="none"
                stroke="currentColor"
                viewBox="0 0 24 24"
              >
                <path
                  strokeLinecap="round"
                  strokeLinejoin="round"
                  strokeWidth={2}
                  d="M12 8v4m0 4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z"
                />
              </svg>
              <div>
                <p className="text-sm font-semibold text-red-900 mb-1">
                  Upload Failed
                </p>
                <p className="text-xs text-red-800">{error}</p>
              </div>
            </div>
          </div>
        )}
      </div>

      {/* Info */}
      <div className="mt-4 p-3 bg-blue-50 border border-blue-200 rounded-lg">
        <p className="text-xs text-blue-800">
          <span className="font-semibold">ℹ️ Note:</span> PDF will be processed
          automatically (text extraction → chunking → embedding → vector store).
          This may take a few seconds depending on document size.
        </p>
      </div>
    </div>
  );
}


