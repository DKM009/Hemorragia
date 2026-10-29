using UnityEngine;

/// <summary>
/// Muestra un mensaje en pantalla cuando el jugador se acerca. Sirve para el tutorial
/// y las pistas del nivel. No necesita collider.
/// </summary>
public class ZonaMensaje : MonoBehaviour
{
    [TextArea(2, 5)]
    public string texto    = "";
    public float  radio    = 4f;
    public float  duracion = 6f;
    public bool   unaVez   = true;

    bool mostrado, dentro;

    void Update()
    {
        bool cerca = JugadorRef.EstaCerca(transform.position, radio);

        if (cerca && !dentro && !(unaVez && mostrado))
        {
            UIMensaje.Mostrar(texto, duracion);
            mostrado = true;
        }
        dentro = cerca;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.3f, 0.8f, 1f, 0.4f);
        Gizmos.DrawWireSphere(transform.position, radio);
    }
}
