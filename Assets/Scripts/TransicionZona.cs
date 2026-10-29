using UnityEngine;

/// <summary>
/// Trigger que lleva al jugador a otra zona del Nivel 2 (GestorNivel2.IrAZona).
/// Si tiene una puerta con fusible asignada, solo funciona cuando esa puerta esta abierta.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class TransicionZona : MonoBehaviour
{
    public int zonaDestino = 1;
    public PuertaConLlave puerta;

    bool disparado;

    void Awake()
    {
        GetComponent<Collider2D>().isTrigger = true;
    }

    void OnTriggerEnter2D(Collider2D otro) { Intentar(otro); }
    void OnTriggerStay2D(Collider2D otro)  { Intentar(otro); }

    void Intentar(Collider2D otro)
    {
        if (disparado || !otro.CompareTag("Player")) return;
        if (puerta != null && !puerta.Abierta) return;

        var gestor = FindFirstObjectByType<GestorNivel2>();
        if (gestor == null) return;

        disparado = true;
        gestor.IrAZona(zonaDestino);
    }

    public void Reiniciar() { disparado = false; }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        var col = GetComponent<Collider2D>();
        if (col != null) Gizmos.DrawWireCube(col.bounds.center, col.bounds.size);
    }
}
