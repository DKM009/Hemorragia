using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Singleton persistente entre escenas para Hemorragia.
/// Maneja:
///   - Checkpoints y respawn con fade a negro
///   - Game Over (pantalla simple, reinicio)
///   - Referencia global a SistemaSangre del jugador
///   - Transicion de escenas con fade
/// Uso: coloca un GameObject "GestorJuego" en la primera escena con este script.
/// DontDestroyOnLoad lo mantiene vivo en todas las escenas.
/// </summary>
public class GestorJuego : MonoBehaviour
{
    // ── Singleton ─────────────────────────────────────────────────────────────
    public static GestorJuego Instancia { get; private set; }

    // ── Checkpoints ───────────────────────────────────────────────────────────
    [Header("Checkpoints")]
    [Tooltip("Posicion del ultimo checkpoint activado (se guarda al tocar).")]
    public Vector3 posicionCheckpoint;
    [Tooltip("Nombre de la escena del ultimo checkpoint.")]
    public string escenaCheckpoint = "Nivel1";

    // ── Game Over ─────────────────────────────────────────────────────────────
    [Header("Game Over")]
    [Tooltip("Segundos de pantalla negra antes de respawnear.")]
    public float tiempoGameOver = 2.5f;
    [Tooltip("Velocidad del fade de pantalla.")]
    public float velocidadFade  = 1.5f;

    // ── Estado ────────────────────────────────────────────────────────────────
    public bool EstaEnGameOver { get; private set; }
    public bool EstaEnFade     { get; private set; }

    // ── Fade ──────────────────────────────────────────────────────────────────
    // La pantalla negra se hace controlando la luz global URP de la escena activa.
    // Si en el futuro quieres un CanvasGroup, cambia solo FadeNegro().

    // ─────────────────────────────────────────────────────────────────────────

    void Awake()
    {
        if (Instancia != null && Instancia != this)
        {
            Destroy(gameObject);
            return;
        }
        Instancia = this;
        DontDestroyOnLoad(gameObject);

        // Checkpoint inicial: la primera escena del juego, posicion de spawn por defecto
        if (string.IsNullOrEmpty(escenaCheckpoint))
            escenaCheckpoint = SceneManager.GetActiveScene().name;
    }

    // ── API publica ───────────────────────────────────────────────────────────

    /// <summary>Guarda la posicion actual como checkpoint.</summary>
    public void GuardarCheckpoint(Vector3 posicion)
    {
        posicionCheckpoint = posicion;
        escenaCheckpoint   = SceneManager.GetActiveScene().name;
        Debug.Log($"[GestorJuego] Checkpoint guardado en {posicion} (escena: {escenaCheckpoint}).");
    }

    /// <summary>
    /// Activa el game over: fade a negro, espera y hace respawn.
    /// Llamado desde SistemaSangre cuando sangre <= 0.
    /// </summary>
    public void GameOver()
    {
        if (EstaEnGameOver) return;
        EstaEnGameOver = true;
        StartCoroutine(RutinaGameOver());
    }

    /// <summary>Transicion con fade a una escena por nombre.</summary>
    public void CargarEscena(string nombreEscena)
    {
        StartCoroutine(RutinaCargarEscena(nombreEscena));
    }

    // ── Coroutines ────────────────────────────────────────────────────────────

    IEnumerator RutinaGameOver()
    {
        // Congelar al jugador durante la muerte
        var mov = FindFirstObjectByType<MovimientoJugador>();
        if (mov != null) mov.enabled = false;

        yield return StartCoroutine(FadeNegro(apagar: true));
        yield return new WaitForSeconds(tiempoGameOver);

        // Restaurar sangre en el sistema
        var sangre = FindFirstObjectByType<SistemaSangre>();
        if (sangre != null) sangre.ResetearSangre();

        // Cargar escena del checkpoint
        if (SceneManager.GetActiveScene().name != escenaCheckpoint)
        {
            SceneManager.LoadScene(escenaCheckpoint);
            // Esperar a que la escena cargue para posicionar al jugador
            yield return null;
            yield return null;
        }

        // Mover jugador al checkpoint
        var jugador = GameObject.FindGameObjectWithTag("Player");
        if (jugador != null && posicionCheckpoint != Vector3.zero)
            jugador.transform.position = posicionCheckpoint;

        if (mov != null) mov.enabled = true;

        // Enemigos de vuelta a su sitio
        var nivel1 = FindFirstObjectByType<GestorNivel1>();
        if (nivel1 != null) nivel1.ReiniciarTrasMuerte();
        var nivel2 = FindFirstObjectByType<GestorNivel2>();
        if (nivel2 != null) nivel2.ReiniciarTrasMuerte();

        EstaEnGameOver = false;

        yield return StartCoroutine(FadeNegro(apagar: false));
    }

    IEnumerator RutinaCargarEscena(string nombreEscena)
    {
        yield return StartCoroutine(FadeNegro(apagar: true));
        SceneManager.LoadScene(nombreEscena);
    }

    // Intensidad original de la luz guardada antes de cada fade a negro
    float _intensidadLuzOriginal = -1f;

    IEnumerator FadeNegro(bool apagar)
    {
        EstaEnFade = true;

        Light2D luzGlobal = null;
        foreach (var l in FindObjectsByType<Light2D>(FindObjectsSortMode.None))
            if (l.lightType == Light2D.LightType.Global) { luzGlobal = l; break; }

        float inicio, fin;
        if (luzGlobal != null)
        {
            if (apagar)
            {
                // Guardar intensidad real para poder recuperarla
                _intensidadLuzOriginal = luzGlobal.intensity > 0.001f
                    ? luzGlobal.intensity
                    : (_intensidadLuzOriginal > 0f ? _intensidadLuzOriginal : 1f);
                inicio = _intensidadLuzOriginal;
                fin    = 0f;
            }
            else
            {
                inicio = 0f;
                fin    = _intensidadLuzOriginal > 0f ? _intensidadLuzOriginal : 1f;
            }
        }
        else
        {
            inicio = fin = 0f;
        }

        float t = 0f;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime * velocidadFade;
            if (luzGlobal != null)
                luzGlobal.intensity = Mathf.Lerp(inicio, fin, Mathf.Clamp01(t));
            yield return null;
        }
        if (luzGlobal != null) luzGlobal.intensity = fin;

        EstaEnFade = false;
    }
}
