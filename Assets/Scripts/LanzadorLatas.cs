using UnityEngine;

/// <summary>
/// Va en el Player. Q lanza una lata hacia donde mira Jeremy. Al chocar hace ruido,
/// los Purgadores cercanos van a investigar y, si pega a un enemigo, le quita vida.
/// La lata tiene diseno propio (sprite pixel-art generado en codigo) y gira en el aire.
/// </summary>
public class LanzadorLatas : MonoBehaviour
{
    public float fuerza         = 8f;
    public float impulsoVertical = 4.5f;
    public float cooldown       = 0.4f;

    InventarioJugador inventario;
    SpriteRenderer    sprite;
    float             tCooldown;
    static Sprite     spriteLata;

    void Start()
    {
        inventario = GetComponent<InventarioJugador>();
        sprite     = GetComponentInChildren<SpriteRenderer>();
    }

    void Update()
    {
        tCooldown -= Time.deltaTime;
        if (tCooldown > 0f) return;
        if (!JugadorRef.PulsoLanzar()) return;

        if (inventario == null || !inventario.UsarLata())
        {
            UIMensaje.Mostrar("No tienes latas.", 1.5f);
            return;
        }

        tCooldown = cooldown;
        Lanzar();
    }

    void Lanzar()
    {
        float dir = (sprite != null && sprite.flipX) ? -1f : 1f;

        var go = new GameObject("Lata");
        go.transform.position   = transform.position + new Vector3(dir * 0.6f, 0.4f, 0f);
        go.transform.localScale = new Vector3(0.6f, 0.6f, 1f);

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite       = SpriteLata();
        sr.sortingOrder = 6;

        var rb = go.AddComponent<Rigidbody2D>();
        rb.gravityScale    = 2.5f;
        rb.linearVelocity  = new Vector2(dir * fuerza, impulsoVertical);
        rb.angularVelocity = -dir * 540f;

        var col = go.AddComponent<CircleCollider2D>();
        col.radius = 0.38f;

        // Contra el jefe, la lata apunta sola a la garganta (arco calculado)
        var objetivo = ArenaJefe.Instancia != null ? ArenaJefe.Instancia.ObjetivoLata : null;
        if (objetivo.HasValue)
        {
            Vector2 desde = go.transform.position;
            float dx = objetivo.Value.x - desde.x, dy = objetivo.Value.y - desde.y;
            if (dx * dir > 0f && Mathf.Abs(dx) < 24f)
            {
                float T = Mathf.Clamp(Mathf.Abs(dx) / 11f, 0.75f, 1.5f);
                float g = Mathf.Abs(Physics2D.gravity.y) * rb.gravityScale;
                rb.linearVelocity = new Vector2(dx / T, dy / T + 0.5f * g * T);
            }
        }

        go.AddComponent<LataProyectil>();

        foreach (var propio in GetComponents<Collider2D>())
            Physics2D.IgnoreCollision(col, propio);
    }

    /// <summary>Lata de conserva oxidada, 10x14 px: tapa y base de metal, etiqueta roja con franja blanca.</summary>
    public static Sprite SpriteLata()
    {
        if (spriteLata != null) return spriteLata;
        const int W = 10, H = 14;
        var tex = new Texture2D(W, H, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        tex.wrapMode = TextureWrapMode.Clamp;

        Color32 clear = new Color32(0, 0, 0, 0);
        Color32 contorno = new Color32(26, 22, 24, 255);
        for (int y = 0; y < H; y++)
        {
            for (int x = 0; x < W; x++)
            {
                Color32 c = clear;
                bool dentro = x >= 1 && x <= W - 2;
                if (dentro)
                {
                    // metal con luz a la izquierda y sombra a la derecha
                    byte g = x == 1 ? (byte)225 : x == 2 ? (byte)190 : x >= W - 3 ? (byte)95 : (byte)150;
                    c = new Color32(g, g, (byte)Mathf.Min(255, g + 8), 255);
                    if (y == 0 || y == H - 1) c = new Color32(64, 62, 70, 255);               // aro
                    else if (y == 1 || y == H - 2) c = new Color32((byte)(g * 0.8f), (byte)(g * 0.8f), (byte)(g * 0.85f), 255);
                    else if (y >= 4 && y <= 9)                                                // etiqueta
                    {
                        if (y == 6 || y == 7) c = x >= 3 && x <= 6 ? new Color32(226, 220, 200, 255) : new Color32(150, 28, 24, 255);
                        else c = x == 1 ? new Color32(205, 58, 48, 255) : x >= W - 3 ? new Color32(100, 18, 18, 255) : new Color32(168, 32, 28, 255);
                    }
                    // oxido en un par de pixeles
                    if ((x == 6 && y == 11) || (x == 3 && y == 2) || (x == 7 && y == 3)) c = new Color32(120, 66, 34, 255);
                }
                else if ((x == 0 || x == W - 1) && y >= 1 && y <= H - 2) c = contorno;
                if (dentro && (y == 0 || y == H - 1)) c = new Color32(30, 28, 32, 255);
                tex.SetPixel(x, y, c);
            }
        }
        tex.Apply();
        spriteLata = Sprite.Create(tex, new Rect(0, 0, W, H), new Vector2(0.5f, 0.5f), 16f);
        return spriteLata;
    }
}
