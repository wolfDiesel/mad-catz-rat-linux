#!/usr/bin/env bash
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
PKG="$ROOT/packaging/appimage"
BUILD="$ROOT/build/appimage"
APPDIR="$BUILD/AppDir"
PUBLISH="$BUILD/publish"
TOOLS="$BUILD/tools"
APP_BIN_DIR="$APPDIR/usr/bin"

export ARCH="${ARCH:-x86_64}"
APPIMAGE_VERSION="${APPIMAGE_VERSION:-1.0.0}"
OUTPUT_NAME="${OUTPUT_NAME:-Rat6-${APPIMAGE_VERSION}-x86_64.AppImage}"
export APPIMAGE_EXTRACT_AND_RUN=1

need_cmd() {
  command -v "$1" >/dev/null 2>&1 || {
    echo "Missing required command: $1" >&2
    exit 1
  }
}

ensure_tools() {
  mkdir -p "$TOOLS"
  local appimagetool="$TOOLS/appimagetool-${ARCH}.AppImage"
  if [ ! -f "$appimagetool" ]; then
    curl -fsSL -o "$appimagetool" \
      "https://github.com/AppImage/AppImageKit/releases/download/continuous/appimagetool-${ARCH}.AppImage"
    chmod +x "$appimagetool"
  fi
  export APPIMAGETOOL="$appimagetool"
}

prepare_icon() {
  local icon="$BUILD/rat6.png"
  mkdir -p "$BUILD"
  if [ -f "$icon" ] && [ ! "$PKG/rat6.svg" -nt "$icon" ]; then
    echo "$icon"
    return
  fi
  rsvg-convert -w 512 -h 512 "$PKG/rat6.svg" -o "$icon"
  echo "$icon"
}

publish_app() {
  echo "Publishing Avalonia app (linux-x64, self-contained)..."
  rm -rf "$PUBLISH"
  dotnet publish "$ROOT/src/Rat6.App/Rat6.App.csproj" \
    -c Release \
    -r linux-x64 \
    --self-contained true \
    -p:PublishSingleFile=false \
    -p:DebugType=none \
    -p:DebugSymbols=false \
    -p:Version="$APPIMAGE_VERSION" \
    -o "$PUBLISH"
}

assemble_appdir() {
  echo "Assembling AppDir..."
  rm -rf "$APPDIR"
  mkdir -p "$APP_BIN_DIR"
  cp -a "$PUBLISH/." "$APP_BIN_DIR/"

  rm -f \
    "$APP_BIN_DIR/createdump" \
    "$APP_BIN_DIR/libcoreclrtraceptprovider.so" \
    "$APP_BIN_DIR/libmscordbi.so" \
    "$APP_BIN_DIR/libmscordaccore.so"

  chmod +x "$APP_BIN_DIR/Rat6.App" "$APP_BIN_DIR/Rat6.Helper"

  install -Dm644 "$PKG/rat6.desktop" "$APPDIR/rat6.desktop"
  install -Dm644 "$PKG/rat6.desktop" "$APPDIR/usr/share/applications/rat6.desktop"
  install -Dm644 "$PKG/rat6.appdata.xml" "$APPDIR/usr/share/metainfo/io.github.wolfdiesel.rat6.appdata.xml"
  install -Dm644 "$PKG/rat6.svg" "$APPDIR/usr/share/icons/hicolor/scalable/apps/rat6.svg"

  cat > "$APPDIR/AppRun" <<'EOF'
#!/bin/sh
HERE="$(dirname "$(readlink -f "$0")")"
exec "$HERE/usr/bin/Rat6.App" "$@"
EOF
  chmod +x "$APPDIR/AppRun"
}

main() {
  need_cmd dotnet
  need_cmd curl
  need_cmd rsvg-convert
  need_cmd desktop-file-validate
  desktop-file-validate "$PKG/rat6.desktop"
  ensure_tools
  publish_app
  assemble_appdir

  local icon
  icon="$(prepare_icon)"
  install -Dm644 "$icon" "$APPDIR/rat6.png"
  ln -sfn rat6.png "$APPDIR/.DirIcon"

  mkdir -p "$ROOT/dist"
  local out="$ROOT/dist/$OUTPUT_NAME"
  rm -f "$out"
  "$APPIMAGETOOL" "$APPDIR" "$out"
  chmod +x "$out"
  echo "AppImage: $out"
  ls -lh "$out"
}

main "$@"
