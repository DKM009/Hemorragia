using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Componente de interaccion para PUERTAS y TELETRANSPORTES en Hemorragia.
///
/// Modos de uso:
///   1. PUERTA LOCAL — el GameObject tiene un BoxCollider2D solido (no trigger).
///      Al interactuar (E / F) alterna entre abierta y cerrada.
///      Si 'requiereItem' esta marcado, solo abre si el jugador lleva el item correcto.
///
///   2. TELETRANSPORTE DE CUARTO — cambia el "cuarto activo" dentro de la misma escena,
///      desplazando al jugador a un punto de spawn indicado.
///      Ideal para transicion entre Cuarto 1 -> Cuarto 2 -> Cuarto 3... sin cargar escena.
///
///   3. CAMBIO DE ESCENA — carga otra escena de Unity (SceneManager.LoadScene).
///      Usa esto para el salto entre Nivel 1 -> Nivel 2, etc.
///
/// Setup rapido:
///   • Agrega este script al GameObject de la puerta.
///   • Elige el modo en el Inspector.
///   • Asigna destino / escena segun el modo.
///   • El jugador debe tener tag "Player".
/// </summary>
public class Interactuable : MonoBehaviour
{
    public enum ModoInteraccion
    {
        PuertaLocal,
        TeletransporteCuarto,
        CambioEscena
    }

    [Header("Modo")]
    public ModoInteraccion modo = ModoInteraccion.PuertaLocal;

    // ── PUERTA LOCAL ──────────────────────────────────────────
    [Header("Puerta Local")]
    [Tooltip("Si esta activo, la puerta empieza cerrada.")]
    public bool empiezaCerrada = true;
    [Tooltip("Si esta marcado, solo se abre si el jugador lleva el item correcto.")]
    public bool requiereItem   = false;
    [Tooltip("Nombre del item requerido (debe coincidir con InventarioJugador).")]
    public string nombreItem   = "";
    [Tooltip("Cuanto se desplaza la puerta al abrirse (en unidades, eje Y).")]
    public float desplazamientoApertura = 2.5f;
    [Tooltip("Tiempo en segundos para abrir/cerrar.")]
    public float duracionApertura = 0.4f;

    // ── TELETRANSPORTE DE CUARTO ──────────────────────────────
    [Header("Teletransporte de Cuarto")]
    [Tooltip("Punto de spawn del cuarto destino (Transform vacio en la escena).")]
    public Transform puntoDestino;
    [Tooltip("Limites de camara del cuarto destino (para CamaraSeguimiento.limites).")]
    public Collider2D limitesCuartoDestino;
    [Tooltip("Fade a negro antes de teletransportar (recomendado).")]
    public bool usarFade = true;
    [Tooltip("Duracion del fade en segundos.")]
    public float duracionFade = 0.3f;

    // ── CAMBIO DE ESCENA ──────────────────────────────────────
    [Header("Cambio de Escena")]
    [Tooltip("Nombre exacto de la escena a cargar (debe estar en Build Settings).")]
    public string nombreEscena = "";

    // ── INDICADOR DE INTERACCION ──────────────────────────────
    [Header("Indicador visual (opcional)")]
    [Tooltip("GameObject con el icono/texto '[ E ]'. Se activa al acercarse.")]
    public GameObject indicadorUI;

    // ─────────────────────────────────────────────────────────
    //  Estado interno
    // ─────────────────────────────────────────────────────────
    bool estaAbierta;
    bool jugadorCerca;
    bool enTransicion;
    Vector3 posicionCerrada;
    Vector3 posicionAbierta;
    Collider2D colisionadorPuerta;

    void Awake()
    {
        posicionCerrada     = transform.position;
        posicionAbierta     = posicionCerrada + Vector3.up * desplazamientoApertura;
        colisionadorPuerta  = GetComponent<Collider2D>();
        estaAbierta         = !empiezaCerrada;

        if (indicadorUI != null)
            indicadorUI.SetActive(false);

        if (estaAbierta) AplicarEstadoAbierta(inmediato: true);
    }

    void Update()
    {
        if (!jugadorCerca || enTransicion) return;
        if (PulsarInteraccion()) Interactuar();
    }

    // Jugador entra al radio de interaccion
    void OnTriggerEnter2D(Collider2D otro)
    {
        if (!otro.CompareTag("Player")) return;
        jugadorCerca = true;
        if (indicadorUI != null) indicadorUI.SetActive(true);
    }

    void OnTriggerExit2D(Collider2D otro)
    {
        if (!otro.CompareTag("Player")) return;
        jugadorCerca = false;
        if (indicadorUI != null) indicadorUI.SetActive(false);
    }

