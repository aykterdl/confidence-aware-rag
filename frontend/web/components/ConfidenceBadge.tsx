import { ConfidenceInfo } from '@/types';

interface ConfidenceBadgeProps {
  confidence: ConfidenceInfo;
}

export function ConfidenceBadge({ confidence }: ConfidenceBadgeProps) {
  const { level, maxSimilarity, averageSimilarity, explanation } = confidence;

  // Style variants based on confidence level
  const variants = {
    high: {
      bg: 'bg-green-50',
      border: 'border-green-200',
      text: 'text-green-800',
      icon: '✓',
      label: 'High Confidence',
      description: 'Strong match found in documents',
    },
    low: {
      bg: 'bg-yellow-50',
      border: 'border-yellow-200',
      text: 'text-yellow-800',
      icon: '⚠',
      label: 'Low Confidence',
      description: 'Answer may be incomplete or uncertain',
    },
    none: {
      bg: 'bg-red-50',
      border: 'border-red-200',
      text: 'text-red-800',
      icon: '✕',
      label: 'No Relevant Information',
      description: 'Question not sufficiently relevant to available documents',
    },
  };

  const variant = variants[level];

  return (
    <div
      className={`${variant.bg} ${variant.border} ${variant.text} border rounded-lg p-3`}
    >
      <div className="flex items-start space-x-2">
        <span className="text-lg">{variant.icon}</span>
        <div className="flex-1">
          <div className="flex items-center justify-between">
            <span className="font-semibold text-sm">{variant.label}</span>
            <div className="text-xs space-x-2">
              <span title="Maximum similarity score">
                Max: {maxSimilarity.toFixed(4)}
              </span>
              <span title="Average similarity score">
                Avg: {averageSimilarity.toFixed(4)}
              </span>
            </div>
          </div>
          <p className="text-xs mt-1 opacity-90">
            {explanation || variant.description}
          </p>
        </div>
      </div>
    </div>
  );
}


