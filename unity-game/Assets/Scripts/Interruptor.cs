using UnityEngine;

public class Interruptor : MonoBehaviour
{
    public GameObject puertaAsociada;
    private bool bolaEncima = false;
    private bool yaActivado = false;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            bolaEncima = true;
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        bolaEncima = false;
    }

    public void IntentarActivar()
    {
        if (bolaEncima && !yaActivado)
        {
            ActivarMecanismo();
        }
    }

    private void ActivarMecanismo()
    {
        yaActivado = true;

        GameManagerLaberinto gm = FindObjectOfType<GameManagerLaberinto>();
        if (gm != null)
        {
            Vector3 posicionSegura = transform.position + new Vector3(0, 1.0f, 0);
            gm.ActualizarCheckpoint(posicionSegura);
        }

        if (puertaAsociada != null) puertaAsociada.SetActive(false);
        GetComponent<SpriteRenderer>().color = Color.green;
    }
}
