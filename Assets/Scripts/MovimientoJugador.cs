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

    // Estado publico (para SistemaSangre, animaciones, audio, etc.)
    public bool EstaEnSuelo   { get; private set; }
    public bool EstaCorriendo { get; private set; }
    public float Horizontal   { get; private set; }

    Rigidbody2D      rb;
    CapsuleCollider2D col;
    SpriteRenderer   sprite;
    float tCoyote, tBuffer;
    bool saltar, cortarSalto;

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
        EstaCorriendo = LeerCorrer() && Mathf.Abs(Horizontal) > 0.01f;
        EstaEnSuelo   = DetectarSuelo();

        if (EstaEnSuelo) tCoyote = tiempoCoyote; else tCoyote -= Time.deltaTime;
        if (SaltoPresionado()) tBuffer = tiempoBufferSalto; else tBuffer -= Time.deltaTime;

        if (tBuffer > 0f && tCoyote > 0f)
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
        v.x = Horizontal * (EstaCorriendo ? velocidadCorrer : velocidadCaminar);

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
