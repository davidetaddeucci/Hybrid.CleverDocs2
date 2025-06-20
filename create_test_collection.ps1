# PowerShell script to create a test collection for heavy bulk upload
$loginUrl = "http://localhost:5252/api/auth/login"
$collectionsUrl = "http://localhost:5252/api/UserCollections"

$loginData = @{
    email = "info@hybrid.it"
    password = "Florealia2025!"
} | ConvertTo-Json

try {
    Write-Host "Creating test collection for heavy bulk upload..."
    
    # Login to get JWT token
    $loginResponse = Invoke-RestMethod -Uri $loginUrl -Method POST -Body $loginData -ContentType "application/json"
    
    if ($loginResponse.accessToken) {
        Write-Host "Login successful!"
        
        # Create collection
        $headers = @{
            "Authorization" = "Bearer $($loginResponse.accessToken)"
            "Content-Type" = "application/json"
        }
        
        $collectionData = @{
            name = "Heavy Bulk Upload Test Collection"
            description = "Test collection for R2R ingestion status verification with 20 x 2MB files"
            isPublic = $false
            color = "#007bff"
            icon = "fas fa-file-upload"
        } | ConvertTo-Json
        
        $collectionResponse = Invoke-RestMethod -Uri $collectionsUrl -Method POST -Body $collectionData -Headers $headers
        
        if ($collectionResponse.id) {
            Write-Host "✅ Collection created successfully!"
            Write-Host "  ID: $($collectionResponse.id)"
            Write-Host "  Name: $($collectionResponse.name)"
            
            # Save to file for use in test
            $collectionResponse.id | Out-File -FilePath "collection_id.txt" -Encoding UTF8
            
            return $collectionResponse.id
        } else {
            Write-Error "Failed to create collection"
            return $null
        }
    } else {
        Write-Error "Login failed"
        return $null
    }
} catch {
    Write-Error "Error: $($_.Exception.Message)"
    if ($_.Exception.Response) {
        $reader = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
        $responseBody = $reader.ReadToEnd()
        Write-Host "Response body: $responseBody" -ForegroundColor Red
    }
    return $null
}
