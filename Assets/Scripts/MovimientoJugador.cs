using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Controlador 2D de plataformas para Hemorragia: caminar, correr (Shift) y saltar.
/// Funciona con el Input System nuevo o con el Input Manager clasico.
/// Controles: A/D o flechas = mover, Shift = correr, Espacio/W/Flecha arriba = saltar.
///
/// VALORES AFINADOS (v1.3):
///   - alturaSalto 1.8 u: supera el camion (1.4) con margen sin sentirse "de luna".
///   - velocidadCaminar 3.2: paso de trabajador cansado, no de corredor.
///   - velocidadCorrer 6.5: sprint urgente; desbloquea el sistema de sangre.
///   - escalaGravedad 3.5: caidas contundentes, tierra rapida.
///   - multiplicadorCaida 1.6: asimetria salto/caida para no flotar.
///   - tiempoCoyote 0.12: margen justo; errores de borde no se sienten injustos.
///   - tiempoBufferSalto 0.12: salto de anticipacion suave.
/// NOTA: Usa CapsuleCollider2D (no Box) para evitar que el jugador se atore
///       en las esquinas de paredes y plataformas (corner-catching).
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(CapsuleCollider2D))]
public class MovimientoJugador : MonoBehaviour
{
    [Header("Movimiento")]
    [Tooltip("Paso de trabajador cansado. Modificar con cautela.")]
    public float velocidadCaminar = 3.2f;
    [Tooltip("Sprint urgente. Activar desangra mas rapido (ver SistemaSangre).")]
    public float velocidadCorrer = 6.5f;

    [Header("Salto")]
    [Tooltip("Altura maxima en unidades. 1.8 = supera el camion (1.4) con margen.")]
    public float alturaSalto = 1.8f;
    [Tooltip("Gravedad base. 3.5 = caidas contundentes.")]
    public float escalaGravedad = 3.5f;
    [Tooltip("Gravedad extra al caer: evita la sensacion de flotar. 1.6 = asimetria notable.")]
    public float multiplicadorCaida = 1.6f;
    [Tooltip("Ventana para saltar justo despues de salir de un borde.")]
    public float tiempoCoyote = 0.12f;
    [Tooltip("Ventana para que un salto presionado antes de aterrizar cuente.")]
    public float tiempoBufferSalto = 0.12f;

    [Header("Agacharse (S / Flecha abajo)")]
    [Tooltip("Altura del personaje agachado (1 = de pie).")]
    public float escalaAgachado = 0.7f;
    [Tooltip("Velocidad al ir agachado.")]
    public float velocidadAgachado = 1.6f;

    // Estado publico (para SistemaSangre, animaciones, audio, etc.)
    public bool EstaEnSuelo   { get; private set; }
    public bool EstaCorriendo { get; private set; }
    public bool EstaAgachado  { get; private set; }
    public float Horizontal   { get; private set; }

    Rigidbody2D      rb;
    CapsuleCollider2D col;
    SpriteRenderer   sprite;
    float tCoyote, tBuffer;
    bool saltar, cortarSalto;
    float escalaYBase = 1f;
    float tAtascado;

    void Awake()
    {
        rb     = GetComponent<Rigidbody2D>();
        col    = GetComponent<CapsuleCollider2D>();
        sprite = GetComponentInChildren<SpriteRenderer>();

        rb.constraints   = RigidbodyConstraints2D.FreezeRotation;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        rb.gravityScale  = escalaGravedad;

        // Material sin friccion: evita que el jugador se pegue a las paredes.
        // CapsuleCollider2D + friction=0 elimina el "corner catching" en esquinas.
        var mat = new PhysicsMaterial2D("SinFriccion") { friction = 0f, bounciness = 0f };
        col.sharedMaterial = mat;
    }

    void Update()
    {
        Horizontal    = LeerHorizontal();
        EstaEnSuelo   = DetectarSuelo();
        ActualizarAgachado();
        EstaCorriendo = !EstaAgachado && LeerCorrer() && Mathf.Abs(Horizontal) > 0.01f;

        if (EstaEnSuelo) tCoyote = tiempoCoyote; else tCoyote -= Time.deltaTime;
        if (SaltoPresionado()) tBuffer = tiempoBufferSalto; else tBuffer -= Time.deltaTime;

        if (tBuffer > 0f && tCoyote > 0f && !EstaAgachado)
        {
            saltar  = true;
            tBuffer = 0f;
            tCoyote = 0f;
        }

        // Soltar el boton a mitad del salto lo acorta (salto variable)
        if (SaltoSoltado() && rb.linearVelocity.y > 0f) cortarSalto = true;

        if (sprite != null && Mathf.Abs(Horizontal) > 0.01f)
            sprite.flipX = Horizontal < 0f;
    }

