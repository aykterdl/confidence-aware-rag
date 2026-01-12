import { ConfidenceInfo } from '@/types';

interface ConfidenceBadgeProps {
  confidence: ConfidenceInfo;
}

export function ConfidenceBadge({ confidence }: ConfidenceBadgeProps) {
  const { level, maxSimilarity, averageSimilarity, explanation } = confidence;

  // Style variants based on confidence level (theme-aware)
  const variants = {
    high: {
      bg: 'bg-green-50 dark:bg-green-900/20',
      border: 'border-green-200 dark:border-green-800',
      text: 'text-green-800 dark:text-green-200',
      iconBg: 'bg-green-100 dark:bg-green-800/30',
      icon: (
        <svg className="w-4 h-4" fill="currentColor" viewBox="0 0 20 20">
          <path
            fillRule="evenodd"
            d="M10 18a8 8 0 100-16 8 8 0 000 16zm3.707-9.293a1 1 0 00-1.414-1.414L9 10.586 7.707 9.293a1 1 0 00-1.414 1.414l2 2a1 1 0 001.414 0l4-4z"
            clipRule="evenodd"
          />
        </svg>
      ),
      label: 'High Confidence',
      description: 'Strong match found in documents',
    },
    low: {
      bg: 'bg-yellow-50 dark:bg-yellow-900/20',
      border: 'border-yellow-200 dark:border-yellow-800',
      text: 'text-yellow-800 dark:text-yellow-200',
      iconBg: 'bg-yellow-100 dark:bg-yellow-800/30',
      icon: (
        <svg className="w-4 h-4" fill="currentColor" viewBox="0 0 20 20">
          <path
            fillRule="evenodd"
            d="M8.257 3.099c.765-1.36 2.722-1.36 3.486 0l5.58 9.92c.75 1.334-.213 2.98-1.742 2.98H4.42c-1.53 0-2.493-1.646-1.743-2.98l5.58-9.92zM11 13a1 1 0 11-2 0 1 1 0 012 0zm-1-8a1 1 0 00-1 1v3a1 1 0 002 0V6a1 1 0 00-1-1z"
            clipRule="evenodd"
          />
        </svg>
      ),
      label: 'Moderate Confidence',
      description: 'Answer may be incomplete or uncertain',
    },
    none: {
      bg: 'bg-slate-50 dark:bg-slate-800',
      border: 'border-slate-200 dark:border-slate-700',
      text: 'text-slate-700 dark:text-slate-300',
      iconBg: 'bg-slate-100 dark:bg-slate-700',
      icon: (
        <svg className="w-4 h-4" fill="currentColor" viewBox="0 0 20 20">
          <path
            fillRule="evenodd"
            d="M18 10a8 8 0 11-16 0 8 8 0 0116 0zm-7 4a1 1 0 11-2 0 1 1 0 012 0zm-1-9a1 1 0 00-1 1v4a1 1 0 102 0V6a1 1 0 00-1-1z"
            clipRule="evenodd"
          />
        </svg>
      ),
      label: 'No Relevant Information',
      description: 'Question not sufficiently relevant to available documents',
    },
  };

  const variant = variants[level];

  return (
    <div
      className={`${variant.bg} ${variant.border} ${variant.text} border rounded-lg p-3 shadow-sm`}
    >
      <div className="flex items-start space-x-3">
        {/* Icon */}
        <div className={`${variant.iconBg} rounded-full p-1.5 flex-shrink-0`}>
          {variant.icon}
        </div>

        {/* Content */}
        <div className="flex-1 min-w-0">
          <div className="flex items-center justify-between gap-2 mb-1">
            <span className="font-semibold text-sm">{variant.label}</span>
            {/* Similarity scores (subtle, only for non-none levels) */}
            {level !== 'none' && (
              <div className="flex items-center gap-2 text-xs opacity-75">
                <span title="Match strength" className="flex items-center gap-1">
                  <svg
                    className="w-3 h-3"
                    fill="none"
                    stroke="currentColor"
                    viewBox="0 0 24 24"
                  >
                    <path
                      strokeLinecap="round"
                      strokeLinejoin="round"
                      strokeWidth={2}
                      d="M13 7h8m0 0v8m0-8l-8 8-4-4-6 6"
                    />
                  </svg>
                  {(maxSimilarity * 100).toFixed(1)}%
                </span>
              </div>
            )}
          </div>
          <p className="text-xs leading-relaxed opacity-90">
            {explanation || variant.description}
          </p>
        </div>
      </div>
    </div>
  );
}
