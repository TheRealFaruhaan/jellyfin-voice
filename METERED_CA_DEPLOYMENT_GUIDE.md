# Deployment Guide - Voice Chat with Metered.ca (Free TURN Server)

## Overview

This guide uses **Metered.ca** for TURN/STUN servers instead of setting up coturn. Much simpler!

✅ **No TURN server setup needed**
✅ **Free tier available**
✅ **Works across all networks**
✅ **Professional infrastructure**

---

## Part 1: Get Your Metered.ca Credentials

### Step 1: Login to Metered.ca

Go to: https://dashboard.metered.ca/

### Step 2: Get Your Credentials

After logging in:

1. Go to **Dashboard**
2. Find your **API Key** (this is your username)
3. Find your **Secret Key** (this is your credential)

**Save these - you'll need them soon!**

Example:

-   Username: `abc123def456`
-   Credential: `xyz789uvw012`

---

## Part 2: Build on Windows (One Time Only)

### Step 1: Build Jellyfin Server

```powershell
cd "Path\To\jellyfin-voice"

# Publish for Linux
dotnet publish Jellyfin.Server --configuration Release --runtime linux-x64 --self-contained true --output ./publish
```

**Wait for:** `Build succeeded` message

### Step 2: Build Jellyfin Web

```powershell
cd "Path\To\jellyfin-voice-web"

# Initialize git (if not already done)
git init
git add .
git commit -m "Initial commit"

# Build
npm run build:production
```

**Wait for:** Build to complete (ignore line ending warnings)

---

## Part 3: Transfer Files to Server

### Recommended: Use WinSCP (Easy GUI)

1. **Download WinSCP**: https://winscp.net/eng/download.php
2. **Install and open WinSCP**
3. **Connect:**

    - Host: `xxx.xxx.xxx.xxx`
    - User: `root`
    - Password: (your server password)
    - Click **Login**

4. **Upload files:**

    **Left panel** (your computer) → **Right panel** (server):

    - Navigate left to: `Path\To\jellyfin-voice\publish`
    - Navigate right to: `/tmp/jellyfin-server/`
    - Select all files on left, drag to right

    - Navigate left to: `Path\To\jellyfin-voice-web\dist`
    - Navigate right to: `/tmp/jellyfin-web/`
    - Select all files on left, drag to right

### Alternative: Command Line (PowerShell)

```powershell
# Create directories on server
ssh root@217.216.109.130 "mkdir -p /tmp/jellyfin-server /tmp/jellyfin-web"

# Transfer server files
scp -r "Path\To\jellyfin-voice\publish\*" root@217.216.109.130:/tmp/jellyfin-server/

# Transfer web files
scp -r "Path\To\jellyfin-voice-web\dist\*" root@217.216.109.130:/tmp/jellyfin-web/
```

---

## Part 4: Install on Ubuntu Server

SSH into your server:

```bash
ssh root@your.ip
```

### Step 1: Install Prerequisites

```bash
sudo apt update
sudo apt install -y ffmpeg libssl-dev libcurl4-openssl-dev
```

**Note:** No need to install coturn! We're using Metered.ca.

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

## Part 5: Configure Voice Chat with Metered.ca

### Step 1: Create Configuration File

```bash
sudo nano /etc/jellyfin/voicechat.json
```

### Step 2: Paste This Configuration

