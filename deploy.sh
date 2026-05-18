#!/bin/bash

# Production Deployment Script pour AUTH-Service
# Usage: ./deploy.sh [start|stop|restart|logs|backup|update]

set -e

COMPOSE_FILE="docker-compose.yml"
ENV_FILE=".env.production"
SERVICE_NAME="auth-service"
TIMESTAMP=$(date +%Y%m%d-%H%M%S)
BACKUP_DIR="backups"

# Colors pour output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Fonctions utilitaires
log_info() {
    echo -e "${GREEN}[INFO]${NC} $1"
}

log_warn() {
    echo -e "${YELLOW}[WARN]${NC} $1"
}

log_error() {
    echo -e "${RED}[ERROR]${NC} $1"
}

# Vérifier les prérequis
check_requirements() {
    if ! command -v docker &> /dev/null; then
        log_error "Docker n'est pas installé"
        exit 1
    fi

    if ! command -v docker-compose &> /dev/null; then
        log_error "Docker Compose n'est pas installé"
        exit 1
    fi

    if [ ! -f "$ENV_FILE" ]; then
        log_error "Fichier $ENV_FILE non trouvé"
        exit 1
    fi

    log_info "✓ Prérequis vérifiés"
}

# Démarrer les services
start_services() {
    log_info "Démarrage des services..."
    docker-compose -f $COMPOSE_FILE --env-file $ENV_FILE up -d
    
    log_info "Attente de la disponibilité des services..."
    sleep 10
    
    # Vérifier l'état des services
    if docker-compose ps | grep -q "Exit"; then
        log_error "Certains services n'ont pas pu démarrer"
        docker-compose logs
        exit 1
    fi
    
    log_info "✓ Services démarrés avec succès"
    docker-compose ps
}

# Arrêter les services
stop_services() {
    log_info "Arrêt des services..."
    docker-compose -f $COMPOSE_FILE down
    log_info "✓ Services arrêtés"
}

# Redémarrer les services
restart_services() {
    stop_services
    sleep 5
    start_services
}

# Afficher les logs
show_logs() {
    local service=$1
    if [ -z "$service" ]; then
        log_info "Logs de tous les services (Ctrl+C pour quitter):"
        docker-compose -f $COMPOSE_FILE logs -f
    else
        log_info "Logs de $service (Ctrl+C pour quitter):"
        docker-compose -f $COMPOSE_FILE logs -f $service
    fi
}

# Sauvegarde de la base de données
backup_database() {
    log_info "Création de la sauvegarde de la base de données..."
    mkdir -p $BACKUP_DIR
    
    # Sauvegarde SQL Server
    docker-compose -f $COMPOSE_FILE exec -T sqlserver \
        /opt/mssql-tools/bin/sqlcmd \
        -S localhost -U sa -P $SA_PASSWORD \
        -Q "BACKUP DATABASE [AuthServiceDb] TO DISK = '/var/opt/mssql/backup/AuthServiceDb_${TIMESTAMP}.bak'"
    
    # Copier la sauvegarde
    docker cp auth-sqlserver:/var/opt/mssql/backup/AuthServiceDb_${TIMESTAMP}.bak \
        $BACKUP_DIR/
    
    log_info "✓ Sauvegarde créée: $BACKUP_DIR/AuthServiceDb_${TIMESTAMP}.bak"
}

# Mettre à jour l'application
update_application() {
    local new_version=$1
    
    if [ -z "$new_version" ]; then
        log_error "Version requise. Usage: ./deploy.sh update <version>"
        exit 1
    fi
    
    log_warn "Mise à jour vers la version $new_version"
    
    # Sauvegarde avant mise à jour
    backup_database
    
    # Arrêter les services
    stop_services
    
    # Mettre à jour l'image
    log_info "Téléchargement de la nouvelle image..."
    docker pull zoubairmabrouk/auth-service:$new_version
    
    # Mettre à jour docker-compose.yml
    sed -i "s/zoubairmabrouk\/auth-service:.*/zoubairmabrouk\/auth-service:$new_version/" $COMPOSE_FILE
    
    # Redémarrer
    start_services
    
    log_info "✓ Mise à jour vers $new_version réussie"
}

# Vérifier la santé des services
health_check() {
    log_info "Vérification de la santé des services..."
    
    local auth_status=$(docker inspect --format='{{.State.Health.Status}}' auth-service 2>/dev/null || echo "Unknown")
    local db_status=$(docker inspect --format='{{.State.Health.Status}}' auth-sqlserver 2>/dev/null || echo "Unknown")
    local redis_status=$(docker inspect --format='{{.State.Health.Status}}' auth-redis 2>/dev/null || echo "Unknown")
    
    echo "  auth-service: $auth_status"
    echo "  sqlserver:    $db_status"
    echo "  redis:        $redis_status"
    
    if [ "$auth_status" = "healthy" ] && [ "$db_status" = "healthy" ] && [ "$redis_status" = "healthy" ]; then
        log_info "✓ Tous les services sont sains"
        return 0
    else
        log_warn "⚠ Certains services ne sont pas sains"
        return 1
    fi
}

# Menu principal
main() {
    check_requirements
    
    case "${1:-}" in
        start)
            start_services
            ;;
        stop)
            stop_services
            ;;
        restart)
            restart_services
            ;;
        logs)
            show_logs "$2"
            ;;
        backup)
            backup_database
            ;;
        update)
            update_application "$2"
            ;;
        health)
            health_check
            ;;
        *)
            echo "Usage: $0 {start|stop|restart|logs|backup|update|health}"
            echo ""
            echo "Commandes:"
            echo "  start              Démarrer tous les services"
            echo "  stop               Arrêter tous les services"
            echo "  restart            Redémarrer tous les services"
            echo "  logs [service]     Afficher les logs (optionnel: service spécifique)"
            echo "  backup             Sauvegarder la base de données"
            echo "  update <version>   Mettre à jour vers une nouvelle version"
            echo "  health             Vérifier la santé des services"
            exit 1
            ;;
    esac
}

main "$@"