    // ─────────────────────────────────────────────────────────
    //  Logica de interaccion
    // ─────────────────────────────────────────────────────────
    void Interactuar()
    {
        switch (modo)
        {
            case ModoInteraccion.PuertaLocal:
                if (requiereItem && !JugadorTieneItem())
                {
                    Debug.Log($"[Interactuable] Se requiere el item '{nombreItem}' para abrir esta puerta.");
                    // Aqui podrias mostrar un mensaje de UI o reproducir sonido de "bloqueado"
                    return;
                }
                StartCoroutine(AnimarPuerta());
                break;

            case ModoInteraccion.TeletransporteCuarto:
                if (puntoDestino == null)
                {
                    Debug.LogError("[Interactuable] No se asigno 'puntoDestino'. Asignalo en el Inspector.");
                    return;
                }
                StartCoroutine(TeletransportarConFade());
                break;

            case ModoInteraccion.CambioEscena:
                if (string.IsNullOrEmpty(nombreEscena))
                {
                    Debug.LogError("[Interactuable] No se asigno 'nombreEscena'. Asignala en el Inspector.");
                    return;
                }
                StartCoroutine(CambiarEscenaConFade());
                break;
        }
    }

    // ─────────────────────────────────────────────────────────
    //  Coroutines
    // ─────────────────────────────────────────────────────────

    IEnumerator AnimarPuerta()
    {
        enTransicion = true;

        Vector3 origen  = estaAbierta ? posicionAbierta  : posicionCerrada;
        Vector3 destino = estaAbierta ? posicionCerrada  : posicionAbierta;
        estaAbierta     = !estaAbierta;

        // Mientras la puerta se mueve, desactivar su collider para no bloquear al jugador
        if (colisionadorPuerta != null) colisionadorPuerta.enabled = false;

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / duracionApertura;
            transform.position = Vector3.Lerp(origen, destino, Mathf.SmoothStep(0f, 1f, t));
            yield return null;
        }
        transform.position = destino;

        // Reactivar collider solo si la puerta quedo cerrada
        if (colisionadorPuerta != null)
            colisionadorPuerta.enabled = !estaAbierta;

        enTransicion = false;
    }

    IEnumerator TeletransportarConFade()
    {
        enTransicion = true;
        jugadorCerca = false;
        if (indicadorUI != null) indicadorUI.SetActive(false);

        if (usarFade) yield return StartCoroutine(FadeNegro(entrando: true));

        // Mover jugador
        var jugador = GameObject.FindGameObjectWithTag("Player");
        if (jugador != null)
            jugador.transform.position = puntoDestino.position;

        // Actualizar limites de camara
        if (limitesCuartoDestino != null)
        {
            var camSeg = Camera.main?.GetComponent<CamaraSeguimiento>();
            if (camSeg != null) camSeg.limites = limitesCuartoDestino;
        }

        yield return null; // un frame para que la camara se actualice

        if (usarFade) yield return StartCoroutine(FadeNegro(entrando: false));
        enTransicion = false;
    }

    IEnumerator CambiarEscenaConFade()
    {
        enTransicion = true;
        if (usarFade) yield return StartCoroutine(FadeNegro(entrando: true));
        SceneManager.LoadScene(nombreEscena);
    }

    // Fade simple a negro (sin CanvasGroup: usa la luz global de URP)
    // Si tienes un CanvasGroup de fade en el HUD, reemplaza esto.
    IEnumerator FadeNegro(bool entrando)
    {
        // Busca la luz global de la escena para apagarla/encenderla
        var luces = FindObjectsByType<UnityEngine.Rendering.Universal.Light2D>(FindObjectsSortMode.None);
        UnityEngine.Rendering.Universal.Light2D luzGlobal = null;
        foreach (var l in luces)
            if (l.lightType == UnityEngine.Rendering.Universal.Light2D.LightType.Global)
            { luzGlobal = l; break; }

        float inicio  = entrando ? (luzGlobal != null ? luzGlobal.intensity : 1f) : 0f;
        float final_  = entrando ? 0f : (luzGlobal != null ? luzGlobal.intensity : 1f);
        float t = 0f;

        while (t < 1f)
        {
            t += Time.deltaTime / duracionFade;
            if (luzGlobal != null)
                luzGlobal.intensity = Mathf.Lerp(inicio, final_, t);
            yield return null;
        }
    }

    // ─────────────────────────────────────────────────────────
    //  Utilidades
    // ─────────────────────────────────────────────────────────

    void AplicarEstadoAbierta(bool inmediato)
    {
        if (inmediato)
        {
            transform.position = estaAbierta ? posicionAbierta : posicionCerrada;
            if (colisionadorPuerta != null)
                colisionadorPuerta.enabled = !estaAbierta;
        }
    }

    bool JugadorTieneItem()
    {
        // Stub para InventarioJugador (implementar en siguiente sprint)
        var inventario = GameObject.FindGameObjectWithTag("Player")?
                            .GetComponent<InventarioJugador>();
        if (inventario == null) return false;
        return inventario.TieneItem(nombreItem);
    }

    bool PulsarInteraccion()
    {
#if ENABLE_INPUT_SYSTEM
        var k = UnityEngine.InputSystem.Keyboard.current;
        return k != null && (k.eKey.wasPressedThisFrame || k.fKey.wasPressedThisFrame);
#else
        return Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.F);
#endif
    }
}
