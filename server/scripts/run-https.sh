#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
PROJECT_ROOT="$(dirname "$SCRIPT_DIR")"
KEYSTORE_PATH="$PROJECT_ROOT/src/main/resources/local-dev.p12"

# --- JAVA_HOME detection ---
JAVA_HOME="${JAVA_HOME:-}"
if [ -z "$JAVA_HOME" ] && [ -n "${1:-}" ]; then
    JAVA_HOME="$1"
fi

if [ -z "$JAVA_HOME" ] || [ ! -f "$JAVA_HOME/bin/java" ]; then
    if java_path="$(command -v java 2>/dev/null)"; then
        if command -v realpath &>/dev/null; then
            java_path="$(realpath "$java_path")"
        elif command -v readlink &>/dev/null && readlink -f "$java_path" &>/dev/null 2>&1; then
            java_path="$(readlink -f "$java_path")"
        fi
        JAVA_HOME="$(dirname "$(dirname "$java_path")")"
    fi
fi

if [ -z "$JAVA_HOME" ] || [ ! -f "$JAVA_HOME/bin/java" ]; then
    echo "ERROR: No se encontró JAVA_HOME válido. Configura JAVA_HOME y vuelve a ejecutar." >&2
    exit 1
fi

export JAVA_HOME
export PATH="$JAVA_HOME/bin:$PATH"

if ! command -v keytool &>/dev/null; then
    echo "ERROR: No se encontró keytool en PATH" >&2
    exit 1
fi

# --- Certificate generation ---
if [ ! -f "$KEYSTORE_PATH" ]; then
    LOCAL_IP=""

    # Linux: hostname -I (GNU coreutils)
    LOCAL_IP=$(hostname -I 2>/dev/null | tr ' ' '\n' | grep -oE '[0-9]+\.[0-9]+\.[0-9]+\.[0-9]+' | grep -v '^169\.254\.' | head -1)

    # Linux: ip command (iproute2)
    if [ -z "$LOCAL_IP" ] && command -v ip &>/dev/null; then
        LOCAL_IP=$(ip -4 addr show scope global 2>/dev/null | grep -oE '[0-9]+\.[0-9]+\.[0-9]+\.[0-9]+' | grep -v '^169\.254\.' | head -1)
    fi

    # macOS/BSD: ifconfig
    if [ -z "$LOCAL_IP" ] && command -v ifconfig &>/dev/null; then
        LOCAL_IP=$(ifconfig 2>/dev/null | grep -oE 'inet [0-9]+\.[0-9]+\.[0-9]+\.[0-9]+' | awk '{print $2}' | grep -v '^127\.' | grep -v '^169\.254\.' | head -1)
    fi

    if [ -z "$LOCAL_IP" ]; then
        LOCAL_IP="127.0.0.1"
    fi

    echo "Generando certificado autofirmado para IP: $LOCAL_IP"

    keytool -genkeypair \
        -alias tfg-local \
        -keyalg RSA \
        -keysize 2048 \
        -validity 3650 \
        -storetype PKCS12 \
        -keystore "$KEYSTORE_PATH" \
        -storepass changeit \
        -keypass changeit \
        -dname "CN=$LOCAL_IP, OU=TFG, O=TFG, L=Local, ST=Local, C=ES" \
        -ext "SAN=dns:localhost,ip:127.0.0.1,ip:$LOCAL_IP"

    echo "Certificado generado en $KEYSTORE_PATH"
fi

cd "$PROJECT_ROOT"
exec ./mvnw spring-boot:run -Dspring-boot.run.profiles=https
