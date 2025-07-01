# R2R API Integration Guide

## Overview
Integration with SciPhi AI R2R API for document processing, embedding generation, and intelligent search capabilities.

## Configuration
- **R2R API Endpoint**: http://192.168.1.4:7272
- **Authentication**: API Key based
- **Rate Limits**: 10 req/s document ingestion, 5 req/s embedding, 20 req/s search

## Features
- Document ingestion and processing
- Vector embeddings generation
- Hybrid search capabilities
- Knowledge graph integration
- Conversation management
- RAG (Retrieval Augmented Generation)

## Rate Limiting Strategy
- Token bucket algorithm
- Exponential backoff with jitter
- Circuit breaker pattern
- Priority queues for different operations
- Dead letter queues for failed operations

## Processing Pipeline
1. Document upload to WebUI
2. Validation and metadata extraction
3. Queue for R2R processing
4. R2R ingestion and embedding
5. Status updates via SignalR
6. Document ready for search

This document validates R2R integration processing.