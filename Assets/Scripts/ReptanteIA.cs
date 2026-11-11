using UnityEngine;

/// <summary>
/// El Reptante sangrante: sigue tu rastro de sangre. Si te ve de cerca, corre hacia ti.
/// Si te escondes (Escondite) o te cauterizas, pierde el rastro y se queda olfateando.
/// Si te alcanza, te mata. Se crea por codigo: ReptanteIA.Crear(posicion).
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class ReptanteIA : MonoBehaviour
{
    public float velocidadRastro = 2.9f;
    public float velocidadVista  = 4.4f;
    public float radioVision     = 7f;
    public float tiempoOlvido    = 4f;
    public float alcanceAtaque   = 1.5f;

    Rigidbody2D rb;
    SpriteRenderer sr;
    Transform jugador;
    SistemaSangre sangre;
    BancoEnemigos banco;
    Sprite[] idle, mover, ataque;
    Vector3 inicio;
    float t, tPerdido, tAtaque, tRugido;
    bool atacando, goPrev;

    public static ReptanteIA Crear(Vector3 posicion)
    {
        var go = new GameObject("Reptante");
        go.transform.position = posicion;
        var col = go.AddComponent<BoxCollider2D>();
        col.size = new Vector2(2.0f, 1.5f);
        col.offset = new Vector2(0f, 0.85f);
        var rb = go.AddComponent<Rigidbody2D>();
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        rb.gravityScale = 2f;
        var mat = new PhysicsMaterial2D("ReptanteSinFriccion") { friction = 0f, bounciness = 0f };
        col.sharedMaterial = mat;
        var hijo = new GameObject("Sprite");
        hijo.transform.SetParent(go.transform, false);
        var sr = hijo.AddComponent<SpriteRenderer>();
        sr.sortingOrder = 8;
        var ia = go.AddComponent<ReptanteIA>();
        go.AddComponent<SaludEnemigo>().vidaMax = 999f; // invulnerable en la practica: hay que escapar
        return ia;
    }

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponentInChildren<SpriteRenderer>();
        banco = Resources.Load<BancoEnemigos>("BancoEnemigos");
        inicio = transform.position;
        if (banco == null || banco.reptanteCaminar == null) { Debug.LogWarning("[Reptante] Falta BancoEnemigos (Tools > Hemorragia > Enemigos - Configurar)"); UIMensaje.Mostrar("Falta correr Tools > Hemorragia > Enemigos - Configurar", 5f); return; }
        idle   = Cortar(banco.reptanteIdle);
        mover  = Cortar(banco.reptanteCaminar);
        ataque = Cortar(banco.reptanteAtaque);
        if (idle != null && idle.Length > 0) sr.sprite = idle[0];
    }

    Sprite[] Cortar(Texture2D tex)
    {
        if (tex == null) return null;
        tex.filterMode = FilterMode.Point;
        int n = Mathf.Max(1, tex.width / banco.celda);
        var arr = new Sprite[n];
        float pivY = banco.pixelesBajoPies / banco.celda;
        for (int i = 0; i < n; i++)
            arr[i] = Sprite.Create(tex, new Rect(i * banco.celda, 0, banco.celda, banco.celda),
                                   new Vector2(0.5f, pivY), banco.ppu, 0, SpriteMeshType.FullRect);
        return arr;
    }

    void Start()
    {
        var p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) { jugador = p.transform; sangre = p.GetComponent<SistemaSangre>(); }
        AudioAuto.Rugido(transform.position, true);
    }

    void Update()
    {
        t += Time.deltaTime;
        Animar();
    }

    void FixedUpdate()
    {
        if (jugador == null) return;

        bool go = GestorJuego.Instancia != null && GestorJuego.Instancia.EstaEnGameOver;
        if (go) { rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y); goPrev = true; return; }
        if (goPrev) { goPrev = false; transform.position = inicio; atacando = false; tPerdido = 0f; return; }

        if (atacando)
        {
            tAtaque -= Time.fixedDeltaTime;
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            if (tAtaque <= 0f)
            {
                atacando = false;
                if (sangre != null) sangre.RecibirDano(9999f);
                else if (GestorJuego.Instancia != null) GestorJuego.Instancia.GameOver();
            }
            return;
        }

        bool oculto = Escondite.JugadorOculto ||
                      (sangre != null && sangre.Estado == SistemaSangre.EstadoSangre.Cauterizando);
        Vector2 yo = transform.position;
        Vector2 jp = jugador.position;
        float dx = jp.x - yo.x, dy = jp.y - yo.y;

        Vector2? objetivo = null;
        float vel = velocidadRastro;

        if (!oculto && Mathf.Abs(dx) <= radioVision && Mathf.Abs(dy) < 2.8f)
        {
            objetivo = jp; vel = velocidadVista; tPerdido = 0f;
        }
        else if (!oculto && sangre != null)
        {
            var rastro = sangre.ObtenerRastro();
            if (rastro != null && rastro.Count > 0)
            {
                int mejor = 0; float dMin = float.MaxValue;
                for (int i = 0; i < rastro.Count; i++)
                {
                    float d = Vector2.Distance(yo, rastro[i]);
                    if (d < dMin) { dMin = d; mejor = i; }
                }
                int sig = (dMin < 0.7f) ? Mathf.Min(mejor + 1, rastro.Count - 1) : mejor;
                objetivo = rastro[sig];
                tPerdido = 0f;
            }
        }

        if (objetivo == null)
        {
            // Pierde el rastro: olfatea en el lugar
            tPerdido += Time.fixedDeltaTime;
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            return;
        }

        float ox = objetivo.Value.x - yo.x;
        float vx = Mathf.Abs(ox) < 0.3f ? 0f : Mathf.Sign(ox) * vel;
        rb.linearVelocity = new Vector2(vx, rb.linearVelocity.y);
        if (sr != null && Mathf.Abs(vx) > 0.05f) sr.flipX = vx < 0f;

        // Alcanzo al jugador
        if (!oculto && Mathf.Abs(dx) < alcanceAtaque && Mathf.Abs(dy) < 2.0f)
        {
            atacando = true; tAtaque = 0.55f; t = 0f;
            if (sr != null) sr.flipX = dx < 0f;
            AudioAuto.Rugido(yo, true);
        }
    }

    void Animar()
    {
        if (sr == null || idle == null) return;
        Sprite[] f; float fps;
        if (atacando && ataque != null && ataque.Length > 0)
        {
            int i = Mathf.Clamp(Mathf.FloorToInt(t / 0.55f * ataque.Length), 0, ataque.Length - 1);
            sr.sprite = ataque[i];
            return;
        }
        bool mueve = Mathf.Abs(rb.linearVelocity.x) > 0.2f;
        if (mueve && mover != null && mover.Length > 0) { f = mover; fps = Mathf.Abs(rb.linearVelocity.x) > 3.5f ? 14f : 9f; }
        else { f = idle; fps = 3f; }
        sr.sprite = f[Mathf.FloorToInt(t * fps) % f.Length];
    }
}