**Replace** `YOUR_METERED_USERNAME` and `YOUR_METERED_CREDENTIAL` with your actual values from Part 1:

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
            "Urls": ["turn:global.relay.metered.ca:80", "turn:global.relay.metered.ca:80?transport=tcp", "turn:global.relay.metered.ca:443", "turns:global.relay.metered.ca:443?transport=tcp"],
            "Username": "abc123def456",
            "Credential": "xyz789uvw012",
            "CredentialType": "password"
        }
    ],
    "MaxParticipantsPerGroup": 10,
    "SignalingTimeoutSeconds": 30
}
```

**Save:** Press `Ctrl+X`, then `Y`, then `Enter`

### Step 3: Set Permissions

```bash
sudo chown jellyfin:jellyfin /etc/jellyfin/voicechat.json
sudo chmod 644 /etc/jellyfin/voicechat.json
```

---

## Part 6: Configure Firewall

Only need to open Jellyfin's port (no TURN server ports needed):

```bash
sudo ufw allow 8096/tcp
sudo ufw enable
sudo ufw status
```

---

## Part 7: Create Jellyfin Service

```bash
sudo nano /etc/systemd/system/jellyfin.service
```

Paste this:

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

**Save:** `Ctrl+X`, `Y`, `Enter`

---

## Part 8: Start Jellyfin

```bash
sudo systemctl daemon-reload
sudo systemctl start jellyfin
sudo systemctl enable jellyfin
sudo systemctl status jellyfin
```

You should see: **Active: active (running)**

### Check Logs

```bash
sudo journalctl -u jellyfin -f
```

Look for:

-   `Loaded voice chat configuration from /etc/jellyfin/voicechat.json` ✅
-   No errors

Press `Ctrl+C` to exit logs.

---

## Part 9: Access Jellyfin

1. Open browser: `http://your.ip:8096`
2. Complete the setup wizard:
    - Choose language
    - Create admin account
    - Set up media libraries (optional for now)
    - Finish setup

---

## Part 10: Test Voice Chat! 🎉

### Setup

1. **Browser 1** (your computer):

    - Login to Jellyfin
    - Go to any video
    - Click the SyncPlay button (people icon)
    - Create a new group

2. **Browser 2** (another device or incognito window):
    - Login to Jellyfin
    - Go to the same video
    - Click SyncPlay
    - Join the group

### Test Voice Chat

In both browsers:

1. Look for "Join Voice Chat" button (you may need to integrate the UI first - see integration guide)
2. Click it
3. Allow microphone permissions
4. Start talking - you should hear each other!

---

## Updating Configuration Later

Need to change settings? Easy:

```bash
# Edit config
sudo nano /etc/jellyfin/voicechat.json

# Make your changes, save

# Restart Jellyfin
sudo systemctl restart jellyfin
```

**No rebuild needed!** 🎉

## To Update Full Server Binary With Web UI

1. Build the server using the following command

```powershell
cd "C:\Users\faruh\OneDrive\Documents\Projects\media-server\jellyfin-master"

# Publish for Linux
dotnet publish Jellyfin.Server --configuration Release --runtime linux-x64 --self-contained true --output ./publish
```

2. Build the Web UI

```powershell
cd "C:\Users\faruh\OneDrive\Documents\Projects\media-server\jellyfin-web-master"

# Build NPM
npm run build:production
```

3. Copy and replace the contents in the `dist` folder inside web master folder to `publish\jellyfin-web` folder inside jellyfin master folder
4. Zip all the files inside `publish` folder
5. Copy the zip file to server `/tmp/jellyfin-server` folder
6. Run the following commands:

```bash
unzip publish.zip
rm publish.zip
sudo systemctl stop jellyfin
sudo rm -rf /opt/jellyfin/*
sudo mv /tmp/jellyfin-server/* /opt/jellyfin/
sudo chown -R jellyfin:jellyfin /opt/jellyfin
sudo chown -R jellyfin:jellyfin /opt/jellyfin/jellyfin-web
sudo chmod +x /opt/jellyfin/jellyfin
sudo systemctl start jellyfin
sudo systemctl restart nginx
```

---

## Troubleshooting

### Can't Access Jellyfin

```bash
# Check if running
sudo systemctl status jellyfin

# Check logs
sudo journalctl -u jellyfin -n 50

# Check firewall
sudo ufw status
```

### Voice Chat Not Working

1. **Check config loaded:**

    ```bash
    sudo journalctl -u jellyfin | grep "voice chat"
    ```

    Should see: `Loaded voice chat configuration`

2. **Verify config file:**

    ```bash
    cat /etc/jellyfin/voicechat.json
    ```

    Check credentials are correct

3. **Test in browser console (F12):**

    - Should see WebRTC connections being established
    - Check for errors

4. **Verify Metered.ca credentials:**
    - Login to dashboard.metered.ca
    - Check API key and secret are correct
    - Check free tier limits haven't been exceeded

### No Audio

