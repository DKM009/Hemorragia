using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// IA de Los Purgadores — enemigos pequenos que siguen el rastro de sangre de Jeremy.
/// Comportamiento:
///   PATRULLAR   → caminar entre puntos de patrulla.
///   RASTREAR    → detectaron un charco; se dirigen a el y buscan al jugador.
///   PERSEGUIR   → tienen al jugador en linea de vista; lo persiguen directamente.
///   ESCONDIDO   → el jugador esta en un escondite; el Purgador espera y ronda.
///
/// El Purgador NO ataca: si toca al jugador, activa una penitencia
/// (perdida extra de sangre y empuje hacia atras).
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class PurgadorIA : MonoBehaviour
{
    public enum Estado { Patrullar, Rastrear, Perseguir, Escondido }

    // ── Movimiento ───────────────────────────────────────────────────────────
    [Header("Movimiento")]
    public float velocidadPatrulla  = 1.2f;
    public float velocidadRastreo   = 2.0f;
    public float velocidadPersecucion = 3.5f;
    [Tooltip("Distancia en unidades a la que el Purgador 'llega' a un punto.")]
    public float radioLlegada       = 0.4f;

    // ── Patrulla ─────────────────────────────────────────────────────────────
    [Header("Patrulla")]
    [Tooltip("Puntos de patrulla. Si esta vacio, ronda en el lugar.")]
    public Transform[] puntosPatrulla;
    public bool        patrullaLoop  = true;

    // ── Deteccion ────────────────────────────────────────────────────────────
    [Header("Deteccion")]
    [Tooltip("Radio de vision directa del jugador.")]
    public float radioVision        = 5f;
    [Tooltip("Radio en el que detecta charcos de sangre.")]
    public float radioOlfato        = 4f;
    [Tooltip("Angulo de vision frontal (grados totales).")]
    public float anguloVision       = 120f;
    [Tooltip("Capa de obstaculos para el raycast de vision.")]
    public LayerMask capaObstaculos;

    // ── Contacto ─────────────────────────────────────────────────────────────
    [Header("Contacto con jugador")]
    [Tooltip("Sangre extra que pierde Jeremy al ser tocado.")]
    public float danoPorContacto    = 15f;
    [Tooltip("Fuerza de empuje al tocar al jugador.")]
    public float fuerzaEmpuje       = 5f;
    [Tooltip("Cooldown entre toques (segundos).")]
    public float cooldownContacto   = 1.5f;

    // ── Audio / Feedback ─────────────────────────────────────────────────────
    [Header("Sonido (opcional)")]
    public AudioClip sonidoDeteccion;
    public AudioClip sonidoContacto;
    [Range(0f, 1f)] public float volumen = 0.6f;

    // ── Estado ───────────────────────────────────────────────────────────────
    public Estado EstadoActual { get; private set; } = Estado.Patrullar;

    // ── Privado ───────────────────────────────────────────────────────────────
    Rigidbody2D    rb;
    SpriteRenderer sprite;
    SistemaSangre  sistemaSangre;
    Transform      jugador;

    int   indicePatrulla;
    bool  patrullaInvertida;

    Vector2 destinoActual;
    float   tContacto;
    float   tSinRastro;

    // Ultimo charco al que se dirige
    Vector2 charcoObjetivo;
    bool    tieneCharcoObjetivo;

    // ─────────────────────────────────────────────────────────────────────────

    void Awake()
    {
        rb     = GetComponent<Rigidbody2D>();
        sprite = GetComponentInChildren<SpriteRenderer>();
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        rb.gravityScale = 2f;
    }

    void Start()
    {
        var jugadorGO = GameObject.FindGameObjectWithTag("Player");
        if (jugadorGO != null)
        {
            jugador      = jugadorGO.transform;
            sistemaSangre = jugadorGO.GetComponent<SistemaSangre>();
        }

        if (puntosPatrulla != null && puntosPatrulla.Length > 0)
            destinoActual = puntosPatrulla[0].position;
        else
            destinoActual = transform.position;
    }

    void Update()
    {
        tContacto -= Time.deltaTime;
        ActualizarDeteccion();
        ActualizarEstado();
    }

    void FixedUpdate()
    {
        ActualizarMovimiento();
    }

    // ── Deteccion ─────────────────────────────────────────────────────────────
    void ActualizarDeteccion()
    {
        if (jugador == null) return;

        bool puedeVerJugador = TieneVision();

        if (puedeVerJugador)
        {
            if (EstadoActual != Estado.Perseguir)
                Alertar(sonidoDeteccion);
            EstadoActual = Estado.Perseguir;
            tSinRastro   = 0f;
            return;
        }

        // Buscar charcos de sangre del jugador
        if (EstadoActual != Estado.Perseguir && sistemaSangre != null)
        {
            var rastro = sistemaSangre.ObtenerRastro();
            foreach (var charco in rastro)
            {
                if (Vector2.Distance(transform.position, charco) <= radioOlfato)
                {
                    charcoObjetivo     = charco;
                    tieneCharcoObjetivo = true;

                    if (EstadoActual != Estado.Rastrear)
                        Alertar(null);
                    EstadoActual = Estado.Rastrear;
                    return;
                }
            }
        }

        // Perder al jugador
        if (EstadoActual == Estado.Perseguir)
        {
            tSinRastro += Time.deltaTime;
            if (tSinRastro > 3f)
            {
                EstadoActual        = Estado.Patrullar;
                tieneCharcoObjetivo = false;
                tSinRastro          = 0f;
            }
        }
        else if (EstadoActual == Estado.Rastrear && !tieneCharcoObjetivo)
        {
            EstadoActual = Estado.Patrullar;
        }
    }

    bool TieneVision()
    {
        if (jugador == null) return false;
        float dist = Vector2.Distance(transform.position, jugador.position);
        if (dist > radioVision) return false;

        Vector2 dir = ((Vector2)jugador.position - (Vector2)transform.position).normalized;
        Vector2 frente = sprite != null && sprite.flipX ? Vector2.left : Vector2.right;
        float angulo = Vector2.Angle(frente, dir);
        if (angulo > anguloVision * 0.5f) return false;

        // Raycast para ver si hay pared entre el purgador y el jugador
        var hit = Physics2D.Raycast(transform.position, dir, dist, capaObstaculos);
        return hit.collider == null;
    }

    // ── Estado / Movimiento ───────────────────────────────────────────────────
    void ActualizarEstado()
    {
        switch (EstadoActual)
        {
            case Estado.Patrullar:
                ActualizarPatrulla();
                break;
            case Estado.Rastrear:
                if (tieneCharcoObjetivo)
                    destinoActual = charcoObjetivo;
                if (Vector2.Distance(transform.position, charcoObjetivo) < radioLlegada)
                    tieneCharcoObjetivo = false;
                break;
            case Estado.Perseguir:
                if (jugador != null)
                    destinoActual = jugador.position;
                break;
        }
    }

    void ActualizarPatrulla()
    {
        if (puntosPatrulla == null || puntosPatrulla.Length == 0) return;

        destinoActual = puntosPatrulla[indicePatrulla].position;
        if (Vector2.Distance(transform.position, destinoActual) < radioLlegada)
        {
            if (patrullaLoop)
                indicePatrulla = (indicePatrulla + 1) % puntosPatrulla.Length;
            else
            {
                if (!patrullaInvertida && indicePatrulla >= puntosPatrulla.Length - 1)
                    patrullaInvertida = true;
                else if (patrullaInvertida && indicePatrulla <= 0)
                    patrullaInvertida = false;

                indicePatrulla += patrullaInvertida ? -1 : 1;
                indicePatrulla  = Mathf.Clamp(indicePatrulla, 0, puntosPatrulla.Length - 1);
            }
        }
    }

    void ActualizarMovimiento()
    {
        float velocidad = EstadoActual switch
        {
            Estado.Rastrear    => velocidadRastreo,
            Estado.Perseguir   => velocidadPersecucion,
            _                  => velocidadPatrulla
        };

        float dx = destinoActual.x - transform.position.x;
        float vx = Mathf.Sign(dx) * velocidad;

        // No moverse si ya llego (horizontalmente)
        if (Mathf.Abs(dx) < radioLlegada) vx = 0f;

        rb.linearVelocity = new Vector2(vx, rb.linearVelocity.y);

        // Voltear sprite
        if (sprite != null && Mathf.Abs(vx) > 0.05f)
            sprite.flipX = vx < 0f;
    }

    // ── Contacto con el jugador ───────────────────────────────────────────────
    void OnCollisionEnter2D(Collision2D col)
    {
        if (!col.collider.CompareTag("Player")) return;
        if (tContacto > 0f) return;

        tContacto = cooldownContacto;

        // Dano de sangre
        sistemaSangre?.RecibirDano(danoPorContacto);

        // Empuje
        var rbJugador = col.rigidbody;
        if (rbJugador != null)
        {
            Vector2 dir = (col.transform.position - transform.position).normalized;
            rbJugador.AddForce(dir * fuerzaEmpuje, ForceMode2D.Impulse);
        }

        if (sonidoContacto != null)
            AudioSource.PlayClipAtPoint(sonidoContacto, transform.position, volumen);
    }

    // ── Utilidades ────────────────────────────────────────────────────────────
    void Alertar(AudioClip clip)
    {
        if (clip != null)
            AudioSource.PlayClipAtPoint(clip, transform.position, volumen);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, radioVision);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, radioOlfato);

        if (puntosPatrulla != null)
        {
            Gizmos.color = Color.cyan;
            for (int i = 0; i < puntosPatrulla.Length - 1; i++)
            {
                if (puntosPatrulla[i] != null && puntosPatrulla[i + 1] != null)
                    Gizmos.DrawLine(puntosPatrulla[i].position, puntosPatrulla[i + 1].position);
            }
        }
    }
}
