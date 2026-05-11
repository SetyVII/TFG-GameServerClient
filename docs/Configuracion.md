# Guía de Configuración

## Tabla de Contenidos
1. [Requisitos Previos](#requisitos-previos)
2. [Configuración del Servidor Java](#configuración-del-servidor-java)
3. [Configuración de Unity](#configuración-de-unity)
4. [Configuración del Mando Móvil](#configuración-del-mando-móvil)
5. [Niveles de Sensibilidad](#niveles-de-sensibilidad)
6. [Solución de Problemas](#solución-de-problemas)

## Requisitos Previos

### Software Necesario
- Java JDK 21 (LTS)
- Maven 3.9+
- Unity 6 LTS
- Navegador moderno (Chrome/Safari)
- PowerShell (para script HTTPS)

### Red
- Móvil y PC en la misma red WiFi
- Puerto 8443 libre (HTTPS)
- Puerto 5000 libre (TCP Unity)

## Configuración del Servidor Java

### 1. Instalar JDK
```powershell
# Verificar JDKs disponibles
Get-ChildItem C:\Users\madrid\.jdks

# Ejecutar con JAVA_HOME explícito
$env:JAVA_HOME="C:\Users\madrid\.jdks\corretto-21.0.11"
```

### 2. Configurar application.properties
```properties
# server/src/main/resources/application.properties
spring.application.name=TFG
unity.bridge.host=127.0.0.1
unity.bridge.port=5000
```

### 3. Configurar HTTPS
El script `run-https.ps1` genera automáticamente:
- Certificado autofirmado (`local-dev.p12`)
- Subject Alternative Name (SAN) con IP local
- Keystore con alias `tfg-local`

### 4. Iniciar Servidor
```powershell
# En directorio server/
$env:JAVA_HOME="C:\Users\madrid\.jdks\corretto-21.0.11"
.\scripts\run-https.ps1
```

**Salida esperada:**
```
Tomcat started on port 8443 (https)
Started TfgApplication in X seconds
```

## Configuración de Unity

### 1. Abrir Proyecto
- Abrir `unity-game/` en Unity Hub
- Esperar importación de assets

### 2. Configurar GameManager
En el Inspector del GameManager:
```
Configuración de Control:
  Fuerza Mando: 5 (se sobreescribe desde Java)
  Fuerza Teclado: 25
  Deadzone Movil: 0.25
  Velocidad Maxima Movil: 10 (se sobreescribe)

Mapeo Giroscopio:
  Max Inclinacion Grados: 35
  Zona Muerta Movil: 0.08
  Suavizado Movil: 10
```

### 3. Asignar Referencias UI
```
Referencias de Interfaz:
  Texto Vidas HUD: [Asignar TextMeshPro]
  Texto Monedas HUD: [Asignar TextMeshPro]
  Texto Estado Conexion: [Asignar TextMeshPro] ← IMPORTANTE
```

### 4. Probar en Editor
- Presionar Play
- Seleccionar "Elegir Teclado" para pruebas locales
- Verificar que la bola se mueve correctamente

## Configuración del Mando Móvil

### 1. Acceder desde Móvil
```
URL: https://[IP-LOCAL]:8443/mobile.html
Ejemplo: https://10.20.30.25:8443/mobile.html
```

**Nota:** Requiere HTTPS para sensores en iOS/Android

### 2. Activar Sensores
1. Tocar botón "Activar Sensores"
2. Conceder permisos de orientación/movimiento
3. Esperar calibración (30 frames ~ 0.5 segundos)
4. Verificar que los datos aparecen en la UI

### 3. Modo Apaisado
- Girar móvil a landscape
- O presionar "Voltear" en la barra superior
- El juego inicia automáticamente al detectar apaisado

## Niveles de Sensibilidad

### Descripción de Niveles

| Nivel | Icono | Fuerza | Velocidad Máx | Sensación |
|-------|-------|--------|---------------|-----------|
| **Bajo** | `>` | 0.5 | 3 u/s | Muy lento, preciso |
| **Medio** | `>>` | 5.0 | 10 u/s | Balance natural |
| **Alto** | `>>>` | 20.0 | 25 u/s | Rápido, inercia alta |
| **Custom** | `⚙` | 1-100 | Variable | Ajustable |

### Cómo Cambiar Sensibilidad

1. Abrir `settings.html` desde el móvil
2. Seleccionar nivel deseado
3. Pulsar "Guardar configuración"
4. Volver a `mobile.html`
5. La configuración se aplica automáticamente

### Valores Custom

**Rango:** 1 - 100

**Fórmulas:**
- Fuerza = valor / 10
- Velocidad Máx = Fuerza × 1.5

**Ejemplos:**
- Custom = 80 → Fuerza = 8.0, Vel.Max = 12.0
- Custom = 25 → Fuerza = 2.5, Vel.Max = 3.75

## Solución de Problemas

### Error: "Unity: no conectado"
**Causas posibles:**
- Unity no está en modo Play
- Java no se ha conectado aún (esperar 3 segundos)
- Firewall bloqueando puerto 5000

**Solución:**
```
1. Verificar que Unity está en Play
2. Reiniciar servidor Java
3. Comprobar que el puerto 5000 está libre
```

### Error: "Sensores no disponibles"
**Causas:**
- Conexión HTTP en lugar de HTTPS
- Navegador no soporta DeviceOrientation

**Solución:**
```
1. Usar HTTPS obligatoriamente
2. Probar con Chrome o Safari actualizado
3. Asegurar permisos de sensores en el navegador
```

### Pelota no se mueve
**Causas:**
- Calibración no completada
- Móvil en portrait (no landscape)
- Deadzone muy alta

**Solución:**
```
1. Girar móvil a landscape
2. Esperar 1 segundo tras "Activar Sensores"
3. Verificar en settings que deadzone < 0.3
```

### Pelota se va muy rápido
**Causas:**
- Sensibilidad en "Alto"
- Valor custom muy alto
- Sin deadzone

**Solución:**
```
1. Cambiar a nivel "Medio" o "Bajo"
2. Aumentar deadzone a 0.25 o más
3. Verificar que el offset se calibró correctamente
```

## Configuración Avanzada

### Modificar Intervalo de Heartbeat
En `UnityTcpForwarder.java`:
```java
private static final int HEARTBEAT_INTERVAL_MS = 5000; // Cambiar valor
```

### Cambiar Puerto TCP
En `application.properties`:
```properties
unity.bridge.port=5001
```

Y en `SocketServer.cs`:
```csharp
public int port = 5001;
```

### Ajustar Calibración
En `GameManagerLaberinto.cs`:
```csharp
private const int CALIBRATION_NEEDED = 30; // Frames de calibración
```

## Verificación de Configuración

### Checklist de Funcionamiento

- [ ] Servidor Java inicia sin errores
- [ ] Unity muestra "Esperando controlador..."
- [ ] Java log: "handshake successful"
- [ ] Móvil muestra "Unity: conectado"
- [ ] Giro a landscape inicia el juego
- [ ] Pelota responde a inclinación
- [ ] Cambio de sensibilidad se aplica
- [ ] Velocidad es perceptible entre niveles

### Logs Importantes

**Java (Consola):**
```
java->unity handshake successful
java->unity heartbeat ok
java->game config updated: sensitivity=medium force=45
```

**Unity (Console):**
```
[SocketServer] Handshake exitoso - enviado UNITY_OK
[SocketServer] CONFIG APLICADA: medium | Fuerza: 5 | Vel.Max: 10
[GameManager] Calibrado completado. Offset: -2.3
```

**Móvil (DevTools):**
```javascript
// WebSocket messages
{"type":"unityStatus","connected":true}
{"type":"configSaved","success":true}
```
