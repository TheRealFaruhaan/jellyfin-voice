# Complete Deployment Guide - Jellyfin with Cloudflare SSL, Voice Chat & Media Acquisition

## Overview

This comprehensive guide sets up:

- ✅ Jellyfin Media Server on HTTPS (port 443)
- ✅ Cloudflare SSL (Full Strict mode)
- ✅ Nginx reverse proxy
- ✅ Voice Chat with Metered.ca TURN/STUN
- ✅ Media Acquisition with qBittorrent & Prowlarr
- ✅ All services secured behind SSL

---

## Prerequisites

- Domain name pointed to your server via Cloudflare DNS
- Cloudflare account with domain configured
- Ubuntu server (20.04+ recommended)
- Windows machine for building (or use pre-built releases)

---

## Part 1: Build on Windows

### Step 1: Build Jellyfin Server

```powershell
cd "C:\Users\faruh\OneDrive\Documents\Projects\media-server\jellyfin-master"

# Publish for Linux
dotnet publish Jellyfin.Server --configuration Release --runtime linux-x64 --self-contained true --output ./publish
```

**Wait for:** `Build succeeded` message

### Step 2: Build Jellyfin Web

```powershell
cd "C:\Users\faruh\OneDrive\Documents\Projects\media-server\jellyfin-web-master"

# Initialize git (if not already done)
git init
git add .
git commit -m "Initial commit"

# Build
npm run build:production
```

**Wait for:** Build to complete (ignore line ending warnings)

---

## Part 2: Transfer Files to Server

### Using WinSCP (Recommended)

1. **Download WinSCP**: https://winscp.net/eng/download.php
2. **Connect:**
   - Host: `your.server.ip`
   - User: `root`
   - Password: (your server password)
3. **Upload files:**
   - `jellyfin-master\publish\*` → `/tmp/jellyfin-server/`
   - `jellyfin-web-master\dist\*` → `/tmp/jellyfin-web/`

### Or Command Line (PowerShell)

```powershell
ssh root@your.server.ip "mkdir -p /tmp/jellyfin-server /tmp/jellyfin-web"

scp -r "C:\path\to\jellyfin-master\publish\*" root@your.server.ip:/tmp/jellyfin-server/
scp -r "C:\path\to\jellyfin-web-master\dist\*" root@your.server.ip:/tmp/jellyfin-web/
```

---

## Part 3: Install on Ubuntu Server

SSH into your server:

```bash
ssh root@your.server.ip
```

### Step 1: Install Prerequisites

```bash
sudo apt update
sudo apt install -y ffmpeg libssl-dev libcurl4-openssl-dev nginx certbot python3-certbot-nginx curl sqlite3
```

### Step 2: Create Jellyfin User & Directories

```bash
sudo useradd -r -s /bin/false jellyfin
sudo mkdir -p /opt/jellyfin /var/lib/jellyfin /var/log/jellyfin /etc/jellyfin
sudo chown -R jellyfin:jellyfin /opt/jellyfin /var/lib/jellyfin /var/log/jellyfin /etc/jellyfin
```

### Step 3: Move Files to Final Location

```bash
# Move server files
sudo mv /tmp/jellyfin-server/* /opt/jellyfin/
sudo chown -R jellyfin:jellyfin /opt/jellyfin

# Move web files
sudo mkdir -p /opt/jellyfin/jellyfin-web
sudo mv /tmp/jellyfin-web/* /opt/jellyfin/jellyfin-web/
sudo chown -R jellyfin:jellyfin /opt/jellyfin/jellyfin-web

# Make executable
sudo chmod +x /opt/jellyfin/jellyfin
```

---

## Part 4: Configure Voice Chat with Metered.ca

### Step 1: Get Metered.ca Credentials

1. Go to: https://dashboard.metered.ca/
2. Login/create account
3. Get your **API Key** (username) and **Secret Key** (credential)

### Step 2: Create Voice Chat Configuration

```bash
sudo nano /etc/jellyfin/voicechat.json
```

Paste (replace credentials):

```json
{
    "Enabled": true,
    "IceServers": [
        {
            "Urls": ["stun:stun.relay.metered.ca:80"],
            "Username": null,
            "Credential": null,
            "CredentialType": "password"
        },
        {
            "Urls": [
                "turn:global.relay.metered.ca:80",
                "turn:global.relay.metered.ca:80?transport=tcp",
                "turn:global.relay.metered.ca:443",
                "turns:global.relay.metered.ca:443?transport=tcp"
            ],
            "Username": "YOUR_METERED_USERNAME",
            "Credential": "YOUR_METERED_CREDENTIAL",
            "CredentialType": "password"
        }
    ],
    "MaxParticipantsPerGroup": 10,
    "SignalingTimeoutSeconds": 30
}
```

