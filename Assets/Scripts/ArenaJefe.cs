using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Pelea final contra La Masa. Se construye por codigo lejos del nivel (sin tocar escenas).
/// - La Masa no se mueve. Solo recibe dano cuando abre la boca: las latas (Q) apuntan solas a la garganta.
/// - Ataque de manos: una pila de manos sale del piso donde estas parado (avisa antes).
/// - Escupe chatarra al rojo vivo en arco hacia ti mientras tiene la boca abierta.
/// - Al morir, pasa a la EscenaFinal. Si mueres, reapareces en la entrada y la pelea se reinicia.
/// </summary>
public class ArenaJefe : MonoBehaviour
{
    public static ArenaJefe Instancia { get; private set; }

    // ── Medidas del mundo ────────────────────────────────────────────────────
    public const float X0 = 400f;       // centro de la arena (lejos del Nivel 2)
    const float ANCHO = 26f, ALTO = 14.5f;
    const float ALTO_JEFE = 6.6f;

    [Header("Jefe")]
    public float vidaMax = 10f;
    public float vida;
    public float duracionCerrada = 4.5f;
    public float duracionAbierta = 4.2f;
    public float intervaloManos = 5.5f;
    public float intervaloChatarra = 1.5f;
    public float danoManos = 22f;
    public float danoChatarra = 18f;

    enum Est { Dormido, Despertando, Cerrada, Abriendo, Abierta, Cerrando, Muriendo }
    Est est = Est.Dormido;
    float t;

    BancoJefe b;
    Sprite sprCerrado, sprAbierto, sprBrazos, sprEnojada, sprMuerte, sprManos, sprLata, sprVenda, sprSalp;
    Sprite[] sprChatarra;
    Transform bossT;
    SpriteRenderer srCerrado, srAbierto, srBrazos, srEnojada, srMuerte;
    float aAb, aBr, aEn, aMu, tEnojo, tRugido, tDorm, bxPos, byPos;
    bool furiaHecha;
    Color tinte = Color.white;
    Light2D luzBoca;
    Vector2 posBoca;
    Collider2D limites;
    RectTransform barraRelleno;
    GameObject uiRaiz;

    Transform jugador;
    SistemaSangre sangre;
    Rigidbody2D rbJ;

    float tManos, tChatarra, tCan, tVenda, tDanio, tFlash, tMuerte, tAbrir;
    bool goPrev, final;
    float fase => vida <= vidaMax * 0.5f ? 1.35f : 1f;   // mas rapido a media vida

    class Proy { public Transform t; public Vector2 v; public float vida = 4f; }
    class Mano { public Transform t; public Transform aviso; public float e; public bool golpeo; }
    readonly List<Proy> proys = new List<Proy>();
    readonly List<Mano> manos = new List<Mano>();
    readonly List<GameObject> pickups = new List<GameObject>();

    /// <summary>Punto al que apuntan las latas (la garganta). Null si no hay pelea activa.</summary>
    public Vector2? ObjetivoLata =>
        (est == Est.Cerrada || est == Est.Abriendo || est == Est.Abierta || est == Est.Cerrando)
            ? (Vector2?)posBoca : null;

    // ── API estatica ─────────────────────────────────────────────────────────
    public static bool Disponible()
    {
        var bj = Resources.Load<BancoJefe>("BancoJefe");
        return bj != null && bj.arena != null && bj.cerrado != null && bj.abierto != null && bj.brazos != null
            && bj.enojada != null && bj.muerte != null && bj.chatarra != null && bj.manos != null;
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
        b = Resources.Load<BancoJefe>("BancoJefe");
        if (b == null) return;
        Construir();
    }

