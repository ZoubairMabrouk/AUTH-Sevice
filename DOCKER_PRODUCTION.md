# Production Deployment Guide

## 🚀 Prérequis
- Docker Engine 20.10+
- Docker Compose 2.0+
- Au minimum 6GB RAM disponible
- Au minimum 20GB d'espace disque

## 📋 Configuration

### 1. Créer le fichier `.env.production`
```bash
cp .env.example .env.production
```

### 2. Configurer les secrets
Modifier `.env.production` avec vos valeurs sécurisées :
- `SA_PASSWORD` : Mot de passe SQL Server (min 8 caractères, majuscules, chiffres, caractères spéciaux)
- `JWT_SECRET_KEY` : Clé JWT sécurisée (min 32 caractères)
- `REDIS_PASSWORD` : Mot de passe Redis (min 16 caractères)

## 🔒 Sécurité

Les améliorations de sécurité implémentées :
- ✅ Authentification Redis avec mot de passe
- ✅ Chiffrage des connexions SQL Server
- ✅ Filesystem read-only avec tmpfs pour les logs
- ✅ Pas de privilèges élevés (no-new-privileges)
- ✅ Limites de ressources CPU/Mémoire
- ✅ Timeouts de healthcheck améliorés
- ✅ Logging structuré avec rotation

## 🐳 Démarrage

### Démarrage initial
```bash
docker-compose --file docker-compose.yml --env-file .env.production up -d
```

### Arrêt gracieux
```bash
docker-compose down
```

### Sauvegarde des données
```bash
docker run --rm -v auth-sqlserver_data:/data -v $(pwd)/backups:/backup \
  busybox tar czf /backup/sqlserver-$(date +%Y%m%d-%H%M%S).tar.gz -C /data .
```

## 📊 Monitoring

### Vérifier l'état des services
```bash
docker-compose ps
```

### Voir les logs
```bash
# Tous les services
docker-compose logs -f

# Service spécifique
docker-compose logs -f auth-service
docker-compose logs -f sqlserver
docker-compose logs -f redis
```

## 🔍 Healthchecks

Les services ont des healthchecks configurés :
- **auth-service** : vérifie `/health` endpoint
- **sqlserver** : vérifie la connexion SQL
- **redis** : vérifie avec `PING` command

Vérifier les healthchecks :
```bash
docker ps --no-trunc
```

## 📦 Versions d'Images

⚠️ **IMPORTANT** : Mettre à jour les versions dans `docker-compose.yml` :
```yaml
auth-service:
  image: zoubairmabrouk/auth-service:1.0.0  # ← Spécifier votre version
```

Utiliser un versioning sémantique (MAJOR.MINOR.PATCH).

## 🔄 Mises à Jour

### Mettre à jour l'application
```bash
# Arrêter les services
docker-compose down

# Mettre à jour l'image
docker pull zoubairmabrouk/auth-service:X.X.X

# Modifier la version dans docker-compose.yml
# Redémarrer
docker-compose up -d
```

## 🆘 Troubleshooting

### Service ne démarre pas
```bash
docker-compose logs auth-service
```

### Problèmes de connexion DB
```bash
docker-compose exec sqlserver sqlcmd -U sa -P $SA_PASSWORD -Q "SELECT @@VERSION"
```

### Redis problèmes
```bash
docker-compose exec redis redis-cli -a $REDIS_PASSWORD ping
```

## 📈 Performance Tuning

### Limites de ressources
- auth-service: 1 CPU, 512MB RAM
- sqlserver: 2 CPU, 3GB RAM
- redis: 0.5 CPU, 512MB RAM

Ajuster selon vos besoins dans `docker-compose.yml`.

### Volumes et Backups
- `sqlserver_data` : Données SQL Server
- `sqlserver_log` : Logs et backups
- `redis_data` : Données Redis (avec AOF)

## 🔐 Variables d'Environnement Sensibles

**NE JAMAIS** committer `.env.production` !
Ajouter à `.gitignore` :
```
.env.production
.env.local
.env.*.local
```

Utiliser une gestion des secrets au niveau du cluster (Kubernetes secrets, Docker Swarm secrets, etc.)

## ✅ Checklist Pré-Production

- [ ] Mots de passe forts configurés
- [ ] Volumes de sauvegarde configurés
- [ ] Limites de ressources validées
- [ ] Logs configurés et rotatés
- [ ] Healthchecks testés
- [ ] Disaster recovery plan créé
- [ ] Monitoring et alertes configurés
- [ ] Tests de charge effectués
- [ ] Plan de rollback défini

