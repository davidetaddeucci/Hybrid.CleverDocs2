param(
    [Parameter(Mandatory=$true)]
    [string]$RepositoryPath
)

Write-Host "🔧 Risoluzione problemi AugmentCode per repository: $RepositoryPath" -ForegroundColor Green

# Verifica che la directory esista
if (-not (Test-Path $RepositoryPath)) {
    Write-Error "La directory $RepositoryPath non esiste!"
    exit 1
}

# Naviga alla directory
Set-Location $RepositoryPath

Write-Host "📁 Configurazione permessi..." -ForegroundColor Yellow
try {
    # Rimuovi attributi sola lettura
    Get-ChildItem -Recurse | ForEach-Object { 
        if ($_.Attributes -band [System.IO.FileAttributes]::ReadOnly) {
            $_.Attributes = $_.Attributes -band (-bnot [System.IO.FileAttributes]::ReadOnly)
        }
    }
    
    # Concedi permessi completi
    icacls . /grant "$env:USERNAME:(OI)(CI)F" /T /Q
    Write-Host "✅ Permessi configurati correttamente" -ForegroundColor Green
} catch {
    Write-Warning "⚠️ Errore nella configurazione permessi: $($_.Exception.Message)"
}

Write-Host "🔧 Configurazione Git..." -ForegroundColor Yellow
try {
    git config core.autocrlf false
    git config core.filemode false
    git config core.symlinks false
    Write-Host "✅ Git configurato correttamente" -ForegroundColor Green
} catch {
    Write-Warning "⚠️ Errore nella configurazione Git: $($_.Exception.Message)"
}

Write-Host "🧹 Pulizia cache..." -ForegroundColor Yellow
try {
    # Chiudi VS Code
    Get-Process "Code" -ErrorAction SilentlyContinue | Stop-Process -Force
    
    # Pulisci cache VS Code
    $vscodeCache = "$env:APPDATA\Code\User\workspaceStorage"
    if (Test-Path $vscodeCache) {
        Remove-Item -Recurse -Force "$vscodeCache\*" -ErrorAction SilentlyContinue
    }
    
    Write-Host "✅ Cache pulita" -ForegroundColor Green
} catch {
    Write-Warning "⚠️ Errore nella pulizia cache: $($_.Exception.Message)"
}

Write-Host "⚙️ Creazione configurazione VS Code..." -ForegroundColor Yellow
try {
    # Crea directory .vscode se non esiste
    if (-not (Test-Path ".vscode")) {
        New-Item -ItemType Directory -Path ".vscode" | Out-Null
    }
    
    # Crea settings.json ottimizzato
    $settings = @{
        "files.exclude" = @{
            "**/.git" = $true
            "**/node_modules" = $true
            "**/.DS_Store" = $true
            "**/Thumbs.db" = $true
            "**/*.log" = $true
        }
        "search.exclude" = @{
            "**/node_modules" = $true
            "**/bower_components" = $true
            "**/.git" = $true
        }
        "files.watcherExclude" = @{
            "**/.git/objects/**" = $true
            "**/.git/subtree-cache/**" = $true
            "**/node_modules/**" = $true
        }
        "typescript.disableAutomaticTypeAcquisition" = $true
        "files.autoSave" = "off"
    }
    
    $settings | ConvertTo-Json -Depth 3 | Out-File -FilePath ".vscode\settings.json" -Encoding UTF8
    Write-Host "✅ Configurazione VS Code creata" -ForegroundColor Green
} catch {
    Write-Warning "⚠️ Errore nella creazione configurazione: $($_.Exception.Message)"
}

Write-Host "🛡️ Configurazione Windows Defender..." -ForegroundColor Yellow
try {
    Add-MpPreference -ExclusionPath $RepositoryPath -ErrorAction SilentlyContinue
    Write-Host "✅ Esclusione Windows Defender aggiunta" -ForegroundColor Green
} catch {
    Write-Warning "⚠️ Impossibile aggiungere esclusione Windows Defender (richiede privilegi amministratore)"
}

Write-Host "🎉 Risoluzione completata! Riavvia VS Code e prova AugmentCode." -ForegroundColor Green
Write-Host "💡 Se i problemi persistono, esegui questo script come Amministratore." -ForegroundColor Cyan