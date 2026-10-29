using UnityEngine;

/// <summary>
/// Va en el Player. Q lanza una lata hacia donde mira Jeremy. Al chocar hace ruido
/// y los Purgadores cercanos van a investigar (ver LataProyectil).
/// </summary>
public class LanzadorLatas : MonoBehaviour
{
    public float fuerza         = 7f;
    public float impulsoVertical = 4.5f;
    public float cooldown       = 0.4f;

    InventarioJugador inventario;
    SpriteRenderer    sprite;
    float             tCooldown;

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
        go.transform.localScale = new Vector3(0.25f, 0.35f, 1f);

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite       = JugadorRef.SpriteBlanco();
        sr.color        = new Color(0.75f, 0.75f, 0.8f);
        sr.sortingOrder = 6;

        var rb = go.AddComponent<Rigidbody2D>();
        rb.gravityScale   = 2.5f;
        rb.linearVelocity = new Vector2(dir * fuerza, impulsoVertical);

        var col = go.AddComponent<CircleCollider2D>();
        col.radius = 0.5f;

        go.AddComponent<LataProyectil>();

        foreach (var propio in GetComponents<Collider2D>())
            Physics2D.IgnoreCollision(col, propio);
    }
}
