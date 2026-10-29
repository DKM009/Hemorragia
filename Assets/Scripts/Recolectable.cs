using UnityEngine;

/// <summary>
/// Objeto que se recoge al acercarse: venda (cura sangre), fusible (abre la puerta
/// de la Bodega) o lata (se lanza con Q para distraer a los Purgadores).
/// No necesita collider: detecta al jugador por distancia.
/// </summary>
public class Recolectable : MonoBehaviour
{
    public enum Tipo { Venda, Fusible, Lata }

    public Tipo  tipo     = Tipo.Venda;
    [Tooltip("Sangre que recupera una venda.")]
    public float curacion = 30f;
    [Tooltip("Distancia a la que se recoge.")]
    public float radio    = 0.9f;
    [Tooltip("Texto al recoger. Vacio = texto por defecto del tipo.")]
    public string mensaje = "";

    Vector3 posBase;

    void Start()
    {
        posBase = transform.position;
    }

    void Update()
    {
        // Flotar suave para que se note
        transform.position = posBase + Vector3.up * (Mathf.Sin(Time.time * 3f) * 0.08f);

        if (!JugadorRef.EstaCerca(posBase, radio)) return;
        Recoger();
    }

    void Recoger()
    {
        var jugador = JugadorRef.Transform;
        if (jugador == null) return;

        switch (tipo)
        {
            case Tipo.Venda:
            {
                var sangre = jugador.GetComponent<SistemaSangre>();
                if (sangre != null)
                {
                    if (sangre.PctSangre >= 0.999f)
                    {
                        UIMensaje.Pista("Sangre llena");
                        return;
                    }
                    sangre.Curar(curacion);
                }
                UIMensaje.Mostrar(string.IsNullOrEmpty(mensaje) ? "Venda: recuperas sangre." : mensaje, 3f);
                break;
            }
            case Tipo.Fusible:
            {
                ObtenerInventario(jugador).AgregarItem("Fusible");
                UIMensaje.Mostrar(string.IsNullOrEmpty(mensaje)
                    ? "Fusible recogido. Alguna puerta sin energia lo necesita." : mensaje, 4f);
                break;
            }
            case Tipo.Lata:
            {
                ObtenerInventario(jugador).AgregarLata();
                UIMensaje.Mostrar(string.IsNullOrEmpty(mensaje)
                    ? "Lata recogida. Pulsa Q para lanzarla y distraer a los Purgadores." : mensaje, 3.5f);
                break;
            }
        }

        gameObject.SetActive(false);
    }

    static InventarioJugador ObtenerInventario(Transform jugador)
    {
        var inv = jugador.GetComponent<InventarioJugador>();
        if (inv == null) inv = jugador.gameObject.AddComponent<InventarioJugador>();
        return inv;
    }
}
