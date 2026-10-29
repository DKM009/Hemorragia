using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Sistema de sangre central de Hemorragia.
/// - Jeremy sangra pasivamente; correr acelera la perdida.
/// - Cauterizar (estar sobre ZonaCauterizacion) frena y sana.
/// - La sangre deja marcas en el suelo que los Purgadores siguen.
/// - La camara se sacude mas a medida que la sangre baja.
/// Agrega este script al Player.
/// </summary>
public class SistemaSangre : MonoBehaviour
{
    // ── Constantes de estado ────────────────────────────────────────────────
    public enum EstadoSangre { Sano, Sangrando, Cauterizando, Critico }

    // ── Parametros de diseno ─────────────────────────────────────────────────
    [Header("Barra de sangre")]
    [Range(0f, 100f)] public float sangre = 100f;
    public float sangreMaxima = 100f;

    [Header("Perdida de sangre (por segundo)")]
    [Tooltip("Sangrado pasivo mientras camina.")]
    public float sangradoCaminando = 0.8f;
    [Tooltip("Sangrado adicional al correr (se suma al pasivo).")]
    public float sangradoCorriendo = 2.5f;
    [Tooltip("Por debajo de este valor el estado se vuelve Critico.")]
    public float umbralCritico = 20f;

    [Header("Cauterizacion")]
    [Tooltip("Sangre recuperada por segundo sobre la tuberia caliente.")]
    public float tasaCauterizacion = 12f;
    [Tooltip("Segundos que debe mantenerse sobre la tuberia para que surta efecto.")]
    public float tiempoActivacion  = 0.8f;
    [Tooltip("Color de la luz durante cauterizacion.")]
    public Color colorCauterizacion = new Color(1f, 0.4f, 0.1f);

    [Header("Charcos de sangre")]
    [Tooltip("Prefab de charco (SpriteRenderer simple, destruye en X seg).")]
    public GameObject prefabCharco;
    [Tooltip("Cada cuantos metros de recorrido se deja un charco.")]
    public float distanciaEntreCharcos = 1.5f;
    [Tooltip("Radio en unidades que detectan los Purgadores.")]
    public float radioDeteccionCharco = 3f;

    [Header("Muerte por desangrado")]
    public float sangradoMuerte = 0f; // si llega a 0 -> game over

    // ── Referencias ──────────────────────────────────────────────────────────
    [Header("Referencias")]
    public CamaraSeguimiento camara;
    public HUDSangre         hud;  // puede quedar null si aun no existe

    // ── Estado publico ───────────────────────────────────────────────────────
    public EstadoSangre Estado      { get; private set; } = EstadoSangre.Sano;
    public float         PctSangre  => sangre / sangreMaxima;
    public bool          EstaEnZonaCauterizacion { get; private set; }

    // ── Estado interno ───────────────────────────────────────────────────────
    MovimientoJugador movimiento;
    List<Vector2>     rastroCharcos = new List<Vector2>();
    List<float>       tiemposCharcos = new List<float>();
    Vector2           ultimaPos;
    float             distAcumulada;
    float             tActivacionCauterizacion;
    bool              cauterizandoActivo;
    Coroutine         rutinaEfectoCauterizacion;

    // ── Luz de cauterizacion ─────────────────────────────────────────────────
    Light2D luzCauterizacion;
    bool _reiniciando;

    // ─────────────────────────────────────────────────────────────────────────

    void Awake()
    {
        movimiento = GetComponent<MovimientoJugador>();
        if (movimiento == null)
            Debug.LogWarning("[SistemaSangre] No se encontro MovimientoJugador en el mismo GameObject.");

        if (camara == null)
            camara = Camera.main?.GetComponent<CamaraSeguimiento>();

        // Crear una luz tenue de cauterizacion dinamica
        var goLuz = new GameObject("Luz_Cauterizacion_Jugador");
        goLuz.transform.SetParent(transform, false);
        luzCauterizacion = goLuz.AddComponent<Light2D>();
        luzCauterizacion.lightType            = Light2D.LightType.Point;
        luzCauterizacion.color                = colorCauterizacion;
        luzCauterizacion.intensity            = 0f;
        luzCauterizacion.pointLightOuterRadius = 3f;
        luzCauterizacion.pointLightInnerRadius = 0f;
    }

