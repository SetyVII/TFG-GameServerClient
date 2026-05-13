# Documentación Técnica - Tilt Maze Unity

## Índice

1. [Introducción](#1-introducción)
2. [Sistema de Vidas Visual](#2-sistema-de-vidas-visual)
3. [Sistema de Monedas Visual](#3-sistema-de-monedas-visual)
4. [Arquitectura de Escenas](#4-arquitectura-de-escenas)
5. [Gestión de Red Persistente](#5-gestión-de-red-persistente)
6. [Flujo de Juego](#6-flujo-de-juego)
7. [Solución de Problemas](#7-solución-de-problemas)
8. [Agradecimientos](#8-agradecimientos)
9. [Bibliografía](#9-bibliografía)

---

## 1. Introducción

Este documento detalla todas las modificaciones realizadas al proyecto Unity **Tilt Maze** durante la sesión de desarrollo. Se cubren los cambios en la interfaz de usuario, la arquitectura de escenas y el sistema de comunicación en red.

**Herramientas utilizadas:**
- Unity 6.3 LTS
- Visual Studio / VS Code
- TextMeshPro
- C# .NET

---

## 2. Sistema de Vidas Visual

### 2.1 Objetivo
Reemplazar el texto "Vidas: X" por un sistema visual de 3 corazones que muestre el estado actual de las vidas del jugador.

### 2.2 Implementación

#### Assets necesarios:
- `CorazonCompleto.png` - Sprite de corazón lleno
- `CorazonVacio.png` - Sprite de corazón vacío
- Ubicación: `Assets/Resources/`

#### Modificaciones en `GameManagerLaberinto.cs`:

```csharp
[Header("Referencias de Interfaz (HUD)")]
public Image[] imagenesVidas = new Image[3]; // 3 corazones en el HUD
public Sprite spriteCorazonCompleto;
public Sprite spriteCorazonVacio;
```

#### Método `ActualizarHUD()`:

```csharp
private void ActualizarHUD()
{
    // Actualizar corazones del HUD
    for (int i = 0; i < imagenesVidas.Length; i++)
    {
        if (imagenesVidas[i] != null)
        {
            if (i < vidasActuales)
                imagenesVidas[i].sprite = spriteCorazonCompleto;
            else
                imagenesVidas[i].sprite = spriteCorazonVacio;
        }
    }
    if (textoMonedasHUD != null) 
        textoMonedasHUD.text = monedasRecogidas.ToString();
}
```

### 2.3 Configuración en Unity
1. Crear 3 objetos `Image` en el Canvas del HUD
2. Asignar el sprite `CorazonCompleto` como imagen inicial
3. Asignar los 3 objetos al array `imagenesVidas` en el Inspector
4. Asignar los sprites a `spriteCorazonCompleto` y `spriteCorazonVacio`

### 2.4 Resultado Visual
```
[Corazon] [Corazon] [Corazon]   [Moneda] 0
```

Cuando el jugador pierde una vida, el último corazón cambia automáticamente al sprite vacío.

---

## 3. Sistema de Monedas Visual

### 3.1 Objetivo
Reemplazar el texto "Monedas: X" por un icono de moneda seguido del número.

### 3.2 Implementación

#### Modificaciones en `GameManagerLaberinto.cs`:

```csharp
public Image iconoMonedaHUD;             // Icono de moneda en el HUD
public TextMeshProUGUI textoMonedasHUD;  // Texto del número de monedas
```

#### Actualización del texto:

```csharp
if (textoMonedasHUD != null) 
    textoMonedasHUD.text = monedasRecogidas.ToString();
```

### 3.3 Configuración en Unity
1. Crear un objeto `Image` llamado `IconoMoneda`
2. Asignar el sprite `moneda.png` al campo Source Image
3. Ajustar tamaño: Width: 50, Height: 50
4. Colocar al lado del texto de monedas
5. Asignar la referencia en `GameManagerLaberinto`

### 3.4 Solución de Problemas

**Problema:** El sprite aparece recortado.

**Solución:**
1. Seleccionar `moneda.png` en la carpeta Resources
2. En el Inspector, cambiar **Mesh Type** a `Full Rect`
3. Presionar **Apply**

---

## 4. Arquitectura de Escenas

### 4.1 Estructura de Escenas

El proyecto se organiza en 3 escenas principales:

1. **PanelGameTiltMaze** - Menú principal del juego
2. **MenuLevelsTiltMaze** - Selector de niveles
3. **Level8TiltMaze** - Nivel de juego

### 4.2 Separación de Responsabilidades

#### Antes (Monolítico):
- Todo en una sola escena
- GameManager controlaba menús y juego
- Difícil de clonar para nuevos niveles

#### Después (Modular):
- **PanelGameTiltMaze**: Solo menú y navegación
- **MenuLevelsTiltMaze**: Selector de niveles
- **Level8TiltMaze**: Solo gameplay

### 4.3 Scripts Creados

#### MenuManager.cs
Gestiona la navegación del menú principal:

```csharp
public class MenuManager : MonoBehaviour
{
    public GameObject panelMenu;
    public GameObject panelInstrucciones;
    public GameObject panelOpciones;
    public GameObject panelIP;
    
    public void ElegirTeclado() { ... }
    public void ElegirMovil() { ... }
    public void Jugar() { ... }
}
```

#### LevelSelector.cs
Script simple para cargar niveles desde el menú de selección:

```csharp
public class LevelSelector : MonoBehaviour
{
    public string nombreEscenaNivel = "Level8TiltMaze";
    
    public void JugarNivel()
    {
        SceneManager.LoadScene(nombreEscenaNivel);
    }
}
```

### 4.4 Modificaciones a GameManagerLaberinto

Se eliminaron las funcionalidades de menú y se mantuvieron solo las del nivel:

**Eliminado:**
- `panelMenu`
- `panelInstrucciones`
- `panelOpciones`
- `panelIP`
- Métodos de navegación de menú

**Mantenido:**
- `panelJuego`
- `panelVictoria`
- `panelDerrota`
- Sistema de vidas y monedas
- Control de física

### 4.5 Build Settings

Orden de escenas en Build:
1. PanelGameTiltMaze (índice 0)
2. MenuLevelsTiltMaze (índice 1)
3. Level8TiltMaze (índice 2)

---

## 5. Gestión de Red Persistente

### 5.1 Problema Inicial

Cuando se separaron las escenas, el servidor TCP se cerraba al cambiar de escena, rompiendo la conexión con Java.

**Error:**
```
Solo se permite un uso de cada dirección de socket 
(protocolo/dirección de red/puerto)
```

### 5.2 Solución: PersistentNetworkManager

Se creó un servidor TCP persistente que sobrevive entre escenas usando `DontDestroyOnLoad`.

#### Características:
- **Singleton**: Solo existe una instancia
- **Persistente**: No se destruye al cambiar de escena
- **Puerto 5000**: Escucha conexiones de Java
- **Callbacks**: Notifica cuando Java se conecta

#### Implementación:

```csharp
public class PersistentNetworkManager : MonoBehaviour
{
    public static PersistentNetworkManager Instance { get; private set; }
    
    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }
    
    public void IniciarServidor() { ... }
    public void DetenerServidor() { ... }
    public string GetLocalIPAddress() { ... }
}
```

### 5.3 Integración con MenuManager

Cuando el usuario elige "Móvil":

```csharp
public void ElegirMovil()
{
    // Crear o usar PersistentNetworkManager
    PersistentNetworkManager networkManager = PersistentNetworkManager.Instance;
    if (networkManager == null)
    {
        GameObject go = new GameObject("PersistentNetworkManager");
        networkManager = go.AddComponent<PersistentNetworkManager>();
    }
    
    networkManager.onJavaConnected = OnJavaConnected;
    networkManager.IniciarServidor();
}
```

### 5.4 Integración con GameManagerLaberinto

Al cargar el nivel, verifica si existe el persistente:

```csharp
void Start()
{
    // Conectar con PersistentNetworkManager si existe
    if (usaMovil && PersistentNetworkManager.Instance != null)
    {
        PersistentNetworkManager.Instance.onDataReceived = ProcesarDatosMovil;
    }
}
```

### 5.5 Modificaciones a SocketServer

`SocketServer.cs` en Level8TiltMaze verifica si ya existe el persistente:

```csharp
void Start()
{
    // Verificar si PersistentNetworkManager ya existe
    if (PersistentNetworkManager.Instance != null)
    {
        PersistentNetworkManager.Instance.onDataReceived = ProcesarDatosPersistentes;
        return; // No iniciar servidor propio
    }
    
    StartServer(); // Iniciar solo si no existe persistente
}
```

### 5.6 Flujo de Datos

```
Java Server → PersistentNetworkManager (puerto 5000)
                     ↓
              [Cambio de escena]
                     ↓
         GameManagerLaberinto ← Datos del móvil
                     ↓
              Control del juego
```

---

## 6. Flujo de Juego

### 6.1 Secuencia Completa

1. **Inicio**: Carga `PanelGameTiltMaze`
2. **Menú Principal**: Muestra opciones (Jugar, Instrucciones, etc.)
3. **Selección de Control**:
   - **Teclado**: Guarda preferencia y carga `MenuLevelsTiltMaze`
   - **Móvil**: 
     - Muestra Panel_IP con dirección IP
     - Inicia `PersistentNetworkManager` en puerto 5000
     - Espera conexión de Java
     - Al conectar, carga `MenuLevelsTiltMaze`
4. **Selector de Niveles**: Muestra niveles disponibles
5. **Carga Nivel**: Carga `Level8TiltMaze`
   - Si viene de móvil, se conecta al `PersistentNetworkManager`
   - Si viene de teclado, inicia juego normalmente
6. **Gameplay**: Control por inclinación o teclado
7. **Victoria/Derrota**: Muestra panel correspondiente
8. **Volver al Menú**: Recarga `PanelGameTiltMaze`

### 6.2 Diagrama de Estados

```
[PanelGameTiltMaze]
       ↓
   [Elegir Control]
   /             \
Teclado         Móvil
   |               |
   |         [Mostrar IP]
   |               |
   |         [Esperar Java]
   |               |
   \             /
[MenuLevelsTiltMaze]
       ↓
[Level8TiltMaze]
       ↓
[Victoria/Derrota]
       ↓
[Volver al Menú]
```

---

## 7. Solución de Problemas

### 7.1 Errores de Compilación

#### Error: `Can't add script component 'MenuManager'`
**Causa:** Caracteres especiales (tildes) en el código C#
**Solución:** Reescribir el script sin caracteres especiales

#### Error: `GameManagerLaberinto does not contain a definition for 'textDisplayIP'`
**Causa:** Eliminación de variables que usa `SocketServer.cs`
**Solución:** Restaurar las variables `textDisplayIP` y `textoEstadoConexion`

#### Error: `Object.FindObjectOfType is obsolete`
**Causa:** Unity 6 deprecó `FindObjectOfType`
**Solución:** Reemplazar por `FindFirstObjectByType` en todos los scripts:
- BolaDeteccion.cs
- Lava.cs
- Moneda.cs
- MetaLaberinto.cs
- Interruptor.cs
- UnityMainThreadDispatcher.cs

### 7.2 Problemas Visuales

#### Problema: Canvas vacío tras copiar
**Causa:** Los objetos UI necesitan un Canvas
**Solución:** Crear Canvas y hacer los paneles hijos del Canvas

#### Problema: Objetos UI muy pequeños
**Causa:** Escala incorrecta (0.009)
**Solución:** Cambiar Scale a (1, 1, 1)

#### Problema: Video de fondo no se ve
**Causa:** VideoPlayer necesita Render Texture
**Solución:**
1. Crear Render Texture
2. Asignar al VideoPlayer
3. Agregar componente RawImage
4. Asignar Render Texture al RawImage

### 7.3 Problemas de Red

#### Problema: Puerto 5000 ya en uso
**Causa:** Dos servidores intentan usar el mismo puerto
**Solución:** `SocketServer` verifica si existe `PersistentNetworkManager` antes de iniciar

#### Problema: Conexión se pierde al cambiar de escena
**Causa:** El servidor TCP se destruye con la escena
**Solución:** Usar `DontDestroyOnLoad` en `PersistentNetworkManager`

---

## 8. Agradecimientos

Agradecimientos al equipo de desarrollo y a los colaboradores que hicieron posible este proyecto.

---

## 9. Bibliografía

- Unity Technologies. (2024). *Unity Documentation*. https://docs.unity3d.com/
- Microsoft. (2024). *C# Documentation*. https://docs.microsoft.com/en-us/dotnet/csharp/
- TextMeshPro Documentation. https://docs.unity3d.com/Packages/com.unity.textmeshpro@3.0/
- .NET Networking. https://docs.microsoft.com/en-us/dotnet/framework/network-programming/

---

*Documento generado el 13 de mayo de 2026*
*Proyecto: TFG - Game Server Client*
*Unity Version: 6.3 LTS*
