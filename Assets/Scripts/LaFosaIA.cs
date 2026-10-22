using UnityEngine;

/// <summary>
/// IA de La Fosa — enemigo grande e imparable del Nivel 1.
/// La Fosa NO sigue rastros de sangre: responde al RUIDO.
/// - Si el ruido acumulado supera el umbral, La Fosa se activa y persigue al jugador.
/// - Mientras esta activa es imparable: no se puede esquivar, solo huir.
/// - Si el jugador se esconde (tag Escondite + trigger) y hace silencio, La Fosa se retira.
/// - Golpear una pared/puerta genera ruido. Correr genera ruido continuo.
/// El GestorRuido.cs registra eventos de ruido y los notifica a esta IA.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class LaFosaIA : MonoBehaviour
{
    public enum Estado { Dormida, Alertada, Persiguiendo, Retirandose }

    // ── Movimiento ────────────────────────────────────────────────────────────
    [Header("Movimiento")]
    public float velocidadAlerta     = 1.8f;
    [Tooltip("Velocidad cuando persigue activamente. El jugador corriendo va a 6.5, asi que siempre alcanza si no hay escondite.")]
    public float velocidadPersecucion = 5.8f;
    public float velocidadRetiro      = 1.0f;

    // ── Ruido ─────────────────────────────────────────────────────────────────
    [Header("Ruido")]
    [Tooltip("Umbral de ruido (0-100) para que La Fosa se despierte.")]
    public float umbralActivacion     = 40f;
    [Tooltip("Umbral por debajo del cual se retira (tiene histeresis para no oscilar).")]
    public float umbralRetiro         = 10f;
    [Tooltip("Decaimiento de ruido por segundo cuando el jugador esta quieto/escondido.")]
    public float decaimientoRuido     = 8f;

    // ── Deteccion de escondite ────────────────────────────────────────────────
    [Header("Escondite")]
    [Tooltip("Si el jugador entra en un trigger con tag Escondite, La Fosa lo pierde.")]
    public float tiempoParaPerder     = 4f;

    // ── Contacto ──────────────────────────────────────────────────────────────
    [Header("Contacto")]
    public float danoPorContacto      = 30f;
    public float cooldownContacto     = 2f;

    // ── Feedback ──────────────────────────────────────────────────────────────
    [Header("Feedback")]
    public AudioClip sonidoDespertar;
    public AudioClip sonidoPasos;
    [Range(0f, 1f)] public float volumen = 0.8f;
    [Tooltip("El suelo vibra cuando La Fosa persigue (sacudir camara).")]
    public float intensidadVibracion  = 3.5f;

    // ── Estado publico ────────────────────────────────────────────────────────
    public Estado    EstadoActual { get; private set; } = Estado.Dormida;
    public float     Ruido        { get; private set; } = 0f;
    public bool      EstaActiva   => EstadoActual == Estado.Persiguiendo || EstadoActual == Estado.Alertada;

    // ── Privado ───────────────────────────────────────────────────────────────
    Rigidbody2D   rb;
    SpriteRenderer sprite;
    SistemaSangre sistemaSangre;
    CamaraSeguimiento camara;
    Transform     jugador;

    float tContacto;
    float tJugadorEscondido;
    bool  jugadorEscondido;

    AudioSource audioSource;

    // ─────────────────────────────────────────────────────────────────────────

    void Awake()
    {
        rb     = GetComponent<Rigidbody2D>();
        sprite = GetComponentInChildren<SpriteRenderer>();
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        rb.gravityScale = 3f;

        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.spatialBlend = 0f;
        audioSource.volume       = volumen;
    }

    void Start()
    {
        var jugadorGO = GameObject.FindGameObjectWithTag("Player");
        if (jugadorGO != null)
        {
            jugador       = jugadorGO.transform;
            sistemaSangre = jugadorGO.GetComponent<SistemaSangre>();
        }
        camara = Camera.main?.GetComponent<CamaraSeguimiento>();
    }

    void Update()
    {
        tContacto -= Time.deltaTime;
        ActualizarRuido();
        ActualizarMaquinaEstados();
        ActualizarCamara();
    }

    void FixedUpdate()
    {
        ActualizarMovimiento();
    }

    // ── Ruido ─────────────────────────────────────────────────────────────────
    void ActualizarRuido()
    {
        // Decaimiento natural
        if (jugadorEscondido || (EstadoActual == Estado.Dormida))
            Ruido = Mathf.Max(0f, Ruido - decaimientoRuido * Time.deltaTime * 2f);
        else
            Ruido = Mathf.Max(0f, Ruido - decaimientoRuido * Time.deltaTime);
    }

    /// <summary>Llamado por GestorRuido cuando algo genera ruido en la escena.</summary>
    public void RecibirRuido(float cantidad, Vector3 origen)
    {
        // El ruido se atenua con la distancia
        float dist      = Vector3.Distance(transform.position, origen);
        float atenuacion = Mathf.Clamp01(1f - dist / 20f);
        Ruido = Mathf.Min(100f, Ruido + cantidad * atenuacion);
    }

    // ── Maquina de estados ────────────────────────────────────────────────────
    void ActualizarMaquinaEstados()
    {
        // Jugador escondido
        if (jugadorEscondido)
        {
            tJugadorEscondido += Time.deltaTime;
            if (tJugadorEscondido >= tiempoParaPerder && EstadoActual != Estado.Retirandose)
                CambiarEstado(Estado.Retirandose);
        }
        else
        {
            tJugadorEscondido = 0f;
        }

        switch (EstadoActual)
        {
            case Estado.Dormida:
                if (Ruido >= umbralActivacion)
                    CambiarEstado(Estado.Alertada);
                break;

            case Estado.Alertada:
                if (jugador != null && Vector2.Distance(transform.position, jugador.position) < 8f)
                    CambiarEstado(Estado.Persiguiendo);
                if (Ruido < umbralRetiro)
                    CambiarEstado(Estado.Dormida);
                break;

            case Estado.Persiguiendo:
                if (Ruido < umbralRetiro && jugadorEscondido)
                    CambiarEstado(Estado.Retirandose);
                break;

            case Estado.Retirandose:
                if (Ruido >= umbralActivacion)
                    CambiarEstado(Estado.Persiguiendo);
                break;
        }
    }

    void CambiarEstado(Estado nuevo)
    {
        if (EstadoActual == nuevo) return;
        EstadoActual = nuevo;

        if (nuevo == Estado.Alertada || nuevo == Estado.Persiguiendo)
            if (sonidoDespertar != null)
                audioSource.PlayOneShot(sonidoDespertar, volumen);
    }

    // ── Movimiento ────────────────────────────────────────────────────────────
    void ActualizarMovimiento()
    {
        if (jugador == null) return;

        float vx = 0f;

        switch (EstadoActual)
        {
            case Estado.Persiguiendo:
                vx = Mathf.Sign(jugador.position.x - transform.position.x) * velocidadPersecucion;
                break;
            case Estado.Alertada:
                vx = Mathf.Sign(jugador.position.x - transform.position.x) * velocidadAlerta;
                break;
            case Estado.Retirandose:
                // Se aleja del jugador
                vx = Mathf.Sign(transform.position.x - jugador.position.x) * velocidadRetiro;
                break;
        }

        rb.linearVelocity = new Vector2(vx, rb.linearVelocity.y);

        if (sprite != null && Mathf.Abs(vx) > 0.05f)
            sprite.flipX = vx < 0f;
    }

    // ── Camara ────────────────────────────────────────────────────────────────
    void ActualizarCamara()
    {
        if (camara == null) return;
        if (EstadoActual == Estado.Persiguiendo)
        {
            float dist = jugador != null ? Vector2.Distance(transform.position, jugador.position) : 20f;
            float fac  = Mathf.Clamp01(1f - dist / 15f);
            camara.AjustarIntensidad(1f + fac * intensidadVibracion);
        }
    }

    // ── Contacto ──────────────────────────────────────────────────────────────
    void OnCollisionStay2D(Collision2D col)
    {
        if (!col.collider.CompareTag("Player")) return;
        if (tContacto > 0f) return;
        tContacto = cooldownContacto;
        sistemaSangre?.RecibirDano(danoPorContacto);
    }

    // ── Escondite ─────────────────────────────────────────────────────────────
    // El GestorEscondite.cs (o el trigger del escondite) llama a estos metodos:
    public void JugadorSeEscondio()  { jugadorEscondido = true;  tJugadorEscondido = 0f; }
    public void JugadorSalioEscondite() { jugadorEscondido = false; }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.4f);
        Gizmos.DrawWireSphere(transform.position, 20f); // radio de escucha maximo
    }
}
