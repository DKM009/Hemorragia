using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Checkpoint de Hemorragia.
/// Coloca un BoxCollider2D isTrigger en este GameObject.
/// Al entrar Jeremy lo activa: guarda posicion y restaura un poco de sangre.
/// Visual: la luz del checkpoint cambia de gris a verde al activarse.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class Checkpoint : MonoBehaviour
{
    [Header("Configuracion")]
    [Tooltip("Cuanta sangre recupera al activar el checkpoint.")]
    public float restaurarSangre  = 20f;
    [Tooltip("Offset vertical para el spawn (evitar enterrarse en el suelo).")]
    public float offsetSpawn      = 0.1f;

    [Header("Luz URP 2D (opcional)")]
    public Light2D luzCheckpoint;
    public Color   colorInactivo = new Color(0.3f, 0.3f, 0.3f);
    public Color   colorActivo   = new Color(0.15f, 0.8f, 0.25f);

    bool activado;

    void Awake()
    {
        // Asegurar que el collider sea trigger
        var col = GetComponent<Collider2D>();
        col.isTrigger = true;

        if (luzCheckpoint != null)
            luzCheckpoint.color = colorInactivo;
    }

    void OnTriggerEnter2D(Collider2D otro)
    {
        if (activado) return;
        if (!otro.CompareTag("Player")) return;

        activado = true;

        // Guardar posicion de spawn
        Vector3 posSpawn = transform.position + Vector3.up * offsetSpawn;
        if (GestorJuego.Instancia != null)
            GestorJuego.Instancia.GuardarCheckpoint(posSpawn);

        // Restaurar sangre
        var sangre = otro.GetComponent<SistemaSangre>();
        if (sangre != null)
            sangre.sangre = Mathf.Min(sangre.sangreMaxima, sangre.sangre + restaurarSangre);

        // Cambiar luz
        if (luzCheckpoint != null)
            luzCheckpoint.color = colorActivo;

        Debug.Log($"[Checkpoint] Activado en {posSpawn}. Sangre restaurada: +{restaurarSangre}");
    }

    void OnDrawGizmos()
    {
        Gizmos.color = activado ? Color.green : Color.grey;
        Gizmos.DrawWireCube(transform.position, Vector3.one * 0.8f);
    }
}
