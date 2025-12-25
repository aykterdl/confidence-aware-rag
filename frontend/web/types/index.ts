export interface ConfidenceInfo {
  level: 'high' | 'low' | 'none';
  maxSimilarity: number;
  averageSimilarity: number;
  explanation?: string;
}

export interface Source {
  chunkId: string;
  documentId: string;
  documentTitle: string;
  chunkIndex: number;
  similarityScore: number;
  contentPreview: string;
}

export interface Message {
  id: string;
  role: 'user' | 'assistant';
  content: string;
  timestamp: Date;
  confidence?: ConfidenceInfo;
  sources?: Source[];
  language?: string;
}


