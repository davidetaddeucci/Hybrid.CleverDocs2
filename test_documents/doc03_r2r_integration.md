# R2R Integration

## SciPhi AI R2R API Integration
The system integrates with R2R (RAG to Riches) API for advanced document processing and AI capabilities.

### Core Features
- **Document Ingestion**: Automatic processing of uploaded documents
- **Vector Search**: Semantic search across document collections
- **RAG Capabilities**: Retrieval-Augmented Generation for Q&A
- **Conversation Management**: Chat interface with document context

### API Endpoints Used
- `/documents` - Document upload and management
- `/collections` - Collection management
- `/search` - Vector and hybrid search
- `/conversations` - Chat and Q&A functionality

### Rate Limiting
- Document ingestion: 10 requests/second
- Search operations: 20 requests/second
- Embedding generation: 5 requests/second

### Data Flow
1. Document uploaded to WebUI
2. File sent to WebServices for validation
3. WebServices forwards to R2R for processing
4. R2R processes and extracts content
5. Status updates sent via SignalR to WebUI
6. Document becomes searchable and chat-ready

### Error Handling
- Retry logic with exponential backoff
- Dead letter queues for failed processing
- Circuit breaker pattern for API resilience

The R2R integration provides enterprise-grade AI capabilities while maintaining data security and performance.