    void FixedUpdate()
    {
        Vector2 v = rb.linearVelocity;
        v.x = Horizontal * (EstaAgachado ? velocidadAgachado : (EstaCorriendo ? velocidadCorrer : velocidadCaminar));

        if (saltar)
        {
            v.y    = Mathf.Sqrt(2f * Mathf.Abs(Physics2D.gravity.y) * escalaGravedad * alturaSalto);
            saltar = false;
        }
        else if (cortarSalto && v.y > 0f)
        {
            v.y *= 0.5f;
        }
        cortarSalto = false;

        rb.gravityScale    = v.y < 0f ? escalaGravedad * multiplicadorCaida : escalaGravedad;
        rb.linearVelocity  = v;
    }

    // ---------- Agacharse ----------

    void ActualizarAgachado()
    {
        bool quiere = LeerAgacharse() && EstaEnSuelo;
        if (quiere && !EstaAgachado)
        {
            escalaYBase = transform.localScale.y;
            CambiarEscalaY(escalaYBase * escalaAgachado);
            EstaAgachado = true;
        }
        else if (!quiere && EstaAgachado)
        {
            // Si algo raro bloquea el levantarse, tras 0.6 s se levanta igual (antes podia quedar agachado para siempre)
            tAtascado += Time.unscaledDeltaTime;
            if (HayEspacioParaLevantarse() || tAtascado > 0.6f)
            {
                CambiarEscalaY(escalaYBase);
                EstaAgachado = false;
                tAtascado = 0f;
            }
        }
        else tAtascado = 0f;
    }

    /// <summary>Cambia la altura del personaje manteniendo los pies en su lugar.</summary>
    void CambiarEscalaY(float y)
    {
        float pies = col.bounds.min.y;
        var s = transform.localScale; s.y = y; transform.localScale = s;
        Physics2D.SyncTransforms();
        float dif = pies - col.bounds.min.y;
        transform.position += new Vector3(0f, dif, 0f);
        Physics2D.SyncTransforms();
    }

    bool HayEspacioParaLevantarse()
    {
        Bounds b = col.bounds;
        float alto = b.size.y / Mathf.Max(0.1f, escalaAgachado);
        Vector2 centro = new Vector2(b.center.x, b.min.y + alto * 0.5f + 0.05f);
        Vector2 tam = new Vector2(b.size.x * 0.8f, alto - 0.12f);
        foreach (var h in Physics2D.OverlapBoxAll(centro, tam, 0f))
            if (h != col && !h.isTrigger && !h.usedByEffector && h.attachedRigidbody != rb) return false;
        return true;
    }

    bool LeerAgacharse()
    {
#if ENABLE_INPUT_SYSTEM
        var k = Keyboard.current;
        return k != null && (k.sKey.isPressed || k.downArrowKey.isPressed);
#else
        return Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow);
#endif
    }

    bool DetectarSuelo()
    {
        if (rb.linearVelocity.y > 0.1f) return false;

        Bounds b       = col.bounds;
        // Usamos un radio pequeño en la base de la cápsula para detectar suelo
        Vector2 origen = new Vector2(b.center.x, b.min.y);
        float   radio  = b.size.x * 0.4f;

        foreach (var h in Physics2D.OverlapCircleAll(origen, radio + 0.08f))
            if (h != col && !h.isTrigger) return true;
        return false;
    }

    // ---------- Entrada (compatible con ambos sistemas) ----------

    float LeerHorizontal()
    {
#if ENABLE_INPUT_SYSTEM
        var k = Keyboard.current;
        if (k == null) return 0f;
        float h = 0f;
        if (k.aKey.isPressed || k.leftArrowKey.isPressed) h -= 1f;
        if (k.dKey.isPressed || k.rightArrowKey.isPressed) h += 1f;
        return h;
#else
        return Input.GetAxisRaw("Horizontal");
#endif
    }

    bool LeerCorrer()
    {
#if ENABLE_INPUT_SYSTEM
        var k = Keyboard.current;
        return k != null && k.leftShiftKey.isPressed;
#else
        return Input.GetKey(KeyCode.LeftShift);
#endif
    }

    bool SaltoPresionado()
    {
#if ENABLE_INPUT_SYSTEM
        var k = Keyboard.current;
        return k != null && (k.spaceKey.wasPressedThisFrame || k.wKey.wasPressedThisFrame || k.upArrowKey.wasPressedThisFrame);
#else
        return Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow);
#endif
    }

    bool SaltoSoltado()
    {
#if ENABLE_INPUT_SYSTEM
        var k = Keyboard.current;
        return k != null && (k.spaceKey.wasReleasedThisFrame || k.wKey.wasReleasedThisFrame || k.upArrowKey.wasReleasedThisFrame);
#else
        return Input.GetKeyUp(KeyCode.Space) || Input.GetKeyUp(KeyCode.W) || Input.GetKeyUp(KeyCode.UpArrow);
#endif
    }
}
