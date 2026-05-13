using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelSelector : MonoBehaviour
{
    [Header("Configuracion")]
    public string nombreEscenaNivel = "Level8TiltMaze";

    public void JugarNivel()
    {
        SceneManager.LoadScene(nombreEscenaNivel);
    }

    public void VolverAlMenu()
    {
        SceneManager.LoadScene("PanelGameTiltMaze");
    }

    public void SalirDelJuego()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