-   Grant microphone permissions in browser
-   Check browser console for errors
-   Use headphones to avoid feedback
-   Test with https://webrtc.github.io/samples/src/content/peerconnection/trickle-ice/
    -   Add your Metered.ca TURN server
    -   Should see "relay" candidates

---

## What You Saved

By using Metered.ca instead of self-hosted coturn:

✅ **No TURN server setup** - Saved 30+ minutes
✅ **No firewall complexity** - Only port 8096 needed
✅ **No maintenance** - Metered handles updates
✅ **Professional infrastructure** - Better reliability
✅ **Free tier** - 50GB/month bandwidth included

---

## Quick Reference

```bash
# Restart Jellyfin
sudo systemctl restart jellyfin

# View logs
sudo journalctl -u jellyfin -f

# Edit voice chat config
sudo nano /etc/jellyfin/voicechat.json

# Check status
sudo systemctl status jellyfin

# View config
cat /etc/jellyfin/voicechat.json
```

---

## Summary

✅ Jellyfin server running on Ubuntu
✅ Voice chat configured with Metered.ca
✅ No TURN server to maintain
✅ Works across all networks
✅ Free tier included

**Next Step:** Follow the **VOICE_CHAT_INTEGRATION.md** guide to integrate the voice chat UI with SyncPlay!

---

## Metered.ca Free Tier Limits

-   **50GB/month** bandwidth
-   **Unlimited** concurrent connections
-   **Global** relay servers

Perfect for testing and small deployments! Upgrade if you need more.

**Your deployment is complete!** 🚀

---

## Part 11: Media Acquisition Setup (Optional)

The Media Acquisition module allows you to search for and download missing TV episodes and movies using qBittorrent and Prowlarr/Jackett indexers.

### Prerequisites

1. **qBittorrent** with Web UI enabled
2. **Prowlarr** (recommended) or Jackett for indexer management

---

### Step 1: Install and Configure Prowlarr

#### Install Prowlarr on Ubuntu

```bash
# Install dependencies
sudo apt install curl sqlite3

# Download and run the official installer
curl -o /tmp/prowlarr-install.sh https://raw.githubusercontent.com/Servarr/Wiki/master/assets/scripts/servarr-install-script.sh
sudo bash /tmp/prowlarr-install.sh prowlarr
```

**Alternative: Manual Installation**

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

Prowlarr will be available at: `http://your.ip:9696`

#### Configure Prowlarr

1. **Open Prowlarr**: `http://your.ip:9696`
2. **Complete initial setup** (create username/password)
3. **Add Indexers**:
   - Go to **Indexers** → **Add Indexer**
   - Search for your preferred trackers (e.g., 1337x, RARBG, etc.)
   - Configure each indexer with required credentials
