#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
PROJECT_ROOT="$(dirname "$SCRIPT_DIR")"
KEYSTORE_PATH="$PROJECT_ROOT/src/main/resources/local-dev.p12"

JAVA_HOME="${JAVA_HOME:-}"
if [ -z "$JAVA_HOME" ]; then
    if [ -n "${1:-}" ]; then
        JAVA_HOME="$1"
    fi
fi

if [ -z "$JAVA_HOME" ] || [ ! -f "$JAVA_HOME/bin/java" ]; then
    JAVA_HOME="$(dirname "$(dirname "$(readlink -f "$(which java 2>/dev/null || echo /nonexistent)")")")"
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

if [ ! -f "$KEYSTORE_PATH" ]; then
    LOCAL_IP=$(hostname -I 2>/dev/null | awk '{print $1}')
    if [ -z "$LOCAL_IP" ]; then
        LOCAL_IP=$(ip -4 addr show scope global 2>/dev/null | grep -oP '(?<=inet\s)\d+(\.\d+){3}' | grep -v '^169\.254\.' | head -1)
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
