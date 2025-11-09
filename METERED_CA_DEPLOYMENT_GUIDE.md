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

- Username: `abc123def456`
- Credential: `xyz789uvw012`

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
      "Urls": [
        "turn:global.relay.metered.ca:80",
        "turn:global.relay.metered.ca:80?transport=tcp",
        "turn:global.relay.metered.ca:443",
        "turns:global.relay.metered.ca:443?transport=tcp"
      ],
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

- `Loaded voice chat configuration from /etc/jellyfin/voicechat.json` ✅
- No errors

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

- Grant microphone permissions in browser
- Check browser console for errors
- Use headphones to avoid feedback
- Test with https://webrtc.github.io/samples/src/content/peerconnection/trickle-ice/
  - Add your Metered.ca TURN server
  - Should see "relay" candidates

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

- **50GB/month** bandwidth
- **Unlimited** concurrent connections
- **Global** relay servers

Perfect for testing and small deployments! Upgrade if you need more.

**Your deployment is complete!** 🚀
