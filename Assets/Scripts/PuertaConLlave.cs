using System.Collections;
using UnityEngine;

/// <summary>
/// Puerta solida que solo abre si el jugador lleva un item (por defecto "Fusible").
/// Pon este script en el objeto de la puerta (que ya tiene su BoxCollider2D solido).
/// No necesita triggers: detecta al jugador por distancia. E / F para usarla.
/// </summary>
public class PuertaConLlave : MonoBehaviour
{
    public string nombreItem        = "Fusible";
    public float  radioInteraccion  = 2.6f;
    public float  desplazamiento    = 4.5f;
    public float  duracion          = 0.5f;
    public string mensajeBloqueada  = "Sin energia. Necesitas un fusible.";
    public string mensajeAbierta    = "Colocas el fusible. La puerta cede.";

    bool abierta, animando;

    /// <summary>True cuando la puerta ya termino de abrirse.</summary>
    public bool Abierta => abierta;
    Collider2D[] colisionadores;
    Vector3 posInicial;

    void Awake()
    {
        colisionadores = GetComponents<Collider2D>();
        posInicial     = transform.position;
    }

    void Update()
    {
        if (abierta || animando) return;
        if (!JugadorRef.EstaCerca(transform.position, radioInteraccion)) return;

        var inv = JugadorRef.Transform.GetComponent<InventarioJugador>();
        bool tiene = inv != null && inv.TieneItem(nombreItem);

        UIMensaje.Pista(tiene ? "[E] Colocar fusible" : "[E] Puerta sin energia");

        if (!JugadorRef.PulsoInteractuar()) return;

        if (!tiene)
        {
            UIMensaje.Mostrar(mensajeBloqueada, 3f);
            return;
        }

        inv.UsarItem(nombreItem);
        UIMensaje.Mostrar(mensajeAbierta, 3f);
        GestorRuido.Emitir(8f, transform.position);
        StartCoroutine(Abrir());
    }

    IEnumerator Abrir()
    {
        animando = true;
        foreach (var c in colisionadores) c.enabled = false;

        Vector3 destino = posInicial + Vector3.up * desplazamiento;
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / duracion;
            transform.position = Vector3.Lerp(posInicial, destino, Mathf.SmoothStep(0f, 1f, t));
            yield return null;
        }
        transform.position = destino;

        abierta  = true;
        animando = false;
    }
}
