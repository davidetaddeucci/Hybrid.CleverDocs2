# PowerShell script to run SQL fix
$env:PGPASSWORD = "MiaPassword123"

try {
    Write-Host "Fixing user role in database..."
    
    # Run the SQL fix
    $result = psql -h 192.168.1.4 -p 5433 -U admin -d cleverdocs -f fix_user_role.sql
    
    Write-Host "SQL execution result:"
    Write-Host $result
    
    Write-Host "User role fix completed!"
    
} catch {
    Write-Host "Error running SQL fix: $($_.Exception.Message)"
}
