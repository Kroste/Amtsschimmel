#!/usr/bin/env bash
# Baut das AppImage aus einem fertigen linux-x64-Publish-Ordner.
# Aufruf: packaging/linux/build-appimage.sh <version> <publish-dir>
set -euo pipefail

VERSION="$1"
PUBLISH_DIR="$2"
APPDIR="AppDir"

rm -rf "$APPDIR"
mkdir -p "$APPDIR/usr/bin"
cp -r "$PUBLISH_DIR"/* "$APPDIR/usr/bin/"
cp packaging/linux/amtsschimmel.desktop "$APPDIR/"
cp Amtsschimmel/Assets/amtsschimmel.png "$APPDIR/"
cp packaging/linux/AppRun "$APPDIR/AppRun"
chmod +x "$APPDIR/AppRun" "$APPDIR/usr/bin/Amtsschimmel"

curl -sSL -o appimagetool \
  https://github.com/AppImage/appimagetool/releases/download/continuous/appimagetool-x86_64.AppImage
chmod +x appimagetool
./appimagetool --appimage-extract-and-run "$APPDIR" "Amtsschimmel-${VERSION}-x86_64.AppImage"
echo "AppImage gebaut: Amtsschimmel-${VERSION}-x86_64.AppImage"
