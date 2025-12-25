import { NextRequest, NextResponse } from 'next/server';

/**
 * PDF upload proxy endpoint
 * Simple proxy without streaming - just waits for backend to complete
 */
export async function POST(request: NextRequest) {
  try {
    console.log('[API Route] Received PDF upload request');
    const formData = await request.formData();
    
    const file = formData.get('file');
    const title = formData.get('title');
    
    console.log('[API Route] File:', file ? 'Present' : 'Missing');
    console.log('[API Route] Title:', title);

    // Forward to backend - NO TIMEOUT (let it run as long as needed)
    console.log('[API Route] Forwarding to backend: http://localhost:8080/api/ingest/pdf');
    
    const response = await fetch('http://localhost:8080/api/ingest/pdf', {
      method: 'POST',
      body: formData,
    });

    console.log('[API Route] Backend response status:', response.status);

    if (!response.ok) {
      const errorText = await response.text();
      console.error('[API Route] Backend error:', errorText);
      return NextResponse.json(
        { error: 'PDF upload failed', details: errorText },
        { status: response.status }
      );
    }

    const data = await response.json();
    console.log('[API Route] Success! Chunk count:', data.chunkCount);
    
    return NextResponse.json(data);
  } catch (error: any) {
    console.error('[API Route] Exception:', error);
    return NextResponse.json(
      { error: 'Failed to upload PDF', message: error.message, stack: error.stack },
      { status: 500 }
    );
  }
}
