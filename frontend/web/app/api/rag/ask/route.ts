import { NextRequest, NextResponse } from 'next/server';

/**
 * Proxy endpoint to avoid CORS issues
 * Forwards requests from frontend to backend API Gateway
 */
export async function POST(request: NextRequest) {
  try {
    const body = await request.json();

    // Forward request to backend API Gateway
    const response = await fetch('http://localhost:8080/api/rag/ask', {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify(body),
    });

    if (!response.ok) {
      const errorText = await response.text();
      return NextResponse.json(
        { error: 'Backend request failed', details: errorText },
        { status: response.status }
      );
    }

    const data = await response.json();
    return NextResponse.json(data);
  } catch (error) {
    console.error('Proxy error:', error);
    return NextResponse.json(
      { error: 'Failed to connect to backend', message: (error as Error).message },
      { status: 500 }
    );
  }
}


