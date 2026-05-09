using UnityEngine;

public class Moneda : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            FindObjectOfType<GameManagerLaberinto>().SumarMoneda();
            Destroy(gameObject); // La moneda desaparece
        }
    }
}