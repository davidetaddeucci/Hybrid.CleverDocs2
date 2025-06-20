# PowerShell script to get JWT token from login
$loginUrl = "http://localhost:5252/api/auth/login"
$loginData = @{
    email = "info@hybrid.it"
    password = "Florealia2025!"
} | ConvertTo-Json

try {
    Write-Host "Logging in to get JWT token..."
    
    $response = Invoke-RestMethod -Uri $loginUrl -Method POST -Body $loginData -ContentType "application/json"
    
    if ($response.accessToken) {
        Write-Host "Login successful!"
        Write-Host "Access Token: $($response.accessToken)"
        
        # Save token to file for decoding
        $response.accessToken | Out-File -FilePath "jwt_token.txt" -Encoding UTF8
        
        # Decode the token
        .\decode_jwt.ps1 -Token $response.accessToken
    } else {
        Write-Host "Login failed - no access token received"
        Write-Host "Response: $($response | ConvertTo-Json)"
    }
} catch {
    Write-Host "Error during login: $($_.Exception.Message)"
    if ($_.Exception.Response) {
        $reader = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
        $responseBody = $reader.ReadToEnd()
        Write-Host "Response body: $responseBody"
    }
}
