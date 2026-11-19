using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Monstruitos: la masa roja en version chica y rapida. Usan la IA del Purgador (vista, olfato, dano)
/// pero corren mas, ven en todas direcciones y brincan para seguirte sobre cajas, tubos y cruces de lava.
/// Se colocan solos al cargar el Nivel 1 (cuarto de la emboscada) y el Nivel 2. Para mover o quitar
/// alguno, cambia la lista "Puntos" de abajo.
/// </summary>
public class PequenoMonstruo : MonoBehaviour
{
    // ── Donde aparecen ────────────────────────────────────────────────────────
    // Nivel1: x local dentro del cuarto Cuarto4_Emboscada (suelo en y=-3.5).
    // Nivel2: x del mundo; el piso se busca solo con un rayo hacia abajo.
    static readonly float[] PuntosNivel1 = { 0.5f, 5.8f };
    static readonly float[] PuntosNivel2 = { 46f, 58f };
    // El molesto de la lava: mas rapido, ve de mas lejos y brinca mas seguido. Espera sobre las plataformas del cruce.
    static readonly float PuntoMolesto = 73f;

    // ── Ajustes ───────────────────────────────────────────────────────────────
    public float ppu = 270f;                         // mas alto = mas chico
    public Vector2 tamCollider = new Vector2(0.7f, 0.65f);
    public float fuerzaSalto = 7.5f;
    public float pausaSalto = 0.8f;
    public float fpsIdle = 5f, fpsMover = 12f, fpsAtaque = 12f;

    BancoEnemigos b;
    PurgadorIA ia;
    Rigidbody2D rb;
    SpriteRenderer sr;
    Sprite[] fIdle, fMover, fAtaque;
    Transform jugador;
    Vector3 posInicial;
    bool listo;
    float t, tSalto, prevX, vxSuave;

    // ── Instalacion automatica ───────────────────────────────────────────────
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Registrar() { SceneManager.sceneLoaded += (e, md) => Instalar(); }

    static void Instalar()
    {
        if (Object.FindFirstObjectByType<PequenoMonstruo>(FindObjectsInactive.Include) == null) CrearTodos();
        HacerImpredecibles();
    }

