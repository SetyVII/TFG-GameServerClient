# Guía de Pruebas

## Objetivo
Verificar que todos los componentes del sistema funcionan correctamente y cumplen con los requisitos funcionales.

## Entorno de Pruebas

### Hardware Recomendado
- **PC:** Windows 10/11 con Unity 6 LTS
- **Móvil:** Android 10+ o iOS 14+
- **Red:** WiFi 5GHz local

### Software
- Java JDK 21
- Maven 3.9+
- Unity 6 LTS
- Chrome Mobile / Safari Mobile

## Casos de Prueba

### 1. Conexión Básica

#### TC-001: Handshake Java ↔ Unity
**Precondiciones:**
- Unity en modo Play
- Servidor Java detenido

**Pasos:**
1. Iniciar Unity (Play → Elegir Móvil)
2. Iniciar servidor Java (`run-https.ps1`)
3. Esperar 5 segundos

**Resultado Esperado:**
```
[Unity] Servidor iniciado en puerto 5000
[Java]  java->unity connecting to 127.0.0.1:5000
[Java]  java->unity handshake successful
[Java]  java->unity heartbeat ok (cada 5 segundos)
```

**Criterio de Éxito:** ✅ Handshake exitoso y heartbeat activo

---

#### TC-002: Conexión Móvil → Java
**Precondiciones:**
- Servidor Java ejecutándose
- Móvil en misma red WiFi

**Pasos:**
1. Abrir `https://[IP]:8443/mobile.html`
2. Verificar estado "Socket: conectado"
3. Verificar estado "Unity: conectado"

**Resultado Esperado:**
- WebSocket conectado
- Mensaje recibido: `{"type":"unityStatus","connected":true}`

**Criterio de Éxito:** ✅ Ambos estados muestran "conectado"

---

#### TC-003: Inicio de Juego por Orientación
**Precondiciones:**
- Móvil conectado a WebSocket
- Unity esperando en Panel IP

**Pasos:**
1. Mantener móvil en portrait (vertical)
2. Verificar que Panel IP sigue visible
3. Girar móvil a landscape (horizontal)
4. O presionar "Voltear"

**Resultado Esperado:**
- Java envía: `0,0,0,register`
- Unity detecta "register"
- Panel Juego aparece
- Bola empieza a caer por gravedad

**Criterio de Éxito:** ✅ Juego inicia solo en landscape

---

### 2. Control del Juego

#### TC-004: Movimiento Básico
**Precondiciones:**
- Juego iniciado (bola visible)
- Sensores activados

**Pasos:**
1. Inclinar móvil ligeramente a la izquierda
2. Inclinar móvil ligeramente a la derecha
3. Poner móvil plano

**Resultado Esperado:**
- Inclinación izquierda: Bola se mueve ←
- Inclinación derecha: Bola se mueve →
- Plano: Bola se detiene (con deadzone)

**Criterio de Éxito:** ✅ Movimiento proporcional a la inclinación

---

#### TC-005: Calibración Automática
**Precondiciones:**
- Juego recién iniciado

**Pasos:**
1. Notar que la bola no se mueve los primeros segundos
2. Verificar logs de Unity

**Resultado Esperado:**
```
[GameManager] Calibrado completado. Offset: X.XX (30 frames)
```

**Criterio de Éxito:** ✅ Calibración completada en ~0.5 segundos

---

#### TC-006: Salto (Botón A)
**Precondiciones:**
- Bola en plataforma

**Pasos:**
1. Presionar botón "A" (o "cambiar" en settings)
2. Verificar movimiento vertical

**Resultado Esperado:**
- Bola salta hacia arriba
- Cooldown de 1 segundo
- Animación de escala (se estira)

**Criterio de Éxito:** ✅ Salto funcional con cooldown

---

#### TC-007: Validar/Interactuar (Botón B)
**Precondiciones:**
- Bola cerca de un interruptor

**Pasos:**
1. Mover bola cerca de interruptor
2. Presionar botón "B" (o "validar")

**Resultado Esperado:**
- Interruptor se activa (cambia color)
- Puerta asociada se abre

**Criterio de Éxito:** ✅ Interacción con mecanismos funciona

---

### 3. Configuración de Sensibilidad

#### TC-008: Cambio a Nivel Bajo
**Precondiciones:**
- Juego iniciado con sensibilidad "Medio"

**Pasos:**
1. Abrir `settings.html`
2. Seleccionar "Bajo" (`>`)
3. Guardar
4. Volver a `mobile.html`
5. Jugar 10 segundos

**Resultado Esperado:**
- Bola se mueve muy lentamente
- Fuerza: ~0.5
- Velocidad máxima: ~3 u/s

