# Registro de Cambios

## Versión 1.1.0 - Integración de Validación y Configuración

**Fecha:** 2026-05-11
**Autor:** Sistema de Desarrollo
**Estado:** Completado

---

## Resumen Ejecutivo

Esta versión introduce mejoras críticas de seguridad, validación de conexión y configuración dinámica del control. El sistema ahora requiere un handshake bidireccional antes de procesar datos y permite ajustar la sensibilidad del mando en tiempo real.

---

## Cambios Realizados

### 🔒 Seguridad y Validación

#### Handshake Bidireccional (Java ↔ Unity)
- **Archivos:** `UnityTcpForwarder.java`, `SocketServer.cs`
- **Descripción:** Implementación de protocolo de validación de 3 pasos
  1. Java envía `JAVA_HANDSHAKE`
  2. Unity responde `UNITY_OK`
  3. Conexión marcada como validada
- **Impacto:** Unity no procesa datos de sensores sin validación previa
- **Estado:** ✅ Implementado y probado

#### Heartbeat (Latido de Vida)
- **Archivos:** `UnityTcpForwarder.java`
- **Descripción:** Ping/Pong cada 5 segundos para detectar desconexiones
  - `JAVA_PING` → `UNITY_PONG`
- **Impacto:** Detección temprana de fallos de conexión
- **Estado:** ✅ Implementado y funcionando

#### Timeout de Socket
- **Archivo:** `UnityTcpForwarder.java`
- **Descripción:** Configuración de timeout a 10 segundos
- **Impacto:** No queda conexiones colgadas
- **Estado:** ✅ Implementado

---

### 🎮 Configuración Dinámica

#### Página settings.html (NUEVA)
- **Ubicación:** `mobile-web/settings.html`
- **Características:**
  - Modo oscuro/claro
  - Tamaño de texto ajustable (12-24px)
  - 4 niveles de sensibilidad:
    - Bajo (`>`) - Fuerza: 0.5
    - Medio (`>>`) - Fuerza: 5.0
    - Alto (`>>>`) - Fuerza: 20.0
    - Custom (1-100)
  - Persistencia en localStorage
- **Estado:** ✅ Completamente funcional

#### GameConfig.java (NUEVO)
- **Ubicación:** `server/src/.../config/GameConfig.java`
- **Descripción:** Componente Spring para almacenar configuración global
- **Datos almacenados:**
  - sensitivity (string)
  - force (int)
  - darkMode (boolean)
  - fontSize (int)
- **Estado:** ✅ Inyectado y funcionando

#### Envío de Configuración
- **Archivos:** `settings.html`, `MotionSocketHandler.java`, `SocketServer.cs`
- **Flujo:**
  1. Usuario guarda en settings.html
  2. Se almacena en localStorage
  3. Se envía por WebSocket al servidor
  4. Java reenvía a Unity vía TCP: `CONFIG,medium,45`
  5. Unity actualiza valores en tiempo real
- **Estado:** ✅ Funcionando correctamente

---

### 📱 Mejoras en Mando Móvil

#### Botón de Configuración
- **Archivo:** `mobile.html`
- **Descripción:** Botón ⚙️ en barra superior que redirige a settings.html
- **Estilo:** Hover effect, posicionado a la derecha
- **Estado:** ✅ Implementado

#### Envío de Config al Conectar
- **Archivo:** `mobile.html`
- **Descripción:** Al abrir WebSocket, se envía automáticamente la última configuración guardada
- **Código:**
```javascript
function sendSavedConfig() {
    const saved = localStorage.getItem('wiiCellGameConfig');
    if (saved && socket.readyState === WebSocket.OPEN) {
        socket.send(JSON.stringify({
            type: "config",
            role: "mobile",
            sensitivity: config.sensitivity,
            force: config.force
        }));
    }
}
```
- **Estado:** ✅ Implementado

#### Control por Orientación
- **Archivo:** `mobile.html`
- **Descripción:** El juego solo inicia cuando el móvil está en landscape
- **Comportamiento:**
  - Portrait: Muestra hint "Gira el móvil"
  - Landscape: Envía `register` e inicia juego
- **Estado:** ✅ Funcionando

---

### 🎯 Mejoras en Unity

#### Campos de Configuración
- **Archivo:** `GameManagerLaberinto.cs`
- **Nuevos campos:**
  - `fuerzaMando` (float) - Ajustable desde Java
  - `velocidadMaximaMovil` (float) - Ajustable desde Java
  - `textoEstadoConexion` (TextMeshProUGUI) - Muestra estado
- **Estado:** ✅ Integrados

#### Calibración Automática
- **Archivo:** `GameManagerLaberinto.cs`
- **Descripción:** Promedia 30 frames (~0.5 segundos) para calcular offset del acelerómetro
- **Variables:**
  - `gammaOffset`: Valor base del sensor
  - `calibrationFrames`: Contador de frames
  - `CALIBRATION_NEEDED`: 30 frames
- **Estado:** ✅ Funcionando

