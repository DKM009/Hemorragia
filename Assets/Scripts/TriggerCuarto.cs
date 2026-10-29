using UnityEngine;

/// <summary>
/// Trigger generico de Hemorragia para activar eventos del nivel.
/// Coloca un BoxCollider2D isTrigger en este GameObject.
///
/// Modos:
///   TransicionCuarto    → llama GestorNivel1.IrAlCuarto(indiceCuartoDestino)
///   ActivarEmboscada    → activa los Purgadores de la Bodega
///   ActivarClimax       → activa La Fosa en el Cuarto 4
///   CambiarEscena       → carga una nueva escena (entre niveles)
///   ActivarCheckpoint   → guarda checkpoint + muestra luz
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class TriggerCuarto : MonoBehaviour
{
    public enum TipoTrigger
    {
        TransicionCuarto,
        ActivarEmboscada,
        ActivarClimax,
        CambiarEscena,
        ActivarCheckpoint
    }

    [Header("Tipo")]
    public TipoTrigger tipo = TipoTrigger.TransicionCuarto;

    [Header("Transicion de cuarto")]
    [Tooltip("Indice del cuarto destino (0=Cuarto1, 1=Cuarto2, 2=Cuarto3, 3=Cuarto4).")]
    public int indiceCuartoDestino = 1;

    [Header("Cambio de escena")]
    public string nombreEscena = "";

    [Header("Solo una vez")]
    public bool soloUnaVez = true;

    GestorNivel1 gestorNivel1;
    bool         disparado;

    void Awake()
    {
        var col = GetComponent<Collider2D>();
        col.isTrigger = true;
    }

    void Start()
    {
        gestorNivel1 = FindFirstObjectByType<GestorNivel1>();
    }

    void OnTriggerEnter2D(Collider2D otro)
    {
        if (!otro.CompareTag("Player")) return;
        if (soloUnaVez && disparado) return;
        disparado = true;

        switch (tipo)
        {
            case TipoTrigger.TransicionCuarto:
                gestorNivel1?.IrAlCuarto(indiceCuartoDestino);
                break;

            case TipoTrigger.ActivarEmboscada:
                gestorNivel1?.ActivarEmboscadaBodega();
                break;

            case TipoTrigger.ActivarClimax:
                gestorNivel1?.ActivarClimax();
                break;

            case TipoTrigger.CambiarEscena:
                if (!string.IsNullOrEmpty(nombreEscena))
                {
                    if (GestorJuego.Instancia != null)
                        GestorJuego.Instancia.CargarEscena(nombreEscena);
                    else
                        UnityEngine.SceneManagement.SceneManager.LoadScene(nombreEscena);
                }
                break;

            case TipoTrigger.ActivarCheckpoint:
                if (GestorJuego.Instancia != null)
                    GestorJuego.Instancia.GuardarCheckpoint(transform.position);
                break;
        }
    }

    /// <summary>Permite que el trigger vuelva a dispararse (tras morir el jugador).</summary>
    public void Reiniciar()
    {
        disparado = false;
    }

    void OnDrawGizmos()
    {
        Gizmos.color = tipo switch
        {
            TipoTrigger.TransicionCuarto => Color.cyan,
            TipoTrigger.ActivarEmboscada => Color.yellow,
            TipoTrigger.ActivarClimax    => Color.red,
            TipoTrigger.CambiarEscena    => Color.green,
            _                            => Color.white
        };
        var col = GetComponent<Collider2D>();
        if (col != null) Gizmos.DrawWireCube(transform.position, col.bounds.size);
    }
}
