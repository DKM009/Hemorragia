using UnityEngine;

/// <summary>
/// Mata al jugador instantaneamente al caer en zona peligrosa (escoria, magma, vacio).
/// Coloca en un BoxCollider2D isTrigger con tag "ZonaDeathFall".
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class ZonaDeathFall : MonoBehaviour
{
    [Tooltip("Perdida de sangre instantanea al caer (0 = muerte inmediata).")]
    public float danioInstantaneo = 100f;

    void Awake()
    {
        GetComponent<Collider2D>().isTrigger = true;
    }

    void OnTriggerEnter2D(Collider2D otro)
    {
        if (!otro.CompareTag("Player")) return;

        var sangre = otro.GetComponent<SistemaSangre>();
        if (sangre != null)
        {
            sangre.RecibirDano(danioInstantaneo);
        }
        else
        {
            // Sin SistemaSangre: ir directo al GameOver
            GestorJuego.Instancia?.GameOver();
        }
    }
}
