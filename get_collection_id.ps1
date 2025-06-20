# PowerShell script to get a collection ID for testing
$loginUrl = "http://localhost:5252/api/auth/login"
$collectionsUrl = "http://localhost:5252/api/UserCollections"

$loginData = @{
    email = "info@hybrid.it"
    password = "Florealia2025!"
} | ConvertTo-Json

try {
    Write-Host "Getting collection ID for testing..."
    
    # Login to get JWT token
    $loginResponse = Invoke-RestMethod -Uri $loginUrl -Method POST -Body $loginData -ContentType "application/json"
    
    if ($loginResponse.accessToken) {
        Write-Host "Login successful!"
        
        # Get collections
        $headers = @{
            "Authorization" = "Bearer $($loginResponse.accessToken)"
        }
        
        $collectionsResponse = Invoke-RestMethod -Uri $collectionsUrl -Method GET -Headers $headers
        
        if ($collectionsResponse.items -and $collectionsResponse.items.Count -gt 0) {
            $collection = $collectionsResponse.items[0]
            Write-Host "Collection found:"
            Write-Host "  ID: $($collection.id)"
            Write-Host "  Name: $($collection.name)"
            
            # Save to file for use in test
            $collection.id | Out-File -FilePath "collection_id.txt" -Encoding UTF8
            
            return $collection.id
        } else {
            Write-Warning "No collections found"
            return $null
        }
    } else {
        Write-Error "Login failed"
        return $null
    }
} catch {
    Write-Error "Error: $($_.Exception.Message)"
    return $null
}
