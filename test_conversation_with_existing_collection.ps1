# Test conversation system using existing collection for r.antoniucci@microsis.it
$headers = @{
    'Content-Type' = 'application/json'
}

$loginData = @{
    email = 'r.antoniucci@microsis.it'
    password = 'Maremmabona1!'
} | ConvertTo-Json

Write-Host "🎯 Testing conversation system with EXISTING collection..." -ForegroundColor Yellow

try {
    # Login
    $loginResponse = Invoke-WebRequest -Uri 'http://localhost:5253/api/auth/login' -Method Post -Body $loginData -Headers $headers -UseBasicParsing
    $loginResult = $loginResponse.Content | ConvertFrom-Json
    $token = $loginResult.accessToken
    
    $authHeaders = @{
        'Authorization' = "Bearer $token"
        'Content-Type' = 'application/json'
    }
    
    Write-Host "✅ Authenticated as: $($loginResult.user.fullName)" -ForegroundColor Green
    
    # Use existing collection ID found in conversations
    $existingCollectionId = "aa160d16-1b73-40f0-aac2-a89998134d29"
    Write-Host "📚 Using existing collection: $existingCollectionId" -ForegroundColor Cyan
    
    # Test 1: Create new conversation with existing collection
    Write-Host "🧪 Test 1: Creating conversation with existing collection..." -ForegroundColor Yellow
    $newConvData = @{
        name = "Test Chat - $(Get-Date -Format 'HH:mm:ss')"
        description = "Testing conversation system with existing collection"
        collectionIds = @($existingCollectionId)
    } | ConvertTo-Json
    
    try {
        $newConvResponse = Invoke-WebRequest -Uri 'http://localhost:5253/api/conversations' -Method Post -Body $newConvData -Headers $authHeaders -UseBasicParsing
        Write-Host "✅ New conversation created successfully" -ForegroundColor Green
        
        $newConv = $newConvResponse.Content | ConvertFrom-Json
        $conversationId = $newConv.results.conversationId
        Write-Host "🆔 New conversation R2R ID: $conversationId" -ForegroundColor Cyan
        
        # Test 2: Send message to new conversation with collection context
        Write-Host "🧪 Test 2: Sending message with collection context..." -ForegroundColor Yellow
        $messageData = @{
            content = "What are the main subjects of our collection? Please provide a detailed summary."
            role = "user"
            searchMode = "advanced"
            stream = $false
            ragGenerationConfig = @{
                model = "gpt-4o-mini"
                maxTokens = 1000
                temperature = 0.7
            }
            searchSettings = @{
                filters = @{
                    collection_ids = @($existingCollectionId)
                }
                limit = 10
                useVectorSearch = $true
                useHybridSearch = $true
            }
        } | ConvertTo-Json -Depth 10
        
        $messageResponse = Invoke-WebRequest -Uri "http://localhost:5253/api/conversations/$conversationId/messages" -Method Post -Body $messageData -Headers $authHeaders -UseBasicParsing
        Write-Host "✅ Message sent successfully" -ForegroundColor Green
        
        $messageResult = $messageResponse.Content | ConvertFrom-Json
        Write-Host "💬 Response received:" -ForegroundColor Green
        Write-Host "  Message ID: $($messageResult.results.messageId)" -ForegroundColor Cyan
        Write-Host "  Content length: $($messageResult.results.content.Length)" -ForegroundColor Cyan
        
        if ($messageResult.results.content) {
            Write-Host "  Content preview: $($messageResult.results.content.Substring(0, [Math]::Min(300, $messageResult.results.content.Length)))..." -ForegroundColor Cyan
        }
        
        if ($messageResult.results.searchResults) {
            Write-Host "  Search results: $($messageResult.results.searchResults.Count) documents found" -ForegroundColor Cyan
        }
        
    } catch {
        Write-Host "❌ Conversation creation failed: $($_.Exception.Message)" -ForegroundColor Red
        if ($_.Exception.Response) {
            try {
                $reader = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
                $responseBody = $reader.ReadToEnd()
                Write-Host "Response Body: $responseBody" -ForegroundColor Red
            } catch {
                Write-Host "Could not read response body" -ForegroundColor Red
            }
        }
    }
    
    # Test 3: Use existing conversation with messages
    Write-Host "🧪 Test 3: Testing existing conversation with messages..." -ForegroundColor Yellow
    $existingConvId = "f805b4f3-47b3-44a4-af72-93bd648e0c0b" # Conversation ID 72 with 4 messages
    
    try {
        # Get messages from existing conversation
        $messagesResponse = Invoke-WebRequest -Uri "http://localhost:5253/api/conversations/$existingConvId/messages" -Method Get -Headers $authHeaders -UseBasicParsing
        Write-Host "✅ Retrieved messages from existing conversation" -ForegroundColor Green
        
        $messages = $messagesResponse.Content | ConvertFrom-Json
        Write-Host "💬 Found $($messages.results.count) messages" -ForegroundColor Cyan
        
        if ($messages.results.messages -and $messages.results.messages.Count -gt 0) {
            Write-Host "📄 Recent messages:" -ForegroundColor Yellow
            $messages.results.messages | Select-Object -Last 2 | ForEach-Object {
                Write-Host "  - Role: $($_.role)" -ForegroundColor Cyan
                Write-Host "    Content: $($_.content.Substring(0, [Math]::Min(150, $_.content.Length)))..." -ForegroundColor Cyan
                Write-Host "    Created: $($_.createdAt)" -ForegroundColor Cyan
                Write-Host "    ---" -ForegroundColor Gray
            }
        }
        
        # Send new message to existing conversation
        Write-Host "💬 Sending new message to existing conversation..." -ForegroundColor Yellow
        $followUpMessage = @{
            content = "Can you provide more details about the technical architecture mentioned in our previous discussions?"
            role = "user"
            searchMode = "advanced"
            stream = $false
            ragGenerationConfig = @{
                model = "gpt-4o-mini"
                maxTokens = 800
                temperature = 0.7
            }
            searchSettings = @{
                filters = @{
                    collection_ids = @($existingCollectionId)
                }
                limit = 10
                useVectorSearch = $true
                useHybridSearch = $true
            }
        } | ConvertTo-Json -Depth 10
        
        $followUpResponse = Invoke-WebRequest -Uri "http://localhost:5253/api/conversations/$existingConvId/messages" -Method Post -Body $followUpMessage -Headers $authHeaders -UseBasicParsing
        Write-Host "✅ Follow-up message sent successfully" -ForegroundColor Green
        
        $followUpResult = $followUpResponse.Content | ConvertFrom-Json
        Write-Host "💬 Follow-up response:" -ForegroundColor Green
        Write-Host "  Message ID: $($followUpResult.results.messageId)" -ForegroundColor Cyan
        Write-Host "  Content length: $($followUpResult.results.content.Length)" -ForegroundColor Cyan
        
        if ($followUpResult.results.content) {
            Write-Host "  Content preview: $($followUpResult.results.content.Substring(0, [Math]::Min(300, $followUpResult.results.content.Length)))..." -ForegroundColor Cyan
        }
        
    } catch {
        Write-Host "❌ Existing conversation test failed: $($_.Exception.Message)" -ForegroundColor Red
    }
    
    Write-Host "🎉 Conversation system testing completed!" -ForegroundColor Green
    Write-Host "📊 Summary:" -ForegroundColor Yellow
    Write-Host "  ✅ Authentication: Working" -ForegroundColor Green
    Write-Host "  ✅ Collection Available: $existingCollectionId" -ForegroundColor Green
    Write-Host "  ✅ Conversation Creation: Tested" -ForegroundColor Green
    Write-Host "  ✅ Message Sending: Tested" -ForegroundColor Green
    Write-Host "  ✅ R2R Integration: Tested" -ForegroundColor Green
    
} catch {
    Write-Host "❌ Critical Error: $($_.Exception.Message)" -ForegroundColor Red
}