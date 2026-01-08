'use client';

import { useState } from 'react';
import { Source } from '@/types';

interface SourcesPanelProps {
  sources: Source[];
}

export function SourcesPanel({ sources }: SourcesPanelProps) {
  const [expandedSources, setExpandedSources] = useState<Set<string>>(
    new Set()
  );

  const toggleSource = (chunkId: string) => {
    setExpandedSources((prev) => {
      const next = new Set(prev);
      if (next.has(chunkId)) {
        next.delete(chunkId);
      } else {
        next.add(chunkId);
      }
      return next;
    });
  };

  if (!sources || sources.length === 0) {
    return null;
  }

  return (
    <div className="border-t border-slate-200 pt-3 mt-3">
      <div className="flex items-center space-x-2 mb-2">
        <svg
          className="w-4 h-4 text-slate-600"
          fill="none"
          stroke="currentColor"
          viewBox="0 0 24 24"
        >
          <path
            strokeLinecap="round"
            strokeLinejoin="round"
            strokeWidth={2}
            d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z"
          />
        </svg>
        <span className="text-sm font-semibold text-slate-700">
          Sources ({sources.length})
        </span>
      </div>

      <div className="space-y-2">
        {sources.map((source, index) => {
          const isExpanded = expandedSources.has(source.chunkId);
          // Use combination of chunkId and index for guaranteed uniqueness
          const uniqueKey = `${source.chunkId}-${index}`;
          return (
            <div
              key={uniqueKey}
              className="bg-slate-50 border border-slate-200 rounded-lg overflow-hidden"
            >
              {/* Source Header - Always Visible */}
              <button
                onClick={() => toggleSource(source.chunkId)}
                className="w-full px-3 py-2 flex items-center justify-between hover:bg-slate-100 transition-colors"
              >
                <div className="flex items-center space-x-2 text-left flex-1">
                  <span className="text-xs font-mono bg-slate-200 text-slate-700 px-2 py-0.5 rounded">
                    #{index + 1}
                  </span>
                  <span className="text-sm font-medium text-slate-800 truncate">
                    {source.documentTitle}
                  </span>
                  {source.articleNumber && (
                    <span className="text-xs text-slate-500">
                      Article {source.articleNumber}
                    </span>
                  )}
                </div>
                <div className="flex items-center space-x-2">
                  <span className="text-xs font-mono text-slate-600 bg-slate-200 px-2 py-0.5 rounded">
                    {typeof source.similarityScore === 'number' 
                      ? source.similarityScore.toFixed(4)
                      : 'N/A'}
                  </span>
                  <svg
                    className={`w-4 h-4 text-slate-600 transition-transform ${
                      isExpanded ? 'rotate-180' : ''
                    }`}
                    fill="none"
                    stroke="currentColor"
                    viewBox="0 0 24 24"
                  >
                    <path
                      strokeLinecap="round"
                      strokeLinejoin="round"
                      strokeWidth={2}
                      d="M19 9l-7 7-7-7"
                    />
                  </svg>
                </div>
              </button>

              {/* Content Preview - Expandable */}
              {isExpanded && (
                <div className="px-3 py-2 border-t border-slate-200 bg-white">
                  <p className="text-xs text-slate-700 leading-relaxed whitespace-pre-wrap">
                    {source.content}
                  </p>
                  {source.articleTitle && (
                    <div className="mt-2 text-xs font-medium text-slate-600">
                      {source.articleTitle}
                    </div>
                  )}
                  <div className="mt-2 flex items-center space-x-3 text-xs text-slate-500">
                    <span title="Chunk ID">ID: {source.chunkId.slice(0, 8)}...</span>
                    <span title="Document ID">
                      Doc: {source.documentId.slice(0, 8)}...
                    </span>
                    {source.sectionType && (
                      <span title="Section Type">Type: {source.sectionType}</span>
                    )}
                  </div>
                </div>
              )}
            </div>
          );
        })}
      </div>
    </div>
  );
}