    // Monstruitos y Purgadores del corredor (zona B) se vuelven impredecibles y un poco mas agresivos
    static void HacerImpredecibles()
    {
        foreach (var m in Object.FindObjectsByType<PequenoMonstruo>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (m.GetComponent<Impredecible>() == null) m.gameObject.AddComponent<Impredecible>();
        if (SceneManager.GetActiveScene().name != "Nivel2") return;
        // Alerta por cercania: todos los Purgadores del Nivel 2 te detectan al acercarte a su zona (360 grados),
        // sin depender de que lances una lata ni de que estes de frente.
        foreach (var pu in Object.FindObjectsByType<PurgadorIA>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            pu.anguloVision = 360f;
            pu.radioVision = Mathf.Max(pu.radioVision, 8f);
            pu.radioOlfato = Mathf.Max(pu.radioOlfato, 7f);
        }
        var g = Object.FindFirstObjectByType<GestorNivel2>(FindObjectsInactive.Include);
        if (g == null || g.purgadoresZonaB == null) return;
        foreach (var p in g.purgadoresZonaB)
        {
            if (p == null || p.GetComponent<Impredecible>() != null) continue;
            p.radioVision *= 1.12f; p.radioOlfato *= 1.12f; p.cooldownContacto *= 0.8f;
            p.gameObject.AddComponent<Impredecible>();
        }
    }

    static void CrearTodos()
    {
        string escena = SceneManager.GetActiveScene().name;
        if (escena == "Nivel1")
        {
            Transform cuarto = null;
            foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (t.name == "Cuarto4_Emboscada") { cuarto = t; break; }
            if (cuarto == null) return;
            for (int i = 0; i < PuntosNivel1.Length; i++)
                Crear(cuarto, "Monstruito_N1_" + i, new Vector3(PuntosNivel1[i], -3.5f + 0.5f, 0f), false);
        }
        else if (escena == "Nivel2")
        {
            Transform padre = null;
            foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (t.name == "Gameplay_Nivel2") { padre = t; break; }
            for (int i = 0; i < PuntosNivel2.Length; i++)
                Crear(padre, "Monstruito_N2_" + i, new Vector3(PuntosNivel2[i], 0f, 0f), true);
            Crear(padre, "Monstruito_N2_Molesto", new Vector3(PuntoMolesto, 0f, 0f), true, true);
        }
    }

    static void Crear(Transform padre, string nombre, Vector3 pos, bool buscarPiso, bool molesto = false)
    {
        var go = new GameObject(nombre);
        if (padre != null) go.transform.SetParent(padre, false);
        go.transform.localPosition = pos;
        go.AddComponent<SpriteRenderer>().sortingOrder = 4;
        var rb = go.AddComponent<Rigidbody2D>();
        rb.freezeRotation = true;
        go.AddComponent<BoxCollider2D>();
        var ia = go.AddComponent<PurgadorIA>();
        ia.velocidadPatrulla = 1.8f;
        ia.velocidadRastreo = 3.8f;
        ia.velocidadPersecucion = 5.6f;      // camina 3.2 / corre 6.5: casi te alcanza corriendo
        ia.radioVision = 8f;
        ia.radioOlfato = 7f;
        ia.anguloVision = 360f;              // te ve tambien por atras
        ia.danoPorContacto = 10f;
        ia.cooldownContacto = 0.8f;
        if (molesto)
        {
            ia.velocidadRastreo = 4.6f;
            ia.velocidadPersecucion = 6.4f;  // casi tan rapido como Jeremy corriendo (6.5)
            ia.radioVision = 10f;
            ia.radioOlfato = 9f;
            ia.danoPorContacto = 6f;         // pega poco pero seguido
            ia.cooldownContacto = 0.6f;
        }
        var m = go.AddComponent<PequenoMonstruo>();
        m.buscarPiso = buscarPiso;
        if (molesto) { m.fuerzaSalto = 9f; m.pausaSalto = 0.45f; }
    }

    [HideInInspector] public bool buscarPiso;

    // ── Vida del objeto ──────────────────────────────────────────────────────
    void Awake()
    {
        b = Resources.Load<BancoEnemigos>("BancoEnemigos");
        ia = GetComponent<PurgadorIA>();
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
        if (b == null || b.rojaIdle == null) { enabled = false; return; }

        fIdle = Cortar(b.rojaIdle); fMover = Cortar(b.rojaCaminar); fAtaque = Cortar(b.rojaAtaque);
        sr.sprite = fIdle[0];
        var col = GetComponent<BoxCollider2D>();
        col.size = tamCollider;
        col.offset = new Vector2(0f, tamCollider.y * 0.5f - 0.5f);      // pies 0.5 bajo el centro
        rb.gravityScale = 2f;
        if (GetComponent<SaludEnemigo>() == null) gameObject.AddComponent<SaludEnemigo>();
        SistemaSangre.AlResetear += AlReaparecerJugador;
        // no se amontonan unos sobre otros
        foreach (var o in todos)
            if (o != null) { var c2 = o.GetComponent<BoxCollider2D>(); if (c2 != null) Physics2D.IgnoreCollision(col, c2); }
        todos.Add(this);
    }

    static readonly List<PequenoMonstruo> todos = new List<PequenoMonstruo>();

    void OnDestroy() { SistemaSangre.AlResetear -= AlReaparecerJugador; todos.Remove(this); }

    void Start()
    {
        if (buscarPiso)
        {
            var origen = new Vector2(transform.position.x, 6f);
            foreach (var h in Physics2D.RaycastAll(origen, Vector2.down, 30f))
            {
                if (h.collider == null || h.collider.isTrigger || h.collider.transform.IsChildOf(transform)) continue;
                if (h.collider.CompareTag("Player") || h.collider.GetComponentInParent<PurgadorIA>() != null) continue;
                transform.position = new Vector3(transform.position.x, h.point.y + 0.52f, 0f);
                break;
            }
        }
        posInicial = transform.position;
        listo = true;
        prevX = transform.position.x;
    }

    void AlReaparecerJugador()
    {
        if (this == null || !listo) return;
        transform.position = posInicial;
        if (ia != null) ia.Reiniciar();
    }

    Sprite[] Cortar(Texture2D tex)
    {
        tex.filterMode = FilterMode.Point;
        int celda = b.celda, n = Mathf.Max(1, tex.width / celda);
        var arr = new Sprite[n];
        float pivY = Mathf.Clamp01((0.5f * ppu + b.pixelesBajoPies) / celda);
        for (int i = 0; i < n; i++)
            arr[i] = Sprite.Create(tex, new Rect(i * celda, 0, celda, celda), new Vector2(0.5f, pivY), ppu, 0, SpriteMeshType.FullRect);
        return arr;
    }

    bool EnSuelo()
    {
        var c = GetComponent<BoxCollider2D>().bounds;
        foreach (var h in Physics2D.OverlapCircleAll(new Vector2(c.center.x, c.min.y), 0.12f))
            if (h != null && !h.isTrigger && h.gameObject != gameObject && h.GetComponentInParent<PurgadorIA>() == null
                && !h.CompareTag("Player")) return true;
        return false;
    }

    void FixedUpdate()
    {
        if (jugador == null) { var p = GameObject.FindGameObjectWithTag("Player"); if (p != null) jugador = p.transform; return; }
        tSalto -= Time.fixedDeltaTime;
        bool caza = ia.EstadoActual == PurgadorIA.Estado.Perseguir || ia.EstadoActual == PurgadorIA.Estado.Rastrear;
        if (!caza || tSalto > 0f || !EnSuelo()) return;

        float dx = jugador.position.x - transform.position.x;
        float dy = jugador.position.y - transform.position.y;
        bool trabado = Mathf.Abs(rb.linearVelocity.x) < 0.3f && Mathf.Abs(dx) > 0.8f;     // choca con una caja
        bool jugadorArriba = dy > 1f && Mathf.Abs(dx) < 3f;
        if (trabado || jugadorArriba)
        {
            rb.linearVelocity = new Vector2(Mathf.Sign(dx) * 3.5f, fuerzaSalto);
            tSalto = pausaSalto;
        }
    }

    void LateUpdate()
    {
        if (fIdle == null) return;
        t += Time.deltaTime;
        float dt = Mathf.Max(Time.deltaTime, 0.0001f);
        vxSuave = Mathf.Lerp(vxSuave, (transform.position.x - prevX) / dt, 0.3f);
        prevX = transform.position.x;
        bool ataca = ia.EstadoActual == PurgadorIA.Estado.Perseguir && jugador != null &&
                     Vector2.Distance(jugador.position, transform.position) < 1.2f;
        Sprite[] f; float fps;
        if (ataca) { f = fAtaque; fps = fpsAtaque; }
        else if (Mathf.Abs(vxSuave) > 0.15f) { f = fMover; fps = fpsMover; }
        else { f = fIdle; fps = fpsIdle; }
        if (f == null || f.Length == 0) return;
        sr.sprite = f[Mathf.FloorToInt(t * fps) % f.Length];
    }
}

/// <summary>
/// Hace a un enemigo (PurgadorIA) menos predecible: cada cierto tiempo, al azar, se lanza con una arremetida,
/// titubea un instante o da un brinco. La velocidad base se respeta el resto del tiempo.
/// </summary>
[RequireComponent(typeof(PurgadorIA))]
public class Impredecible : MonoBehaviour
{
    PurgadorIA ia;
    Rigidbody2D rb;
    float vPat, vRas, vPer;
    float tSiguiente, tEfecto, mult = 1f;
    bool listo;

