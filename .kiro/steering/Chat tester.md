---
inclusion: fileMatch
fileMatchPattern: ['**/Chat/**', '**/Conversation**', '**/chat**', '**/conversation**']
---

# Chat System Testing Guidelines

## Test User Credentials
- **Email**: r.antoniucci@microsis.it
- **Password**: Maremmabona1!
- **Role**: User (required for chat functionality)

## Chat Workflow Testing Protocol

### 1. Authentication & Navigation
- Login with test user credentials
- Navigate to `/chat` endpoint
- Verify user has access to chat interface

### 2. Conversation Initialization
- Click "New Conversation" button
- Verify conversation window opens correctly
- Check SignalR connection establishment

### 3. Collection Configuration
- Click conversation "Settings" button
- Select available collection from dropdown
- **CRITICAL**: Always SAVE settings before proceeding
- Verify collection is properly linked to conversation

### 4. R2R Integration Testing
- Use test query: "What are the main subjects of our collection?"
- Monitor complete request/response cycle:
  - Frontend → WebServices API
  - WebServices → R2R API (192.168.1.4:7272/v3/)
  - R2R processing and response
  - WebServices response handling
  - Frontend real-time updates via SignalR

### 5. Validation Points
- **R2R API Reception**: Verify R2R receives conversation commands and questions
- **Response Generation**: Confirm R2R generates appropriate answers
- **WebServices Integration**: Ensure conversation management methods handle R2R responses correctly
- **Real-time Updates**: Validate SignalR delivers responses to frontend immediately
- **Error Handling**: Test failure scenarios and recovery mechanisms

## Key Components to Monitor
- `ConversationController` API endpoints
- `R2RClientService` integration methods
- `ConversationHub` SignalR hub
- R2R API logs at 192.168.1.4:7272
- Database conversation persistence