4. **Get API Key**:
   - Go to **Settings** → **General**
   - Copy the **API Key** (you'll need this for Jellyfin)

#### Open Firewall for Prowlarr (if needed locally only)

```bash
# Only if you need external access to Prowlarr
sudo ufw allow 9696/tcp
```

---

### Step 2: Install and Configure qBittorrent

#### Install qBittorrent on Ubuntu

```bash
sudo apt install qbittorrent-nox

# Create systemd service
sudo nano /etc/systemd/system/qbittorrent.service
```

Paste this service configuration:

```ini
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
```

Start qBittorrent:

```bash
sudo systemctl daemon-reload
sudo systemctl start qbittorrent
sudo systemctl enable qbittorrent
```

qBittorrent Web UI will be available at: `http://your.ip:8080`

**Default credentials:**
- Username: `admin`
- Password: `adminadmin`

**Important:** Change the default password immediately!

1. Open `http://your.ip:8080`
2. Go to **Tools** → **Options** → **Web UI**
3. Change the password

---

### Step 3: Configure Jellyfin Media Acquisition

Create the Media Acquisition configuration file:

```bash
sudo nano /etc/jellyfin/mediaacquisition.json
```

Paste this configuration (update with your credentials):

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
sudo chown jellyfin:jellyfin /etc/jellyfin/mediaacquisition.json
sudo chmod 644 /etc/jellyfin/mediaacquisition.json
```

---

### Step 4: Update Jellyfin Configuration

Add the Media Acquisition settings to your main Jellyfin config:

```bash
sudo nano /etc/jellyfin/appsettings.json
```

Add the MediaAcquisition section:

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

Restart Jellyfin:

```bash
sudo systemctl restart jellyfin
```

---

### Step 5: Using Media Acquisition

1. **Access the Dashboard**: Login as an admin user
2. **Navigate to Downloads**: In the admin dashboard sidebar, click "Media Acquisition"
3. **View Missing Episodes**: The "Missing Episodes" tab shows all virtual/missing episodes
4. **Search for Torrents**: Click on a missing episode to search for available torrents
5. **Start Download**: Select a torrent result to start downloading via qBittorrent
6. **Monitor Progress**: The "Active Downloads" tab shows real-time download progress
7. **Auto-Import**: Completed downloads are automatically imported into your library

---

### Media Acquisition Configuration Options

| Option | Description | Default |
|--------|-------------|---------|
| `Enabled` | Enable/disable the module | `false` |
| `QBittorrentUrl` | qBittorrent Web UI URL | `http://localhost:8080` |
| `QBittorrentUsername` | qBittorrent username | `admin` |
| `QBittorrentPassword` | qBittorrent password | (required) |
| `AutoImportEnabled` | Auto-import completed downloads | `true` |
| `Indexers` | Array of Torznab indexers | `[]` |

### Indexer Configuration Options

| Option | Description |
|--------|-------------|
| `Name` | Display name for the indexer |
| `BaseUrl` | Indexer API URL |
| `ApiKey` | API key for authentication |
| `Enabled` | Enable/disable this indexer |
| `Priority` | Search priority (lower = higher priority) |

---

### Adding Multiple Indexers

You can add multiple indexers for better search coverage:

```json
{
  "MediaAcquisition": {
    "Enabled": true,
    "QBittorrentUrl": "http://localhost:8080",
    "QBittorrentUsername": "admin",
    "QBittorrentPassword": "your_password",
    "AutoImportEnabled": true,
    "Indexers": [
      {
        "Name": "Prowlarr",
        "BaseUrl": "http://localhost:9696",
        "ApiKey": "prowlarr_api_key",
        "Enabled": true,
        "Priority": 1
      },
      {
        "Name": "Jackett",
        "BaseUrl": "http://localhost:9117",
        "ApiKey": "jackett_api_key",
        "Enabled": true,
        "Priority": 2
      }
    ]
  }
}
```

---

### Troubleshooting Media Acquisition

#### Check if Module is Loaded

```bash
sudo journalctl -u jellyfin | grep -i "media acquisition"
```

#### Test qBittorrent Connection

```bash
# Check if qBittorrent is running
curl -s http://localhost:8080/api/v2/app/version
```

#### Test Prowlarr Connection

```bash
# Check if Prowlarr is running
curl -s "http://localhost:9696/api/v1/health?apikey=YOUR_API_KEY"
```

#### Common Issues

1. **"Failed to authenticate with qBittorrent"**
   - Verify username/password are correct
   - Check qBittorrent Web UI is enabled and accessible

2. **"No search results"**
   - Verify Prowlarr has working indexers configured
   - Check Prowlarr API key is correct
   - Test search directly in Prowlarr UI

3. **"Download not starting"**
   - Check qBittorrent has sufficient disk space
   - Verify download path is writable
   - Check qBittorrent logs for errors

---

### Quick Reference - Media Acquisition

```bash
# Restart all services
sudo systemctl restart jellyfin qbittorrent prowlarr

# View Jellyfin logs
sudo journalctl -u jellyfin -f

# View qBittorrent logs
sudo journalctl -u qbittorrent -f

# View Prowlarr logs
sudo journalctl -u prowlarr -f

# Check service status
sudo systemctl status jellyfin qbittorrent prowlarr
```

---

### Security Considerations

1. **Change default passwords** for qBittorrent and Prowlarr
2. **Use localhost URLs** when services are on the same machine
3. **Don't expose qBittorrent/Prowlarr** to the internet unless necessary
4. **Use a VPN** for torrent downloads if required in your jurisdiction
5. **Media Acquisition is admin-only** - regular users cannot access it
