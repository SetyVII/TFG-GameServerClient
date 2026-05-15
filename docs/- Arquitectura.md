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
- **Thread-safe:** Implementado con `ConcurrentHashMap` para acceso concurrente desde múltiples hilos WebSocket

#### 1.4 WebSocketConfig
- **Propósito:** Registrar el endpoint WebSocket `/ws/motion`
- **Característica:** Permite cualquier origen con `.setAllowedOriginPatterns("*")` para conexiones desde dispositivos móviles en la red local

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
  - Tamaño de texto (12-24px)
  - Niveles de sensibilidad (Bajo/Medio/Alto/Custom)
- **Persistencia:** Guarda en `localStorage` como `wiiCellSettings` y `wiiCellGameConfig`

#### 2.3 Otras Aplicaciones Web
- **viewer.html:** Visualizador de pelota en Canvas que recibe datos de inclinación por WebSocket
- **mapper.html:** Mapea inclinación del móvil a eventos de teclado (`KeyboardEvent`) con zona muerta configurable (0.05-0.70)
- **hole.html:** Juego "bolita al agujero" con 3 niveles, física de colisiones AABB-círculo y detección de victoria por velocidad
- **fishing.html:** Simulador de pesca 3D con Three.js, caña de pescar con yaw/pitch, boya con estados (`ready` → `casting` → `floating` → `hooked`), peces procedurales y sistema de lanzamiento con carga
- **game.html:** Shooter sci-fi "Project Reincarnation Space" con Canvas 2D, música procedural, sistema de ritmo (BPM 120) y 3 fases de dificultad
- **testvib.html:** Página de diagnóstico para probar APIs de vibración del dispositivo

#### 2.4 Características del Mando
- **Micrófono:** Detección de soplado mediante `AnalyserNode` (RMS) con sliders de umbral (5-50), cooldown (200-2000ms) y escala visual (1-10)
- **Vibración:** Haptic feedback con múltiples fallbacks (`navigator.vibrate` → `navigator.haptic`), detección de plataforma (iOS/Android/Windows/Mac/Linux)
- **Orientación:** Bloqueo de pantalla a landscape con `screen.orientation.lock()` y fullscreen
- **D-pad Visual:** Punto rojo que se desplaza según `tiltX`/`tiltY`

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
  - Calibración automática de sensores (30 frames ~0.5s para calcular `gammaOffset`)
  - Deadzone configurable (`deadzoneMovil`: 0.25 por defecto)
  - Física realista con `AddForce` y `CollisionDetectionMode2D.Continuous`
  - Velocidad máxima limitada (`velocidadMaximaMovil`)
  - Sistema de vidas (3 corazones visuales) y monedas
  - Salto con cooldown (`cooldownSalto`: 1.0s) y animación de escala
  - **Invulnerabilidad durante el salto:** `estaSaltando` evita daño de lava
  - Sistema de checkpoints: guarda posición de respawn al activar interruptores
  - HUD con corazones, icono de moneda y texto de estado de conexión

#### 3.3 UnityMainThreadDispatcher.cs
- **Propósito:** Patrón de despachador al hilo principal de Unity
- **Implementación:** Cola thread-safe (`Queue<Action>`) con `lock` que se procesa en `Update()`
- **Uso:** Todos los scripts de socket encolan acciones para manipular GameObjects y UI de forma segura

#### 3.4 PersistentNetworkManager.cs
- **Propósito:** Servidor TCP persistente que sobrevive entre escenas (`DontDestroyOnLoad`)
- **Patrón:** Singleton - solo existe una instancia
- **Funcionalidades:**
  - Escucha en puerto 5000
  - Handshake (`JAVA_HANDSHAKE` ↔ `UNITY_OK`) y heartbeat
  - Callbacks: `onJavaConnected` y `onDataReceived`
  - Reemplaza conexiones antiguas (un solo cliente a la vez)

#### 3.5 MenuManager.cs
- **Propósito:** Gestión de navegación del menú principal
- **Funcionalidades:**
  - Paneles: Menú, Instrucciones, Opciones, IP
  - Selección de control: Teclado o Móvil (guardado en `PlayerPrefs`)
  - Al elegir móvil: instancia `PersistentNetworkManager`, muestra IP local, espera conexión Java

#### 3.6 LevelSelector.cs
- **Propósito:** Script simple para botones de selección de nivel
- **Método:** `JugarNivel()` carga la escena indicada

#### 3.7 Scripts de Mecánicas de Nivel
- **Interruptor.cs:** Palanca activada por botón B (`AccionBotonB`) cuando la bola está encima. Desactiva puerta asociada y actualiza checkpoint
- **MetaLaberinto.cs:** Zona de victoria (tag "Player") que llama a `HasGanado()`
- **Moneda.cs:** Objeto coleccionable que llama a `SumarMoneda()` y se autodestruye
- **Lava.cs:** Zona de daño (tag "Player" o nombre "circle") que llama a `HasTocadoLava()`
- **BolaDeteccion.cs:** Script adicional en la bola con detección de triggers. Respeta invulnerabilidad por salto

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

## Notas Técnicas Importantes

### Parseo JSON Manual
`MotionSocketHandler.java` utiliza parseo manual con expresiones regulares (`Pattern`/`Matcher`) en lugar de Jackson/ObjectMapper. Esto es ligero pero puede ser frágil ante JSON complejo o con espacios irregulares.

### Lombok Declarado pero No Usado
Aunque `lombok` está declarado en `pom.xml`, ninguna clase Java del proyecto lo utiliza actualmente. Las clases usen getters/setters y constructores manuales.

### Versión de Spring Boot
El proyecto usa Spring Boot `4.0.5` según `pom.xml` (nota: verificar compatibilidad, ya que la línea estable actual es 3.x).

## Extensiones Futuras Posibles

- Soporte para múltiples jugadores simultáneos
- Modo espectador (viewer.html) - ya existe como aplicación web
- Estadísticas de rendimiento en tiempo real
- Configuración por QR/NFC
