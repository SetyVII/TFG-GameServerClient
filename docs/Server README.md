# Servidor Java - TFG Game Server Client

Servidor puente WebSocket-to-TCP desarrollado en **Spring Boot** que conecta el mando móvil con el juego Unity.

## Stack Tecnológico

| Tecnología | Versión |
|-----------|---------|
| Spring Boot | 4.0.5 |
| Java | 21 (LTS) |
| Maven | 3.9+ |
| WebSocket | Raw (sin STOMP/SockJS) |
| TCP | Socket nativo Java |

## Estructura del Servidor

```
server/
├── src/main/java/com/testing/tfg/
│   ├── TfgApplication.java              # Punto de entrada Spring Boot
│   ├── config/
│   │   ├── WebSocketConfig.java         # Registro endpoint /ws/motion
│   │   └── GameConfig.java              # Configuración global en memoria
│   └── websocket/
│       ├── MotionSocketHandler.java     # Handler WebSocket del móvil
│       └── UnityTcpForwarder.java       # Cliente TCP hacia Unity
├── src/main/resources/
│   ├── application.properties           # Configuración base
│   ├── application-https.properties     # Perfil HTTPS (puerto 8443)
│   └── local-dev.p12                    # Certificado autofirmado (generado)
├── scripts/
│   ├── run-https.ps1                    # Script PowerShell (Windows)
│   └── run-https.sh                     # Script Bash (Linux/macOS)
├── pom.xml                              # Dependencias Maven
└── mvnw / mvnw.cmd                      # Maven Wrapper
```

## Componentes Principales

### UnityTcpForwarder
Gestiona la conexión TCP persistente con Unity:
- **Handshake:** `JAVA_HANDSHAKE` ↔ `UNITY_OK`
- **Heartbeat:** Ping/Pong cada 5 segundos
- **Reconexión automática:** Reintentos cada 3 segundos
- **Thread-safe:** Acceso sincronizado con `synchronized`
- **Locale.US:** Formato numérico con punto decimal garantizado

### MotionSocketHandler
Gestiona WebSockets desde dispositivos móviles:
- **Broadcast:** Retransmite mensajes a todos los clientes conectados
- **Filtrado:** Solo reenvía mensajes con `role: "mobile"` a Unity
- **Parseo JSON:** Manual con expresiones regulares
- **Tipos soportados:** `motion`, `action`, `register`, `config`, `blow`

### GameConfig
Bean Spring de configuración global:
- `sensitivity`: Nivel de sensibilidad (low/medium/high/custom)
- `force`: Fuerza del mando (0-100)
- `darkMode`: Modo oscuro/claro
- `fontSize`: Tamaño de texto (12-24px)
- **Thread-safe:** Implementado con `ConcurrentHashMap`

## Configuración

### application.properties
```properties
spring.application.name=TFG
spring.web.resources.static-locations=classpath:/static/,file:../mobile-web/
unity.bridge.host=127.0.0.1
unity.bridge.port=5000
```

### application-https.properties
```properties
server.port=8443
server.ssl.enabled=true
server.ssl.key-store=classpath:local-dev.p12
server.ssl.key-store-type=PKCS12
server.ssl.key-store-password=changeit
server.ssl.key-alias=tfg-local
```

## Ejecución

### Modo HTTPS (Recomendado)
```powershell
# Windows
.\scripts\run-https.ps1

# Linux/macOS
./scripts/run-https.sh
```

El script automáticamente:
1. Detecta JDK instalado (escanea múltiples rutas)
2. Genera certificado autofirmado con IP local como SAN
3. Inicia servidor en `https://localhost:8443`

### Modo HTTP (Desarrollo)
```bash
./mvnw spring-boot:run
```

## Dependencias Maven

- `spring-boot-starter-webmvc`: Controladores REST y servlet stack
- `spring-boot-starter-websocket`: Soporte WebSocket
- `lombok`: Declarado pero no utilizado actualmente

## Puertos y Endpoints

| Servicio | Puerto | Ruta | Protocolo |
|----------|--------|------|-----------|
| HTTPS | 8443 | / | HTTPS |
| WebSocket | 8443 | /ws/motion | WSS |
| TCP Unity | 5000 | - | TCP |
| Contenido estático | 8443 | /mobile-web/ | HTTPS |

## Notas

- **Lombok** está declarado en `pom.xml` pero ninguna clase lo utiliza actualmente.
- El certificado autofirmado se regenera automáticamente si no existe.
- La IP local se detecta dinámicamente para incluirla en el SAN del certificado.
