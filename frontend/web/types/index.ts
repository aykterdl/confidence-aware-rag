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
  content: string;
  similarityScore?: number;  // Optional - may be undefined in low confidence scenarios
  sectionType?: string;
  articleNumber?: string;
  articleTitle?: string;
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