    Sprite Crear(Texture2D tex, float ppu, Vector2 piv)
    {
        tex.filterMode = FilterMode.Bilinear;
        return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), piv, ppu, 0, SpriteMeshType.FullRect);
    }

    Transform Hijo(string nombre, Vector2 pos, Transform padre = null)
    {
        var go = new GameObject(nombre);
        go.transform.SetParent(padre != null ? padre : transform, false);
        go.transform.position = new Vector3(pos.x, pos.y, 0f);
        return go.transform;
    }

    SpriteRenderer SR(Transform t, Sprite s, int orden)
    {
        var sr = t.gameObject.AddComponent<SpriteRenderer>();
        sr.sprite = s; sr.sortingOrder = orden;
        return sr;
    }

    void Caja(string nombre, Vector2 centro, Vector2 tam)
    {
        var tr = Hijo(nombre, centro);
        var c = tr.gameObject.AddComponent<BoxCollider2D>();
        c.size = tam;
    }

    void Plataforma(float x, float y, float w)
    {
        var tr = Hijo("Plataforma", new Vector2(x, y));
        var sr = SR(tr, JugadorRef.SpriteBlanco(), 1);
        sr.color = new Color(0.30f, 0.20f, 0.15f, 1f);
        tr.localScale = new Vector3(w, 0.35f, 1f);
        var c = tr.gameObject.AddComponent<BoxCollider2D>();
        c.usedByEffector = true;
        var ef = tr.gameObject.AddComponent<PlatformEffector2D>();
        ef.useOneWay = true; ef.surfaceArc = 170f;
        var bor = Hijo("Borde", new Vector2(x, y + 0.16f));
        var sb = SR(bor, JugadorRef.SpriteBlanco(), 2);
        sb.color = new Color(0.62f, 0.34f, 0.18f, 1f);
        bor.localScale = new Vector3(w, 0.06f, 1f);
    }

    void Construir()
    {
        // Fondo (la linea del piso de la ilustracion queda en y = 0)
        float ppuF = b.arena.width / ANCHO;
        sprFondo = Crear(b.arena, ppuF, new Vector2(0.5f, 0.5f));
        var f = Hijo("Fondo", new Vector2(X0, 2.9f));
        SR(f, sprFondo, -50);

        // Suelo, paredes y limites de camara
        Caja("Suelo", new Vector2(X0, -1f), new Vector2(ANCHO + 4f, 2f));
        Caja("ParedIzq", new Vector2(X0 - ANCHO / 2f - 0.5f, 5f), new Vector2(1f, 30f));
        Caja("ParedDer", new Vector2(X0 + ANCHO / 2f + 0.5f, 5f), new Vector2(1f, 30f));
        var lim = Hijo("LimitesCamara", new Vector2(X0, 2.9f));
        var lc = lim.gameObject.AddComponent<BoxCollider2D>();
        lc.size = new Vector2(ANCHO, ALTO); lc.isTrigger = true; limites = lc;

        // Plataformas para esquivar
        Plataforma(X0 - 6.5f, 1.5f, 4f);
        Plataforma(X0 - 1.5f, 3.0f, 4f);

        // Jefe: varias poses del mismo tamano superpuestas; las de encima aparecen con fundido
        float ppuB = b.cerrado.height / ALTO_JEFE;
        sprCerrado = Crear(b.cerrado, ppuB, new Vector2(0.5f, 0f));
        sprAbierto = Crear(b.abierto, ppuB, new Vector2(0.5f, 0f));
        sprBrazos  = Crear(b.brazos,  ppuB, new Vector2(0.5f, 0f));
        sprEnojada = Crear(b.enojada, ppuB, new Vector2(0.5f, 0f));
        sprMuerte  = Crear(b.muerte,  ppuB, new Vector2(0.5f, 0f));
        float ancho = b.cerrado.width / ppuB;
        bxPos = X0 + 7.4f; byPos = -0.9f;
        float bx = bxPos;
        bossT = Hijo("Jefe", new Vector2(bx, byPos));
        srCerrado = SR(Hijo("Cerrado", new Vector2(bx, byPos), bossT), sprCerrado, 0);
        srAbierto = SR(Hijo("Abierto", new Vector2(bx, byPos), bossT), sprAbierto, 1);
        srBrazos  = SR(Hijo("Brazos",  new Vector2(bx, byPos), bossT), sprBrazos, 2);
        srEnojada = SR(Hijo("Enojada", new Vector2(bx, byPos), bossT), sprEnojada, 3);
        srMuerte  = SR(Hijo("Muerte",  new Vector2(bx, byPos), bossT), sprMuerte, 4);
        var cuerpoT = Hijo("Cuerpo", new Vector2(bx, byPos), bossT);
        var pc = cuerpoT.gameObject.AddComponent<PolygonCollider2D>();
        pc.points = new[] {
            new Vector2(-5.4f, 0.9f), new Vector2(-3.2f, 2.2f), new Vector2(-1.4f, 4.1f),
            new Vector2(1.4f, 4.1f), new Vector2(3.2f, 2.2f), new Vector2(5.4f, 0.9f) };

        // La garganta (centro del fuego en la imagen con la boca abierta)
        posBoca = new Vector2(bx + 0.01f * ancho, byPos + ALTO_JEFE * (1f - 0.257f));

        // Luces: ambiente de la arena y fuego de la boca
        var la = Hijo("LuzArena", new Vector2(X0, 4f));
        var l1 = la.gameObject.AddComponent<Light2D>();
        l1.lightType = Light2D.LightType.Point;
        l1.color = new Color(1f, 0.86f, 0.75f);
        l1.intensity = 1.1f; l1.pointLightOuterRadius = 22f; l1.pointLightInnerRadius = 8f;
        var lb = Hijo("LuzBoca", posBoca);
        luzBoca = lb.gameObject.AddComponent<Light2D>();
        luzBoca.lightType = Light2D.LightType.Point;
        luzBoca.color = new Color(1f, 0.55f, 0.15f);
        luzBoca.intensity = 0f; luzBoca.pointLightOuterRadius = 10f;

        var chs = new List<Sprite>();
        foreach (var tx in new[] { b.chatarra, b.chatarra2, b.chatarra3 })
            if (tx != null) chs.Add(Crear(tx, tx.width / 1.8f, new Vector2(0.5f, 0.5f)));
        sprChatarra = chs.ToArray();
        if (b.salpicadura != null) sprSalp = Crear(b.salpicadura, b.salpicadura.width / 1.6f, new Vector2(0.5f, 0.5f));
        sprManos = Crear(b.manos, b.manos.width / 3.6f, new Vector2(0.5f, 0f));
        sprLata = LanzadorLatas.SpriteLata();
        sprVenda = CrearVenda();

        ConstruirUI();
        gameObject.SetActive(true);
    }
    Sprite sprFondo;

    Sprite CrearVenda()
    {
        const int N = 10;
        var tex = new Texture2D(N, N, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
        for (int y = 0; y < N; y++)
            for (int x = 0; x < N; x++)
            {
                bool borde = x == 0 || y == 0 || x == N - 1 || y == N - 1;
                bool cruz = (x >= 4 && x <= 5) || (y >= 4 && y <= 5);
                Color c = borde ? new Color(0.35f, 0.3f, 0.3f) : cruz ? new Color(0.8f, 0.08f, 0.1f) : new Color(0.92f, 0.9f, 0.84f);
                tex.SetPixel(x, y, c);
            }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, N, N), new Vector2(0.5f, 0.5f), 12f);
    }

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
        var ir = rel.AddComponent<Image>(); ir.color = new Color(0.75f, 0.08f, 0.08f, 1f); ir.raycastTarget = false;
        barraRelleno = ir.rectTransform;
        barraRelleno.anchorMin = new Vector2(0.005f, 0.12f); barraRelleno.anchorMax = new Vector2(0.995f, 0.88f);
        barraRelleno.offsetMin = barraRelleno.offsetMax = Vector2.zero;

        var tx = new GameObject("Nombre");
        tx.transform.SetParent(uiRaiz.transform, false);
        var tt = tx.AddComponent<Text>();
        tt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        tt.text = "LA MASA"; tt.fontSize = 22; tt.alignment = TextAnchor.LowerCenter;
        tt.color = new Color(1f, 0.85f, 0.8f, 1f); tt.raycastTarget = false;
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
        UIMensaje.Mostrar("Algo enorme duerme al fondo. Cuando abra la boca, lanzale latas (Q) a la garganta. Cuidado con las manos del piso.", 7f);
    }

    void ReiniciarPelea()
    {
        vida = vidaMax; est = Est.Dormido; t = 0f; tDorm = 0f;
        tManos = 999f; tChatarra = 1f; tCan = 3f; tVenda = 8f; tMuerte = 0f; tAbrir = 0f; tFlash = 0f;
        aAb = aBr = aEn = aMu = 0f; tEnojo = tRugido = 0f; furiaHecha = false; tinte = Color.white;
        bossT.position = new Vector3(bxPos, byPos, 0f);
        foreach (var p in proys) if (p.t != null) Destroy(p.t.gameObject);
        proys.Clear();
        foreach (var m in manos) { if (m.t != null) Destroy(m.t.gameObject); if (m.aviso != null) Destroy(m.aviso.gameObject); }
        manos.Clear();
        foreach (var g in pickups) if (g != null) Destroy(g);
        pickups.Clear();
        luzBoca.intensity = 0f;
        Pintar(0f);
        for (int i = 0; i < 3; i++) SoltarPickup(true, X0 - 10f + i * 3.5f);
        SoltarPickup(false, X0 - 3f);
        ActualizarBarra();
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

    void Logica(float dt)
    {
        tDanio -= dt;
        Respirar();

        if (est == Est.Muriendo) { Morir(dt); return; }

        if (est == Est.Dormido)
        {
            tDorm += dt;
            if (jugador.position.x > X0 - 6.5f || tDorm > 12f)
            {
                est = Est.Despertando; t = 1.9f; tRugido = 1.9f;
                AudioAuto.Rugido(posBoca, true);
            }
            ActualizarPickups(dt);
            return;
        }
        if (est == Est.Despertando)
        {
            t -= dt;
            luzBoca.intensity = 1.6f * Mathf.Clamp01(1f - t / 1.9f);
            if (t <= 0f) { est = Est.Cerrada; t = duracionCerrada; tManos = 0.5f; luzBoca.intensity = 0f; }
            ActualizarPickups(dt);
            return;
        }

        t -= dt;
        switch (est)
        {
            case Est.Cerrada:
                tManos -= dt;
                if (tManos <= 0f)
                {
                    tManos = 999f;
                    SoltarManos(jugador.position.x);
                    if (fase > 1f) SoltarManos(jugador.position.x + (Random.value < 0.5f ? -3.8f : 3.8f));
                }
                if (t <= 0f) { est = Est.Abriendo; tAbrir = 0f; AudioAuto.Rugido(posBoca, true); }
                break;
            case Est.Abriendo:
                tAbrir += dt;
                float a = Mathf.Clamp01(tAbrir / 0.5f);
                aAb = a;
                luzBoca.intensity = 2.2f * a;
                if (a >= 1f) { est = Est.Abierta; t = duracionAbierta / fase; tChatarra = 0.8f; }
                break;
            case Est.Abierta:
                tChatarra -= dt;
                if (tChatarra <= 0f) { Escupir(); tChatarra = intervaloChatarra / fase; }
                luzBoca.intensity = 2f + 0.5f * Mathf.Sin(Time.time * 14f) + 0.2f * Mathf.Sin(Time.time * 31f);
                if (t <= 0f) { est = Est.Cerrando; tAbrir = 0.5f; }
                break;
            case Est.Cerrando:
                tAbrir -= dt;
                float c = Mathf.Clamp01(tAbrir / 0.5f);
                aAb = c;
                luzBoca.intensity = 2.2f * c;
                if (c <= 0f) { est = Est.Cerrada; t = duracionCerrada / fase; tManos = 0.6f; }
                break;
        }

        ActualizarManos(dt);
        ActualizarProyectiles(dt);
        ComprobarLatas();
        ActualizarPickups(dt);
        ActualizarFlash(dt);
    }

    /// <summary>Aplica fundidos y color a las poses.</summary>
    void Pintar(float dt)
    {
        tEnojo -= dt; tRugido -= dt;
        aBr = Mathf.MoveTowards(aBr, tEnojo > 0f ? 1f : 0f, dt / 0.3f);
        aEn = Mathf.MoveTowards(aEn, tRugido > 0f ? 1f : 0f, dt / 0.2f);
        aMu = Mathf.MoveTowards(aMu, est == Est.Muriendo ? 1f : 0f, dt / 1.6f);
        srCerrado.color = new Color(tinte.r, tinte.g, tinte.b, aMu >= 1f ? 0f : 1f);
        srAbierto.color = new Color(tinte.r, tinte.g, tinte.b, aAb);
        srBrazos.color  = new Color(tinte.r, tinte.g, tinte.b, aBr);
        srEnojada.color = new Color(tinte.r, tinte.g, tinte.b, aEn);
        srMuerte.color  = new Color(tinte.r, tinte.g, tinte.b, aMu);
    }

    void Respirar()
    {
        float vel = est == Est.Dormido ? 1.1f : 1.7f;
        float s2 = 1f + 0.008f * Mathf.Sin(Time.time * vel);
        if (est == Est.Dormido) s2 = 1f + 0.012f * Mathf.Sin(Time.time * vel);
        bossT.localScale = new Vector3(1f, s2, 1f);
        // temblor al rugir, al abrir la boca y cuando levanta los brazos
        float sh = (est == Est.Abriendo || est == Est.Abierta) ? 0.04f : 0f;
        if (tRugido > 0f) sh = 0.12f;
        else if (tEnojo > 0f) sh = 0.07f;
        var cap = new[] { srCerrado, srAbierto, srBrazos, srEnojada, srMuerte };
        foreach (var sr in cap)
            sr.transform.localPosition = new Vector3(Mathf.Sin(Time.time * 47f) * sh, 0f, 0f);
    }

    void ActualizarFlash(float dt)
    {
        if (tFlash > 0f) { tFlash -= dt; tinte = new Color(1f, 0.45f, 0.4f, 1f); }
        else if (est != Est.Muriendo) tinte = Color.white;
    }

    // ── Dano al jefe ─────────────────────────────────────────────────────────
    void ComprobarLatas()
    {
        bool vulnerable = est == Est.Abierta || (est == Est.Abriendo && tAbrir > 0.3f);
        foreach (var l in FindObjectsByType<LataProyectil>(FindObjectsSortMode.None))
        {
            if (l == null) continue;
            if (Vector2.Distance(l.transform.position, posBoca) > 1.7f) continue;
            if (!vulnerable) continue;
            Destello(l.transform.position, 1.6f, 0.25f, new Color(1f, 0.7f, 0.2f, 1f));
            Destroy(l.gameObject);
            AudioAuto.Golpe();
            vida -= 1f; tFlash = 0.15f;
            ActualizarBarra();
            if (vida <= 0f) { est = Est.Muriendo; tMuerte = 0f; AudioAuto.Rugido(posBoca, true); }
            else if (!furiaHecha && vida <= vidaMax * 0.5f)
            {
                furiaHecha = true; tRugido = 1.4f;
                AudioAuto.Rugido(posBoca, true);
            }
        }
    }

    void ActualizarBarra()
    {
        if (barraRelleno != null)
            barraRelleno.anchorMax = new Vector2(Mathf.Lerp(0.005f, 0.995f, Mathf.Clamp01(vida / vidaMax)), 0.88f);
    }

    void Morir(float dt)
    {
        tMuerte += dt;
        foreach (var p in proys) if (p.t != null) Destroy(p.t.gameObject);
        proys.Clear();
        foreach (var m in manos) { if (m.t != null) Destroy(m.t.gameObject); if (m.aviso != null) Destroy(m.aviso.gameObject); }
        manos.Clear();
        aAb = Mathf.MoveTowards(aAb, 0f, dt / 0.6f);
        tEnojo = 0f; tRugido = 0f;
        luzBoca.intensity = Mathf.Lerp(2.2f, 0f, tMuerte / 2.5f);
        float k = Mathf.Lerp(1f, 0.25f, Mathf.Clamp01((tMuerte - 1.5f) / 2.5f));
        tinte = new Color(k, k * 0.85f, k * 0.85f, 1f);
        bossT.position = new Vector3(bxPos, Mathf.Lerp(byPos, byPos - 0.45f, tMuerte / 2f), 0f);
        if (tMuerte > 4.2f && !final)
        {
            final = true;
            if (GestorJuego.Instancia != null) GestorJuego.Instancia.CargarEscena("EscenaFinal");
        }
    }

    // ── Ataque: chatarra ─────────────────────────────────────────────────────
    void Escupir()
    {
        var go = new GameObject("Chatarra");
        go.transform.position = (Vector3)(posBoca + new Vector2(-0.8f, -0.4f));
        var sr = go.AddComponent<SpriteRenderer>(); sr.sprite = sprChatarra[Random.Range(0, sprChatarra.Length)]; sr.sortingOrder = 8;
        go.transform.localScale = Vector3.one;
        Vector2 desde = go.transform.position;
        Vector2 hasta = (Vector2)jugador.position + Vector2.up * 0.4f;
        float T = 1.1f / Mathf.Sqrt(fase);
        float g = 19.62f;
        var v = new Vector2((hasta.x - desde.x) / T, (hasta.y - desde.y) / T + 0.5f * g * T);
        proys.Add(new Proy { t = go.transform, v = v });
        AudioAuto.Golpe();
    }

    void ActualizarProyectiles(float dt)
    {
        for (int i = proys.Count - 1; i >= 0; i--)
        {
            var p = proys[i];
            if (p.t == null) { proys.RemoveAt(i); continue; }
            p.v.y -= 19.62f * dt;
            p.t.position += (Vector3)(p.v * dt);
            p.t.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(p.v.y, p.v.x) * Mathf.Rad2Deg - 225f);
            p.vida -= dt;
            Vector2 pos = p.t.position;
            bool toca = Vector2.Distance(pos, (Vector2)jugador.position + Vector2.up * 0.4f) < 0.95f;
            bool suelo = pos.y <= 0.35f;
            if (toca) { Danar(danoChatarra, true); Efecto(sprSalp, pos, 1f, 0.5f); }
            if (toca || suelo || p.vida <= 0f)
            {
                Destello(new Vector2(pos.x, Mathf.Max(pos.y, 0.3f)), 1.3f, 0.3f, new Color(1f, 0.5f, 0.1f, 0.9f));
                Destroy(p.t.gameObject);
                proys.RemoveAt(i);
            }
        }
    }

    // ── Ataque: manos ────────────────────────────────────────────────────────
    void SoltarManos(float x)
    {
        x = Mathf.Clamp(x, X0 - ANCHO / 2f + 1.5f, X0 + 4.2f);
        var m = new Mano();
        var av = new GameObject("AvisoManos");
        av.transform.position = new Vector3(x, 0.08f, 0f);
        av.transform.localScale = new Vector3(3.2f, 0.22f, 1f);
        var sa = av.AddComponent<SpriteRenderer>(); sa.sprite = JugadorRef.SpriteBlanco(); sa.sortingOrder = 5;
        sa.color = new Color(0.8f, 0.05f, 0.05f, 0.5f);
        m.aviso = av.transform;
        var go = new GameObject("Manos");
        go.transform.position = new Vector3(x, -0.05f, 0f);
        go.transform.localScale = new Vector3(1f, 0f, 1f);
        var sr = go.AddComponent<SpriteRenderer>(); sr.sprite = sprManos; sr.sortingOrder = 6;
        m.t = go.transform;
        manos.Add(m);
        tEnojo = 2.7f;   // la masa se enoja y levanta los brazos mientras salen las manos
    }

    void ActualizarManos(float dt)
    {
        for (int i = manos.Count - 1; i >= 0; i--)
        {
            var m = manos[i];
            if (m.t == null) { manos.RemoveAt(i); continue; }
            m.e += dt;
            float e = m.e;
            // 0-1.0 aviso parpadeando; 1.0-1.25 sube; 1.25-1.9 arriba; 1.9-2.2 baja
            if (m.aviso != null)
            {
                var sa = m.aviso.GetComponent<SpriteRenderer>();
                sa.color = new Color(0.8f, 0.05f, 0.05f, e < 1.2f ? (0.25f + 0.35f * Mathf.Abs(Mathf.Sin(e * 14f))) : 0f);
            }
            float h = e < 1.0f ? 0f : e < 1.25f ? (e - 1.0f) / 0.25f : e < 1.9f ? 1f : Mathf.Clamp01(1f - (e - 1.9f) / 0.3f);
            m.t.localScale = new Vector3(1f, h, 1f);
            if (!m.golpeo && e >= 1.12f && e < 1.9f)
            {
                float dx = Mathf.Abs(jugador.position.x - m.t.position.x);
                bool alcance = dx < 1.5f && jugador.position.y < 2.9f;
                if (alcance)
                {
                    m.golpeo = true;
                    Danar(danoManos, false);
                    if (rbJ != null) rbJ.linearVelocity = new Vector2(0f, 9f);
                }
            }
            if (e >= 2.2f)
            {
                if (m.aviso != null) Destroy(m.aviso.gameObject);
                Destroy(m.t.gameObject);
                manos.RemoveAt(i);
            }
        }
    }

    void Danar(float cantidad, bool chatarra)
    {
        if (tDanio > 0f || sangre == null) return;
        tDanio = 0.7f;
        sangre.RecibirDano(cantidad);
    }

    // ── Objetos ──────────────────────────────────────────────────────────────
    void SoltarPickup(bool lata, float x)
    {
        var go = new GameObject(lata ? "Lata_Arena" : "Venda_Arena");
        go.transform.position = new Vector3(x, 0.9f, 0f);
        go.transform.localScale = lata ? new Vector3(0.9f, 0.9f, 1f) : new Vector3(0.75f, 0.75f, 1f);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = lata ? sprLata : sprVenda; sr.sortingOrder = 5;
        var r = go.AddComponent<Recolectable>();
        r.tipo = lata ? Recolectable.Tipo.Lata : Recolectable.Tipo.Venda;
        r.curacion = 30f; r.radio = 1.3f;
        r.mensaje = lata ? "Lata. Lanzala (Q) a la garganta cuando abra la boca." : "Venda: recuperas sangre.";
        pickups.Add(go);
    }

    void ActualizarPickups(float dt)
    {
        for (int i = pickups.Count - 1; i >= 0; i--)
            if (pickups[i] == null || !pickups[i].activeSelf) { if (pickups[i] != null) Destroy(pickups[i]); pickups.RemoveAt(i); }

        int latas = 0, vendas = 0;
        foreach (var g in pickups) { if (g.name.StartsWith("Lata")) latas++; else vendas++; }

        tCan -= dt; tVenda -= dt;
        if (tCan <= 0f) { tCan = 6f; if (latas < 3) SoltarPickup(true, X0 - 11f + Random.value * 12f); }
        if (tVenda <= 0f) { tVenda = 14f; if (vendas < 1) SoltarPickup(false, X0 - 11f + Random.value * 12f); }
    }

    void Efecto(Sprite sp, Vector2 pos, float tam, float dur)
    {
        if (sp == null) return;
        var go = new GameObject("Efecto");
        go.transform.position = new Vector3(pos.x, pos.y, 0f);
        var sr = go.AddComponent<SpriteRenderer>(); sr.sprite = sp; sr.sortingOrder = 9;
        var e = go.AddComponent<EfectoSprite>(); e.dur = dur; e.tam0 = tam * 0.6f; e.tam1 = tam;
    }

    class EfectoSprite : MonoBehaviour
    {
        public float dur = 0.4f, tam0 = 0.6f, tam1 = 1f; float t; SpriteRenderer sr;
        void Awake() { sr = GetComponent<SpriteRenderer>(); }
        void Update()
        {
            t += Time.deltaTime; float k = t / dur;
            if (k >= 1f) { Destroy(gameObject); return; }
            transform.localScale = Vector3.one * Mathf.Lerp(tam0, tam1, k);
            var c = sr.color; c.a = 1f - k * k; sr.color = c;
        }
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
