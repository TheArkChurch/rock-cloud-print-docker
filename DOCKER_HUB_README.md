# Rock Cloud Print — Docker

A community-maintained Docker port of the [Rock RMS](https://www.rockrms.com/) Cloud Print proxy service.

The original application is Windows-only. This image runs as a headless Linux container with a browser-based admin UI and is designed to run on any Linux server — including a Raspberry Pi — on the same network as your label printers.

```
Rock RMS Server  ──WebSocket──▶  This container  ──TCP:9100──▶  Local Printer
```

Commonly used for Rock Check-in label printing where printers are on a local church network but the Rock server is cloud-hosted.

---

## Quick start

```bash
# 1. Create a config folder (settings file is written here by the web UI)
mkdir -p rock-cloudprint/config

# 2. Create docker-compose.yml
cat > rock-cloudprint/docker-compose.yml <<'EOF'
services:
  rock-cloudprint:
    image: asdfinit/rock-cloudprint:latest
    network_mode: host
    volumes:
      - ./config:/app/config
    restart: unless-stopped
EOF

# 3. Start the container
cd rock-cloudprint
docker compose up -d

# 4. Open the web UI
# Navigate to http://<server-ip>:8080
```

---

## Configuration

Settings can be provided three ways. Environment variables take precedence over the web UI.

### Web UI (easiest)

Open `http://<server-ip>:8080`, go to **Settings**, and fill in:

| Field | Description |
|---|---|
| Rock Server URL | Full URL of your Rock instance, e.g. `https://origin.church.com` |
| Proxy ID | The **IdKey** (e.g. `da0BJR0Bpz`) from Rock's Cloud Print Proxy device record |
| Proxy Name | Optional friendly name; defaults to the container hostname |

### Environment variables

```yaml
environment:
  - Url=https://origin.church.com
  - Id=da0BJR0Bpz
  - Name=Office Proxy
  - Password=mypin        # optional PIN to protect the web UI
```

### Config file

Edit `config/appsettings.json` in the host config directory. Changes are detected automatically — no restart needed.

```json
{
  "Url": "https://origin.church.com",
  "Id": "da0BJR0Bpz",
  "Name": "Office Proxy"
}
```

---

## PIN / password protection

The web UI is open by default. To require a login:

- **Via web UI:** Settings → Security → Set PIN
- **Via env var:** Add `- Password=mypin` to your `docker-compose.yml` environment block

Tokens are in-memory only. Users must log in again after a container restart.

---

## Networking

This image uses `network_mode: host`. This is required on Linux so the container can open raw TCP connections to printers at their local IP addresses (port 9100). The web UI is then available on the host at port 8080 with no port mapping needed.

> **macOS / Windows Docker Desktop:** Host networking is not supported on these platforms. This image is intended for Linux servers only.

If your server runs UFW, allow port 8080:

```bash
sudo ufw allow 8080/tcp
```

---

## Printer addressing

Printers are configured in Rock — not in this app. In Rock's check-in printer settings, set the printer address to the printer's local IP:

- `192.168.1.50` — uses default port 9100
- `192.168.1.50:9100` — explicit port

---

## CDN / origin URL note

If your Rock site sits behind Cloudflare or another CDN, use the **origin server URL** rather than the primary domain. CDNs typically do not forward WebSocket upgrade requests, causing connection failures with the error:

```
The server returned status code '400' when status code '101' was expected.
```

Your origin URL is often `https://origin.yourdomain.com` or the direct IP/hostname of your web server.

---

## Auto-start on reboot

To ensure the container starts automatically after a server reboot, enable the systemd service (Ubuntu/Debian):

```bash
sudo bash -c 'cat > /etc/systemd/system/rock-cloudprint.service <<EOF
[Unit]
Description=Rock Cloud Print Proxy
After=docker.service
Requires=docker.service

[Service]
Type=oneshot
RemainAfterExit=yes
WorkingDirectory=/opt/rock-cloudprint
ExecStart=/usr/bin/docker compose up -d
ExecStop=/usr/bin/docker compose down
TimeoutStartSec=300

[Install]
WantedBy=multi-user.target
EOF'
sudo systemctl daemon-reload
sudo systemctl enable rock-cloudprint
```

---

## Web UI reference

| Tab | Description |
|---|---|
| Dashboard | Connection status, uptime, labels printed counter |
| Logs | Live service log (last 250 entries), color-coded by level |
| Settings → Connection | Rock server URL, Proxy ID, Proxy Name |
| Settings → Security | Set, change, or remove web UI PIN |

---

## Useful commands

```bash
docker compose up -d --build   # build and start
docker compose logs -f         # live logs
docker compose restart         # restart container
docker compose down            # stop
```

---

## Source

[hub.docker.com/r/asdfinit/rock-cloudprint](https://hub.docker.com/r/asdfinit/rock-cloudprint)

Based on [Rock RMS](https://github.com/SparkDevNetwork/Rock) — licensed under the [Rock Community License](http://www.rockrms.com/license).
