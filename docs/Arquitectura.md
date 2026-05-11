# Arquitectura del Sistema

## Visión General

El proyecto **WiiCellGame** es un sistema de control de videojuegos por movimiento que transforma un dispositivo móvil en un gamepad inalámbrico mediante sensores inerciales (acelerómetro y giroscopio).

## Stack Tecnológico

| Componente | Tecnología | Versión |
|-----------|-----------|---------|
| Servidor Backend | Spring Boot | 3.x (Java 21) |
| Frontend Móvil | HTML5 + JavaScript | ES6+ |
| Motor de Juego | Unity | 6 LTS |
| Protocolo de Comunicación | WebSocket + TCP | - |
| Build Tool | Maven | 3.9+ |

## Diagrama de Arquitectura

```
┌─────────────┐     HTTPS/WSS     ┌──────────────┐     TCP      ┌──────────┐
│   Móvil     │◄─────────────────►│  Java Server │◄──────────►│  Unity   │
│ (Navegador) │     Puerto 8443   │  (Spring)    │  Puerto    │  (Juego) │
│             │                   │              │  5000      │          │
└─────────────┘                   └──────────────┘            └──────────┘
       │                                  │
       │ WebSocket                        │ Handshake + Heartbeat
       │ /ws/motion                       │ Validación Bidireccional
       │                                  │
       ▼                                  ▼
┌─────────────────────────────────────────────────────────────┐
│                     FLUJO DE DATOS                            │
│                                                              │
│  1. Móvil envía datos de sensores (JSON)                    │
│  2. Java reenvía a Unity (CSV: alpha,beta,gamma,action)    │
│  3. Unity procesa y controla el juego                        │
└─────────────────────────────────────────────────────────────┘
```

## Componentes Principales

### 1. Servidor Java (Spring Boot)

**Ubicación:** `server/`

#### 1.1 UnityTcpForwarder
- **Propósito:** Gestiona la conexión TCP con Unity
- **Características:**
  - Handshake bidireccional (`JAVA_HANDSHAKE` ↔ `UNITY_OK`)
  - Heartbeat cada 5 segundos (`JAVA_PING` ↔ `UNITY_PONG`)
  - Reconexión automática
  - Thread-safe con `synchronized`

#### 1.2 MotionSocketHandler
- **Propósito:** Gestiona WebSockets desde el móvil
- **Características:**
  - Registro de sesiones activas
  - Broadcasting de mensajes
  - Validación de estado Unity antes de reenviar
  - Soporte para configuración remota

#### 1.3 GameConfig
- **Propósito:** Almacenar configuración global del juego
- **Datos:** Sensibilidad, fuerza, modo oscuro, tamaño de texto

### 2. Cliente Móvil (HTML/JS)

**Ubicación:** `mobile-web/`

#### 2.1 mobile.html
- **Propósito:** Controlador del juego
- **Funcionalidades:**
  - Captura de sensores (DeviceOrientation/Motion)
  - Botones de acción (Saltar, Validar)
  - Control por inclinación
  - Detección de orientación (landscape/portrait)

#### 2.2 settings.html
- **Propósito:** Configuración del control
- **Opciones:**
  - Modo oscuro/claro
  - Tamaño de texto
  - Niveles de sensibilidad (Bajo/Medio/Alto/Custom)

### 3. Juego Unity (C#)

**Ubicación:** `unity-game/Assets/Scripts/`

#### 3.1 SocketServer.cs
- **Propósito:** Escuchar conexiones TCP desde Java
- **Funcionalidades:**
  - Responder handshake
  - Procesar heartbeat
  - Aplicar configuración recibida
  - Iniciar juego solo tras validación

#### 3.2 GameManagerLaberinto.cs
- **Propósito:** Control principal del juego
- **Características:**
  - Calibración automática de sensores
  - Deadzone configurable
  - Física realista con AddForce
  - Velocidad máxima limitada

## Flujo de Conexión

```
1. Unity inicia servidor TCP en puerto 5000
   └── Muestra "Esperando controlador..."

2. Java (UnityTcpForwarder) se conecta automáticamente
   └── Envía: JAVA_HANDSHAKE
   └── Espera: UNITY_OK
   └── Handshake exitoso ✓
   └── Inicia heartbeat cada 5s

3. Java marca Unity como validado
   └── isUnityValidated() = true

4. Móvil abre página (mobile.html)
   └── WebSocket conectado
   └── Java envía: {"type":"unityStatus","connected":true}
   └── Móvil muestra: "Unity: conectado"

5. Móvil gira a apaisado (landscape)
   └── Envía: {"type":"register","role":"mobile"}
   └── Java reenvía: 0,0,0,register
   └── Unity inicia juego

6. Juego activo con controles
   └── Móvil envía datos de sensores
   └── Java reenvía a Unity
   └── Unity aplica fuerzas al Rigidbody2D
```

## Seguridad

- **Handshake obligatorio:** Unity no procesa datos sin validación previa
- **Heartbeat:** Detecta desconexiones cada 5 segundos
- **Validación de origen:** Java rechaza móviles si Unity no está disponible
- **HTTPS obligatorio:** Los sensores del móvil requieren contexto seguro

## Rendimiento

- **Frecuencia de envío:** ~28 FPS (móvil → Java)
- **Latencia típica:** < 50ms en red local
- **Consumo recursos:** 1 thread adicional (heartbeat daemon)
- **Buffering:** No hay acumulación de mensajes

## Extensiones Futuras Posibles

- Soporte para múltiples jugadores simultáneos
- Modo espectador (viewer.html)
- Estadísticas de rendimiento en tiempo real
- Configuración por QR/NFC
