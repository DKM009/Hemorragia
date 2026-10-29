using UnityEngine;

/// <summary>
/// Lata lanzada: al chocar con algo hace ruido. Los Purgadores dentro del radio
/// van a investigar el punto de impacto y La Fosa tambien lo escucha.
/// </summary>
public class LataProyectil : MonoBehaviour
{
    public float ruido           = 30f;
    public float radioAtraccion  = 14f;
    public float segundosInvestigar = 6f;

    bool impactado;

    void OnCollisionEnter2D(Collision2D col)
    {
        if (impactado) return;
        impactado = true;

        Vector2 punto = transform.position;
        GestorRuido.Emitir(ruido, punto);

        foreach (var purgador in FindObjectsByType<PurgadorIA>(FindObjectsSortMode.None))
        {
            if (Vector2.Distance(purgador.transform.position, punto) <= radioAtraccion)
                purgador.Investigar(punto, segundosInvestigar);
        }

        Destroy(gameObject, 0.5f);
    }
}