    void Start()
    {
        ultimaPos = transform.position;
    }

    void Update()
    {
        ActualizarSangrado();
        ActualizarCauterizacion();
        PurgarRastroViejo();
        ActualizarRastro();
        ActualizarEstado();
        ActualizarCamara();
        ActualizarHUD();
    }

    // ── Sangrado pasivo ──────────────────────────────────────────────────────
    void ActualizarSangrado()
    {
        if (EstaEnZonaCauterizacion && cauterizandoActivo) return; // cauterizar detiene el sangrado

        float perdida = sangradoCaminando;
        if (movimiento != null && movimiento.EstaCorriendo)
            perdida += sangradoCorriendo;

        sangre = Mathf.Max(sangradoMuerte, sangre - perdida * Time.deltaTime);

        if (sangre <= sangradoMuerte)
            ActivarMuerte();
    }

    // ── Cauterizacion ────────────────────────────────────────────────────────
    void ActualizarCauterizacion()
    {
        if (!EstaEnZonaCauterizacion)
        {
            tActivacionCauterizacion = 0f;
            cauterizandoActivo       = false;
            luzCauterizacion.intensity = Mathf.Lerp(luzCauterizacion.intensity, 0f, Time.deltaTime * 4f);
            return;
        }

        tActivacionCauterizacion += Time.deltaTime;

        // Fase de activacion (0 -> tiempoActivacion): luz crece, aun no sana
        float pct = Mathf.Clamp01(tActivacionCauterizacion / tiempoActivacion);
        luzCauterizacion.intensity = Mathf.Lerp(0f, 1.5f, pct);

        if (tActivacionCauterizacion >= tiempoActivacion)
        {
            cauterizandoActivo = true;
            sangre = Mathf.Min(sangreMaxima, sangre + tasaCauterizacion * Time.deltaTime);
        }
    }

    // ── Rastro de charcos ────────────────────────────────────────────────────
    void ActualizarRastro()
    {
        Vector2 pos = transform.position;
        distAcumulada += Vector2.Distance(pos, ultimaPos);
        ultimaPos = pos;

        // Solo dejar charcos si hay sangrado real (no en cauterizacion)
        float threshold = distanciaEntreCharcos;
        // Correr deja charcos mas seguido
        if (movimiento != null && movimiento.EstaCorriendo) threshold *= 0.5f;

        if (distAcumulada >= threshold && PctSangre < 0.85f)
        {
            DejarCharco(pos);
            distAcumulada = 0f;
        }
    }

    void DejarCharco(Vector2 pos)
    {
        GameObject charco;
        float tamano = Mathf.Lerp(0.2f, 0.8f, 1f - PctSangre);

        if (prefabCharco != null)
        {
            charco = Instantiate(prefabCharco, pos + Vector2.down * 0.5f, Quaternion.identity);
            charco.transform.localScale = Vector3.one * tamano;
        }
        else
        {
            // Sin prefab asignado: se genera una mancha oscura plana sobre el suelo
            charco = new GameObject("Charco");
            float y = pos.y - 0.3f;
            foreach (var h in Physics2D.RaycastAll(pos, Vector2.down, 3f))
            {
                if (h.collider == null || h.collider.isTrigger) continue;
                if (h.collider.transform.IsChildOf(transform)) continue;
                y = h.point.y + 0.03f;
                break;
            }
            charco.transform.position   = new Vector3(pos.x, y, 0f);
            charco.transform.localScale = new Vector3(tamano * 1.6f, 0.12f, 1f);
            var sr = charco.AddComponent<SpriteRenderer>();
            sr.sprite       = JugadorRef.SpriteBlanco();
            sr.color        = new Color(0.45f, 0.02f, 0.03f, 0.9f);
            sr.sortingOrder = 2;
        }

        // Registrar posicion para los Purgadores (max 30 charcos)
        rastroCharcos.Add(pos);
        tiemposCharcos.Add(Time.time);
        if (rastroCharcos.Count > 30)
        {
            rastroCharcos.RemoveAt(0);
            tiemposCharcos.RemoveAt(0);
        }

        // El charco visual desaparece a los 60 s (y su rastro tambien, ver PurgarRastroViejo)
        Destroy(charco, 60f);
    }

