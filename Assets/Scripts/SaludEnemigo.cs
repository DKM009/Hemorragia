using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Vida de un enemigo. Se agrega solo a cada PurgadorIA al cargar la escena (sin tocar escenas).
/// Las latas lanzadas (LataProyectil) le quitan vida. Al morir se desvanece y se desactiva;
/// reaparece cuando el jugador muere y el nivel se reinicia (PurgadorIA.Reiniciar -> Revivir).
/// Tambien sirve para jefes: pon vidaMax mas alta y usa la barra de vida.
/// </summary>
[DefaultExecutionOrder(500)]
[DisallowMultipleComponent]
public class SaludEnemigo : MonoBehaviour
{
    public float vidaMax = 3f;
    public float vida = -1f;
    public bool  mostrarBarra = true;
    public bool  Muerto { get; private set; }
    public float Pct => vidaMax <= 0f ? 0f : Mathf.Clamp01(vida / vidaMax);

    public static event System.Action<SaludEnemigo> AlMorir;

    PurgadorIA ia;
    Rigidbody2D rb;
    SpriteRenderer[] srs;
    Color[] baseColor;
    Collider2D[] cols;
    Collider2D colPrincipal;
    Transform barraFondo, barraRelleno;
    SpriteRenderer srFondo, srRelleno;
    float tFlash, tAturdido, tBarra, tMuerte;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Registrar() { SceneManager.sceneLoaded += (e, m) => Instalar(); }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Instalar()
    {
        foreach (var p in FindObjectsByType<PurgadorIA>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (p.GetComponent<SaludEnemigo>() == null)
            {
                var s = p.gameObject.AddComponent<SaludEnemigo>();
                s.vidaMax = p.name.Contains("N2_D") ? 8f : 3f;
            }
    }

    void Awake()
    {
        ia  = GetComponent<PurgadorIA>();
        rb  = GetComponent<Rigidbody2D>();
        srs = GetComponentsInChildren<SpriteRenderer>(true);
        cols = GetComponentsInChildren<Collider2D>(true);
        colPrincipal = GetComponent<Collider2D>();
    }

    void Start()
    {
        if (vida < 0f) vida = vidaMax;
        baseColor = new Color[srs.Length];
        for (int i = 0; i < srs.Length; i++) baseColor[i] = srs[i].color;
        CrearBarra();
    }

    void CrearBarra()
    {
        var f = new GameObject("BarraVida_Fondo");
        var r = new GameObject("BarraVida_Relleno");
        barraFondo = f.transform; barraRelleno = r.transform;
        srFondo = f.AddComponent<SpriteRenderer>();
        srRelleno = r.AddComponent<SpriteRenderer>();
        srFondo.sprite = srRelleno.sprite = JugadorRef.SpriteBlanco();
        srFondo.color = new Color(0f, 0f, 0f, 0.75f);
        srRelleno.color = new Color(0.8f, 0.08f, 0.08f, 1f);
        srFondo.sortingOrder = 50; srRelleno.sortingOrder = 51;
        srFondo.enabled = srRelleno.enabled = false;
    }

    void OnDestroy()
    {
        if (barraFondo != null) Destroy(barraFondo.gameObject);
        if (barraRelleno != null) Destroy(barraRelleno.gameObject);
    }

    // ── API ──────────────────────────────────────────────────────────────────
    public void RecibirDano(float dano, Vector2 origen)
    {
        if (Muerto) return;
        vida -= dano;
        tFlash = 0.18f;
        tBarra = 3f;

        if (ia != null) ia.Investigar(origen, 5f);

        bool ligero = vidaMax <= 5f;
        if (ligero)
        {
            tAturdido = 0.35f;
            if (ia != null) ia.enabled = false;
            if (rb != null)
            {
                float dir = Mathf.Sign(transform.position.x - origen.x); if (dir == 0f) dir = 1f;
                rb.linearVelocity = new Vector2(dir * 4f, 3f);
            }
        }
        AudioAuto.Golpe();
        if (vida <= 0f) Morir();
    }

    void Morir()
    {
        Muerto = true;
        tMuerte = 0f;
        if (ia != null) ia.enabled = false;
        foreach (var c in cols) if (c != null) c.enabled = false;
        if (rb != null) { rb.linearVelocity = Vector2.zero; rb.simulated = false; }
        AudioAuto.Rugido(transform.position, true);
        AlMorir?.Invoke(this);
    }

    /// <summary>Lo llama PurgadorIA.Reiniciar() cuando el jugador muere y el nivel se reinicia.</summary>
    public void Revivir()
    {
        bool estabaMuerto = Muerto;
        Muerto = false;
        vida = vidaMax;
        tMuerte = 0f; tFlash = 0f; tAturdido = 0f;
        foreach (var c in cols) if (c != null) c.enabled = true;
        if (rb != null) rb.simulated = true;
        if (ia != null) ia.enabled = true;
        if (estabaMuerto) gameObject.SetActive(true);   // al morir se desactivaba; vuelve al reiniciar el nivel
        if (baseColor != null)
            for (int i = 0; i < srs.Length; i++) if (srs[i] != null) srs[i].color = baseColor[i];
    }

    // ── Update ───────────────────────────────────────────────────────────────
    void Update()
    {
        if (Muerto) return;
        if (tAturdido > 0f)
        {
            tAturdido -= Time.deltaTime;
            if (tAturdido <= 0f && ia != null) ia.enabled = true;
        }
        if (tBarra > 0f) tBarra -= Time.deltaTime;
    }

    void LateUpdate()
    {
        if (baseColor == null) return;

        if (Muerto)
        {
            tMuerte += Time.deltaTime;
            float a = 1f - Mathf.Clamp01(tMuerte / 0.8f);
            for (int i = 0; i < srs.Length; i++)
                if (srs[i] != null) { var c = srs[i].color; c.a = a; srs[i].color = c; }
            if (srFondo != null) srFondo.enabled = srRelleno.enabled = false;
            if (tMuerte >= 0.8f) gameObject.SetActive(false);
            return;
        }

        if (tFlash > 0f)
        {
            tFlash -= Time.deltaTime;
            for (int i = 0; i < srs.Length; i++)
                if (srs[i] != null) srs[i].color = Color.Lerp(baseColor[i], new Color(1f, 0.15f, 0.15f, baseColor[i].a), 0.85f);
        }
        else if (tFlash > -1f)
        {
            tFlash = -2f;
            for (int i = 0; i < srs.Length; i++) if (srs[i] != null) srs[i].color = baseColor[i];
        }

        bool ver = mostrarBarra && tBarra > 0f;
        if (srFondo != null)
        {
            srFondo.enabled = srRelleno.enabled = ver;
            if (ver)
            {
                float ancho = vidaMax > 5f ? 2.2f : 0.9f;
                float top = colPrincipal != null ? colPrincipal.bounds.max.y : transform.position.y + 1f;
                Vector3 c = new Vector3(transform.position.x, top + 0.3f, 0f);
                barraFondo.position = c;
                barraFondo.localScale = new Vector3(ancho + 0.08f, 0.16f, 1f);
                float w = ancho * Pct;
                barraRelleno.position = c + new Vector3(-(ancho - w) * 0.5f, 0f, 0f);
                barraRelleno.localScale = new Vector3(Mathf.Max(0.001f, w), 0.1f, 1f);
            }
        }
    }
}
