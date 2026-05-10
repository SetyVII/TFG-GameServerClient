using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class GameManagerLaberinto : MonoBehaviour
{
    [Header("Paneles de Navegaci�n")]
    public GameObject panelMenu;
    public GameObject panelInstrucciones;
    public GameObject panelEleccionMando;
    public GameObject panelIP;
    public GameObject panelVictoria;
    public GameObject panelDerrota; // NUEVO: Panel para cuando pierdes
    public GameObject panelJuego;

    [Header("Referencias de Interfaz (HUD)")]
    public TextMeshProUGUI textoVidasHUD;    // Texto de vidas durante el juego
    public TextMeshProUGUI textoMonedasHUD;  // Texto de monedas durante el juego

    [Header("Referencias de Interfaz (Final)")]
    public TextMeshProUGUI textoFinalVidas;
    public TextMeshProUGUI textoFinalMonedas;
    public TextMeshProUGUI textoFinalTitulo;
    public Color colorVictoria = new Color(0.2f, 0.8f, 0.2f, 1f);
    public Color colorDerrota = new Color(0.8f, 0.2f, 0.2f, 1f);

    [Header("Referencias de Juego")]
    public Rigidbody2D rbBola;
    public TextMeshProUGUI textDisplayIP;

    [Header("Configuraci�n de Control")]
    public float fuerzaMando = 20f;
    public float fuerzaTeclado = 25f;
    public float multiplicadorMovil = 1.0f;
    public float fuerzaSalto = 12f;
    public float tiempoSalto = 0.6f;
    public float cooldownSalto = 1.0f;
    
    [Header("Mapeo giroscopio m�vil")]
    public float maxInclinacionGrados = 35f;
    public float zonaMuertaMovil = 0.08f;
    public float suavizadoMovil = 10f;
    public bool invertirEjeXMovil = false;
    public bool invertirEjeYMovil = false;
    public bool habilitarSaltoPorInclinacion = true;
    [Range(0.1f, 1f)] public float umbralSaltoInclinacion = 0.25f;
    [Range(0.05f, 0.9f)] public float umbralRearmeSaltoInclinacion = 0.12f;

    [Header("Configuraci�n de Partida")]
    public int vidasTotales = 3;
    private int vidasActuales;
    private int monedasRecogidas = 0;

    [HideInInspector] public float sensor_Alpha, sensor_Beta, sensor_Gamma;
    [HideInInspector] public bool estaSaltando = false;
    public bool EsperandoConexionMovil => esperandoConexionMovil;
    private bool puedeSaltar = true;

    private bool juegoIniciado = false;
    private bool usaMovil = false;
    private bool esperandoConexionMovil = false;
    private Vector3 escalaOriginal;
    private Vector3 posicionCheckpoint;
    private Vector2 entradaMovilSuavizada = Vector2.zero;
    private bool gestoSaltoMovilActivo = false;

    void Start()
    {
        Debug.Log("[GameManager] Start() - inicializando juego");
        vidasActuales = vidasTotales;
        monedasRecogidas = 0;
        ActualizarHUD();

        if (rbBola != null)
        {
            escalaOriginal = rbBola.transform.localScale;
            posicionCheckpoint = rbBola.transform.position;
            rbBola.gravityScale = 2f;
            rbBola.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        }

        rbBola.simulated = false;
        MostrarMenuPrincipal();
    }

    // --- SISTEMA DE PROGRESO ---

    public void SumarMoneda()
    {
        monedasRecogidas++;
        ActualizarHUD();
    }

    private void ActualizarHUD()
    {
        if (textoVidasHUD != null) textoVidasHUD.text = "Vidas: " + vidasActuales;
        if (textoMonedasHUD != null) textoMonedasHUD.text = "Monedas: " + monedasRecogidas;
    }

    public void ActualizarCheckpoint(Vector3 nuevaPosicion)
    {
        posicionCheckpoint = nuevaPosicion;
        UnityEngine.Debug.Log("Checkpoint actualizado a: " + nuevaPosicion);
    }

    public void HasTocadoLava()
    {
        if (!estaSaltando && juegoIniciado)
        {
            vidasActuales--;
            ActualizarHUD();

            if (vidasActuales <= 0)
            {
                TerminarJuego(false);
            }
            else
            {
                // Reset a checkpoint
                rbBola.linearVelocity = Vector2.zero;
                rbBola.angularVelocity = 0f;
                rbBola.transform.position = posicionCheckpoint;
            }
        }
    }

    // --- FIN DEL JUEGO ---

    public void HasGanado()
    {
        TerminarJuego(true);
    }

    private void TerminarJuego(bool victoria)
    {
        juegoIniciado = false;
        rbBola.linearVelocity = Vector2.zero;
        rbBola.simulated = false;

        if (textoFinalVidas != null) textoFinalVidas.text = "Vidas: " + vidasActuales;
        if (textoFinalMonedas != null) textoFinalMonedas.text = "Monedas: " + monedasRecogidas;

        DesactivarTodo();

        GameObject panelFinal = victoria ? panelVictoria : panelDerrota;
        if (panelFinal == null) panelFinal = panelVictoria;

        if (textoFinalTitulo != null)
        {
            textoFinalTitulo.text = victoria ? "Victoria!" : "Has Perdido";
            textoFinalTitulo.color = victoria ? colorVictoria : colorDerrota;
        }

        panelFinal.SetActive(true);
    }

    // --- NAVEGACI�N ---

    public void MostrarMenuPrincipal() { esperandoConexionMovil = false; DesactivarTodo(); panelMenu.SetActive(true); }
    public void AbrirInstrucciones() { panelInstrucciones.SetActive(true); }
    public void CerrarInstrucciones() { panelInstrucciones.SetActive(false); }
    public void IrAEleccionMando() { DesactivarTodo(); panelEleccionMando.SetActive(true); }
    public void ElegirTeclado() { esperandoConexionMovil = false; usaMovil = false; EmpezarJuego(); }
    public void ElegirMovil()
    {
        esperandoConexionMovil = true;
        usaMovil = true;
        DesactivarTodo();
        panelIP.SetActive(true);

        SocketServer socket = GetComponent<SocketServer>();
        bool yaConectado = socket != null && socket.IsClientConnected;
        Debug.Log($"[GameManager] ElegirMovil() - IsClientConnected={yaConectado}");

        if (yaConectado)
        {
            Debug.Log("[GameManager] Cliente TCP ya conectado, iniciando juego directamente.");
            EmpezarJuego();
        }
    }

    public void EmpezarJuego()
    {
        esperandoConexionMovil = false;
        DesactivarTodo();
        panelJuego.SetActive(true);
        rbBola.simulated = true;
        entradaMovilSuavizada = Vector2.zero;
        gestoSaltoMovilActivo = false;
        juegoIniciado = true;
    }

    private void DesactivarTodo()
    {
        panelMenu.SetActive(false);
        panelInstrucciones.SetActive(false);
        panelEleccionMando.SetActive(false);
        panelIP.SetActive(false);
        panelVictoria.SetActive(false);
        if (panelDerrota != null) panelDerrota.SetActive(false);
        panelJuego.SetActive(false);
    }

    // --- CONTROL ---

    void Update()
    {
        if (!juegoIniciado) return;

        if (!usaMovil)
        {
            if (Input.GetKeyDown(KeyCode.Space) && puedeSaltar) AccionBotonA();
            if (Input.GetKeyDown(KeyCode.E)) AccionBotonB();
            if (Input.GetKeyDown(KeyCode.F)) AccionSoplar();
        }
        else
        {
            ProcesarSaltoPorInclinacion();
        }
    }

    void FixedUpdate()
    {
        if (!juegoIniciado) return;

        Vector2 entradaMovimiento;
        float fuerzaAplicada;

        if (usaMovil)
        {
            Vector2 entradaMovil = ObtenerEntradaMovil();
            float lerpFactor = 1f - Mathf.Exp(-suavizadoMovil * Time.fixedDeltaTime);
            entradaMovilSuavizada = Vector2.Lerp(entradaMovilSuavizada, entradaMovil, lerpFactor);
            entradaMovimiento = entradaMovilSuavizada;
            fuerzaAplicada = fuerzaTeclado * multiplicadorMovil;
        }
        else
        {
            float moveX = Input.GetAxis("Horizontal");
            float moveY = Input.GetAxis("Vertical");
            entradaMovimiento = new Vector2(moveX, moveY);
            fuerzaAplicada = fuerzaTeclado;
        }

        rbBola.AddForce(entradaMovimiento * fuerzaAplicada, ForceMode2D.Force);

        if (rbBola.linearVelocity.magnitude > 25f)
        {
            rbBola.linearVelocity = rbBola.linearVelocity.normalized * 25f;
        }
    }

    private Vector2 ObtenerEntradaMovil()
    {
        Vector2 entrada = ObtenerEntradaMovilSinZonaMuerta();
        float x = entrada.x;
        float y = entrada.y;

        if (Mathf.Abs(x) < zonaMuertaMovil) x = 0f;
        if (Mathf.Abs(y) < zonaMuertaMovil) y = 0f;

        return new Vector2(x, y);
    }

    private Vector2 ObtenerEntradaMovilSinZonaMuerta()
    {
        float x;
        float y;

        bool yaNormalizado = Mathf.Abs(sensor_Gamma) <= 1.2f && Mathf.Abs(sensor_Beta) <= 1.2f;

        // sensor_Gamma = beta del móvil  -> izquierda/derecha (landscape)
        // sensor_Beta  = gamma del móvil -> arriba/abajo   (landscape)
        if (yaNormalizado)
        {
            x = Mathf.Clamp(sensor_Gamma, -1f, 1f);
            y = Mathf.Clamp(sensor_Beta,  -1f, 1f);
        }
        else
        {
            float gradosMax = Mathf.Max(1f, maxInclinacionGrados);
            x = Mathf.Clamp(sensor_Gamma / gradosMax, -1f, 1f);
            y = Mathf.Clamp(sensor_Beta  / gradosMax, -1f, 1f);
        }

        if (invertirEjeXMovil) x = -x;
        if (invertirEjeYMovil) y = -y;

        return new Vector2(x, y);
    }

    private void ProcesarSaltoPorInclinacion()
    {
        if (!habilitarSaltoPorInclinacion) return;

        Vector2 entradaMovilRaw = ObtenerEntradaMovilSinZonaMuerta();
        float inclinacionVertical = entradaMovilRaw.y;
        float umbralDisparo = umbralSaltoInclinacion;
        float umbralRearme = Mathf.Min(umbralRearmeSaltoInclinacion, umbralDisparo - 0.01f);

        // gamma negativo = salto (inclinar móvil hacia adelante)
        if (!gestoSaltoMovilActivo && inclinacionVertical <= -umbralDisparo)
        {
            gestoSaltoMovilActivo = true;
            AccionBotonA();
            return;
        }

        if (gestoSaltoMovilActivo && inclinacionVertical >= -umbralRearme)
        {
            gestoSaltoMovilActivo = false;
        }
    }

    public void AccionBotonA()
    {
        if (!juegoIniciado || !puedeSaltar) return;
        rbBola.linearVelocity = new Vector2(rbBola.linearVelocity.x, fuerzaSalto);
        StartCoroutine(RutinaSaltoYCooldown());
    }

    IEnumerator RutinaSaltoYCooldown()
    {
        estaSaltando = true;
        puedeSaltar = false;

        float mitadTiempoSalto = tiempoSalto / 2f;
        float t = 0;

        while (t < mitadTiempoSalto)
        {
            t += Time.deltaTime;
            rbBola.transform.localScale = Vector3.Lerp(escalaOriginal, escalaOriginal * 1.5f, t / mitadTiempoSalto);
            yield return null;
        }

        t = 0;
        while (t < mitadTiempoSalto)
        {
            t += Time.deltaTime;
            rbBola.transform.localScale = Vector3.Lerp(escalaOriginal * 1.5f, escalaOriginal, t / mitadTiempoSalto);
            yield return null;
        }

        rbBola.transform.localScale = escalaOriginal;
        estaSaltando = false;
        yield return new WaitForSeconds(cooldownSalto);
        puedeSaltar = true;
    }

    public void AccionBotonB()
    {
        if (!juegoIniciado) return;
        Interruptor[] todos = UnityEngine.Object.FindObjectsByType<Interruptor>(FindObjectsSortMode.None);
        foreach (Interruptor inter in todos) inter.IntentarActivar();
    }

    public void AccionSoplar()
    {
        if (!juegoIniciado) return;
        Debug.Log("[GameManager] AccionSoplar() - buscando Soplables...");
        Soplable[] todos = UnityEngine.Object.FindObjectsByType<Soplable>(FindObjectsSortMode.None);
        foreach (Soplable s in todos) s.Activar();
    }

    public void VolverAlMenuPrincipal() { Debug.Log("[GameManager] VolverAlMenuPrincipal - recargando escena..."); SceneManager.LoadScene(SceneManager.GetActiveScene().name); }

    public void SalirDelJuego()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        UnityEngine.Application.Quit();
#endif
    }
}
