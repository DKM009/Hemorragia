using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Trigger de cambio de nivel. Usa tag Player, no nombre.
/// Soporta ultimo nivel -> escena final sin crashear.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class CambiarNivel : MonoBehaviour
{
    [Header("Destino")]
    public string nombreEscena = "";
    public string escenaFinal  = "EscenaFinal";
    public float  duracionFade = 0.5f;

    bool disparado;

    void Awake()
    {
        GetComponent<Collider2D>().isTrigger = true;
    }

    void OnTriggerEnter2D(Collider2D otro)
    {
        if (disparado) return;
        if (!otro.CompareTag("Player")) return;
        disparado = true;

        string destino = ResolverDestino();

        if (GestorJuego.Instancia != null)
            GestorJuego.Instancia.CargarEscena(destino);
        else
            SceneManager.LoadScene(destino);
    }

    string ResolverDestino()
    {
        if (!string.IsNullOrEmpty(nombreEscena))
            return nombreEscena;

        int actual    = SceneManager.GetActiveScene().buildIndex;
        int siguiente = actual + 1;

        if (siguiente >= SceneManager.sceneCountInBuildSettings)
            return escenaFinal;

        return SceneManager.GetSceneByBuildIndex(siguiente).name;
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        var col = GetComponent<Collider2D>();
        if (col != null) Gizmos.DrawWireCube(transform.position, col.bounds.size * 1.05f);
    }
}
