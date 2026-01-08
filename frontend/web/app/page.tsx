'use client';

import { useState } from 'react';
import { ChatMessage } from '@/components/ChatMessage';
import { ChatInput } from '@/components/ChatInput';
import { PdfUpload } from '@/components/PdfUpload';
import type { Message } from '@/types';

export default function Home() {
  const [messages, setMessages] = useState<Message[]>([]);
  const [isLoading, setIsLoading] = useState(false);
  const [conversationId, setConversationId] = useState<string | null>(null);

  const handleSendMessage = async (question: string) => {
    // Add user message immediately
    const userMessage: Message = {
      id: Date.now().toString(),
      role: 'user',
      content: question,
      timestamp: new Date(),
    };
    setMessages((prev) => [...prev, userMessage]);
    setIsLoading(true);
    
    try {
      console.log('🔍 Sending question to RAG system:', question);
      
      const response = await fetch('http://localhost:8080/api/query/answer', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({
          query: question,
          topK: 5,
          documentId: null,
          language: null,
        }),
        mode: 'cors',
      });

      if (!response.ok) {
        const errorData = await response.json().catch(() => ({ error: 'Unknown error' }));
        console.error('❌ RAG query failed:', response.status, errorData);
        
        // Show error message to user
        const errorMessage: Message = {
          id: (Date.now() + 1).toString(),
          role: 'assistant',
          content: `Sorry, I encountered an error while processing your question:\n\n${errorData.error || 'Unknown error'}\n\nPlease make sure:\n- Documents have been uploaded\n- The backend service is running\n- Ollama is running with the required models`,
          timestamp: new Date(),
          confidence: {
            level: 'none',
            maxSimilarity: 0,
            averageSimilarity: 0,
            explanation: 'Error occurred',
          },
          sources: [],
        };
        setMessages((prev) => [...prev, errorMessage]);
        setIsLoading(false);
        return;
      }

      const data = await response.json();
      console.log('✅ RAG response received:', {
        confidence: data.confidence,
        sourceCount: data.sourceCount,
        llmInvoked: data.llmInvoked
      });

      // Map confidence string to Message confidence object
      const confidenceLevel = data.confidence as 'high' | 'low' | 'none';
      
      // Calculate average similarity with defensive guards
      const validScores = data.sources?.filter((s: any) => typeof s.similarityScore === 'number') || [];
      const averageSimilarity = validScores.length > 0
        ? validScores.reduce((sum: number, s: any) => sum + s.similarityScore, 0) / validScores.length
        : 0;
      
      const assistantMessage: Message = {
        id: (Date.now() + 1).toString(),
        role: 'assistant',
        content: data.answer,
        timestamp: new Date(),
        confidence: {
          level: confidenceLevel,
          maxSimilarity: data.sources?.[0]?.similarityScore || 0,
          averageSimilarity: averageSimilarity,
          explanation: data.confidenceExplanation || '',
        },
        sources: data.sources?.map((source: any, idx: number) => ({
          chunkId: source.chunkId || `fallback-chunk-${Date.now()}-${idx}`,  // Unique fallback key
          documentId: source.documentId || `fallback-doc-${Date.now()}-${idx}`,  // Unique fallback key
          documentTitle: source.documentTitle || 'Unknown Document',
          content: source.content || '',
          similarityScore: source.similarityScore,  // Can be undefined - defensive rendering in SourcesPanel
          sectionType: source.sectionType,
          articleNumber: source.articleNumber,
          articleTitle: source.articleTitle,
        })) || [],
      };
      
      setMessages((prev) => [...prev, assistantMessage]);
      setIsLoading(false);
      
    } catch (error: any) {
      console.error('❌ Unexpected error in RAG query:', error);
      
      const errorMessage: Message = {
        id: (Date.now() + 1).toString(),
        role: 'assistant',
        content: `I encountered an unexpected error. This could mean:\n\n` +
                 `- The backend service is not running\n` +
                 `- Ollama is not accessible\n` +
                 `- No documents have been uploaded yet\n\n` +
                 `Technical details: ${error.message || 'Unknown error'}\n\n` +
                 `Please check the console for more information.`,
        timestamp: new Date(),
        confidence: {
          level: 'none',
          maxSimilarity: 0,
          averageSimilarity: 0,
          explanation: 'Service unavailable',
        },
        sources: [],
      };
      setMessages((prev) => [...prev, errorMessage]);
      setIsLoading(false);
    }
  };

  const handleNewConversation = () => {
    setMessages([]);
    setConversationId(null);
  };

  const [showUpload, setShowUpload] = useState(false);

  return (
    <div className="min-h-screen bg-gradient-to-br from-slate-50 to-slate-100 flex flex-col">
      {/* Header */}
      <header className="bg-white border-b border-slate-200 shadow-sm">
        <div className="max-w-5xl mx-auto px-4 py-4 flex items-center justify-between">
          <div>
            <h1 className="text-2xl font-bold text-slate-900">RAG Demo</h1>
            <p className="text-sm text-slate-600">
              Retrieval Augmented Generation System
            </p>
          </div>
          <div className="flex items-center space-x-2">
            <button
              onClick={() => setShowUpload(!showUpload)}
              className="px-4 py-2 text-sm font-medium text-blue-700 bg-blue-50 border border-blue-200 rounded-lg hover:bg-blue-100 transition-colors"
            >
              {showUpload ? 'Hide Upload' : '📄 Upload PDF'}
            </button>
            {messages.length > 0 && (
              <button
                onClick={handleNewConversation}
                className="px-4 py-2 text-sm font-medium text-slate-700 bg-white border border-slate-300 rounded-lg hover:bg-slate-50 transition-colors"
              >
                New Conversation
              </button>
            )}
          </div>
        </div>
      </header>

      {/* Chat Area */}
      <div className="flex-1 overflow-y-auto">
        <div className="max-w-5xl mx-auto px-4 py-8">
          {/* PDF Upload Panel */}
          {showUpload && (
            <div className="mb-8">
              <PdfUpload />
            </div>
          )}
          {messages.length === 0 ? (
            <div className="text-center py-16">
              <div className="inline-flex items-center justify-center w-16 h-16 bg-blue-100 rounded-full mb-4">
                <svg
                  className="w-8 h-8 text-blue-600"
                  fill="none"
                  stroke="currentColor"
                  viewBox="0 0 24 24"
                >
                  <path
                    strokeLinecap="round"
                    strokeLinejoin="round"
                    strokeWidth={2}
                    d="M8 10h.01M12 10h.01M16 10h.01M9 16H5a2 2 0 01-2-2V6a2 2 0 012-2h14a2 2 0 012 2v8a2 2 0 01-2 2h-5l-5 5v-5z"
                  />
                </svg>
              </div>
              <h2 className="text-xl font-semibold text-slate-900 mb-2">
                Ask a Question
              </h2>
              <p className="text-slate-600">
                Start a conversation with the RAG system
              </p>
            </div>
          ) : (
            <div className="space-y-6">
              {messages.map((message) => (
                <ChatMessage key={message.id} message={message} />
              ))}
              {isLoading && (
                <div className="flex items-center space-x-2 text-slate-600">
                  <div className="w-2 h-2 bg-blue-600 rounded-full animate-bounce" />
                  <div
                    className="w-2 h-2 bg-blue-600 rounded-full animate-bounce"
                    style={{ animationDelay: '0.1s' }}
                  />
                  <div
                    className="w-2 h-2 bg-blue-600 rounded-full animate-bounce"
                    style={{ animationDelay: '0.2s' }}
                  />
                  <span className="text-sm ml-2">Thinking...</span>
                </div>
              )}
            </div>
          )}
        </div>
      </div>

      {/* Input Area */}
      <div className="bg-white border-t border-slate-200 shadow-lg">
        <div className="max-w-5xl mx-auto px-4 py-4">
          <ChatInput onSend={handleSendMessage} disabled={isLoading} />
          {conversationId && (
            <div className="mt-2 text-xs text-slate-500 text-center">
              Conversation active • ID: {conversationId.slice(0, 8)}...
            </div>
          )}
        </div>
      </div>
    </div>
  );
}
