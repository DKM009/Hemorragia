using UnityEngine;

/// <summary>
/// Lata lanzada: al chocar hace ruido (los Purgadores cercanos van a investigar) y,
/// si golpea directamente a un enemigo, le quita vida.
/// </summary>
public class LataProyectil : MonoBehaviour
{
    public float ruido           = 30f;
    public float radioAtraccion  = 14f;
    public float segundosInvestigar = 6f;
    public float dano            = 1f;

    bool impactado;

    void OnCollisionEnter2D(Collision2D col)
    {
        if (impactado) return;
        impactado = true;

        Vector2 punto = transform.position;
        GestorRuido.Emitir(ruido, punto);
        AudioAuto.Lata();

        var salud = col.collider.GetComponentInParent<SaludEnemigo>();
        if (salud != null && !salud.Muerto) salud.RecibirDano(dano, punto);

        foreach (var purgador in FindObjectsByType<PurgadorIA>(FindObjectsSortMode.None))
        {
            if (Vector2.Distance(purgador.transform.position, punto) <= radioAtraccion)
                purgador.Investigar(punto, segundosInvestigar);
        }

        Destroy(gameObject, 0.5f);
    }
}
