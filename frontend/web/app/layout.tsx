import type { Metadata } from "next";
import "./globals.css";

export const metadata: Metadata = {
  title: "RAG Demo - Retrieval Augmented Generation",
  description: "Demo interface for RAG system with confidence-aware responses",
};

export default function RootLayout({
  children,
}: Readonly<{
  children: React.ReactNode;
}>) {
  return (
    <html lang="en">
      <body className="antialiased">
        {children}
      </body>
    </html>
  );
}
