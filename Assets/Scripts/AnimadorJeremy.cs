using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Pone a Jeremy como Player SIN tocar escenas: se agrega solo al objeto con tag "Player"
/// al cargar cualquier escena. Corta las hojas en cuadros de 128x128, alinea los pies,
/// ajusta el collider y anima: idle, caminar, correr, salto, herido y muerte.
/// </summary>
[DisallowMultipleComponent]
public class AnimadorJeremy : MonoBehaviour
{
    const int CELDA = 128;
    /// <summary>Que tan grande se ve Jeremy respecto a la version original (1 = muy chico para estos niveles).</summary>
    public static float escala = 2.0f;

    enum Est { Idle, Caminar, Correr, Aire, Herido, Muerte }

    BancoJeremy b;
    SpriteRenderer sr;
    MovimientoJugador mov;
    Rigidbody2D rb;
    SistemaSangre sangre;
    Sprite[] sIdle, sCaminar, sCorrer, sSalto, sHerido, sMuerte;
    Est est = Est.Idle, estPrevio = (Est)(-1);
    float t, tHerido, sangrePrev = -1f;
    bool muertoPrevio;

    // ── Auto-instalacion ──────────────────────────────────────────────────────
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Registrar()
    {
        SceneManager.sceneLoaded += (esc, modo) => Instalar();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Instalar()
    {
        GameObject p;
        try { p = GameObject.FindGameObjectWithTag("Player"); } catch { return; }
        if (p == null || p.GetComponent<AnimadorJeremy>() != null) return;
        if (p.GetComponent<MovimientoJugador>() == null) return;
        p.AddComponent<AnimadorJeremy>();
    }

    // ── Preparacion ───────────────────────────────────────────────────────────
    void Awake()
    {
        b = Resources.Load<BancoJeremy>("BancoJeremy");
        if (b == null || b.idle == null) { enabled = false; return; }

        sr      = GetComponentInChildren<SpriteRenderer>();
        mov     = GetComponent<MovimientoJugador>();
        rb      = GetComponent<Rigidbody2D>();
        sangre  = GetComponent<SistemaSangre>();
        if (sr == null) { enabled = false; return; }

        // El Animator viejo pisaria el sprite
        var an = GetComponent<Animator>();
        if (an != null) Destroy(an);

        float ppu = Mathf.Max(1f, b.pixelesPorUnidad) / Mathf.Max(0.1f, escala);
        float piesPx = b.distanciaPies * b.pixelesPorUnidad;   // pixeles de la hoja bajo los pies
        float piesU  = piesPx / ppu;                            // lo mismo en unidades de mundo
        sIdle    = Cortar(b.idle,    ppu, 0.48f, piesPx);
        sCaminar = Cortar(b.caminar, ppu, 0.46f, piesPx);
        sCorrer  = Cortar(b.correr,  ppu, 0.50f, piesPx);
        sSalto   = Cortar(b.salto,   ppu, 0.50f, piesPx);
        sHerido  = Cortar(b.herido,  ppu, 0.50f, piesPx);
        sMuerte  = Cortar(b.muerte,  ppu, 0.40f, piesPx);

        // Escala normal (antes el cubo estaba estirado x2 en Y)
        transform.localScale = Vector3.one;
        sr.color = Color.white;
        sr.sprite = sIdle != null && sIdle.Length > 0 ? sIdle[0] : null;

        // Collider a la medida del personaje, con los pies donde estaban
        var col = GetComponent<CapsuleCollider2D>();
        if (col != null)
        {
            float alto  = b.altoPersonajePx / ppu;
            float ancho = b.anchoCollider * (100f / ppu);
            col.direction = CapsuleDirection2D.Vertical;
            col.size      = new Vector2(ancho, alto);
            col.offset    = new Vector2(0f, alto * 0.5f - piesU);
        }
    }

    Sprite[] Cortar(Texture2D tex, float ppu, float pivotX, float piesPx)
    {
        if (tex == null) return null;
        tex.filterMode = FilterMode.Point;
        int n = Mathf.Max(1, tex.width / CELDA);
        var arr = new Sprite[n];
        float pivotY = Mathf.Clamp01(piesPx / CELDA);
        for (int i = 0; i < n; i++)
            arr[i] = Sprite.Create(tex, new Rect(i * CELDA, 0, CELDA, CELDA),
                                   new Vector2(pivotX, pivotY), ppu, 0, SpriteMeshType.FullRect);
        return arr;
    }

    // ── Animacion ─────────────────────────────────────────────────────────────
    void LateUpdate()
    {
        if (b == null || sr == null) return;

        bool muerto = GestorJuego.Instancia != null && GestorJuego.Instancia.EstaEnGameOver;
        if (muerto && !muertoPrevio) t = 0f;
        muertoPrevio = muerto;

        // Herido: caida brusca de sangre (golpe de enemigo), no el sangrado normal
        if (sangre != null)
        {
            if (sangrePrev >= 0f && sangrePrev - sangre.sangre >= 8f) tHerido = 0.4f;
            sangrePrev = sangre.sangre;
        }
        if (tHerido > 0f) tHerido -= Time.deltaTime;

        Est nuevo;
        if (muerto) nuevo = Est.Muerte;
        else if (tHerido > 0f) nuevo = Est.Herido;
        else if (EnAire()) nuevo = Est.Aire;
        else if (mov != null && mov.enabled && Mathf.Abs(mov.Horizontal) > 0.01f)
            nuevo = mov.EstaCorriendo ? Est.Correr : Est.Caminar;
        else nuevo = Est.Idle;

        if (nuevo != estPrevio) { if (nuevo != Est.Aire) t = 0f; estPrevio = nuevo; }
        est = nuevo;
        t += Time.deltaTime;

        Sprite[] f; float fps; bool lazo = true;
        switch (est)
        {
            case Est.Caminar: f = sCaminar; fps = b.fpsCaminar; break;
            case Est.Correr:  f = sCorrer;  fps = b.fpsCorrer;  break;
            case Est.Herido:  f = sHerido;  fps = b.fpsHerido;  lazo = false; break;
            case Est.Muerte:  f = sMuerte;  fps = b.fpsMuerte;  lazo = false; break;
            case Est.Aire:    f = sSalto;   fps = 0f; break;
            default:          f = sIdle;    fps = b.fpsIdle;    break;
        }
        if (f == null || f.Length == 0) return;

        int idx;
        if (est == Est.Aire)
        {
            // Cuadro segun la velocidad vertical: subiendo -> inicio, cayendo -> final
            float vy = rb != null ? rb.linearVelocity.y : 0f;
            float v0 = 11f;
            idx = Mathf.Clamp(Mathf.RoundToInt(Mathf.InverseLerp(v0, -v0, vy) * (f.Length - 1)), 0, f.Length - 1);
        }
        else
        {
            int k = Mathf.FloorToInt(t * fps);
            idx = lazo ? k % f.Length : Mathf.Min(k, f.Length - 1);
        }
        sr.sprite = f[idx];
    }

    bool EnAire()
    {
        if (mov != null && mov.enabled) return !mov.EstaEnSuelo;
        return rb != null && Mathf.Abs(rb.linearVelocity.y) > 1.5f;
    }
}
