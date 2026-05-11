using UnityEngine;

public class Lava : MonoBehaviour
{
    // Usamos OnTriggerStay2D por si la bola se queda quieta encima de la lava
    private void OnTriggerStay2D(Collider2D collision)
    {
        if (collision.gameObject.name == "circle" || collision.CompareTag("Player"))
        {
            FindObjectOfType<GameManagerLaberinto>().HasTocadoLava();
        }
    }
}