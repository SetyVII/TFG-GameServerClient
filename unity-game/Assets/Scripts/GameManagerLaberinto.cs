using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
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
    public Image[] imagenesVidas = new Image[3]; // 3 corazones en el HUD
    public Sprite spriteCorazonCompleto;
    public Sprite spriteCorazonVacio;
    public Image iconoMonedaHUD;             // Icono de moneda en el HUD
    public TextMeshProUGUI textoMonedasHUD;  // Texto del número de monedas

    [Header("Referencias de Interfaz (Final)")]
    public TextMeshProUGUI textoFinalVidas;  // Texto de vidas en panel victoria/derrota
    public TextMeshProUGUI textoFinalMonedas;// Texto de monedas en panel victoria/derrota

    [Header("Referencias de Juego")]
    public Rigidbody2D rbBola;
    public TextMeshProUGUI textDisplayIP;
    public TextMeshProUGUI textoEstadoConexion;

    [Header("Configuraci�n de Control")]
    public float fuerzaMando = 4.5f; // Fuerza para AddForce (default = Medio)
    public float fuerzaTeclado = 25f;
    public float deadzoneMovil = 0.25f;
    public float velocidadMaximaMovil = 8f; // Límite de velocidad horizontal (default = Medio)
    public float fuerzaSalto = 12f;
    public float tiempoSalto = 0.6f;
    public float cooldownSalto = 1.0f;

    [Header("Configuraci�n de Partida")]
    public int vidasTotales = 3;
    private int vidasActuales;
    private int monedasRecogidas = 0;

    [HideInInspector] public volatile float sensor_Alpha, sensor_Beta, sensor_Gamma;
    private float smooth_Gamma;
    private float gammaOffset = 0f;
    private bool calibrado = false;
    private int calibrationFrames = 0;
    private const int CALIBRATION_NEEDED = 30; // 30 frames = ~0.5 segundos
    [HideInInspector] public bool estaSaltando = false;
    private bool puedeSaltar = true;

    private bool juegoIniciado = false;
    public bool JuegoIniciado => juegoIniciado;
    private bool usaMovil = false;
    private Vector3 escalaOriginal;
    private Vector3 posicionCheckpoint;

    void Start()
    {
        // Cargar sprites de corazones desde Resources
        if (spriteCorazonCompleto == null)
            spriteCorazonCompleto = Resources.Load<Sprite>("CorazonCompleto");
        if (spriteCorazonVacio == null)
            spriteCorazonVacio = Resources.Load<Sprite>("CorazonVacio");

        // Inicializar valores
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
        if (textoMonedasHUD != null) textoMonedasHUD.text = monedasRecogidas.ToString();
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

        // Escribir resultados en los textos finales
        if (textoFinalVidas != null) textoFinalVidas.text = "Vidas: " + vidasActuales;
        if (textoFinalMonedas != null) textoFinalMonedas.text = "Monedas: " + monedasRecogidas;

        DesactivarTodo();

        if (victoria)
            panelVictoria.SetActive(true);
        else
            panelDerrota.SetActive(true);
    }

    // --- NAVEGACI�N ---

    public void MostrarMenuPrincipal() { DesactivarTodo(); panelMenu.SetActive(true); }
    public void AbrirInstrucciones() { panelInstrucciones.SetActive(true); }
    public void CerrarInstrucciones() { panelInstrucciones.SetActive(false); }
    public void IrAEleccionMando() { DesactivarTodo(); panelEleccionMando.SetActive(true); }
    public void ElegirTeclado() { usaMovil = false; EmpezarJuego(); }
    public void ElegirMovil() { usaMovil = true; DesactivarTodo(); panelIP.SetActive(true); }

    public void EmpezarJuego()
    {
        DesactivarTodo();
        panelJuego.SetActive(true);
        rbBola.simulated = true;
        juegoIniciado = true;
        // Resetear calibración para nueva partida
        calibrado = false;
        calibrationFrames = 0;
        gammaOffset = 0f;
        smooth_Gamma = 0f;
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
        }
    }

    void FixedUpdate()
    {
        if (!juegoIniciado) return;

        if (usaMovil)
        {
            // Fase de calibración: promediar 30 frames (~0.5 segundos) para obtener offset preciso
            if (!calibrado)
            {
                gammaOffset += sensor_Gamma;
                calibrationFrames++;
                
                if (calibrationFrames >= CALIBRATION_NEEDED)
                {
                    gammaOffset /= CALIBRATION_NEEDED;
                    calibrado = true;
                    UnityEngine.Debug.Log("[GameManager] Calibrado completado. Offset: " + gammaOffset + " (" + calibrationFrames + " frames)");
                }
                return; // No mover durante calibración
            }
            
            // Leer gamma directamente del sensor
            float gammaRelativo = sensor_Gamma - gammaOffset;
            
            // Normalizar: 30° de inclinación = input máximo
            float inputX = Mathf.Clamp(gammaRelativo / 30f, -1f, 1f);
            
            // Deadzone
            if (Mathf.Abs(inputX) < deadzoneMovil)
            {
                inputX = 0f;
            }
            
            // OPCIÓN B: Física realista con AddForce
            // Fuerza proporcional a la inclinación
            rbBola.AddForce(new Vector2(inputX, 0f) * fuerzaMando);
            
            // Limitar velocidad horizontal máxima (la gravedad en Y sigue libre)
            if (Mathf.Abs(rbBola.linearVelocity.x) > velocidadMaximaMovil)
            {
                rbBola.linearVelocity = new Vector2(
                    Mathf.Sign(rbBola.linearVelocity.x) * velocidadMaximaMovil,
                    rbBola.linearVelocity.y
                );
            }
        }
        else
        {
            float moveX = Input.GetAxis("Horizontal");
            float moveY = Input.GetAxis("Vertical");
            rbBola.AddForce(new Vector2(moveX, moveY) * fuerzaTeclado);
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

    public void VolverAlMenuPrincipal() { SceneManager.LoadScene(SceneManager.GetActiveScene().name); }

    public void SalirDelJuego()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        UnityEngine.Application.Quit();
#endif
    }
}