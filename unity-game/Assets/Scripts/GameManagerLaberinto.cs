using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class GameManagerLaberinto : MonoBehaviour
{
    [Header("Paneles de Navegación")]
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
    public TextMeshProUGUI textoFinalVidas;  // Texto de vidas en panel victoria/derrota
    public TextMeshProUGUI textoFinalMonedas;// Texto de monedas en panel victoria/derrota

    [Header("Referencias de Juego")]
    public Rigidbody2D rbBola;
    public TextMeshProUGUI textDisplayIP;

    [Header("Configuración de Control")]
    public float fuerzaMando = 20f;
    public float fuerzaTeclado = 25f;
    public float fuerzaSalto = 12f;
    public float tiempoSalto = 0.6f;
    public float cooldownSalto = 1.0f;

    [Header("Configuración de Partida")]
    public int vidasTotales = 3;
    private int vidasActuales;
    private int monedasRecogidas = 0;

    [HideInInspector] public float sensor_Alpha, sensor_Beta, sensor_Gamma;
    [HideInInspector] public bool estaSaltando = false;
    private bool puedeSaltar = true;

    private bool juegoIniciado = false;
    private bool usaMovil = false;
    private Vector3 escalaOriginal;
    private Vector3 posicionCheckpoint;

    void Start()
    {
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
                rbBola.velocity = Vector2.zero;
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
        rbBola.velocity = Vector2.zero;
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

    // --- NAVEGACIÓN ---

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
            Vector2 fuerzaMovil = new Vector2(sensor_Gamma, -sensor_Beta);
            rbBola.AddForce(fuerzaMovil * fuerzaMando);
        }
        else
        {
            float moveX = Input.GetAxis("Horizontal");
            float moveY = Input.GetAxis("Vertical");
            rbBola.AddForce(new Vector2(moveX, moveY) * fuerzaTeclado);
        }

        if (rbBola.velocity.magnitude > 25f)
        {
            rbBola.velocity = rbBola.velocity.normalized * 25f;
        }
    }

    public void AccionBotonA()
    {
        if (!juegoIniciado || !puedeSaltar) return;
        rbBola.velocity = new Vector2(rbBola.velocity.x, fuerzaSalto);
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