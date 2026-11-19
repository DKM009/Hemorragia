using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Pelea final contra el DEMONIO DE FUEGO. Se construye por codigo lejos del nivel (sin tocar escenas).
/// - Se mueve y ataca: garra de cerca, bola de fuego (se esquiva saltando) y deslizamiento (se esquiva saltando).
/// - Queda ATURDIDO despues de deslizarse o de lanzar fuego: ahi las latas (Q) apuntan solas a su pecho y le hacen dano.
/// - Al morir, pasa a la EscenaFinal. Si mueres, reapareces en la entrada y la pelea se reinicia.
/// Arte: Resources/FlameDemon/*.png (tiras de cuadros) y Resources/Arena_Fundicion.jpg.
/// </summary>
public class ArenaJefe : MonoBehaviour
{
    public static ArenaJefe Instancia { get; private set; }

    // ── Medidas del mundo ────────────────────────────────────────────────────
    public const float X0 = 400f;       // centro de la arena (lejos del Nivel 2)
    const float ANCHO = 26f, ALTO = 14.5f;
    const float CENTRO_Y = 4.96f;       // el piso de la ilustracion queda en y = 0
    const float PPU = 15f;              // pixeles por unidad del demonio (mas alto = mas chico)
    const float PIVOTE_Y = 12f / 128f;  // pies del demonio dentro del cuadro

    [Header("Jefe")]
    public float vidaMax = 10f;
    public float vida;
    public float danoGarra = 20f;
    public float danoBola = 18f;
    public float danoDeslizar = 22f;
    public float velocidadBola = 8.5f;
    public float velocidadDeslizar = 11f;

    enum Est { Dormido, Despertando, Acechar, Garra, Fuego, Deslizar, Aturdido, Muriendo }
    Est est = Est.Dormido;
    float t;            // tiempo en el estado actual

    Sprite[] aIdle, aGarra, aFuego, aSlide, aMuerte;
    Sprite sprBola, sprLata, sprVenda, sprFondo, sprChispa;
    Transform bossT;
    SpriteRenderer srBoss;
    Light2D luzJefe;
    Collider2D limites;
    RectTransform barraRelleno;
    GameObject uiRaiz;

    // animacion
    Sprite[] clip; float tClip, fpsClip; bool loopClip;

    float bx, dir = -1f, tDorm, tDanio, tFlash, tMuerte, tCan, tVenda;
    float objetivoX;
    bool furiaHecha, golpeoGarra, disparoHecho, tocoSlide, goPrev, final, primeraVez = true;
    Color tinte = Color.white;
    int ultimoAtaque = -1;

    Transform jugador;
    SistemaSangre sangre;
    Rigidbody2D rbJ;

    float fase => vida <= vidaMax * 0.5f ? 1.3f : 1f;   // mas rapido a media vida

    class Bola { public Transform t; public Vector2 v; public float vida = 5f; }
    readonly List<Bola> bolas = new List<Bola>();
    readonly List<GameObject> pickups = new List<GameObject>();

    float PosX => bossT != null ? bossT.position.x : bx;
    Vector2 Pecho => new Vector2(PosX, 2.1f);

    /// <summary>Punto al que apuntan las latas (su pecho). Null si no hay pelea activa.</summary>
    public Vector2? ObjetivoLata => (est != Est.Dormido && est != Est.Muriendo) ? (Vector2?)Pecho : null;
    bool Vulnerable => est == Est.Aturdido;

    // ── API estatica ─────────────────────────────────────────────────────────
    public static bool Disponible()
    {
        return Resources.Load<Texture2D>("FlameDemon/Idle") != null
            && Resources.Load<Texture2D>("FlameDemon/Attack") != null
            && Resources.Load<Texture2D>("FlameDemon/Attack2") != null
            && Resources.Load<Texture2D>("FlameDemon/Slide") != null
            && Resources.Load<Texture2D>("FlameDemon/Defeat") != null
            && Resources.Load<Texture2D>("Arena_Fundicion") != null;
    }

    public static void Comenzar(Transform jugador)
    {
        if (Instancia == null) new GameObject("ArenaJefe").AddComponent<ArenaJefe>();
        Instancia.Entrar(jugador);
    }

    // ── Construccion ─────────────────────────────────────────────────────────
    void Awake()
    {
        Instancia = this;
        if (!Disponible()) return;
        Construir();
    }

