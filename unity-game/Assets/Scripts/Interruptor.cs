using System.Diagnostics;
using UnityEngine;

public class Interruptor : MonoBehaviour
{
    public GameObject puertaAsociada;
    private bool bolaEncima = false;
    private bool yaActivado = false;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Importante: que el nombre coincida o usa el Tag "Player"
        if (collision.gameObject.name == "circle" || collision.CompareTag("Player"))
        {
            bolaEncima = true;
        }
    }


    // SUSTITUYE TUS MÉTODOS DE TRIGGER POR ESTOS:

    private void OnTriggerStay2D(Collider2D collision)
    {
        // Esto enviará un mensaje CUALQUIER COSA que entre, sea la bola o no
        UnityEngine.Debug.Log("¡ALGO ESTÁ TOCANDO EL SENSOR DE: " + gameObject.name + "!");
        bolaEncima = true;
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        bolaEncima = false;
    }

    public void IntentarActivar()
    {
        // Esto te dirá por qué no se activa
        UnityEngine.Debug.Log(gameObject.name + " -> bolaEncima: " + bolaEncima + " | yaActivado: " + yaActivado);

        if (bolaEncima && !yaActivado)
        {
            ActivarMecanismo();
        }
    }
    void ActivarMecanismo()
    {
        yaActivado = true;

        GameManagerLaberinto gm = FindObjectOfType<GameManagerLaberinto>();
        if (gm != null)
        {
            // En lugar de enviar transform.position (el centro), 
            // enviamos la posición + un poquito hacia arriba (eje Y)
            Vector3 posicionSegura = transform.position + new Vector3(0, 1.0f, 0);
            gm.ActualizarCheckpoint(posicionSegura);
        }

        if (puertaAsociada != null) puertaAsociada.SetActive(false);
        GetComponent<SpriteRenderer>().color = Color.green;
    

   
    }
}