Save and set permissions:

```bash
sudo chown jellyfin:jellyfin /etc/jellyfin/voicechat.json
sudo chmod 644 /etc/jellyfin/voicechat.json
```

---

## Part 5: Configure Jellyfin Service

```bash
sudo nano /etc/systemd/system/jellyfin.service
```

Paste:

```ini
[Unit]
Description=Jellyfin Media Server
After=network-online.target
Wants=network-online.target

[Service]
Type=simple
User=jellyfin
Group=jellyfin
WorkingDirectory=/opt/jellyfin
ExecStart=/opt/jellyfin/jellyfin \
    --datadir=/var/lib/jellyfin \
    --configdir=/etc/jellyfin \
    --logdir=/var/log/jellyfin \
    --webdir=/opt/jellyfin/jellyfin-web
Restart=on-failure
TimeoutSec=15

[Install]
WantedBy=multi-user.target
```

Save and start:

```bash
sudo systemctl daemon-reload
sudo systemctl start jellyfin
sudo systemctl enable jellyfin
sudo systemctl status jellyfin
```

Check it's running:

```bash
curl http://localhost:8096
```

---

## Part 6: Configure Cloudflare

### Step 1: Set DNS Records

In Cloudflare dashboard for your domain:

1. **A Record:**
   - Name: `vstream` (or your subdomain)
   - Type: `A`
   - Content: `your.server.ip`
   - Proxy status: **Proxied** (orange cloud ON)
   - TTL: Auto

This creates: `vstream.yourdomain.com`

### Step 2: Configure SSL/TLS Mode

1. Go to **SSL/TLS** tab
2. Select **Full (strict)** encryption mode

---

## Part 7: Get SSL Certificate (Cloudflare Origin)

### Step 1: Create Origin Certificate

1. In Cloudflare → **SSL/TLS** → **Origin Server**
2. Click **Create Certificate**
3. Keep defaults (RSA, 15 years)
4. Click **Create**
5. Copy **Origin Certificate** and **Private Key**

### Step 2: Install Certificate on Server

Save certificate:

```bash
sudo nano /etc/ssl/certs/cloudflare-origin.pem
# Paste the Origin Certificate
# Save: Ctrl+X, Y, Enter
```

Save private key:

```bash
sudo nano /etc/ssl/private/cloudflare-origin.key
# Paste the Private Key
# Save: Ctrl+X, Y, Enter

# Secure the key
sudo chmod 600 /etc/ssl/private/cloudflare-origin.key
```

---

## Part 8: Install and Configure Prowlarr

### Step 1: Install Prowlarr

```bash
# Create user and directories
sudo useradd -r -s /bin/false prowlarr
sudo mkdir -p /opt/prowlarr /var/lib/prowlarr
sudo chown prowlarr:prowlarr /opt/prowlarr /var/lib/prowlarr

# Download latest release
cd /tmp
wget -q "https://prowlarr.servarr.com/v1/update/master/updatefile?os=linux&runtime=netcore&arch=x64" -O prowlarr.tar.gz
tar -xzf prowlarr.tar.gz
sudo mv Prowlarr/* /opt/prowlarr/
sudo chown -R prowlarr:prowlarr /opt/prowlarr

# Create systemd service
sudo tee /etc/systemd/system/prowlarr.service > /dev/null << 'EOF'
[Unit]
Description=Prowlarr Daemon
After=network.target

[Service]
User=prowlarr
Group=prowlarr
Type=simple
ExecStart=/opt/prowlarr/Prowlarr -nobrowser -data=/var/lib/prowlarr
Restart=on-failure

[Install]
WantedBy=multi-user.target
EOF

# Start and enable
sudo systemctl daemon-reload
sudo systemctl start prowlarr
sudo systemctl enable prowlarr
```

Prowlarr is now running at: `http://localhost:9696`

### Step 2: Configure Prowlarr

