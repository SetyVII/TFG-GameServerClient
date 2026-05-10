using UnityEngine;

public class Soplable : MonoBehaviour
{
    public GameObject objetivo;
    public float cooldown = 1.5f;
    public bool estadoActivo = false;
    private float ultimaActivacion = -999f;

    public void Activar()
    {
        if (Time.time - ultimaActivacion < cooldown) return;
        ultimaActivacion = Time.time;
        estadoActivo = !estadoActivo;

        if (objetivo != null)
            objetivo.SetActive(!objetivo.activeSelf);

        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null)
            sr.color = estadoActivo ? Color.cyan : Color.white;
    }
}
