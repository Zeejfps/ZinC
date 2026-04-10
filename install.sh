#!/bin/bash
set -euo pipefail

REPO="Zeejfps/ZinC"
INSTALL_DIR="$HOME/.zinc/bin"

# Colors
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m'

info() { echo -e "${GREEN}[info]${NC} $1"; }
warn() { echo -e "${YELLOW}[warn]${NC} $1"; }
error() { echo -e "${RED}[error]${NC} $1" >&2; exit 1; }

# Detect download tool
if command -v curl &>/dev/null; then
    fetch() { curl -fsSL "$1"; }
    download() { curl -fsSL -o "$2" "$1"; }
elif command -v wget &>/dev/null; then
    fetch() { wget -qO- "$1"; }
    download() { wget -qO "$2" "$1"; }
else
    error "Neither curl nor wget found. Please install one and try again."
fi

# Detect OS
OS="$(uname -s)"
case "$OS" in
    Linux)   os="linux" ;;
    Darwin)  os="osx" ;;
    MINGW*|MSYS*|CYGWIN*) os="win" ;;
    *) error "Unsupported operating system: $OS" ;;
esac

# Detect architecture
ARCH="$(uname -m)"
case "$ARCH" in
    x86_64|amd64) arch="x64" ;;
    arm64|aarch64) arch="arm64" ;;
    *) error "Unsupported architecture: $ARCH" ;;
esac

RID="${os}-${arch}"

# Validate supported target
case "$RID" in
    linux-x64|osx-x64|osx-arm64|win-x64) ;;
    *) error "No prebuilt binary available for ${RID}." ;;
esac

info "Detected platform: ${RID}"

# Fetch latest release tag
info "Fetching latest release..."
RELEASE_JSON="$(fetch "https://api.github.com/repos/${REPO}/releases/latest")"
TAG="$(echo "$RELEASE_JSON" | grep '"tag_name"' | sed -E 's/.*"tag_name": *"([^"]+)".*/\1/')"

if [ -z "$TAG" ]; then
    error "Failed to determine latest release tag."
fi

info "Latest version: ${TAG}"

# Determine archive extension and binary name
if [ "$os" = "win" ]; then
    EXT="zip"
    BIN_NAME="zinc.exe"
else
    EXT="tar.gz"
    BIN_NAME="zinc"
fi

ARCHIVE="zinc-${TAG}-${RID}.${EXT}"
URL="https://github.com/${REPO}/releases/download/${TAG}/${ARCHIVE}"

# Download to temp directory
TMPDIR="$(mktemp -d)"
trap 'rm -rf "$TMPDIR"' EXIT

info "Downloading ${ARCHIVE}..."
download "$URL" "$TMPDIR/$ARCHIVE"

# Extract
info "Extracting..."
if [ "$EXT" = "tar.gz" ]; then
    tar xzf "$TMPDIR/$ARCHIVE" -C "$TMPDIR"
else
    unzip -qo "$TMPDIR/$ARCHIVE" -d "$TMPDIR"
fi

# Install
mkdir -p "$INSTALL_DIR"
mv "$TMPDIR/$BIN_NAME" "$INSTALL_DIR/$BIN_NAME"
chmod +x "$INSTALL_DIR/$BIN_NAME"

info "Installed ${BIN_NAME} to ${INSTALL_DIR}"

# Update PATH in shell profiles
add_to_path() {
    local profile="$1"
    local line='export PATH="$HOME/.zinc/bin:$PATH"'

    if [ -f "$profile" ] && grep -qF '.zinc/bin' "$profile"; then
        return
    fi

    if [ -f "$profile" ]; then
        echo "" >> "$profile"
        echo "# ZinC" >> "$profile"
        echo "$line" >> "$profile"
        info "Added to PATH in ${profile}"
    fi
}

if echo "$PATH" | grep -qF "$INSTALL_DIR"; then
    info "PATH already contains ${INSTALL_DIR}"
else
    add_to_path "$HOME/.bashrc"
    add_to_path "$HOME/.zshrc"
    warn "Restart your shell or run:  export PATH=\"\$HOME/.zinc/bin:\$PATH\""
fi

echo ""
info "ZinC ${TAG} installed successfully!"
