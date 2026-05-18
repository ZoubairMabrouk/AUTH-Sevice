# 🔒 Recommandations de Sécurité Production

## Configuration actuelle ✅

La configuration fournie inclut déjà :
- ✅ Authentification Redis
- ✅ Chiffrage des connexions SQL
- ✅ Filesystem read-only
- ✅ Pas de privilèges élevés
- ✅ Limites de ressources
- ✅ Logging structuré
- ✅ Healthchecks
- ✅ Isolation réseau (bridge network)
- ✅ Gestion des secrets avec `.env`
- ✅ Politique de redémarrage saine

## Recommandations supplémentaires

### 1. 🔐 Gestion avancée des secrets

#### Option 1: Docker Secrets (Recommended pour Docker Swarm)
```yaml
services:
  auth-service:
    environment:
      - JWT_SECRET_FILE=/run/secrets/jwt_secret
    secrets:
      - jwt_secret
      - db_password

secrets:
  jwt_secret:
    external: true  # Créé via: docker secret create jwt_secret -
  db_password:
    external: true
```

#### Option 2: HashiCorp Vault
```bash
# Installer Vault et synchroniser les secrets
vault read secret/auth-service/credentials
```

#### Option 3: Kubernetes Secrets
```yaml
apiVersion: v1
kind: Secret
metadata:
  name: auth-service-secrets
type: Opaque
data:
  jwt-secret: base64-encoded-value
  db-password: base64-encoded-value
```

### 2. 🌐 Reverse Proxy & TLS

```yaml
# Ajouter un nginx reverse proxy
services:
  nginx:
    image: nginx:alpine
    ports:
      - "443:443"
      - "80:80"
    volumes:
      - ./nginx.conf:/etc/nginx/nginx.conf:ro
      - ./certs:/etc/nginx/certs:ro
    depends_on:
      - auth-service
    networks:
      - platform-network
```

**nginx.conf example:**
```nginx
upstream auth_service {
    server auth-service:8080;
}

server {
    listen 443 ssl http2;
    server_name api.example.com;

    ssl_certificate /etc/nginx/certs/cert.pem;
    ssl_certificate_key /etc/nginx/certs/key.pem;
    ssl_protocols TLSv1.2 TLSv1.3;
    ssl_ciphers HIGH:!aNULL:!MD5;

    location / {
        proxy_pass http://auth_service;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
    }
}
```

### 3. 🔍 Monitoring & Alertes

#### Prometheus + Grafana
```yaml
services:
  prometheus:
    image: prom/prometheus:latest
    volumes:
      - ./prometheus.yml:/etc/prometheus/prometheus.yml
      - prometheus_data:/prometheus
    ports:
      - "9090:9090"
    networks:
      - platform-network

  grafana:
    image: grafana/grafana:latest
    ports:
      - "3000:3000"
    environment:
      - GF_SECURITY_ADMIN_PASSWORD=${GRAFANA_PASSWORD}
    volumes:
      - grafana_data:/var/lib/grafana
    networks:
      - platform-network
```

#### ELK Stack (Elasticsearch, Logstash, Kibana)
```yaml
services:
  elasticsearch:
    image: docker.elastic.co/elasticsearch/elasticsearch:8.0.0
    environment:
      - discovery.type=single-node
      - xpack.security.enabled=false
    ports:
      - "9200:9200"

  kibana:
    image: docker.elastic.co/kibana/kibana:8.0.0
    ports:
      - "5601:5601"
    depends_on:
      - elasticsearch
```

### 4. 🔐 Network Security

#### Firewall Rules (UFW - Linux)
```bash
# Autoriser que le port 443 (HTTPS)
sudo ufw allow 443/tcp
sudo ufw allow 80/tcp  # Redirection HTTPS
sudo ufw deny 5001/tcp  # Docker port - pas d'exposition
sudo ufw deny 1433/tcp  # SQL Server - port interne
sudo ufw deny 6380/tcp  # Redis - port interne
```

#### Network Policy (Kubernetes)
```yaml
apiVersion: networking.k8s.io/v1
kind: NetworkPolicy
metadata:
  name: auth-service-policy
spec:
  podSelector:
    matchLabels:
      app: auth-service
  policyTypes:
  - Ingress
  - Egress
  ingress:
  - from:
    - podSelector:
        matchLabels:
          app: api-gateway
    ports:
    - protocol: TCP
      port: 8080
```

### 5. 📦 Scan de vulnérabilités

```bash
# Trivy - Scan d'images Docker
trivy image zoubairmabrouk/auth-service:1.0.0

# Grype - Scan des dépendances
grype zoubairmabrouk/auth-service:1.0.0

# Snyk - Monitoring des vulnérabilités
snyk monitor --file=AUTH-Sevice.csproj
```

### 6. 🔄 Backup & Disaster Recovery

