# LLM Configuration Troubleshooting Guide

## Overview
This document provides comprehensive troubleshooting for LLM (Large Language Model) configuration issues in the Hybrid.CleverDocs2 system, specifically related to R2R API integration and OpenAI configuration.

## Issue: Chat Returns Fallback Responses Instead of AI Responses

### Symptoms
- Chat interface displays fallback messages like "I apologize, but I'm unable to provide a response at this time"
- R2R conversation shows only user messages, no assistant responses
- Chat infrastructure works correctly (messages appear in real-time)
- Document collections are properly indexed in R2R

### Root Cause Analysis (January 8, 2025)

**INVESTIGATION SUMMARY**:
- ✅ **R2R Collection Status**: 71 documents successfully indexed and searchable
- ✅ **Collection Mapping**: Correct WebUI ↔ R2R collection mapping verified
- ✅ **Chat Infrastructure**: SignalR, WebServices, database persistence all operational
- ✅ **R2R API Communication**: Message requests accepted successfully
- ❌ **LLM Configuration**: Missing OpenAI API key environment variable

**DETAILED FINDINGS**:

1. **Collection Verification**:
   - WebUI Collection: "Skin AI Collection" (`aa160d16-1b73-40f0-aac2-a89998134d29`)
   - R2R Collection: `122fdf6a-e116-546b-a8f6-e4cb2e2c0a09`
   - Document Count: 71 documents with full summaries and content
   - Mapping Status: ✅ Correct via `r2RCollectionId` field

2. **R2R API Analysis**:
   - Service URL: `http://192.168.1.4:7272/v3`
   - Authentication: Working with `super-secret-admin-key`
   - Document Retrieval: Successful with detailed summaries
   - LLM Configuration: Uses LiteLLM with OpenAI models

3. **Chat Flow Analysis**:
   ```
   User Message → ChatHub → R2R API → LLM Provider (MISSING) → Fallback Response
   ```

### Solution: Configure OpenAI API Key

**REQUIRED ENVIRONMENT VARIABLE**:
```bash
OPENAI_API_KEY=your_openai_api_key_here
```

### Implementation Methods

#### Option 1: Docker Container
```bash
# Stop existing R2R container
docker stop r2r-container

# Start with OpenAI API key
docker run -d --name r2r-container \
  -p 7272:7272 \
  -e OPENAI_API_KEY=your_openai_api_key_here \
  r2r-image
```

#### Option 2: Docker Compose
```yaml
# Add to docker-compose.yml
services:
  r2r:
    image: r2r-image
    ports:
      - "7272:7272"
    environment:
      - OPENAI_API_KEY=your_openai_api_key_here
```

#### Option 3: System Service
```bash
# Add to environment file
echo "OPENAI_API_KEY=your_openai_api_key_here" >> /etc/environment

# Restart R2R service
systemctl restart r2r-service
```

### Verification Steps

#### 1. Test R2R API Response Generation
```bash
curl -X POST -H "Authorization: super-secret-admin-key" \
  -H "Content-Type: application/json" \
  -d '{"content": "What can you tell me about skin disease classification?", "role": "user", "collection_ids": ["122fdf6a-e116-546b-a8f6-e4cb2e2c0a09"]}' \
  "http://192.168.1.4:7272/v3/conversations/f805b4f3-47b3-44a4-af72-93bd648e0c0b/messages"
```

#### 2. Verify AI Response Generation
```bash
curl -H "Authorization: super-secret-admin-key" \
  "http://192.168.1.4:7272/v3/conversations/f805b4f3-47b3-44a4-af72-93bd648e0c0b"
```

**Expected Result**: Conversation should show both user and assistant messages.

#### 3. Test WebUI Chat Interface
1. Login to WebUI: `http://localhost:5252`
2. Navigate to Chat interface
3. Select "Skin AI Collection" 
4. Send test message: "What can you tell me about skin disease classification?"
5. **Expected**: AI-generated response appears (not fallback message)

### System Configuration Details

**R2R LLM Settings**:
- Quality LLM: `openai/gpt-4o-mini`
- Fast LLM: `openai/gpt-4o-mini` 
- Provider: LiteLLM (requires OPENAI_API_KEY)
- Temperature: 0.7
- Max Tokens: 1000

**WebUI Configuration**:
- Model: `gpt-4o-mini` (configured in ChatHub)
- Authentication: JWT tokens with HttpOnly cookies
- Real-time: SignalR for message delivery

### Expected Outcome

After proper OpenAI API key configuration:
- ✅ R2R generates AI responses using GPT-4o-mini
- ✅ Chat conversations show both user and assistant messages
- ✅ WebUI displays real AI responses instead of fallback messages
- ✅ Complete end-to-end chat functionality operational

### Additional Diagnostics

**Check R2R System Status**:
```bash
curl -H "Authorization: super-secret-admin-key" "http://192.168.1.4:7272/v3/system/status"
```

**Check R2R Configuration**:
```bash
curl -H "Authorization: super-secret-admin-key" "http://192.168.1.4:7272/v3/system/settings"
```

**Check Collection Documents**:
```bash
curl -H "Authorization: super-secret-admin-key" "http://192.168.1.4:7272/v3/collections/122fdf6a-e116-546b-a8f6-e4cb2e2c0a09/documents?limit=5"
```

## Contact Information

For additional support or questions regarding LLM configuration:
- Review system logs for R2R service startup
- Verify OpenAI API key validity
- Check network connectivity between WebServices and R2R API
- Ensure proper authentication headers in API requests

---

**Document Version**: 1.0  
**Last Updated**: January 8, 2025  
**Investigation Date**: January 8, 2025
