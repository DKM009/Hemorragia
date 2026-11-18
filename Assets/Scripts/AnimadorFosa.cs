using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Le pone arte (la masa roja de ojos y boca) a La Fosa, que antes era un cubo rojo.
/// Mas pequena que antes: ajusta "ppu" (mas alto = mas chica) y "collider" desde el Inspector si quieres.
/// No toca la escena ni la IA; solo aspecto, collider y animacion. Se instala sola en cada LaFosaIA.
/// </summary>
[DisallowMultipleComponent]
public class AnimadorFosa : MonoBehaviour
{
    public float ppu = 135f;                              // 256 px / 135 = ~1.9 unidades de cuadro
    public Vector2 tamCollider = new Vector2(1.5f, 1.4f);
    public float fpsIdle = 4f, fpsMover = 7f, fpsAtaque = 10f;

    BancoEnemigos b;
    LaFosaIA ia;
    SpriteRenderer sr;
    Sprite[] fIdle, fMover, fAtaque;
    Transform jugador;
    float prevX, t, vxSuave;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Registrar() { SceneManager.sceneLoaded += (e, m) => Instalar(); }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Instalar()
    {
        foreach (var f in FindObjectsByType<LaFosaIA>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (f.GetComponent<AnimadorFosa>() == null) f.gameObject.AddComponent<AnimadorFosa>();
    }

    void Awake()
    {
        b = Resources.Load<BancoEnemigos>("BancoEnemigos");
        ia = GetComponent<LaFosaIA>();
        sr = GetComponentInChildren<SpriteRenderer>();
        if (b == null || ia == null || sr == null || b.rojaIdle == null) { enabled = false; return; }

        fIdle   = Cortar(b.rojaIdle);
        fMover  = Cortar(b.rojaCaminar);
        fAtaque = Cortar(b.rojaAtaque);
        if (fIdle != null && fIdle.Length > 0) sr.sprite = fIdle[0];

        ia.umbralActivacion = 15f; ia.decaimientoRuido = 2f;

        // quita el aspecto de cubo: escala normal, sin tinte
        transform.localScale = Vector3.one;
        sr.color = Color.white;
        foreach (var l in GetComponentsInChildren<Transform>(true))
            if (l != transform && l.name == "Luz") l.localScale = Vector3.one;

        var col = GetComponent<BoxCollider2D>();
        if (col != null)
        {
            col.size = tamCollider;
            col.offset = new Vector2(0f, tamCollider.y * 0.5f - 0.5f);   // pies 0.5 bajo el centro
        }
        AsentarEnElSuelo();
        prevX = transform.position.x;
    }

    /// <summary>El cubo era mas alto que la masa: la baja hasta el piso para que no "caiga" al aparecer.</summary>
    void AsentarEnElSuelo()
    {
        var col = GetComponent<BoxCollider2D>();
        Vector2 origen = new Vector2(transform.position.x, transform.position.y);
        foreach (var h in Physics2D.RaycastAll(origen, Vector2.down, 8f))
        {
            if (h.collider == null || h.collider.isTrigger || h.collider.gameObject == gameObject) continue;
            if (h.collider.GetComponent<Rigidbody2D>() != null && h.collider.attachedRigidbody.bodyType == RigidbodyType2D.Dynamic) continue;
            transform.position = new Vector3(transform.position.x, h.point.y + 0.5f + 0.02f, transform.position.z);
            break;
        }
    }

    Sprite[] Cortar(Texture2D tex)
    {
        if (tex == null) return null;
        tex.filterMode = FilterMode.Point;
        int celda = b.celda;
        int n = Mathf.Max(1, tex.width / celda);
        var arr = new Sprite[n];
        float pivY = Mathf.Clamp01((0.5f * ppu + b.pixelesBajoPies) / celda);
        for (int i = 0; i < n; i++)
            arr[i] = Sprite.Create(tex, new Rect(i * celda, 0, celda, celda), new Vector2(0.5f, pivY), ppu, 0, SpriteMeshType.FullRect);
        return arr;
    }

    void LateUpdate()
    {
        if (fIdle == null) return;
        t += Time.deltaTime;
        float dt = Mathf.Max(Time.deltaTime, 0.0001f);
        float vx = (transform.position.x - prevX) / dt;
        prevX = transform.position.x;
        vxSuave = Mathf.Lerp(vxSuave, vx, 0.3f);

        if (jugador == null)
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) jugador = p.transform;
        }

        if (jugador != null && ia.EstadoActual != LaFosaIA.Estado.Persiguiendo
            && Mathf.Abs(jugador.position.x - transform.position.x) is > 5f and < 10f && !(GestorJuego.Instancia != null && GestorJuego.Instancia.EstaEnGameOver))
            ia.Despertar();

        bool persigue = ia.EstadoActual == LaFosaIA.Estado.Persiguiendo;
        bool ataca = persigue && jugador != null && Vector2.Distance(jugador.position, transform.position) < 2f;
        bool mueve = Mathf.Abs(vxSuave) > 0.15f;

        Sprite[] f; float fps;
        if (ataca && fAtaque != null && fAtaque.Length > 0) { f = fAtaque; fps = fpsAtaque; }
        else if (mueve && fMover != null && fMover.Length > 0) { f = fMover; fps = fpsMover * (persigue ? 1.4f : 1f); }
        else { f = fIdle; fps = fpsIdle; }
        if (f == null || f.Length == 0) return;
        sr.sprite = f[Mathf.FloorToInt(t * fps) % f.Length];
    }
}
