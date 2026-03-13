#!/bin/bash
# Deploy to secretletter.plover.net
# Usage: ./deploy.sh /path/to/public_html
#
# Structure on server:
#   public_html/
#     index.html        <- landing page
#     images/           <- landing page assets
#     downloads/        <- Tauri installers
#     play/             <- WASM build (Blazor wwwroot)

set -e

DEST="${1:?Usage: ./deploy.sh /path/to/public_html}"
SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
REPO_DIR="$(dirname "$SCRIPT_DIR")"

echo "Deploying to $DEST..."

# Landing page
cp "$SCRIPT_DIR/index.html" "$DEST/"
mkdir -p "$DEST/images" "$DEST/downloads"
cp "$SCRIPT_DIR/images/"* "$DEST/images/"

# WASM player
WWWROOT="$REPO_DIR/publish/wwwroot"
if [ ! -d "$WWWROOT" ]; then
    WWWROOT="$REPO_DIR/SecretLetter.Browser/bin/Release/net8.0/browser-wasm/publish/wwwroot"
fi

if [ -d "$WWWROOT" ]; then
    mkdir -p "$DEST/play"
    rsync -a --delete "$WWWROOT/" "$DEST/play/"
    echo "WASM player deployed to $DEST/play/"
else
    echo "WARNING: Published wwwroot not found, skipping WASM player."
    echo "  Run: dotnet publish SecretLetter.Browser/SecretLetter.Browser.csproj -c Release"
fi

echo "Done. Landing page at $DEST/index.html"
