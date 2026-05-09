# TFG - Game Server Client

Proyecto que combina un juego de Unity (Tilt Maze) controlado desde el móvil mediante sensores de movimiento, usando un servidor Java como puente de comunicación.

## Arquitectura

```
Móvil (Web) ──WebSocket──> Servidor Java ──TCP──> Unity (Tilt Maze)
   sensors                Spring Boot               juego 3D
   giroscopio             :8443 (HTTPS)             :5000 (TCP)
   acelerómetro           :8080 (WS)
```

1. **mobile-web/** — Cliente web móvil que captura los sensores del dispositivo (acelerómetro, giroscopio) y los envía por WebSocket
2. **server/** — Servidor Java Spring Boot que sirve la web (HTTPS) y enruta los mensajes WebSocket → TCP hacia Unity
3. **unity-game/** — Juego Tilt Maze en Unity que recibe los datos de sensores por TCP y controla la bola en el laberinto

## Requisitos

- **Java 21** — Para compilar y ejecutar el servidor
- **Unity 6 LTS** — Para abrir y ejecutar el juego
- **Maven** — Gestión de dependencias del servidor (incluye wrapper `mvnw`)
- Navegador moderno en el móvil con soporte para Sensor API (DeviceOrientation / Accelerometer)

## Estructura del proyecto

```
TFG-GameServerClient/
├── unity-game/          # Proyecto Unity (Tilt Maze)
│   ├── Assets/          # Scripts, escenas, recursos
│   ├── Packages/        # Dependencias Unity
│   └── ProjectSettings/ # Configuración del proyecto
├── server/              # Servidor Spring Boot
│   ├── src/main/java/   # Código Java
│   ├── src/main/resources/  # Configuración y certificados
│   └── pom.xml          # Dependencias Maven
├── mobile-web/          # Cliente web móvil
│   ├── mobile.html      # Controlador principal con sensores
│   ├── index.html       # Landing page
│   └── *.html           # Páginas auxiliares
├── docs/                # Documentación del TFG
├── .gitignore
├── .gitattributes
└── README.md
```

## Flujo de comunicación

1. El jugador abre `https://<ip-servidor>:8443/mobile.html` en su móvil
2. La página establece WebSocket con el servidor en `/ws/motion`
3. Al activar los sensores, el móvil envía datos de orientación (alpha, beta, gamma) al servidor
4. El servidor reenvía los datos por TCP al juego Unity en `127.0.0.1:5000`
5. Unity recibe los datos y mueve la bola en el laberinto

## Ejecución

### 1. Iniciar el servidor

```bash
cd server/
./mvnw spring-boot:run -Dspring-boot.run.profiles=https
```

El servidor arranca en:
- HTTPS: `https://localhost:8443` (necesario para acceder a los sensores del móvil)
- WebSocket: `wss://localhost:8443/ws/motion`
- TCP bridge: `127.0.0.1:5000` (hacia Unity)

### 2. Iniciar el juego Unity

Abrir `unity-game/` en Unity Hub y ejecutar la escena principal.

### 3. Conectar el móvil

Abrir en el navegador del móvil:
```
https://<ip-del-pc>:8443/mobile.html
```

Pulsar "Activar sensores" y mover el móvil para controlar la bola.

## Skills del proyecto

El proyecto usa skills de Claude Code para desarrollo:

| Skill | Uso |
|---|---|
| `java-springboot` | Desarrollo del servidor Spring Boot |
| `unity-developer` | Desarrollo del juego Unity |
| `websocket-implementation` | Comunicación WebSocket en tiempo real |
| `browser-extension-builder` | Cliente web y APIs de sensores |
| `security-review` | Auditorías de seguridad |