**Criterio de Éxito:** ✅ Movimiento perceptiblemente lento

---

#### TC-009: Cambio a Nivel Alto
**Precondiciones:**
- Juego iniciado con sensibilidad "Medio"

**Pasos:**
1. Abrir `settings.html`
2. Seleccionar "Alto" (`>>>`)
3. Guardar
4. Volver a `mobile.html`
5. Jugar 10 segundos

**Resultado Esperado:**
- Bola se mueve rápidamente
- Fuerza: ~20.0
- Velocidad máxima: ~25 u/s
- Alta inercia

**Criterio de Éxito:** ✅ Movimiento rápido con inercia notable

---

#### TC-010: Valor Custom
**Precondiciones:**
- settings.html abierto

**Pasos:**
1. Seleccionar "Custom"
2. Introducir valor: 15
3. Guardar
4. Verificar en logs: `CONFIG APLICADA: custom | Fuerza: 1.5`

**Resultado Esperado:**
- Fuerza = 15/10 = 1.5
- Velocidad máxima = 1.5 × 1.5 = 2.25

**Criterio de Éxito:** ✅ Valor custom aplicado correctamente

---

### 4. Robustez

#### TC-011: Desconexión/Reconexión
**Precondiciones:**
- Juego activo

**Pasos:**
1. Cerrar pestaña del móvil
2. Esperar 10 segundos
3. Reabrir `mobile.html`
4. Activar sensores

**Resultado Esperado:**
- Java detecta desconexión
- Unity sigue funcionando (si no depende del móvil)
- Reconexión exitosa

**Criterio de Éxito:** ✅ Reconexión sin errores

---

#### TC-012: Cambio de Orientación Durante Juego
**Precondiciones:**
- Juego activo en landscape

**Pasos:**
1. Girar móvil a portrait durante el juego
2. Esperar 2 segundos
3. Volver a landscape

**Resultado Esperado:**
- Datos de sensores pausan (o se adaptan)
- Juego no se reinicia
- Controles vuelven al girar de nuevo

**Criterio de Éxito:** ✅ Juego continúa sin reiniciar

---

### 5. Rendimiento

#### TC-013: Latencia de Control
**Precondiciones:**
- Juego activo
- Red WiFi estable

**Pasos:**
1. Inclinar móvil bruscamente
2. Medir tiempo hasta que bola responde

**Resultado Esperado:**
- Latencia < 100ms (red local)
- Sin lag perceptible

**Criterio de Éxito:** ✅ Respuesta inmediata

---

#### TC-014: Estabilidad (30 minutos)
**Precondiciones:**
- Juego activo
- Heartbeat funcionando

**Pasos:**
1. Mantener juego activo 30 minutos
2. Verificar logs cada 5 minutos

**Resultado Esperado:**
- Sin memory leaks
- Heartbeat continúa
- Sin desconexiones espontáneas

**Criterio de Éxito:** ✅ 30 minutos sin errores

---

## Matriz de Compatibilidad

| Dispositivo | Sistema | Navegador | Sensores | Funciona |
|-----------|---------|-----------|----------|----------|
| iPhone 12+ | iOS 16+ | Safari | ✅ | ✅ |
| Samsung S21 | Android 13 | Chrome | ✅ | ✅ |
| Xiaomi RedMi | Android 11 | Chrome | ✅ | ✅ |
| iPad Pro | iPadOS 16 | Safari | ✅ | ✅ |
| Pixel 7 | Android 14 | Chrome | ✅ | ✅ |

## Herramientas de Depuración

### Logs de Java
```powershell
# Ver logs en tiempo real
tail -f server.log

# Filtrar por componente
Select-String "UnityTcpForwarder" server.log
```

### Logs de Unity
```csharp
// Activar logs detallados en SocketServer.cs
UnityEngine.Debug.Log("[SocketServer] " + mensaje);
```

### Consola del Móvil
```javascript
// En Chrome DevTools (móvil)
console.log("Sensores:", orientation);

// Ver WebSocket messages
// Network → WS → Messages
```

## Métricas de Éxito

### Funcionales
- [x] Handshake exitoso en < 2 segundos
- [x] Heartbeat estable durante 30 min
- [x] Movimiento proporcional a inclinación
- [x] Cambio de sensibilidad perceptible
- [x] Juego inicia solo en landscape

### No Funcionales
- [x] Latencia < 100ms
- [x] Sin memory leaks
- [x] Reconexión automática
- [x] Responsive en móviles

## Certificación

**Fecha de Pruebas:** __/__/____

**Tester:** ___________________

**Resultado Global:** ☐ APROBADO ☐ RECHAZADO

**Observaciones:**
_________________________________
_________________________________

**Firma:** ___________________
