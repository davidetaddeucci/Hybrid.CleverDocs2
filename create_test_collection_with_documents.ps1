# Create test collection and documents for conversation testing
$headers = @{
    'Content-Type' = 'application/json'
}

$loginData = @{
    email = 'r.antoniucci@microsis.it'
    password = 'Maremmabona1!'
} | ConvertTo-Json

Write-Host "🔐 Authenticating..." -ForegroundColor Yellow

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
    
    # Create test collection
    Write-Host "📚 Creating test collection..." -ForegroundColor Yellow
    $collectionData = @{
        name = "Chat Test Collection"
        description = "Test collection for conversation system testing"
        isPublic = $false
    } | ConvertTo-Json
    
    $collectionResponse = Invoke-WebRequest -Uri 'http://localhost:5253/api/collections' -Method Post -Body $collectionData -Headers $authHeaders -UseBasicParsing
    $collection = $collectionResponse.Content | ConvertFrom-Json
    $collectionId = $collection.results.id
    
    Write-Host "✅ Collection created: $collectionId" -ForegroundColor Green
    
    # Create test documents
    $testDocuments = @(
        @{
            title = "Company Overview"
            content = "Our company specializes in AI-powered document management solutions. We provide enterprise-grade tools for document processing, search, and conversation systems."
        },
        @{
            title = "Product Features"
            content = "Key features include: Multi-tenant architecture, Real-time chat with documents, Advanced search capabilities, JWT authentication, SignalR integration, and R2R API support."
        },
        @{
            title = "Technical Architecture"
            content = "The system uses ASP.NET Core WebAPI backend, SignalR for real-time communication, PostgreSQL database, Redis caching, and RabbitMQ for message queuing."
        }
    )
    
    foreach ($doc in $testDocuments) {
        Write-Host "📄 Creating document: $($doc.title)..." -ForegroundColor Yellow
        
        $documentData = @{
            title = $doc.title
            content = $doc.content
            collectionId = $collectionId
            metadata = @{
                source = "test_creation"
                type = "text"
            }
        } | ConvertTo-Json -Depth 10
        
        $docResponse = Invoke-WebRequest -Uri 'http://localhost:5253/api/documents' -Method Post -Body $documentData -Headers $authHeaders -UseBasicParsing
        $document = $docResponse.Content | ConvertFrom-Json
        
        Write-Host "✅ Document created: $($document.results.id)" -ForegroundColor Green
    }
    
    Write-Host "🎉 Test collection and documents created successfully!" -ForegroundColor Green
    Write-Host "Collection ID: $collectionId" -ForegroundColor Cyan
    Write-Host "You can now test the conversation system with this collection." -ForegroundColor Cyan
    
} catch {
    Write-Host "❌ Error: $($_.Exception.Message)" -ForegroundColor Red
}