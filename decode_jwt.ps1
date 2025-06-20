# PowerShell script to decode JWT token and check role claims
param(
    [string]$Token
)

if (-not $Token) {
    Write-Host "Usage: .\decode_jwt.ps1 -Token 'your_jwt_token_here'"
    exit 1
}

# Split the JWT token into parts
$parts = $Token.Split('.')
if ($parts.Length -ne 3) {
    Write-Host "Invalid JWT token format"
    exit 1
}

# Decode the payload (second part)
$payload = $parts[1]

# Add padding if needed
while ($payload.Length % 4 -ne 0) {
    $payload += "="
}

try {
    # Decode from Base64
    $decodedBytes = [System.Convert]::FromBase64String($payload)
    $decodedText = [System.Text.Encoding]::UTF8.GetString($decodedBytes)
    
    Write-Host "JWT Payload:"
    Write-Host $decodedText
    
    # Parse as JSON to extract role
    $jsonPayload = $decodedText | ConvertFrom-Json
    
    Write-Host "`nRole Claims:"
    if ($jsonPayload.role) {
        Write-Host "role: $($jsonPayload.role)"
    }
    if ($jsonPayload."http://schemas.microsoft.com/ws/2008/06/identity/claims/role") {
        Write-Host "ClaimTypes.Role: $($jsonPayload."http://schemas.microsoft.com/ws/2008/06/identity/claims/role")"
    }
    
    Write-Host "`nAll Claims:"
    $jsonPayload | Format-List
    
} catch {
    Write-Host "Error decoding JWT token: $($_.Exception.Message)"
    exit 1
}
