import { useState, useEffect, useRef } from 'react';

interface UseTypingEffectOptions {
  text: string;
  messageId: string; // Stable identifier to track message identity
  speed?: number; // characters per interval
  interval?: number; // ms between updates
  enabled?: boolean; // whether to enable the effect
}

/**
 * Idempotent typing effect hook.
 * 
 * KEY GUARANTEES:
 * - Types each message exactly once
 * - Never resets or restarts after completion
 * - Stable across parent re-renders
 * - Tied to message ID for identity
 */
export function useTypingEffect({
  text,
  messageId,
  speed = 2,
  interval = 30,
  enabled = true,
}: UseTypingEffectOptions) {
  const [displayedText, setDisplayedText] = useState('');
  const [isTyping, setIsTyping] = useState(false);
  
  // Track completion state to prevent restarts
  const completedRef = useRef<Set<string>>(new Set());
  const lastMessageIdRef = useRef(messageId);

  useEffect(() => {
    // If message ID changed, reset completion tracking for this new message
    if (lastMessageIdRef.current !== messageId) {
      lastMessageIdRef.current = messageId;
    }

    // If this message has already been fully typed, show full text and exit
    if (completedRef.current.has(messageId)) {
      setDisplayedText(text);
      setIsTyping(false);
      return;
    }

    // If disabled or empty text, show immediately and mark as completed
    if (!enabled || !text) {
      setDisplayedText(text);
      setIsTyping(false);
      completedRef.current.add(messageId);
      return;
    }

    // Start typing animation
    setDisplayedText('');
    setIsTyping(true);
    
    let currentIndex = 0;
    let isCancelled = false;

    const typeNextChars = () => {
      if (isCancelled) return;
      
      // Type 'speed' characters at a time
      currentIndex = Math.min(currentIndex + speed, text.length);
      setDisplayedText(text.substring(0, currentIndex));
      
      if (currentIndex < text.length) {
        // Schedule next typing update
        setTimeout(typeNextChars, interval);
      } else {
        // Typing completed - mark as done and never restart
        setIsTyping(false);
        completedRef.current.add(messageId);
      }
    };

    // Start first typing update
    const timeoutId = setTimeout(typeNextChars, interval);

    // Cleanup on unmount or dependency change
    return () => {
      isCancelled = true;
      clearTimeout(timeoutId);
    };
  }, [text, messageId, enabled, speed, interval]); // CRITICAL: No state in dependencies

  return { displayedText, isTyping };
}