#### Procesamiento de Config
- **Archivo:** `SocketServer.cs`
- **Descripción:** Parseo de mensajes `CONFIG,sensitivity,force`
- **Mapeo de niveles:**
  - low: fuerza=0.5, vel=3
  - medium: fuerza=5.0, vel=10
  - high: fuerza=20.0, vel=25
  - custom: fuerza=force/10, vel=force/10*1.5
- **Estado:** ✅ Actualizado con valores más perceptibles

---

### 🔧 Cambios en Java Server

#### UnityTcpForwarder.java (MODIFICADO)
**Cambios:**
- Añadido: `BufferedReader` para lectura
- Añadido: `heartbeatThread` para ping/pong
- Añadido: `unityValidated` flag
- Añadido: `sendConfig()` method
- Añadido: `@PostConstruct` para conexión temprana
- Modificado: `ensureConnected()` incluye handshake
- Modificado: `closeCurrentConnection()` limpia heartbeat

**Estado:** ✅ Compila y funciona

#### MotionSocketHandler.java (MODIFICADO)
**Cambios:**
- Añadido: Import `GameConfig`
- Añadido: Inyección de `GameConfig`
- Añadido: `sendUnityStatus()` para notificar estado
- Añadido: `getIntField()` parser
- Modificado: `forwardToUnityIfApplicable()` valida Unity primero
- Añadido: Soporte para mensajes tipo `config`
- Preservado: Soporte para `blow` (acción del compañero)

**Estado:** ✅ Compila y funciona

---

## Archivos Modificados

### Java
- [x] `UnityTcpForwarder.java` - Handshake + heartbeat + config
- [x] `MotionSocketHandler.java` - Validación + config
- [x] `GameConfig.java` - NUEVO: Configuración global

### Unity C#
- [x] `SocketServer.cs` - Procesar config + handshake response
- [x] `GameManagerLaberinto.cs` - Campos configurables + calibración

### HTML/JS
- [x] `mobile.html` - Botón config + envío config + orientación
- [x] `settings.html` - NUEVO: Página de configuración completa

### Scripts
- [x] `run-https.ps1` - Auto-detección de JDKs

---

## Archivos Preservados (del Compañero)

- [x] `mobile.html` - Controles originales (botones, micrófono, vibración)
- [x] `GameManagerLaberinto.cs` - Lógica de juego original
- [x] `SocketServer.cs` - ThreadPool y HandleClient originales
- [x] Soporte para `soplar` (blow action)
- [x] Salto por inclinación
- [x] Detección automática grados/normalizado
- [x] Zona muerta y suavizado del compañero

---

## Problemas Conocidos y Soluciones

### ❌ Problema: "Unity: no conectado"
**Causa:** Java intenta conectar antes de que Unity esté listo
**Solución:** Implementar `@PostConstruct` con reintentos cada 3 segundos
**Estado:** ✅ Resuelto

### ❌ Problema: Configuración no se aplica
**Causa:** mobile.html no envía config al reconectar
**Solución:** Añadir `sendSavedConfig()` en `socket.onopen`
**Estado:** ✅ Resuelto

### ❌ Problema: Velocidad no perceptible entre niveles
**Causa:** Valores demasiado conservadores
**Solución:** Aumentar a fuerza=0.5/5.0/20.0
**Estado:** ✅ Resuelto

---

## Métricas del Proyecto

### Líneas de Código Añadidas
- Java: ~150 líneas
- C#: ~80 líneas
- HTML/JS: ~350 líneas

### Archivos Nuevos
- 1 archivo Java (GameConfig.java)
- 1 archivo HTML (settings.html)
- 4 archivos Markdown (documentación)

### Archivos Modificados
- 2 archivos Java
- 2 archivos C#
- 1 archivo HTML
- 1 script PowerShell

---

## Próximos Pasos Sugeridos

### Mejoras Futuras
- [ ] Soporte para múltiples jugadores simultáneos
- [ ] Reconexión automática del móvil
- [ ] Guardar configuración en servidor (no solo localStorage)
- [ ] Modo espectador (viewer.html)
- [ ] Estadísticas de juego en tiempo real

### Optimizaciones
- [ ] Reducir frecuencia de heartbeat a 10s
- [ ] Comprimir datos de sensores
- [ ] Implementar predicción del lado del cliente

---

## Notas de Implementación

### Decisiones de Diseño
1. **Handshake antes de datos:** Priorizar seguridad sobre velocidad
2. **Config en localStorage:** Más rápido que pedir al servidor cada vez
3. **Calibración en Unity:** Mejor precisión que en JavaScript
4. **AddForce con límite:** Física realista sin perder control

### Compatibilidad
- ✅ Java 21 (GraalVM/Corretto)
- ✅ Unity 6 LTS
- ✅ iOS 14+ (Safari)
- ✅ Android 10+ (Chrome)
- ✅ Windows 10/11

---

**Documento generado el:** 2026-05-11
**Última actualización:** 2026-05-11
**Versión del documento:** 1.0