1. Open `http://your.server.ip:9696` (temporarily, we'll proxy later)
2. Complete initial setup (create username/password)
3. Add indexers: **Indexers** → **Add Indexer**
4. Get API Key: **Settings** → **General** → Copy **API Key**

---

## Part 9: Install and Configure qBittorrent

### Step 1: Install qBittorrent

```bash
sudo apt install -y qbittorrent-nox

# Create systemd service
sudo tee /etc/systemd/system/qbittorrent.service > /dev/null << 'EOF'
[Unit]
Description=qBittorrent-nox
After=network.target

[Service]
Type=forking
User=jellyfin
Group=jellyfin
UMask=007
ExecStart=/usr/bin/qbittorrent-nox -d
Restart=on-failure

[Install]
WantedBy=multi-user.target
EOF

# Start and enable
sudo systemctl daemon-reload
sudo systemctl start qbittorrent
sudo systemctl enable qbittorrent
```

qBittorrent Web UI: `http://localhost:8080`

**Default credentials:**
- Username: `admin`
- Password: `adminadmin`

**Important:** Change the default password!

---

## Part 10: Configure Nginx Reverse Proxy (All Services)

### Step 1: Create Nginx Configuration

```bash
sudo nano /etc/nginx/sites-available/jellyfin
```

Paste this complete configuration:

```nginx
# Upstreams
upstream jellyfin_backend {
    server 127.0.0.1:8096;
}

upstream prowlarr_backend {
    server 127.0.0.1:9696;
}

upstream qbittorrent_backend {
    server 127.0.0.1:8080;
}

# Redirect HTTP to HTTPS
server {
    listen 80;
    server_name vstream.yourdomain.com prow.yourdomain.com qbit.yourdomain.com;
    return 301 https://$server_name$request_uri;
}

# Jellyfin HTTPS
server {
    listen 443 ssl http2;
    server_name vstream.yourdomain.com;

    # SSL Configuration
    ssl_certificate /etc/ssl/certs/cloudflare-origin.pem;
    ssl_certificate_key /etc/ssl/private/cloudflare-origin.key;
    ssl_protocols TLSv1.2 TLSv1.3;
    ssl_ciphers HIGH:!aNULL:!MD5;
    ssl_prefer_server_ciphers on;

    # Security headers
    add_header Strict-Transport-Security "max-age=31536000; includeSubDomains" always;
    add_header X-Frame-Options "SAMEORIGIN" always;
    add_header X-Content-Type-Options "nosniff" always;
    add_header X-XSS-Protection "1; mode=block" always;

    # Max upload size
    client_max_body_size 20G;

    # Main proxy
    location / {
        proxy_pass http://jellyfin_backend;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
        proxy_set_header X-Forwarded-Host $http_host;

        # WebSocket support
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection "upgrade";
        proxy_buffering off;
    }

    # SSE endpoint
    location /System/Events {
        proxy_pass http://jellyfin_backend/System/Events;
        proxy_http_version 1.1;
        proxy_set_header Connection "";
        chunked_transfer_encoding off;
        proxy_buffering off;
        proxy_cache off;
    }
}

# Prowlarr HTTPS
server {
    listen 443 ssl http2;
    server_name prow.yourdomain.com;

    ssl_certificate /etc/ssl/certs/cloudflare-origin.pem;
    ssl_certificate_key /etc/ssl/private/cloudflare-origin.key;
    ssl_protocols TLSv1.2 TLSv1.3;
    ssl_ciphers HIGH:!aNULL:!MD5;
    ssl_prefer_server_ciphers on;

    add_header Strict-Transport-Security "max-age=31536000; includeSubDomains" always;
    add_header X-Frame-Options "SAMEORIGIN" always;

    # Prowlarr authentication is built-in
    location / {
        proxy_pass http://prowlarr_backend;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;

        # WebSocket support
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection "upgrade";
    }
}

# qBittorrent HTTPS
server {
    listen 443 ssl http2;
    server_name qbit.yourdomain.com;

    ssl_certificate /etc/ssl/certs/cloudflare-origin.pem;
    ssl_certificate_key /etc/ssl/private/cloudflare-origin.key;
    ssl_protocols TLSv1.2 TLSv1.3;
    ssl_ciphers HIGH:!aNULL:!MD5;
    ssl_prefer_server_ciphers on;

    add_header Strict-Transport-Security "max-age=31536000; includeSubDomains" always;
    add_header X-Frame-Options "SAMEORIGIN" always;

    # qBittorrent authentication is built-in
    location / {
        proxy_pass http://qbittorrent_backend;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;

        # For large file uploads
        client_max_body_size 100M;

        # WebSocket support
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection "upgrade";
    }
}
```

**Replace `yourdomain.com` with your actual domain!**

### Step 2: Add DNS Records in Cloudflare

Add these A records (all pointing to your server IP, Proxied):
- `vstream` → your.server.ip
- `prow` → your.server.ip
- `qbit` → your.server.ip

### Step 3: Enable and Test

```bash
# Enable the site
sudo ln -sf /etc/nginx/sites-available/jellyfin /etc/nginx/sites-enabled/

# Remove default site
sudo rm -f /etc/nginx/sites-enabled/default

# Test configuration
sudo nginx -t

# If OK, restart Nginx
sudo systemctl restart nginx
sudo systemctl enable nginx
```

---

## Part 11: Configure Media Acquisition in Jellyfin

### Step 1: Create Configuration

```bash
sudo nano /etc/jellyfin/appsettings.json
```

Add or merge this configuration:

```json
{
  "MediaAcquisition": {
    "Enabled": true,
    "QBittorrentUrl": "http://localhost:8080",
    "QBittorrentUsername": "admin",
    "QBittorrentPassword": "YOUR_QBITTORRENT_PASSWORD",
    "AutoImportEnabled": true,
    "Indexers": [
      {
        "Name": "Prowlarr",
        "BaseUrl": "http://localhost:9696",
        "ApiKey": "YOUR_PROWLARR_API_KEY",
        "Enabled": true,
        "Priority": 1
      }
    ]
  }
}
```

**Replace:**
- `YOUR_QBITTORRENT_PASSWORD` - Your qBittorrent Web UI password
- `YOUR_PROWLARR_API_KEY` - Your Prowlarr API key from Settings → General

Set permissions:

```bash
sudo chown jellyfin:jellyfin /etc/jellyfin/appsettings.json
sudo chmod 644 /etc/jellyfin/appsettings.json
```

### Step 2: Restart Jellyfin

```bash
sudo systemctl restart jellyfin
```

---

## Part 12: Configure Firewall

```bash
# Allow HTTP and HTTPS
sudo ufw allow 80/tcp
sudo ufw allow 443/tcp

# Allow SSH (important!)
sudo ufw allow 22/tcp

# Enable firewall
sudo ufw enable
sudo ufw status
```

**Note:** Ports 8096, 8080, 9696 are NOT exposed - only accessible via Nginx proxy.

---

## Part 13: Configure Cloudflare Settings

### SSL/TLS Settings

1. **Encryption mode**: Full (strict) ✅
2. **Always Use HTTPS**: ON
3. **Automatic HTTPS Rewrites**: ON
4. **Minimum TLS Version**: 1.2

### Speed Settings (Optional)

1. **Auto Minify**: Enable HTML, CSS, JS
2. **Brotli**: ON
3. **HTTP/2**: ON
4. **HTTP/3**: ON

---

## Part 14: Test Your Setup

### Test All Services

```bash
# Check all services are running
sudo systemctl status jellyfin prowlarr qbittorrent nginx
```

### Access URLs

- **Jellyfin**: `https://vstream.yourdomain.com`
- **Prowlarr**: `https://prow.yourdomain.com`
- **qBittorrent**: `https://qbit.yourdomain.com`

All should show:
- ✅ Padlock icon (SSL working)
- ✅ No certificate warnings

### Test Voice Chat

1. Login to Jellyfin as two different users
2. Start SyncPlay on a video
3. Join Voice Chat
4. Grant microphone permissions
5. Test audio!

### Test Media Acquisition

1. Login as admin to Jellyfin
2. Navigate to Dashboard → Media Acquisition
3. View Missing Episodes tab
4. Search for torrents
5. Start a download
6. Monitor progress in Active Downloads

---

## Quick Reference

### Service Commands

```bash
# Status
sudo systemctl status jellyfin prowlarr qbittorrent nginx

# Restart all
sudo systemctl restart jellyfin prowlarr qbittorrent nginx

# View logs
sudo journalctl -u jellyfin -f
sudo journalctl -u prowlarr -f
sudo journalctl -u qbittorrent -f
sudo tail -f /var/log/nginx/error.log
```

### Configuration Files

```
/etc/jellyfin/voicechat.json          # Voice chat config
/etc/jellyfin/appsettings.json        # Media acquisition config
/etc/nginx/sites-available/jellyfin   # Nginx reverse proxy
/var/lib/prowlarr/config.xml          # Prowlarr config
```

### SSL Certificates

```
/etc/ssl/certs/cloudflare-origin.pem     # Origin certificate
/etc/ssl/private/cloudflare-origin.key   # Private key
```

---

## Updating Jellyfin

### Full Update Procedure

1. **Build on Windows:**

```powershell
cd "C:\Users\faruh\OneDrive\Documents\Projects\media-server\jellyfin-master"
dotnet publish Jellyfin.Server --configuration Release --runtime linux-x64 --self-contained true --output ./publish

cd "C:\Users\faruh\OneDrive\Documents\Projects\media-server\jellyfin-web-master"
npm run build:production
```

2. **Copy web to publish folder**, then zip and upload to server

3. **On server:**

```bash
cd /tmp/jellyfin-server
unzip publish.zip
rm publish.zip
sudo systemctl stop jellyfin
sudo rm -rf /opt/jellyfin/*
sudo mv /tmp/jellyfin-server/* /opt/jellyfin/
sudo chown -R jellyfin:jellyfin /opt/jellyfin
sudo chmod +x /opt/jellyfin/jellyfin
sudo systemctl start jellyfin
sudo systemctl restart nginx
```

---

## Troubleshooting

### Can't Access HTTPS Site

```bash
# Check Nginx
sudo nginx -t
sudo systemctl status nginx

# Check SSL certificates
sudo ls -la /etc/ssl/certs/cloudflare-origin.pem
sudo ls -la /etc/ssl/private/cloudflare-origin.key

# Check Nginx logs
sudo tail -f /var/log/nginx/error.log
```

### Voice Chat Not Working

1. Ensure HTTPS (required for microphone access)
2. Check browser console for errors
3. Verify Metered.ca credentials in `/etc/jellyfin/voicechat.json`
4. Test TURN server at: https://webrtc.github.io/samples/src/content/peerconnection/trickle-ice/

### Media Acquisition Issues

```bash
# Check module loaded
sudo journalctl -u jellyfin | grep -i "media acquisition"

# Test qBittorrent
curl -s http://localhost:8080/api/v2/app/version

# Test Prowlarr
curl -s "http://localhost:9696/api/v1/health?apikey=YOUR_API_KEY"
```

### Common Media Acquisition Issues

1. **"Failed to authenticate with qBittorrent"**
   - Verify username/password are correct
   - Check qBittorrent Web UI is enabled

2. **"No search results"**
   - Verify Prowlarr has working indexers
   - Check Prowlarr API key is correct
   - Test search directly in Prowlarr

3. **"Download not starting"**
   - Check qBittorrent disk space
   - Verify download path is writable

---

## Security Checklist

- ✅ HTTPS only (port 80 redirects to 443)
- ✅ Cloudflare DDoS protection enabled
- ✅ Strong SSL configuration (TLS 1.2+)
- ✅ Security headers enabled
- ✅ Internal ports not exposed (8096, 8080, 9696)
- ✅ Firewall configured (only 80, 443, 22)
- ✅ Origin Certificate (Cloudflare → Server encrypted)
- ✅ Changed default passwords for all services
- ✅ Media Acquisition is admin-only

---

## Architecture Summary

```
Internet
   ↓
Cloudflare (SSL, DDoS Protection)
   ↓
Your Server
   ↓
Nginx (Port 443, SSL)
   ├── vstream.domain.com → Jellyfin (8096)
   ├── prow.domain.com → Prowlarr (9696)
   └── qbit.domain.com → qBittorrent (8080)

Voice Chat: WebRTC via Metered.ca TURN/STUN
Media Acquisition: Prowlarr → qBittorrent → Auto-import
```

---

## Summary

✅ **Jellyfin on HTTPS** - Secure media streaming
✅ **Cloudflare SSL** - Full (strict) encryption
✅ **Nginx reverse proxy** - All services behind SSL
✅ **Voice Chat** - Metered.ca TURN/STUN configured
✅ **Media Acquisition** - Prowlarr + qBittorrent integration
✅ **Production ready** - Secure and scalable

**Your services are accessible at:**
- Jellyfin: `https://vstream.yourdomain.com`
- Prowlarr: `https://prow.yourdomain.com`
- qBittorrent: `https://qbit.yourdomain.com`

🚀 **Deployment complete!**
