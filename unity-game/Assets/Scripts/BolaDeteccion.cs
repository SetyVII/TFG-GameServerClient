using UnityEngine;

public class BolaDeteccion : MonoBehaviour
{
    private GameManagerLaberinto gm;
    private CircleCollider2D miCollider;

    void Start()
    {
        gm = FindFirstObjectByType<GameManagerLaberinto>();
        miCollider = GetComponent<CircleCollider2D>();
    }

    void OnTriggerStay2D(Collider2D other)
    {
        // Solo morimos si tocamos lava Y NO estamos saltando
        if (other.CompareTag("Trampa") && !gm.estaSaltando)
        {
            gm.HasTocadoLava();
        }

        if (other.CompareTag("Meta"))
        {
            gm.HasGanado();
        }
    }
}