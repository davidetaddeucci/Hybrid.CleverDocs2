$headers = @{
    "Content-Type" = "application/json"
}

$body = @{
    name = "Test Collection PowerShell"
    description = "Test collection created via PowerShell"
    metadata = @{}
} | ConvertTo-Json

try {
    $response = Invoke-RestMethod -Uri "http://192.168.1.4:7272/v3/collections" -Method Post -Headers $headers -Body $body
    Write-Host "SUCCESS: Collection created"
    Write-Host ($response | ConvertTo-Json -Depth 3)
} catch {
    Write-Host "ERROR: $($_.Exception.Message)"
    Write-Host "Response: $($_.Exception.Response)"
    if ($_.Exception.Response) {
        $reader = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
        $responseBody = $reader.ReadToEnd()
        Write-Host "Response Body: $responseBody"
    }
}