'use client';

import { useState } from 'react';
import { Source } from '@/types';

interface SourcesPanelProps {
  sources: Source[];
}

export function SourcesPanel({ sources }: SourcesPanelProps) {
  const [expandedSource, setExpandedSource] = useState<string | null>(null);

  const openSource = (chunkId: string) => {
    setExpandedSource(chunkId);
  };

  const closeSource = () => {
    setExpandedSource(null);
  };

  if (!sources || sources.length === 0) {
    return null;
  }

  const expandedSourceData = expandedSource
    ? sources.find((s, idx) => `${s.chunkId}-${idx}` === expandedSource)
    : null;

  return (
    <div className="border-t border-slate-200 dark:border-slate-700 pt-3 mt-3">
      {/* Header */}
      <div className="flex items-center space-x-2 mb-3">
        <svg
          className="w-4 h-4 text-slate-600 dark:text-slate-400"
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
        <span className="text-sm font-semibold text-slate-700 dark:text-slate-300">
          Referenced Sources ({sources.length})
        </span>
      </div>

      {/* Horizontal Scrollable Cards */}
      <div className="flex gap-3 overflow-x-auto pb-2 scrollbar-thin">
        {sources.map((source, index) => {
          const uniqueKey = `${source.chunkId}-${index}`;
          const similarityPercent = typeof source.similarityScore === 'number'
            ? (source.similarityScore * 100).toFixed(1)
            : null;

          return (
            <button
              key={uniqueKey}
              onClick={() => openSource(uniqueKey)}
              className="flex-shrink-0 w-64 bg-slate-50 dark:bg-slate-800 border border-slate-200 dark:border-slate-700 rounded-lg p-3 hover:bg-slate-100 dark:hover:bg-slate-700 hover:border-slate-300 dark:hover:border-slate-600 transition-all shadow-sm hover:shadow group"
            >
              {/* Card Header */}
              <div className="flex items-start justify-between mb-2">
                <span className="text-xs font-mono bg-slate-200 dark:bg-slate-700 text-slate-700 dark:text-slate-300 px-2 py-0.5 rounded">
                  #{index + 1}
                </span>
                {similarityPercent && (
                  <div className="flex items-center space-x-1">
                    <svg
                      className="w-3 h-3 text-green-600 dark:text-green-400"
                      fill="currentColor"
                      viewBox="0 0 20 20"
                    >
                      <path
                        fillRule="evenodd"
                        d="M10 18a8 8 0 100-16 8 8 0 000 16zm3.707-9.293a1 1 0 00-1.414-1.414L9 10.586 7.707 9.293a1 1 0 00-1.414 1.414l2 2a1 1 0 001.414 0l4-4z"
                        clipRule="evenodd"
                      />
                    </svg>
                    <span className="text-xs font-medium text-green-700 dark:text-green-400">
                      {similarityPercent}%
                    </span>
                  </div>
                )}
              </div>

              {/* Document Title */}
              <h4 className="text-sm font-medium text-slate-800 dark:text-slate-200 mb-1 line-clamp-2 text-left">
                {source.documentTitle}
              </h4>

              {/* Content Preview */}
              <p className="text-xs text-slate-600 dark:text-slate-400 line-clamp-3 text-left">
                {source.content}
              </p>

              {/* View More Indicator */}
              <div className="mt-2 flex items-center text-xs text-blue-600 dark:text-blue-400 font-medium group-hover:text-blue-700 dark:group-hover:text-blue-300">
                <span>View full content</span>
                <svg
                  className="w-3 h-3 ml-1"
                  fill="none"
                  stroke="currentColor"
                  viewBox="0 0 24 24"
                >
                  <path
                    strokeLinecap="round"
                    strokeLinejoin="round"
                    strokeWidth={2}
                    d="M9 5l7 7-7 7"
                  />
                </svg>
              </div>
            </button>
          );
        })}
      </div>

      {/* Expanded Source Modal */}
      {expandedSource && expandedSourceData && (
        <div
          className="fixed inset-0 bg-black/50 backdrop-blur-sm z-50 flex items-center justify-center p-4"
          onClick={closeSource}
        >
          <div
            className="bg-white dark:bg-slate-800 rounded-xl shadow-2xl max-w-3xl w-full max-h-[80vh] overflow-hidden"
            onClick={(e) => e.stopPropagation()}
          >
            {/* Modal Header */}
            <div className="flex items-start justify-between p-4 border-b border-slate-200 dark:border-slate-700">
              <div className="flex-1">
                <h3 className="text-lg font-semibold text-slate-900 dark:text-slate-100">
                  {expandedSourceData.documentTitle}
                </h3>
                {expandedSourceData.articleNumber && (
                  <p className="text-sm text-slate-600 dark:text-slate-400 mt-1">
                    Article {expandedSourceData.articleNumber}
                    {expandedSourceData.articleTitle && ` - ${expandedSourceData.articleTitle}`}
                  </p>
                )}
              </div>
              <button
                onClick={closeSource}
                className="ml-4 text-slate-400 hover:text-slate-600 dark:hover:text-slate-200 transition-colors"
              >
                <svg
                  className="w-6 h-6"
                  fill="none"
                  stroke="currentColor"
                  viewBox="0 0 24 24"
                >
                  <path
                    strokeLinecap="round"
                    strokeLinejoin="round"
                    strokeWidth={2}
                    d="M6 18L18 6M6 6l12 12"
                  />
                </svg>
              </button>
            </div>

            {/* Modal Content */}
            <div className="p-4 overflow-y-auto max-h-[60vh]">
              <p className="text-sm text-slate-800 dark:text-slate-200 leading-relaxed whitespace-pre-wrap">
                {expandedSourceData.content}
              </p>
            </div>

            {/* Modal Footer */}
            <div className="p-4 border-t border-slate-200 dark:border-slate-700 bg-slate-50 dark:bg-slate-900/50">
              <div className="flex flex-wrap items-center gap-3 text-xs text-slate-600 dark:text-slate-400">
                {typeof expandedSourceData.similarityScore === 'number' && (
                  <div className="flex items-center space-x-1">
                    <svg
                      className="w-4 h-4 text-green-600 dark:text-green-400"
                      fill="currentColor"
                      viewBox="0 0 20 20"
                    >
                      <path
                        fillRule="evenodd"
                        d="M10 18a8 8 0 100-16 8 8 0 000 16zm3.707-9.293a1 1 0 00-1.414-1.414L9 10.586 7.707 9.293a1 1 0 00-1.414 1.414l2 2a1 1 0 001.414 0l4-4z"
                        clipRule="evenodd"
                      />
                    </svg>
                    <span className="font-medium">
                      {(expandedSourceData.similarityScore * 100).toFixed(1)}% match
                    </span>
                  </div>
                )}
                <span title="Chunk ID">
                  ID: {expandedSourceData.chunkId.slice(0, 8)}...
                </span>
                <span title="Document ID">
                  Doc: {expandedSourceData.documentId.slice(0, 8)}...
                </span>
                {expandedSourceData.sectionType && (
                  <span title="Section Type">
                    Type: {expandedSourceData.sectionType}
                  </span>
                )}
              </div>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
