# AUTH-Service - Configuration Production-Ready

## 📋 Résumé des améliorations

Votre configuration Docker a été optimisée pour la production avec les améliorations suivantes :

### ✅ Sécurité
- **Authentification Redis** : mot de passe requis pour l'accès
- **Chiffrage SQL Server** : `Encrypt=True` dans les chaînes de connexion
- **Filesystem Read-Only** : le conteneur auth-service a un filesystem read-only
- **Pas de privilèges élevés** : `no-new-privileges:true` pour tous les services
- **Gestion des secrets** : variables sensibles dans `.env.production` (gitignored)

### 🔒 Limites de Ressources
```yaml
auth-service:   1 CPU, 512MB RAM
sqlserver:      2 CPU, 3GB RAM  
redis:          0.5 CPU, 512MB RAM
```

### 📊 Logging & Monitoring
- Logs structurés JSON avec rotation automatique
- Max 10MB par fichier, 3 fichiers conservés
- Labels pour identifier les services
- Healthchecks améliorés avec timeouts appropriés

### 🔄 Disponibilité
- Politique de redémarrage : `on-failure:5` (restart jusqu'à 5 fois en cas d'erreur)
- Healthchecks sur tous les services avec détection automatique
- Timeouts configurés pour éviter les blocages

### 📦 Versioning
- Utilisation de versions spécifiques (`1.0.0`) au lieu de `latest`
- Versionning sémantique (MAJOR.MINOR.PATCH)

## 🚀 Fichiers créés/modifiés

| Fichier | Description |
|---------|-------------|
| `docker-compose.yml` | Configuration production-ready |
| `.env.production` | Variables d'environnement (⚠️ à sécuriser) |
| `.env.example` | Template des variables (safe pour Git) |
| `docker-compose.dev.yml` | Overrides pour développement |
| `.gitignore` | Ajout des fichiers sensibles |
| `DOCKER_PRODUCTION.md` | Guide détaillé de déploiement |
| `deploy.sh` | Script bash de gestion (Linux/Mac) |
| `deploy.ps1` | Script PowerShell (Windows) |

## 🔧 Configuration minimale requise

```bash
# 1. Configurer les secrets
cp .env.example .env.production

# Éditer .env.production avec vos valeurs sécurisées :
# - SA_PASSWORD : Mot de passe SQL Server fort
# - JWT_SECRET_KEY : Clé JWT (min 32 caractères)
# - REDIS_PASSWORD : Mot de passe Redis (min 16 caractères)
```

## 🏃 Démarrage rapide

### Avec Docker Compose (Linux/Mac/Windows Git Bash)
```bash
# Démarrage
docker-compose --env-file .env.production up -d

# Arrêt
docker-compose down

# Logs
docker-compose logs -f
```

### Avec scripts de déploiement

#### Linux/Mac (bash)
```bash
chmod +x deploy.sh

# Démarrer
./deploy.sh start

# Logs
./deploy.sh logs auth-service

# Sauvegarde
./deploy.sh backup

# Mettre à jour
./deploy.sh update 1.1.0

# Vérifier la santé
./deploy.sh health
```

#### Windows (PowerShell)
```powershell
# Démarrer
.\deploy.ps1 -Action start

# Logs
.\deploy.ps1 -Action logs -ServiceName auth-service

# Sauvegarde
.\deploy.ps1 -Action backup

# Mettre à jour
.\deploy.ps1 -Action update -Version 1.1.0

# Vérifier la santé
.\deploy.ps1 -Action health
```

## 📝 Points importants

### 🔑 Gestion des secrets
- **NE JAMAIS** committer `.env.production` 
- Vérifier que `.env.production` est dans `.gitignore`
- Utiliser les secrets au niveau du cluster en production (Kubernetes, Docker Swarm)

### 📦 Versioning des images
Remplacer `1.0.0` par votre version actuelle dans `docker-compose.yml` :
```yaml
auth-service:
  image: zoubairmabrouk/auth-service:1.0.0  # ← Mettre à jour
```

### 💾 Sauvegardes
Les données sont persistées dans :
- `sqlserver_data` : Base de données SQL Server
- `sqlserver_log` : Logs et sauvegardes SQL Server  
- `redis_data` : Données Redis avec AOF (Append Only File)

### 🐛 Troubleshooting

**Service ne démarre pas**
```bash
docker-compose logs auth-service
```

**Problèmes de connexion DB**
```bash
docker-compose exec sqlserver sqlcmd -U sa -P $SA_PASSWORD -Q "SELECT @@VERSION"
```

**Redis ne répond pas**
```bash
docker-compose exec redis redis-cli -a $REDIS_PASSWORD ping
```

## 📖 Documentation complète
Voir [DOCKER_PRODUCTION.md](./DOCKER_PRODUCTION.md) pour le guide détaillé.

## ⚠️ Checklist avant production

- [ ] Secrets configurés avec des mots de passe forts
- [ ] Version d'image mise à jour dans docker-compose.yml
- [ ] `.env.production` ajouté à `.gitignore`
- [ ] Backups testés et validés
- [ ] Limites de ressources validées
- [ ] Healthchecks testés
- [ ] Logs et monitoring configurés
- [ ] Plan de disaster recovery documenté
- [ ] Tests de charge effectués
- [ ] Plan de rollback défini

## 🔄 Mise à jour vers une nouvelle version

```bash
# Bash
./deploy.sh update 1.1.0

# PowerShell
.\deploy.ps1 -Action update -Version 1.1.0
```

Cela va :
1. Sauvegarder la base de données
2. Arrêter les services
3. Télécharger la nouvelle image
4. Mettre à jour docker-compose.yml
5. Redémarrer les services

## 📧 Support

Pour des questions ou des améliorations, consultez la documentation officielle de Docker et de vos services.
