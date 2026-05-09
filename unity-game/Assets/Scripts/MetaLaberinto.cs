using UnityEngine;

public class MetaLaberinto : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            FindObjectOfType<GameManagerLaberinto>().HasGanado();
        }
    }
}