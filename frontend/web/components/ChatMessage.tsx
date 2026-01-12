import { Message } from '@/types';
import { ConfidenceBadge } from './ConfidenceBadge';
import { SourcesPanel } from './SourcesPanel';
import { useTypingEffect } from '@/hooks/useTypingEffect';

interface ChatMessageProps {
  message: Message;
  enableTyping?: boolean;
}

export function ChatMessage({ message, enableTyping = true }: ChatMessageProps) {
  const isUser = message.role === 'user';
  
  // Apply typing effect only to assistant messages
  // CRITICAL: Pass message.id as stable identifier to prevent restarts
  const { displayedText, isTyping } = useTypingEffect({
    text: message.content,
    messageId: message.id, // Stable identifier - prevents typing restart
    enabled: enableTyping && !isUser,
    speed: 3,
    interval: 20,
  });

  return (
    <div className={`flex ${isUser ? 'justify-end' : 'justify-start'}`}>
      <div className={`max-w-3xl ${isUser ? 'w-auto' : 'w-full'}`}>
        {/* Message Bubble */}
        <div
          className={`rounded-lg px-4 py-3 ${
            isUser
              ? 'bg-blue-600 dark:bg-blue-700 text-white'
              : 'bg-white dark:bg-slate-800 border border-slate-200 dark:border-slate-700 shadow-sm'
          }`}
        >
          {/* User Message */}
          {isUser && (
            <div className="flex items-start space-x-2">
              <div className="flex-shrink-0">
                <div className="w-6 h-6 bg-white/20 rounded-full flex items-center justify-center">
                  <svg
                    className="w-4 h-4"
                    fill="currentColor"
                    viewBox="0 0 20 20"
                  >
                    <path
                      fillRule="evenodd"
                      d="M10 9a3 3 0 100-6 3 3 0 000 6zm-7 9a7 7 0 1114 0H3z"
                      clipRule="evenodd"
                    />
                  </svg>
                </div>
              </div>
              <p className="flex-1 text-sm leading-relaxed">{message.content}</p>
            </div>
          )}

          {/* Assistant Message */}
          {!isUser && (
            <div className="space-y-3">
              <div className="flex items-start space-x-2">
                <div className="flex-shrink-0">
                  <div className="w-6 h-6 bg-slate-100 dark:bg-slate-700 rounded-full flex items-center justify-center">
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
                        d="M9.663 17h4.673M12 3v1m6.364 1.636l-.707.707M21 12h-1M4 12H3m3.343-5.657l-.707-.707m2.828 9.9a5 5 0 117.072 0l-.548.547A3.374 3.374 0 0014 18.469V19a2 2 0 11-4 0v-.531c0-.895-.356-1.754-.988-2.386l-.548-.547z"
                      />
                    </svg>
                  </div>
                </div>
                <div className="flex-1">
                  <p className="text-sm leading-relaxed text-slate-800 dark:text-slate-200 whitespace-pre-wrap">
                    {displayedText}
                    {isTyping && (
                      <span className="inline-block w-1 h-4 ml-0.5 bg-slate-800 dark:bg-slate-200 animate-pulse" />
                    )}
                  </p>
                </div>
              </div>

              {/* Confidence Badge */}
              {message.confidence && (
                <ConfidenceBadge confidence={message.confidence} />
              )}

              {/* Sources Panel */}
              {message.sources && message.sources.length > 0 && (
                <SourcesPanel sources={message.sources} />
              )}
            </div>
          )}
        </div>

        {/* Timestamp */}
        <div
          className={`text-xs text-slate-500 mt-1 ${
            isUser ? 'text-right' : 'text-left'
          }`}
        >
          {message.timestamp.toLocaleTimeString()}
        </div>
      </div>
    </div>
  );
}