    /// <summary>Quita del rastro los charcos que ya desaparecieron.</summary>
    void PurgarRastroViejo()
    {
        while (tiemposCharcos.Count > 0 && Time.time - tiemposCharcos[0] > 60f)
        {
            tiemposCharcos.RemoveAt(0);
            rastroCharcos.RemoveAt(0);
        }
    }

    // ── Estado ───────────────────────────────────────────────────────────────
    void ActualizarEstado()
    {
        if (EstaEnZonaCauterizacion && cauterizandoActivo)
        {
            Estado = EstadoSangre.Cauterizando;
        }
        else if (sangre <= umbralCritico)
        {
            Estado = EstadoSangre.Critico;
        }
        else if (PctSangre < 1f)
        {
            Estado = EstadoSangre.Sangrando;
        }
        else
        {
            Estado = EstadoSangre.Sano;
        }
    }

    // ── Camara ───────────────────────────────────────────────────────────────
    void ActualizarCamara()
    {
        if (camara == null) return;

        float intensidad;
        switch (Estado)
        {
            case EstadoSangre.Critico:      intensidad = 3.0f; break;
            case EstadoSangre.Sangrando:    intensidad = 1f + (1f - PctSangre) * 1.5f; break;
            case EstadoSangre.Cauterizando: intensidad = 0.5f; break;
            default:                        intensidad = 1.0f; break;
        }
        camara.AjustarIntensidad(intensidad);
    }

    // ── HUD ──────────────────────────────────────────────────────────────────
    void ActualizarHUD()
    {
        if (hud == null) return;
        hud.ActualizarBarra(PctSangre, Estado);
    }

    // ── Trigger de zona cauterizacion ────────────────────────────────────────
    void OnTriggerEnter2D(Collider2D otro)
    {
        if (otro.CompareTag("ZonaCauterizacion"))
            EstaEnZonaCauterizacion = true;
    }

    void OnTriggerExit2D(Collider2D otro)
    {
        if (otro.CompareTag("ZonaCauterizacion"))
        {
            EstaEnZonaCauterizacion      = false;
            cauterizandoActivo           = false;
            tActivacionCauterizacion     = 0f;
        }
    }

    // ── Game Over ────────────────────────────────────────────────────────────
    void ActivarMuerte()
    {
        Debug.Log("[SistemaSangre] Jeremy se desangro. Game Over.");
        if (GestorJuego.Instancia != null)
            GestorJuego.Instancia.GameOver();
        else if (!_reiniciando)
        {
            // Sin GestorJuego: reinicia la escena actual para no quedar atorado
            _reiniciando = true;
            Debug.LogWarning("[SistemaSangre] GestorJuego no encontrado. Reiniciando escena.");
            UnityEngine.SceneManagement.SceneManager.LoadScene(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
        }
    }

    // ── API publica ──────────────────────────────────────────────────────────

    /// <summary>Devuelve la lista de posiciones de charcos activos (para Purgadores).</summary>
    public List<Vector2> ObtenerRastro() => rastroCharcos;

    /// <summary>Fuerza una perdida instantanea de sangre (caida, golpe de prop).</summary>
    public void RecibirDano(float cantidad)
    {
        sangre = Mathf.Max(0f, sangre - cantidad);
    }

    /// <summary>Recupera sangre (vendas).</summary>
    public void Curar(float cantidad)
    {
        sangre = Mathf.Min(sangreMaxima, sangre + cantidad);
    }

    /// <summary>Resetea la sangre al maximo (uso en checkpoints / depuracion).</summary>
    public void ResetearSangre()
    {
        sangre = sangreMaxima;
        rastroCharcos.Clear();
        tiemposCharcos.Clear();
    }
}