    Sprite Crear(Texture2D tex, float ppu, Vector2 piv, FilterMode f)
    {
        tex.filterMode = f;
        return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), piv, ppu, 0, SpriteMeshType.FullRect);
    }

    Sprite[] Tira(string nombre, int cuadros)
    {
        var tex = Resources.Load<Texture2D>("FlameDemon/" + nombre);
        tex.filterMode = FilterMode.Point;
        int w = tex.width / cuadros;
        var arr = new Sprite[cuadros];
        for (int i = 0; i < cuadros; i++)
            arr[i] = Sprite.Create(tex, new Rect(i * w, 0, w, tex.height), new Vector2(0.5f, PIVOTE_Y), PPU, 0, SpriteMeshType.FullRect);
        return arr;
    }

    Transform Hijo(string nombre, Vector2 pos, Transform padre = null)
    {
        var go = new GameObject(nombre);
        go.transform.SetParent(padre != null ? padre : transform, false);
        go.transform.position = new Vector3(pos.x, pos.y, 0f);
        return go.transform;
    }

    SpriteRenderer SR(Transform tr, Sprite s, int orden)
    {
        var sr = tr.gameObject.AddComponent<SpriteRenderer>();
        sr.sprite = s; sr.sortingOrder = orden;
        return sr;
    }

    void Caja(string nombre, Vector2 centro, Vector2 tam)
    {
        var tr = Hijo(nombre, centro);
        var c = tr.gameObject.AddComponent<BoxCollider2D>();
        c.size = tam;
    }

    Sprite sprRejilla;
    Sprite SpriteRejilla()
    {
        if (sprRejilla != null) return sprRejilla;
        const int W = 32, H = 16;
        var tex = new Texture2D(W, H, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Repeat };
        for (int y = 0; y < H; y++)
            for (int x = 0; x < W; x++)
            {
                Color c = new Color(0.20f, 0.14f, 0.12f, 1f);                      // fondo oscuro
                bool viga = y <= 2 || y >= H - 3;                                    // vigas arriba y abajo
                bool barra = (x % 8) < 3;                                            // barras verticales
                bool cruz = (y >= 6 && y <= 8) && (x % 16) < 12;                     // travesano
                if (viga) c = new Color(0.46f, 0.31f, 0.22f, 1f);
                else if (barra || cruz) c = new Color(0.36f, 0.24f, 0.18f, 1f);
                if (y == H - 1) c = new Color(0.72f, 0.42f, 0.22f, 1f);              // brillo del borde de arriba
                if (((x * 7 + y * 13) % 29) == 0) c = new Color(0.55f, 0.28f, 0.12f, 1f);   // oxido
                tex.SetPixel(x, y, c);
            }
        tex.Apply();
        sprRejilla = Sprite.Create(tex, new Rect(0, 0, W, H), new Vector2(0.5f, 0.5f), 32f, 0, SpriteMeshType.FullRect);
        return sprRejilla;
    }

    // ── Tubos cauterizadores: sanan la hemorragia, pero si te quedas mucho te queman y mueres ──
    Sprite sprTubo, sprTuboBrillo;
    void SpritesTubo()
    {
        if (sprTubo != null) return;
        const int W = 32, H = 16;
        var t1 = new Texture2D(W, H, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Repeat };
        var t2 = new Texture2D(W, H, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Repeat };
        for (int y = 0; y < H; y++)
            for (int x = 0; x < W; x++)
            {
                float v = (y + 0.5f) / H;                                   // 0 abajo, 1 arriba
                float cil = 0.45f + 0.75f * Mathf.Sin(v * Mathf.PI);       // sombreado de cilindro
                float luz = cil * (v > 0.62f && v < 0.8f ? 1.25f : 1f);   // brillo especular
                Color c = new Color(0.34f, 0.29f, 0.27f) * luz;
                int xm = x % 16;
                if (xm <= 1 || xm == 15) c = new Color(0.20f, 0.16f, 0.15f) * (0.6f + 0.6f * cil);                    // junta
                else if (xm == 2 || xm == 14) c = new Color(0.52f, 0.42f, 0.36f) * cil;                               // reborde de la brida
                if ((xm == 4 || xm == 12) && (y == 3 || y == 12)) c = new Color(0.12f, 0.10f, 0.09f);                 // remaches
                if (((x * 11 + y * 17) % 23) == 0) c = new Color(0.50f, 0.25f, 0.10f);                                // oxido
                c.a = 1f; t1.SetPixel(x, y, c);
                // brillo: rendijas por donde sale el calor (se ven al acercarte)
                bool rend = xm >= 5 && xm <= 11 && (y == 7 || y == 8);
                t2.SetPixel(x, y, rend ? new Color(1f, 0.62f, 0.18f, 1f) : new Color(0, 0, 0, 0));
            }
        t1.Apply(); t2.Apply();
        sprTubo = Sprite.Create(t1, new Rect(0, 0, W, H), new Vector2(0.5f, 0.5f), 32f, 0, SpriteMeshType.FullRect);
        sprTuboBrillo = Sprite.Create(t2, new Rect(0, 0, W, H), new Vector2(0.5f, 0.5f), 32f, 0, SpriteMeshType.FullRect);
    }

    // Tubo horizontal (largo) o vertical (rot = 90). Devuelve la raiz; agrega zona de cauterizacion alrededor.
    Transform ConstruirTubo(string nombre, float x, float y, float largo, float rot, Vector2 zonaTam, Vector2 zonaOffset, float maxCalor)
    {
        SpritesTubo();
        var raiz = Hijo(nombre, new Vector2(x, y));
        var cuerpo = Hijo("Cuerpo", new Vector2(x, y), raiz);
        cuerpo.localEulerAngles = new Vector3(0f, 0f, rot);
        var sr = SR(cuerpo, sprTubo, 2);
        sr.drawMode = SpriteDrawMode.Tiled; sr.size = new Vector2(largo, 0.5f);
        var brillo = Hijo("Brillo", new Vector2(x, y), raiz);
        brillo.localEulerAngles = new Vector3(0f, 0f, rot);
        var sb = SR(brillo, sprTuboBrillo, 3);
        sb.drawMode = SpriteDrawMode.Tiled; sb.size = new Vector2(largo, 0.5f);
        sb.color = new Color(1f, 1f, 1f, 0f);

        var luzT = Hijo("Luz", new Vector2(x, y + (rot == 0f ? 0.5f : 0f)), raiz).gameObject.AddComponent<Light2D>();
        luzT.lightType = Light2D.LightType.Point; luzT.color = new Color(1f, 0.55f, 0.15f);
        luzT.intensity = 0.25f; luzT.pointLightOuterRadius = 4f; luzT.pointLightInnerRadius = 0.3f;

        var zona = Hijo("ZonaCauterizacion", new Vector2(x + zonaOffset.x, y + zonaOffset.y), raiz);
        var col = zona.gameObject.AddComponent<BoxCollider2D>();
        col.isTrigger = true; col.size = zonaTam;
        try { zona.gameObject.tag = "ZonaCauterizacion"; } catch { }
        var tc = zona.gameObject.AddComponent<TuboCauterizador>();
        tc.luz = luzT; tc.brillo = sb; tc.cuerpo = sr; tc.max = maxCalor;
        return raiz;
    }

    void CrearTubo(float x)
    {
        // Tubo vertical pegado a la izquierda, con tapa y base
        ConstruirTubo("TuboCauterizador", x, 1.3f, 2.6f, 90f, new Vector2(1.7f, 2.4f), Vector2.zero, 3.6f);
        var tapa = Hijo("Tapa", new Vector2(x, 2.68f), null);
        SR(tapa, sprTubo, 4).drawMode = SpriteDrawMode.Tiled;
        tapa.GetComponent<SpriteRenderer>().size = new Vector2(0.9f, 0.3f);
        var base_ = Hijo("BaseTubo", new Vector2(x, 0.12f), null);
        var sbs = SR(base_, sprTubo, 4); sbs.drawMode = SpriteDrawMode.Tiled; sbs.size = new Vector2(1.2f, 0.25f);
    }

    class TuboCauterizador : MonoBehaviour
    {
        public Light2D luz; public SpriteRenderer brillo, cuerpo; public float max = 3.6f;
        float calor, ultimaVez = -10f, tRestaurar; bool avisado;
        SpriteRenderer srJ; Color orig = Color.white; SistemaSangre sangre;
        Transform pj;

        void OnTriggerStay2D(Collider2D o)
        {
            if (!o.CompareTag("Player")) return;
            ultimaVez = Time.time;
            pj = o.transform;
            if (srJ == null) { srJ = o.GetComponentInChildren<SpriteRenderer>(); if (srJ != null) orig = srJ.color; }
            if (sangre == null) sangre = o.GetComponent<SistemaSangre>();
        }

        void Update()
        {
            if (pj == null && ArenaJefe.Instancia != null) pj = ArenaJefe.Instancia.jugador;
            bool dentro = Time.time - ultimaVez < 0.12f;
            if (tRestaurar > 0f)
            {
                tRestaurar -= Time.deltaTime;
                if (tRestaurar <= 0f && srJ != null) srJ.color = orig;
                calor = 0f; avisado = false;
            }
            else
            {
                calor = Mathf.Clamp(calor + (dentro ? Time.deltaTime : -Time.deltaTime * 1.5f), 0f, max);
                if (srJ != null)
                    srJ.color = calor > 0.15f ? Color.Lerp(orig, new Color(1f, 0.3f, 0.05f, 1f), calor / max * 0.9f) : orig;
                if (calor > max * 0.6f && !avisado) { avisado = true; UIMensaje.Mostrar("¡Te estás quemando! Sal del tubo", 1.5f); }
                if (calor < max * 0.15f) avisado = false;
                if (calor >= max)
                {
                    calor = 0f; tRestaurar = 2f;
                    UIMensaje.Mostrar("El tubo te calcinó...", 2f);
                    if (sangre != null) sangre.RecibirDano(9999f);
                    else if (GestorJuego.Instancia != null) GestorJuego.Instancia.GameOver();
                }
            }

            // Se ilumina al acercarte (y mas aun mientras te quemas)
            float cerca = 0f;
            if (pj != null)
            {
                var c = GetComponent<Collider2D>();
                float d = ((Vector2)c.bounds.ClosestPoint(pj.position) - (Vector2)pj.position).magnitude;
                cerca = Mathf.Clamp01(1f - d / 6f);
            }
            float k = Mathf.Clamp01(cerca * 0.85f + calor / max * 0.5f);
            float parpadeo = 0.9f + 0.1f * Mathf.Sin(Time.time * 11f);
            if (brillo != null) brillo.color = new Color(1f, 1f, 1f, k * parpadeo);
            if (cuerpo != null) cuerpo.color = Color.Lerp(Color.white, new Color(1f, 0.72f, 0.55f), k * 0.6f);
            if (luz != null) luz.intensity = 0.25f + k * 1.9f * parpadeo;
        }
    }

    void Plataforma(float x, float y, float w)
    {
        // Plataforma = tubo cauterizador: caminas encima (de una via) y el calor te sana / te quema si te quedas
        var raiz = ConstruirTubo("PlataformaTubo", x, y, w, 0f, new Vector2(w - 0.3f, 0.9f), new Vector2(0f, 0.65f), 4.5f);
        var cuerpo = raiz.GetChild(0);
        var c = raiz.gameObject.AddComponent<BoxCollider2D>();
        c.size = new Vector2(w, 0.35f);
        c.usedByEffector = true;
        var ef = raiz.gameObject.AddComponent<PlatformEffector2D>();
        ef.useOneWay = true; ef.surfaceArc = 170f;
    }

    void Construir()
    {
        // Fondo (la linea del piso de la ilustracion queda en y = 0)
        var texF = Resources.Load<Texture2D>("Arena_Fundicion");
        sprFondo = Crear(texF, texF.width / ANCHO, new Vector2(0.5f, 0.5f), FilterMode.Bilinear);
        SR(Hijo("Fondo", new Vector2(X0, CENTRO_Y)), sprFondo, -50);

        // Suelo, paredes y limites de camara
        Caja("Suelo", new Vector2(X0, -1f), new Vector2(ANCHO + 4f, 2f));
        Caja("ParedIzq", new Vector2(X0 - ANCHO / 2f - 0.5f, 5f), new Vector2(1f, 30f));
        Caja("ParedDer", new Vector2(X0 + ANCHO / 2f + 0.5f, 5f), new Vector2(1f, 30f));
        var lim = Hijo("LimitesCamara", new Vector2(X0, CENTRO_Y));
        var lc = lim.gameObject.AddComponent<BoxCollider2D>();
        lc.size = new Vector2(ANCHO, ALTO); lc.isTrigger = true; limites = lc;

        // Plataformas para esquivar
        Plataforma(X0 - 6.5f, 1.5f, 4f);
        Plataforma(X0 - 1.5f, 3.0f, 4f);
        CrearTubo(X0 - 9.8f);

        // Demonio de fuego (mira a la izquierda en el dibujo)
        aIdle = Tira("Idle", 8); aGarra = Tira("Attack", 10); aFuego = Tira("Attack2", 12);
        aSlide = Tira("Slide", 8); aMuerte = Tira("Defeat", 16);
        bx = X0 + 7f;
        bossT = Hijo("Demonio", new Vector2(bx, 0f));
        srBoss = SR(bossT, aIdle[0], 3);

        luzJefe = Hijo("LuzJefe", new Vector2(bx, 2f), bossT).gameObject.AddComponent<Light2D>();
        luzJefe.lightType = Light2D.LightType.Point;
        luzJefe.color = new Color(1f, 0.55f, 0.15f);
        luzJefe.intensity = 1.1f; luzJefe.pointLightOuterRadius = 9f; luzJefe.pointLightInnerRadius = 1f;

        // Luz de la arena
        var la = Hijo("LuzArena", new Vector2(X0, 4f));
        var l1 = la.gameObject.AddComponent<Light2D>();
        l1.lightType = Light2D.LightType.Point;
        l1.color = new Color(1f, 0.86f, 0.75f);
        l1.intensity = 1.0f; l1.pointLightOuterRadius = 22f; l1.pointLightInnerRadius = 8f;

        sprBola = CrearBola();
        sprChispa = CrearChispa();
        sprLata = LanzadorLatas.SpriteLata();
        sprVenda = CrearVenda();

        ConstruirUI();
        gameObject.SetActive(true);
    }

    Sprite CrearBola()
    {
        const int N = 32;
        var tex = new Texture2D(N, N, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
        for (int y = 0; y < N; y++)
            for (int x = 0; x < N; x++)
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(N / 2f, N / 2f)) / (N / 2f);
                Color c;
                if (d > 1f) c = new Color(0, 0, 0, 0);
                else if (d < 0.35f) c = new Color(1f, 0.95f, 0.6f, 1f);
                else if (d < 0.65f) c = new Color(1f, 0.6f, 0.1f, 1f);
                else c = new Color(0.85f, 0.2f, 0.03f, 1f - (d - 0.65f) * 1.2f);
                tex.SetPixel(x, y, c);
            }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, N, N), new Vector2(0.5f, 0.5f), 32f);
    }

    Sprite CrearVenda() { return ObjetosArte.Venda(); }

    void ConstruirUI()
    {
        uiRaiz = new GameObject("UI_Jefe");
        uiRaiz.transform.SetParent(transform, false);
        var canvas = uiRaiz.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 800;

        var fondo = new GameObject("Fondo");
        fondo.transform.SetParent(uiRaiz.transform, false);
        var im = fondo.AddComponent<Image>(); im.color = new Color(0f, 0f, 0f, 0.7f); im.raycastTarget = false;
        var rt = im.rectTransform; rt.anchorMin = new Vector2(0.25f, 0.92f); rt.anchorMax = new Vector2(0.75f, 0.95f);
        rt.offsetMin = rt.offsetMax = Vector2.zero;

        var rel = new GameObject("Relleno");
        rel.transform.SetParent(fondo.transform, false);
        var ir = rel.AddComponent<Image>(); ir.color = new Color(0.95f, 0.4f, 0.05f, 1f); ir.raycastTarget = false;
        barraRelleno = ir.rectTransform;
        barraRelleno.anchorMin = new Vector2(0.005f, 0.12f); barraRelleno.anchorMax = new Vector2(0.995f, 0.88f);
        barraRelleno.offsetMin = barraRelleno.offsetMax = Vector2.zero;

        var tx = new GameObject("Nombre");
        tx.transform.SetParent(uiRaiz.transform, false);
        var tt = tx.AddComponent<Text>();
        var ui = BancoUI.Actual;
        tt.font = ui != null && ui.fuente != null ? ui.fuente : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        tt.text = "DEMONIO DE FUEGO"; tt.fontSize = 22; tt.alignment = TextAnchor.LowerCenter;
        tt.color = new Color(1f, 0.85f, 0.7f, 1f); tt.raycastTarget = false;
        var trt = tt.rectTransform; trt.anchorMin = new Vector2(0.25f, 0.95f); trt.anchorMax = new Vector2(0.75f, 0.99f);
        trt.offsetMin = trt.offsetMax = Vector2.zero;
        uiRaiz.SetActive(false);
    }

    // ── Entrada ──────────────────────────────────────────────────────────────
    void Entrar(Transform j)
    {
        jugador = j;
        sangre = j.GetComponent<SistemaSangre>();
        rbJ = j.GetComponent<Rigidbody2D>();

        // Devolver al jugador a la normalidad (la caida lo habia congelado)
        foreach (var c in j.GetComponents<Collider2D>()) c.enabled = true;
        if (rbJ != null) { rbJ.bodyType = RigidbodyType2D.Dynamic; rbJ.linearVelocity = Vector2.zero; }
        var mov = j.GetComponent<MovimientoJugador>(); if (mov != null) mov.enabled = true;
        if (sangre != null) { sangre.enabled = true; sangre.ResetearSangre(); }
        var sr = j.GetComponentInChildren<SpriteRenderer>(); if (sr != null) sr.color = Color.white;

        Vector3 entrada = new Vector3(X0 - 11f, 0.9f, 0f);
        j.position = entrada;

        var cam = Camera.main != null ? Camera.main.GetComponent<CamaraSeguimiento>() : null;
        if (cam != null) { cam.limites = limites; cam.AjustarIntensidad(2.5f); }

        if (GestorJuego.Instancia != null) GestorJuego.Instancia.GuardarCheckpoint(entrada);

        var inv = j.GetComponent<InventarioJugador>();
        if (inv == null) inv = j.gameObject.AddComponent<InventarioJugador>();
        if (inv.latas < 4) inv.AgregarLata(4 - inv.latas);

        uiRaiz.SetActive(true);
        ReiniciarPelea();
        UIMensaje.Mostrar("Algo arde al fondo. Esquiva sus ataques saltando; cuando quede aturdido, lanzale latas (Q) al pecho.", 7f);
    }

    void ReiniciarPelea()
    {
        vida = vidaMax; est = Est.Dormido; t = 0f; tDorm = 0f;
        tCan = 3f; tVenda = 8f; tMuerte = 0f; tFlash = 0f; tDanio = 0f;
        furiaHecha = false; tinte = Color.white; ultimoAtaque = -1;
        bossT.position = new Vector3(bx, 0f, 0f);
        dir = -1f;
        foreach (var b in bolas) if (b.t != null) Destroy(b.t.gameObject);
        bolas.Clear();
        foreach (var g in pickups) if (g != null) Destroy(g);
        pickups.Clear();
        Iniciar(aIdle, 3f, true);
        for (int i = 0; i < 3; i++) SoltarPickup(true, X0 - 10f + i * 3.5f);
        SoltarPickup(false, X0 - 3f);
        ActualizarBarra();
    }

    // ── Animacion ────────────────────────────────────────────────────────────
    void Iniciar(Sprite[] c, float fps, bool loop)
    {
        clip = c; fpsClip = fps; loopClip = loop; tClip = 0f;
        if (srBoss != null && c != null && c.Length > 0) srBoss.sprite = c[0];
    }

    void Pintar(float dt)
    {
        if (clip == null || clip.Length == 0) return;
        tClip += dt;
        int i = Mathf.FloorToInt(tClip * fpsClip);
        i = loopClip ? i % clip.Length : Mathf.Min(i, clip.Length - 1);
        srBoss.sprite = clip[i];
        srBoss.flipX = dir > 0f;            // el dibujo mira a la izquierda

        Color c = tinte;
        if (Vulnerable) { float k = 0.55f + 0.45f * Mathf.Abs(Mathf.Sin(Time.time * 9f)); c = new Color(1f, k, k * 0.8f, 1f); }
        srBoss.color = c;
        luzJefe.intensity = est == Est.Muriendo ? Mathf.Lerp(1.1f, 0f, tMuerte / 3f)
                           : 1.1f + 0.25f * Mathf.Sin(Time.time * 12f) + (est == Est.Fuego ? 0.6f : 0f);
    }

    // ── Bucle ────────────────────────────────────────────────────────────────
    void Update()
    {
        if (jugador == null) return;
        bool go = GestorJuego.Instancia != null && GestorJuego.Instancia.EstaEnGameOver;
        if (!go && goPrev) ReiniciarPelea();
        goPrev = go;
        if (go) return;

        float dt = Time.deltaTime;
        Logica(dt);
        Pintar(dt);
    }

    void MirarAlJugador() { dir = jugador.position.x < PosX ? -1f : 1f; }

    void Cambiar(Est nuevo) { est = nuevo; t = 0f; golpeoGarra = false; disparoHecho = false; tocoSlide = false; }

    void Logica(float dt)
    {
        tDanio -= dt;
        t += dt;

        if (est == Est.Muriendo) { Morir(dt); return; }

        if (est == Est.Dormido)
        {
            tDorm += dt;
            if (jugador.position.x > X0 - 6.5f || tDorm > 12f)
            {
                Cambiar(Est.Despertando);
                AudioAuto.Rugido(Pecho, true);
            }
            ActualizarPickups(dt);
            return;
        }

        MirarSiLibre();

        switch (est)
        {
            case Est.Despertando:
                Iniciar0(aIdle, 8f, true);
                if (t >= 1.6f) { Cambiar(Est.Acechar); }
                break;

            case Est.Acechar:
                Iniciar0(aIdle, 6f, true);
                if (t >= 1.3f / fase) ElegirAtaque();
                break;

            case Est.Garra:
                if (!golpeoGarra && tClip >= 0.29f && tClip <= 0.6f)
                {
                    float dx = jugador.position.x - PosX;
                    bool delante = dx * dir > -0.6f && Mathf.Abs(dx) < 3.8f;
                    if (delante && jugador.position.y < 3.7f)
                    {
                        golpeoGarra = true;
                        Danar(danoGarra);
                        Empujar(dir, 8f);
                    }
                }
                if (tClip >= aGarra.Length / 12f + 0.15f) Cambiar(Est.Acechar);
                break;

            case Est.Fuego:
                if (!disparoHecho && tClip >= 0.6f)
                {
                    disparoHecho = true;
                    Disparar();
                    if (fase > 1f) Invoke(nameof(Disparar), 0.45f);
                }
                if (tClip >= aFuego.Length / 10f + 0.1f) { Cambiar(Est.Aturdido); t = 0f; stunDur = 1.8f; Iniciar(aIdle, 4f, true); }
                break;

            case Est.Deslizar:
                if (t < 0.5f)
                {
                    // aviso: se agacha, tiembla y se pone rojo
                    tinte = Color.Lerp(Color.white, new Color(1f, 0.35f, 0.25f), Mathf.PingPong(t * 8f, 1f));
                    bossT.position = new Vector3(PosX + Mathf.Sin(Time.time * 60f) * 0.04f, 0f, 0f);
                }
                else
                {
                    if (clip != aSlide) { Iniciar(aSlide, 14f, true); tinte = Color.white; }
                    float v = velocidadDeslizar * fase;
                    float nx = PosX + dir * v * dt;
                    float min = X0 - ANCHO / 2f + 3f, max = X0 + ANCHO / 2f - 3f;
                    bool llego = (dir < 0f && nx <= objetivoX) || (dir > 0f && nx >= objetivoX);
                    bool pared = nx < min || nx > max;
                    nx = Mathf.Clamp(nx, min, max);
                    bossT.position = new Vector3(nx, 0f, 0f);
                    if (!tocoSlide && Mathf.Abs(jugador.position.x - nx) < 1.7f && jugador.position.y < 3.0f)
                    {
                        tocoSlide = true;
                        Danar(danoDeslizar);
                        Empujar(dir, 10f);
                    }
                    if (llego || pared || t > 2f) { Cambiar(Est.Aturdido); stunDur = 3.0f; Iniciar(aIdle, 4f, true); AudioAuto.Golpe(); }
                }
                break;

            case Est.Aturdido:
                if (t >= stunDur) { Cambiar(Est.Acechar); tinte = Color.white; }
                break;
        }

        ActualizarBolas(dt);
        ComprobarLatas();
        ActualizarPickups(dt);
        ActualizarFlash(dt);
    }
    float stunDur = 2f;

    void Iniciar0(Sprite[] c, float fps, bool loop) { if (clip != c) Iniciar(c, fps, loop); }

    void MirarSiLibre()
    {
        // durante garra, fuego y deslizamiento mantiene la direccion que eligio
        if (est == Est.Acechar || est == Est.Despertando || est == Est.Aturdido) MirarAlJugador();
    }

    void ElegirAtaque()
    {
        MirarAlJugador();
        float d = Mathf.Abs(jugador.position.x - PosX);
        int a;
        if (d < 4.2f) a = Random.value < 0.7f ? 0 : 2;       // cerca: garra (a veces se desliza)
        else a = Random.value < 0.5f ? 1 : 2;                 // lejos: fuego o deslizamiento
        if (a == ultimoAtaque && Random.value < 0.6f) a = (a + 1) % 3;
        ultimoAtaque = a;

        switch (a)
        {
            case 0: Cambiar(Est.Garra); Iniciar(aGarra, 12f, false); break;
            case 1: Cambiar(Est.Fuego); Iniciar(aFuego, 10f, false); break;
            default:
                Cambiar(Est.Deslizar);
                Iniciar(aIdle, 10f, true);
                objetivoX = jugador.position.x + dir * 2.5f;
                break;
        }
    }

    // ── Dano al jefe ─────────────────────────────────────────────────────────
    void ComprobarLatas()
    {
        foreach (var l in FindObjectsByType<LataProyectil>(FindObjectsSortMode.None))
        {
            if (l == null) continue;
            if (Vector2.Distance(l.transform.position, Pecho) > 2.0f) continue;
            if (!Vulnerable) continue;
            Golpe(l.transform.position, 2.8f);
            Destroy(l.gameObject);
            AudioAuto.Golpe();
            vida -= 1f; tFlash = 0.15f;
            ActualizarBarra();
            if (vida <= 0f)
            {
                Cambiar(Est.Muriendo); tMuerte = 0f; Iniciar(aMuerte, 8f, false);
                AudioAuto.Rugido(Pecho, true);
            }
            else if (!furiaHecha && vida <= vidaMax * 0.5f)
            {
                furiaHecha = true;
                AudioAuto.Rugido(Pecho, true);
                UIMensaje.Mostrar("Se enfurece: ahora es mas rapido.", 3f);
            }
        }
    }

    void ActualizarFlash(float dt)
    {
        if (tFlash > 0f) { tFlash -= dt; tinte = new Color(1f, 0.4f, 0.3f, 1f); }
        else if (est != Est.Deslizar) tinte = Color.white;
    }

    void ActualizarBarra()
    {
        if (barraRelleno != null)
            barraRelleno.anchorMax = new Vector2(Mathf.Lerp(0.005f, 0.995f, Mathf.Clamp01(vida / vidaMax)), 0.88f);
    }

    void Morir(float dt)
    {
        tMuerte += dt;
        foreach (var b in bolas) if (b.t != null) Destroy(b.t.gameObject);
        bolas.Clear();
        tinte = Color.white;
        if (tMuerte > 4.2f && !final)
        {
            final = true;
            Despertar.Reproducir();
        }
    }

    // ── Ataque: bola de fuego ────────────────────────────────────────────────
    void Disparar()
    {
        if (est != Est.Fuego && est != Est.Aturdido) return;
        var go = new GameObject("BolaFuego");
        go.transform.position = new Vector3(PosX + dir * 1.4f, 1.7f, 0f);
        go.transform.localScale = Vector3.one * 1.3f;
        var sr = go.AddComponent<SpriteRenderer>(); sr.sprite = sprBola; sr.sortingOrder = 8;
        var luz = go.AddComponent<Light2D>();
        luz.lightType = Light2D.LightType.Point; luz.color = new Color(1f, 0.6f, 0.15f);
        luz.intensity = 1.2f; luz.pointLightOuterRadius = 4f;

        Vector2 desde = go.transform.position;
        Vector2 hasta = (Vector2)jugador.position + Vector2.up * 0.5f;
        Vector2 d = hasta - desde; d.y *= 0.35f;
        if (Mathf.Sign(d.x) != dir) d.x = dir * 4f;       // siempre sale hacia donde mira
        bolas.Add(new Bola { t = go.transform, v = d.normalized * velocidadBola * (fase > 1f ? 1.15f : 1f) });
        AudioAuto.Golpe();
    }

    void ActualizarBolas(float dt)
    {
        for (int i = bolas.Count - 1; i >= 0; i--)
        {
            var b = bolas[i];
            if (b.t == null) { bolas.RemoveAt(i); continue; }
            b.t.position += (Vector3)(b.v * dt);
            b.t.Rotate(0f, 0f, 360f * dt);
            b.vida -= dt;
            Vector2 pos = b.t.position;
            bool toca = Vector2.Distance(pos, (Vector2)jugador.position + Vector2.up * 0.6f) < 1.0f;
            bool suelo = pos.y <= 0.3f;
            bool fuera = pos.x < X0 - ANCHO / 2f - 1f || pos.x > X0 + ANCHO / 2f + 1f;
            if (toca) { Danar(danoBola); Empujar(Mathf.Sign(b.v.x), 6f); }
            if (toca || suelo || fuera || b.vida <= 0f)
            {
                Golpe(new Vector2(pos.x, Mathf.Max(pos.y, 0.3f)), 2.4f);
                Destroy(b.t.gameObject);
                bolas.RemoveAt(i);
            }
        }
    }

    void Danar(float cantidad)
    {
        if (tDanio > 0f || sangre == null) return;
        tDanio = 0.7f;
        sangre.RecibirDano(cantidad);
    }

    void Empujar(float sentido, float fuerza)
    {
        if (rbJ != null) rbJ.linearVelocity = new Vector2(sentido * fuerza, 7f);
    }

    // ── Objetos ──────────────────────────────────────────────────────────────
    void SoltarPickup(bool lata, float x, float y = 0.9f)
    {
        var go = new GameObject(lata ? "Lata_Arena" : "Venda_Arena");
        go.transform.position = new Vector3(x, y, 0f);
        go.transform.localScale = lata ? new Vector3(0.7f, 0.7f, 1f) : new Vector3(0.55f, 0.55f, 1f);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = lata ? sprLata : sprVenda; sr.sortingOrder = 5;
        var r = go.AddComponent<Recolectable>();
        r.tipo = lata ? Recolectable.Tipo.Lata : Recolectable.Tipo.Venda;
        r.curacion = 30f; r.radio = 1.3f;
        r.mensaje = lata ? "Lata. Lanzala (Q) al pecho del demonio cuando quede aturdido." : "Venda: recuperas sangre.";
        ObjetosArte.Asentar(go.transform);
        pickups.Add(go);
    }

    void ActualizarPickups(float dt)
    {
        for (int i = pickups.Count - 1; i >= 0; i--)
            if (pickups[i] == null || !pickups[i].activeSelf) { if (pickups[i] != null) Destroy(pickups[i]); pickups.RemoveAt(i); }

        int latas = 0, vendas = 0;
        foreach (var g in pickups) { if (g.name.StartsWith("Lata")) latas++; else vendas++; }

        tCan -= dt; tVenda -= dt;
        if (tCan <= 0f)
        {
            tCan = 4.2f;
            if (latas < 4)
            {
                // en distintos lugares: suelo, plataforma izquierda o plataforma alta, a ambos lados de la arena
                int lugar = Random.Range(0, 4);
                float x = lugar == 0 ? X0 - 6.5f + Random.Range(-1.5f, 1.5f)
                        : lugar == 1 ? X0 - 1.5f + Random.Range(-1.5f, 1.5f)
                        : X0 - 11f + Random.value * 22f;
                SoltarPickup(true, x, lugar == 0 ? 2.6f : lugar == 1 ? 4.1f : 0.9f);
            }
        }
        if (tVenda <= 0f) { tVenda = 14f; if (vendas < 1) SoltarPickup(false, X0 - 11f + Random.value * 12f); }
    }

    // ── Efecto de golpe (chispa de pixeles: centro brillante, rayos y anillo) ──
    Sprite CrearChispa()
    {
        const int N = 32;
        var tex = new Texture2D(N, N, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
        float c = (N - 1) / 2f;
        for (int y = 0; y < N; y++)
            for (int x = 0; x < N; x++)
            {
                float dx = x - c, dy = y - c, r = Mathf.Sqrt(dx * dx + dy * dy);
                Color col = new Color(0, 0, 0, 0);
                if (r < 4f) col = new Color(1f, 0.97f, 0.7f, 1f);
                else if (r > 8.5f && r < 10.5f) col = new Color(1f, 0.55f, 0.1f, 0.85f);
                for (int k = 0; k < 8 && col.a == 0f; k++)
                {
                    float a = k * Mathf.PI / 4f;
                    float along = dx * Mathf.Cos(a) + dy * Mathf.Sin(a);
                    float perp = Mathf.Abs(-dx * Mathf.Sin(a) + dy * Mathf.Cos(a));
                    float largo = (k % 2 == 0) ? 15f : 11f;
                    if (along > 3f && along < largo && perp < (k % 2 == 0 ? 1.3f : 0.9f))
                        col = Color.Lerp(new Color(1f, 0.9f, 0.4f, 1f), new Color(1f, 0.35f, 0.05f, 0.9f), along / largo);
                }
                tex.SetPixel(x, y, col);
            }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, N, N), new Vector2(0.5f, 0.5f), 16f);
    }

    class Chispa : MonoBehaviour
    {
        public float dur = 0.32f, tam0 = 0.5f, tam1 = 1.3f; float t; SpriteRenderer sr;
        void Awake() { sr = GetComponent<SpriteRenderer>(); }
        void Update()
        {
            t += Time.deltaTime; float k = t / dur;
            if (k >= 1f) { Destroy(gameObject); return; }
            float e = 1f - (1f - k) * (1f - k);
            transform.localScale = Vector3.one * Mathf.Lerp(tam0, tam1, e);
            transform.Rotate(0f, 0f, 90f * Time.deltaTime);
            var c = sr.color; c.a = 1f - k * k; sr.color = c;
        }
    }

    void Golpe(Vector2 pos, float tam)
    {
        var go = new GameObject("Golpe");
        go.transform.position = new Vector3(pos.x, pos.y, 0f);
        var sr = go.AddComponent<SpriteRenderer>(); sr.sprite = sprChispa; sr.sortingOrder = 10;
        var e = go.AddComponent<Chispa>(); e.tam0 = tam * 0.4f; e.tam1 = tam;
        var luz = go.AddComponent<Light2D>();
        luz.lightType = Light2D.LightType.Point; luz.color = new Color(1f, 0.7f, 0.25f);
        luz.intensity = 1.5f; luz.pointLightOuterRadius = tam * 1.6f;
    }

    void Destello(Vector2 pos, float tam, float dur, Color col)
    {
        var go = new GameObject("Destello");
        go.transform.position = new Vector3(pos.x, pos.y, 0f);
        go.transform.localScale = new Vector3(tam, tam * 0.6f, 1f);
        var sr = go.AddComponent<SpriteRenderer>(); sr.sprite = JugadorRef.SpriteBlanco(); sr.color = col; sr.sortingOrder = 9;
        Destroy(go, dur);
    }
}
