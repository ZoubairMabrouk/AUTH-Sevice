# Production Deployment Script pour AUTH-Service (Windows PowerShell)
# Usage: .\deploy.ps1 -Action start|stop|restart|logs|backup|update|health

param(
    [Parameter(Mandatory=$true)]
    [ValidateSet("start", "stop", "restart", "logs", "backup", "update", "health")]
    [string]$Action,
    
    [Parameter(Mandatory=$false)]
    [string]$ServiceName = "auth-service",
    
    [Parameter(Mandatory=$false)]
    [string]$Version
)

$ErrorActionPreference = "Stop"

$COMPOSE_FILE = "docker-compose.yml"
$ENV_FILE = ".env.production"
$BACKUP_DIR = "backups"
$TIMESTAMP = Get-Date -Format "yyyyMMdd-HHmmss"

# Vérifier les prérequis
function Test-Requirements {
    Write-Host "[INFO] Vérification des prérequis..." -ForegroundColor Green
    
    if (-not (Get-Command docker -ErrorAction SilentlyContinue)) {
        Write-Host "[ERROR] Docker n'est pas installé" -ForegroundColor Red
        exit 1
    }
    
    if (-not (Get-Command docker-compose -ErrorAction SilentlyContinue)) {
        Write-Host "[ERROR] Docker Compose n'est pas installé" -ForegroundColor Red
        exit 1
    }
    
    if (-not (Test-Path $ENV_FILE)) {
        Write-Host "[ERROR] Fichier $ENV_FILE non trouvé" -ForegroundColor Red
        exit 1
    }
    
    Write-Host "[INFO] ✓ Prérequis vérifiés" -ForegroundColor Green
}

# Démarrer les services
function Start-Services {
    Write-Host "[INFO] Démarrage des services..." -ForegroundColor Green
    
    & docker-compose -f $COMPOSE_FILE --env-file $ENV_FILE up -d
    
    Write-Host "[INFO] Attente de la disponibilité des services..." -ForegroundColor Green
    Start-Sleep -Seconds 10
    
    $status = & docker-compose ps
    if ($status -match "Exit") {
        Write-Host "[ERROR] Certains services n'ont pas pu démarrer" -ForegroundColor Red
        & docker-compose logs
        exit 1
    }
    
    Write-Host "[INFO] ✓ Services démarrés avec succès" -ForegroundColor Green
    & docker-compose ps
}

# Arrêter les services
function Stop-Services {
    Write-Host "[INFO] Arrêt des services..." -ForegroundColor Green
    & docker-compose -f $COMPOSE_FILE down
    Write-Host "[INFO] ✓ Services arrêtés" -ForegroundColor Green
}

# Redémarrer les services
function Restart-Services {
    Stop-Services
    Start-Sleep -Seconds 5
    Start-Services
}

# Afficher les logs
function Show-Logs {
    param([string]$Service = "")
    
    if ([string]::IsNullOrEmpty($Service)) {
        Write-Host "[INFO] Logs de tous les services (Ctrl+C pour quitter):" -ForegroundColor Green
        & docker-compose -f $COMPOSE_FILE logs -f
    }
    else {
        Write-Host "[INFO] Logs de $Service (Ctrl+C pour quitter):" -ForegroundColor Green
        & docker-compose -f $COMPOSE_FILE logs -f $Service
    }
}

# Sauvegarde de la base de données
function Backup-Database {
    Write-Host "[INFO] Création de la sauvegarde de la base de données..." -ForegroundColor Green
    
    if (-not (Test-Path $BACKUP_DIR)) {
        New-Item -ItemType Directory -Path $BACKUP_DIR | Out-Null
    }
    
    # Obtenir le mot de passe depuis le fichier .env
    $SA_PASSWORD = (Select-String "^SA_PASSWORD=" $ENV_FILE).Line.Split("=")[1]
    
    # Créer la sauvegarde SQL Server
    & docker-compose -f $COMPOSE_FILE exec -T sqlserver `
        /opt/mssql-tools/bin/sqlcmd `
        -S localhost -U sa -P $SA_PASSWORD `
        -Q "BACKUP DATABASE [AuthServiceDb] TO DISK = '/var/opt/mssql/backup/AuthServiceDb_${TIMESTAMP}.bak'"
    
    # Copier la sauvegarde
    & docker cp auth-sqlserver:/var/opt/mssql/backup/AuthServiceDb_${TIMESTAMP}.bak `
        "$BACKUP_DIR\"
    
    Write-Host "[INFO] ✓ Sauvegarde créée: $BACKUP_DIR\AuthServiceDb_${TIMESTAMP}.bak" -ForegroundColor Green
}

# Mettre à jour l'application
function Update-Application {
    param([string]$NewVersion)
    
    if ([string]::IsNullOrEmpty($NewVersion)) {
        Write-Host "[ERROR] Version requise" -ForegroundColor Red
        exit 1
    }
    
    Write-Host "[WARN] Mise à jour vers la version $NewVersion" -ForegroundColor Yellow
    
    # Sauvegarde avant mise à jour
    Backup-Database
    
    # Arrêter les services
    Stop-Services
    
    # Mettre à jour l'image
    Write-Host "[INFO] Téléchargement de la nouvelle image..." -ForegroundColor Green
    & docker pull zoubairmabrouk/auth-service:$NewVersion
    
    # Mettre à jour docker-compose.yml
    $content = Get-Content $COMPOSE_FILE -Raw
    $content = $content -replace "zoubairmabrouk/auth-service:.*", "zoubairmabrouk/auth-service:$NewVersion"
    Set-Content -Path $COMPOSE_FILE -Value $content
    
    # Redémarrer
    Start-Services
    
    Write-Host "[INFO] ✓ Mise à jour vers $NewVersion réussie" -ForegroundColor Green
}

# Vérifier la santé des services
function Test-Health {
    Write-Host "[INFO] Vérification de la santé des services..." -ForegroundColor Green
    
    $authStatus = & docker inspect --format='{{.State.Health.Status}}' auth-service 2>$null || "Unknown"
    $dbStatus = & docker inspect --format='{{.State.Health.Status}}' auth-sqlserver 2>$null || "Unknown"
    $redisStatus = & docker inspect --format='{{.State.Health.Status}}' auth-redis 2>$null || "Unknown"
    
    Write-Host "  auth-service: $authStatus"
    Write-Host "  sqlserver:    $dbStatus"
    Write-Host "  redis:        $redisStatus"
    
    if ($authStatus -eq "healthy" -and $dbStatus -eq "healthy" -and $redisStatus -eq "healthy") {
        Write-Host "[INFO] ✓ Tous les services sont sains" -ForegroundColor Green
    }
    else {
        Write-Host "[WARN] ⚠ Certains services ne sont pas sains" -ForegroundColor Yellow
    }
}

# Main
Test-Requirements

switch ($Action) {
    "start" { Start-Services }
    "stop" { Stop-Services }
    "restart" { Restart-Services }
    "logs" { Show-Logs -Service $ServiceName }
    "backup" { Backup-Database }
    "update" { Update-Application -NewVersion $Version }
    "health" { Test-Health }
    default { Write-Host "Action inconnue: $Action" -ForegroundColor Red }
}
