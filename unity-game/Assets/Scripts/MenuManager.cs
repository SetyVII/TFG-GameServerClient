using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class MenuManager : MonoBehaviour
{
    [Header("Paneles de Navegacion")]
    public GameObject panelMenu;
    public GameObject panelInstrucciones;
    public GameObject panelOpciones;
    public GameObject panelIP;

    [Header("Referencias IP")]
    public TextMeshProUGUI textDisplayIP;
    public TextMeshProUGUI textoEstadoConexion;

    [Header("Configuracion")]
    public string nombreEscenaNivel = "Level8TiltMaze";
    public string nombreEscenaMenuNiveles = "MenuLevelsTiltMaze";
    public bool usarMovil = false;

    private void Start()
    {
        MostrarMenuPrincipal();
    }

    public void MostrarMenuPrincipal()
    {
        DesactivarTodo();
        panelMenu.SetActive(true);
    }

    public void AbrirInstrucciones()
    {
        panelInstrucciones.SetActive(true);
    }

    public void CerrarInstrucciones()
    {
        panelInstrucciones.SetActive(false);
    }

    public void AbrirOpciones()
    {
        DesactivarTodo();
        panelOpciones.SetActive(true);
    }

    public void IrASeleccionMando()
    {
        DesactivarTodo();
        Jugar();
    }

    public void ElegirTeclado()
    {
        usarMovil = false;
        PlayerPrefs.SetInt("UsarMovil", 0);
        PlayerPrefs.Save();
        Jugar();
    }

    public void ElegirMovil()
    {
        usarMovil = true;
        PlayerPrefs.SetInt("UsarMovil", 1);
        PlayerPrefs.Save();
        DesactivarTodo();
        panelIP.SetActive(true);
        
        // Crear o usar PersistentNetworkManager
        PersistentNetworkManager networkManager = PersistentNetworkManager.Instance;
        if (networkManager == null)
        {
            GameObject go = new GameObject("PersistentNetworkManager");
            networkManager = go.AddComponent<PersistentNetworkManager>();
        }
        
        networkManager.onJavaConnected = OnJavaConnected;
        networkManager.IniciarServidor();
        
        if (textDisplayIP != null)
            textDisplayIP.text = "IP: " + networkManager.GetLocalIPAddress();
        if (textoEstadoConexion != null)
            textoEstadoConexion.text = "Esperando controlador...";
    }
    
    private void OnJavaConnected()
    {
        UnityEngine.Debug.Log("[MenuManager] Java conectado!");
        if (textoEstadoConexion != null)
            textoEstadoConexion.text = "Controlador conectado!";
        
        // Esperar un momento y cargar menu de niveles
        Invoke("CargarMenuNiveles", 1.5f);
    }
    
    private void CargarMenuNiveles()
    {
        SceneManager.LoadScene(nombreEscenaMenuNiveles);
    }

    public void Jugar()
    {
        SceneManager.LoadScene(nombreEscenaMenuNiveles);
    }

    public void VolverAlMenu()
    {
        MostrarMenuPrincipal();
    }

    public void SalirDelJuego()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void DesactivarTodo()
    {
        if (panelMenu != null) panelMenu.SetActive(false);
        if (panelInstrucciones != null) panelInstrucciones.SetActive(false);
        if (panelOpciones != null) panelOpciones.SetActive(false);
        if (panelIP != null) panelIP.SetActive(false);
    }
}