    void Start()
    {
        ia = GetComponent<PurgadorIA>();
        rb = GetComponent<Rigidbody2D>();
        vPat = ia.velocidadPatrulla; vRas = ia.velocidadRastreo; vPer = ia.velocidadPersecucion;
        tSiguiente = Random.Range(0.8f, 2.5f);
        listo = true;
    }

    void Update()
    {
        if (!listo) return;
        bool caza = ia.EstadoActual == PurgadorIA.Estado.Perseguir || ia.EstadoActual == PurgadorIA.Estado.Rastrear;

        if (tEfecto > 0f)
        {
            tEfecto -= Time.deltaTime;
            if (tEfecto <= 0f) mult = 1f;
        }
        else
        {
            tSiguiente -= Time.deltaTime * (caza ? 1.6f : 0.6f);     // mas seguido cuando te persigue
            if (tSiguiente <= 0f)
            {
                tSiguiente = Random.Range(1.2f, 3.2f);
                float r = Random.value;
                if (r < 0.40f) { mult = Random.Range(1.4f, 1.7f); tEfecto = Random.Range(0.5f, 0.9f); }          // arremetida
                else if (r < 0.62f) { mult = 0.4f; tEfecto = Random.Range(0.3f, 0.6f); }                          // titubeo
                else if (r < 0.80f && caza && rb != null && Mathf.Abs(rb.linearVelocity.y) < 0.05f)                // brinco
                    rb.linearVelocity = new Vector2(rb.linearVelocity.x, Random.Range(5.5f, 7.5f));
            }
        }

        ia.velocidadPatrulla = vPat * mult;
        ia.velocidadRastreo = vRas * mult;
        ia.velocidadPersecucion = vPer * mult;
    }
}
