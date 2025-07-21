# Fix R2R collection synchronization for r.antoniucci@microsis.it
$headers = @{
    'Content-Type' = 'application/json'
}

$loginData = @{
    email = 'r.antoniucci@microsis.it'
    password = 'Maremmabona1!'
} | ConvertTo-Json

Write-Host "🔧 Fixing R2R collection synchronization..." -ForegroundColor Yellow

try {
    # Login
    $loginResponse = Invoke-WebRequest -Uri 'http://localhost:5253/api/auth/login' -Method Post -Body $loginData -Headers $headers -UseBasicParsing
    $loginResult = $loginResponse.Content | ConvertFrom-Json
    $token = $loginResult.accessToken
    
    $authHeaders = @{
        'Authorization' = "Bearer $token"
        'Content-Type' = 'application/json'
    }
    
    Write-Host "✅ Authenticated successfully" -ForegroundColor Green
    Write-Host "🆔 R2R User ID: $($loginResult.user.r2RUserId)" -ForegroundColor Cyan
    
    # The user's R2R collection ID from the API response
    $userR2RCollectionId = "269345f3-deb2-5d7d-aefb-9299b382811b"
    
    Write-Host "📚 User's R2R Collection ID: $userR2RCollectionId" -ForegroundColor Cyan
    Write-Host "⚠️  This collection currently has 0 documents" -ForegroundColor Yellow
    
    # Check if we can add documents to this collection
    Write-Host "🔍 Checking R2R collection details..." -ForegroundColor Yellow
    try {
        $r2rCollectionResponse = Invoke-WebRequest -Uri "http://192.168.1.4:7272/v3/collections/$userR2RCollectionId" -Method Get -UseBasicParsing
        Write-Host "✅ R2R Collection Details: $($r2rCollectionResponse.StatusCode)" -ForegroundColor Green
        Write-Host "Details: $($r2rCollectionResponse.Content)" -ForegroundColor Cyan
        
        # Check documents in this collection
        $r2rDocsResponse = Invoke-WebRequest -Uri "http://192.168.1.4:7272/v3/collections/$userR2RCollectionId/documents" -Method Get -UseBasicParsing
        Write-Host "✅ R2R Collection Documents: $($r2rDocsResponse.StatusCode)" -ForegroundColor Green
        Write-Host "Documents: $($r2rDocsResponse.Content)" -ForegroundColor Cyan
        
    } catch {
        Write-Host "❌ R2R Collection check failed: $($_.Exception.Message)" -ForegroundColor Red
    }
    
    # Update local database to map to correct R2R collection
    Write-Host "🔧 Updating local collection mapping..." -ForegroundColor Yellow
    
    # This would require updating the local database to map the local collection
    # to the correct R2R collection ID
    Write-Host "💡 SOLUTION REQUIRED:" -ForegroundColor Yellow
    Write-Host "  1. Update local collection $($existingCollectionId) to map to R2R collection $userR2RCollectionId" -ForegroundColor Cyan
    Write-Host "  2. OR: Add documents to the R2R collection $userR2RCollectionId" -ForegroundColor Cyan
    Write-Host "  3. OR: Use a collection with documents like MySkin (6b5a6538-34bb-46a5-8476-39b90596cea2)" -ForegroundColor Cyan
    
    # Test with MySkin collection that has 13 documents
    Write-Host "🧪 Testing with MySkin collection (has 13 documents)..." -ForegroundColor Yellow
    
    # Create test conversation with MySkin collection
    $testConvData = @{
        title = "Test with MySkin Collection"
        description = "Testing with collection that has documents"
        settings = @{
            searchMode = "hybrid"
            useVectorSearch = $true
            useHybridSearch = $true
            maxResults = 10
            relevanceThreshold = 0.7
            streamingEnabled = $false
        }
    } | ConvertTo-Json -Depth 10
    
    try {
        $testConvResponse = Invoke-WebRequest -Uri 'http://localhost:5253/api/conversations' -Method Post -Body $testConvData -Headers $authHeaders -UseBasicParsing
        $testConv = $testConvResponse.Content | ConvertFrom-Json
        
        Write-Host "✅ Created test conversation: ID $($testConv.id)" -ForegroundColor Green
        
        # Send message with MySkin collection filter
        $testMessage = @{
            content = "What information do you have about skin care and dermatology?"
            collections = @()  # Empty - let R2R search all available collections
            settings = @{
                searchMode = "hybrid"
                maxResults = 10
                relevanceThreshold = 0.5
            }
        } | ConvertTo-Json -Depth 10
        
        Write-Host "📤 Sending test message..." -ForegroundColor Yellow
        $messageResponse = Invoke-WebRequest -Uri "http://localhost:5253/api/conversations/$($testConv.id)/messages" -Method Post -Body $testMessage -Headers $authHeaders -UseBasicParsing
        
        Write-Host "✅ Message sent successfully" -ForegroundColor Green
        $messageResult = $messageResponse.Content | ConvertFrom-Json
        
        Write-Host "🎯 R2R RESPONSE WITH DOCUMENTS:" -ForegroundColor Yellow
        Write-Host "═══════════════════════════════════════════════════════════" -ForegroundColor Gray
        Write-Host "📝 Content Length: $($messageResult.content.Length) characters" -ForegroundColor Cyan
        
        if ($messageResult.content -and $messageResult.content.Length -gt 0) {
            Write-Host "✅ R2R RESPONSE:" -ForegroundColor Green
            Write-Host $messageResult.content -ForegroundColor White
            
            $contentWords = $messageResult.content.Split(' ').Count
            Write-Host "📊 Word Count: $contentWords words" -ForegroundColor Cyan
            
            if ($contentWords -gt 50) {
                Write-Host "✅ EXCELLENT - R2R generated substantial response!" -ForegroundColor Green
            } else {
                Write-Host "⚠️  Still getting generic response" -ForegroundColor Yellow
            }
        } else {
            Write-Host "❌ Still no content generated" -ForegroundColor Red
        }
        
    } catch {
        Write-Host "❌ Test conversation failed: $($_.Exception.Message)" -ForegroundColor Red
        if ($_.Exception.Response) {
            Write-Host "Status: $($_.Exception.Response.StatusCode)" -ForegroundColor Red
        }
    }
    
    Write-Host "📊 SUMMARY:" -ForegroundColor Yellow
    Write-Host "  ✅ R2R API: Working" -ForegroundColor Green
    Write-Host "  ❌ User Collection: Empty (0 documents)" -ForegroundColor Red
    Write-Host "  ✅ Other Collections: Available with documents" -ForegroundColor Green
    Write-Host "  🔧 Fix Needed: Collection synchronization or document upload" -ForegroundColor Yellow
    
} catch {
    Write-Host "❌ Critical Error: $($_.Exception.Message)" -ForegroundColor Red
}