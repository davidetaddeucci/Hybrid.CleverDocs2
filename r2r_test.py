#!/usr/bin/env python3
"""
R2R Proof-of-Concept Testing Script
Tests document ingestion and retrieval capabilities with existing R2R instance at 192.168.1.4:7272
"""

import os
import time
from r2r import R2RClient

# Configuration
R2R_BASE_URL = "http://192.168.1.4:7272"

def test_r2r_connection():
    """Test basic connection to R2R instance"""
    print("🔍 Testing R2R Connection...")
    try:
        client = R2RClient(R2R_BASE_URL)
        health = client.health()
        print(f"✅ R2R Health Status: {health}")
        return client
    except Exception as e:
        print(f"❌ Connection failed: {e}")
        return None

def test_document_ingestion(client, file_path):
    """Test document ingestion with Unstructured.io parsing"""
    print(f"\n📄 Testing Document Ingestion: {file_path}")
    try:
        # Check if file exists
        if not os.path.exists(file_path):
            print(f"❌ File not found: {file_path}")
            return None
            
        # Ingest document
        print("⏳ Starting ingestion...")
        start_time = time.time()
        
        result = client.documents.create(
            file_path=file_path,
            metadata={"source": "r2r_test", "test_type": "poc"}
        )
        
        end_time = time.time()
        processing_time = end_time - start_time
        
        print(f"✅ Document ingested successfully!")
        print(f"📊 Processing time: {processing_time:.2f} seconds")
        print(f"📋 Document ID: {result.get('document_id', 'N/A')}")
        print(f"📋 Result: {result}")
        
        return result.get('document_id')
        
    except Exception as e:
        print(f"❌ Ingestion failed: {e}")
        return None

def test_search_capabilities(client, query, document_id=None):
    """Test search and retrieval capabilities"""
    print(f"\n🔍 Testing Search: '{query}'")
    try:
        # Perform search
        search_results = client.retrieval.search(
            query=query,
            limit=5
        )
        
        print(f"✅ Search completed!")
        print(f"📊 Found {len(search_results.get('results', []))} results")
        
        for i, result in enumerate(search_results.get('results', [])[:3]):
            print(f"\n📄 Result {i+1}:")
            print(f"   Score: {result.get('score', 'N/A')}")
            print(f"   Text: {result.get('text', 'N/A')[:200]}...")
            print(f"   Metadata: {result.get('metadata', {})}")
            
        return search_results
        
    except Exception as e:
        print(f"❌ Search failed: {e}")
        return None

def test_rag_completion(client, query):
    """Test RAG completion with LLM"""
    print(f"\n🤖 Testing RAG Completion: '{query}'")
    try:
        # Perform RAG completion
        completion = client.retrieval.rag(
            query=query,
            use_hybrid_search=True
        )
        
        print(f"✅ RAG completion successful!")
        print(f"📝 Response: {completion.get('results', {}).get('completion', 'N/A')}")
        
        # Show sources if available
        sources = completion.get('results', {}).get('search_results', [])
        if sources:
            print(f"📚 Sources used: {len(sources)}")
            for i, source in enumerate(sources[:2]):
                print(f"   Source {i+1}: {source.get('metadata', {}).get('title', 'Unknown')}")
        
        return completion
        
    except Exception as e:
        print(f"❌ RAG completion failed: {e}")
        return None

def list_documents(client):
    """List all documents in R2R"""
    print("\n📚 Listing Documents...")
    try:
        documents = client.documents.list()
        print(f"✅ Found {len(documents.get('results', []))} documents")
        
        for i, doc in enumerate(documents.get('results', [])[:5):
            print(f"\n📄 Document {i+1}:")
            print(f"   ID: {doc.get('id', 'N/A')}")
            print(f"   Title: {doc.get('metadata', {}).get('title', 'N/A')}")
            print(f"   Type: {doc.get('type', 'N/A')}")
            print(f"   Created: {doc.get('created_at', 'N/A')}")
            
        return documents
        
    except Exception as e:
        print(f"❌ Document listing failed: {e}")
        return None

def main():
    """Main testing function"""
    print("🚀 R2R Proof-of-Concept Testing")
    print("=" * 50)
    
    # Test connection
    client = test_r2r_connection()
    if not client:
        return
    
    # List existing documents
    list_documents(client)
    
    # Test with a sample file (you can modify this path)
    test_file = "sample_document.txt"
    
    # Create a sample document if it doesn't exist
    if not os.path.exists(test_file):
        print(f"\n📝 Creating sample document: {test_file}")
        with open(test_file, 'w', encoding='utf-8') as f:
            f.write("""
Sample Document for R2R Testing

This is a test document to evaluate R2R's document ingestion capabilities.
It contains information about:

1. Document Processing: How R2R handles various file formats
2. Unstructured.io Integration: Parsing capabilities for complex documents
3. Vector Search: Semantic search functionality
4. RAG Capabilities: Retrieval-Augmented Generation features

Key Features to Test:
- PDF parsing with tables and images
- Multi-modal content extraction
- Hybrid search (semantic + keyword)
- Real-time ingestion workflows
- Knowledge graph construction

This document will help us understand how R2R compares to our current
custom implementation using RabbitMQ, custom parsers, and PostgreSQL.
            """)
        print(f"✅ Sample document created")
    
    # Test document ingestion
    document_id = test_document_ingestion(client, test_file)
    
    # Wait a moment for processing
    if document_id:
        print("\n⏳ Waiting for document processing...")
        time.sleep(5)
    
    # Test search capabilities
    test_search_capabilities(client, "document processing capabilities")
    test_search_capabilities(client, "R2R features")
    
    # Test RAG completion
    test_rag_completion(client, "What are the key features mentioned in the document?")
    test_rag_completion(client, "How does R2R compare to custom implementations?")
    
    print("\n🎉 Testing completed!")
    print("=" * 50)

if __name__ == "__main__":
    main()