#### Backup automatisé SQL Server
```bash
#!/bin/bash
# backup.sh
DATE=$(date +%Y%m%d_%H%M%S)
docker-compose exec -T sqlserver \
  /opt/mssql-tools/bin/sqlcmd \
  -S localhost -U sa -P $SA_PASSWORD \
  -Q "BACKUP DATABASE [AuthServiceDb] TO DISK = '/var/opt/mssql/backup/AuthServiceDb_${DATE}.bak'"

# Upload vers S3
aws s3 cp backups/AuthServiceDb_${DATE}.bak s3://my-backup-bucket/
```

#### Cron Job (Linux)
```bash
# Ajouter au crontab:
0 2 * * * /app/backup.sh  # Chaque jour à 2h du matin
```

### 7. 🔐 SQL Server Hardening

```yaml
sqlserver:
  environment:
    - MSSQL_PID=Express
    - MSSQL_SA_PASSWORD=${SA_PASSWORD}
    - MSSQL_MEMORY_LIMIT_MB=2048
    - MSSQL_COLLATION=SQL_Latin1_General_CP1_CI_AS
    - MSSQL_LANGUAGE=English
```

Recommandations supplémentaires :
```sql
-- Activer Transparent Data Encryption (TDE)
CREATE MASTER KEY ENCRYPTION BY PASSWORD = '${STRONG_PASSWORD}'
CREATE CERTIFICATE TDE_Cert WITH SUBJECT = 'TDE'
CREATE DATABASE ENCRYPTION KEY WITH ALGORITHM = AES_256
ENCRYPTION BY SERVER CERTIFICATE TDE_Cert
ALTER DATABASE AuthServiceDb SET ENCRYPTION ON

-- Auditer les connexions
CREATE AUDIT auth_audit
  TO FILE (FILEPATH = '/var/opt/mssql/audit')
CREATE SERVER AUDIT SPECIFICATION auth_spec
  FOR SERVER AUDIT auth_audit
  ADD (FAILED_LOGIN_GROUP, SUCCESSFUL_LOGIN_GROUP)
```

### 8. 🔐 Application Security Headers

```csharp
// Dans Program.cs - ajouter les headers de sécurité
app.Use(async (context, next) =>
{
    context.Response.Headers.Add("Strict-Transport-Security", "max-age=31536000; includeSubDomains");
    context.Response.Headers.Add("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Add("X-Frame-Options", "DENY");
    context.Response.Headers.Add("X-XSS-Protection", "1; mode=block");
    context.Response.Headers.Add("Referrer-Policy", "strict-origin-when-cross-origin");
    context.Response.Headers.Add("Content-Security-Policy", "default-src 'self'");
    
    await next();
});
```

### 9. 📊 Rate Limiting & DDoS Protection

```bash
# Utiliser Cloudflare ou un WAF
# Configuration recommandée :
- Rate limiting: 100 requêtes/minute par IP
- DDoS protection: Activé
- Bot management: Activé
- Web Application Firewall (WAF): Activé
```

### 10. 🔍 Logging & Audit Trail

```yaml
# Centraliser les logs
services:
  syslog:
    image: amir20/dnsmasq:latest
    # Configuration pour centraliser les logs Docker
```

## ✅ Checklist Sécurité Avancée

- [ ] Secrets gérés via Vault/Docker Secrets/K8s Secrets
- [ ] HTTPS/TLS activé avec certificats valides
- [ ] Reverse proxy configuré (nginx/HAProxy)
- [ ] Monitoring actif (Prometheus/Grafana)
- [ ] Logging centralisé (ELK Stack)
- [ ] Backups automatisés et testés
- [ ] Scan de vulnérabilités configurés (Trivy/Grype)
- [ ] Firewall configuré
- [ ] SQL Server chiffré avec TDE
- [ ] Audit trails activés
- [ ] Rate limiting configuré
- [ ] CORS configuré de manière restrictive
- [ ] Tests de sécurité effectués
- [ ] Plan de réponse aux incidents défini
- [ ] Mise à jour de sécurité automatisée

## 📚 Ressources

- [OWASP Docker Security](https://owasp.org/www-project-docker-security/)
- [CIS Docker Benchmark](https://www.cisecurity.org/cis-benchmarks/)
- [Docker Security Best Practices](https://docs.docker.com/engine/security/)
- [SQL Server Security](https://docs.microsoft.com/en-us/sql/relational-databases/security/security-center-for-sql-server-database-engine-and-azure-sql-database)
- [ASP.NET Core Security](https://docs.microsoft.com/en-us/aspnet/core/security/)

## 🚨 Incident Response

En cas de violation de sécurité :

1. **Immédiatement**
   - Arrêter le service : `docker-compose down`
   - Isoler le réseau
   - Collecter les logs

2. **Investigation** (1-2 heures)
   - Analyser les logs Docker et application
   - Identifier la cause
   - Vérifier l'impact

3. **Remédiation** (2-6 heures)
   - Patcher les vulnérabilités
   - Réinitialiser les secrets compromis
   - Restaurer depuis un backup sûr

4. **Récupération**
   - Redéployer avec les corrections
   - Vérifier l'intégrité
   - Monitorer 24-48 heures

5. **Post-Incident**
   - Documenter la leçon apprise
   - Améliorer les défenses
   - Communiquer avec les utilisateurs affectés
