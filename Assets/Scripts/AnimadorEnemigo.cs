using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Pone arte nuevo a los PurgadorIA SIN tocar escenas ni su IA:
///   Nivel 1 y *_N2_A  -> Hazmat verde (enemigo comun)
///   *_N2_B, *_N2_C    -> Masa roja de ojos (segunda oleada)
///   *_N2_D            -> Masa roja grande (el de la lava)
/// El Purgador sigue siendo el mismo (vision, olfato, dano); solo cambia su aspecto y su collider.
/// </summary>
[DisallowMultipleComponent]
public class AnimadorEnemigo : MonoBehaviour
{
    enum Tipo { Hazmat, Roja, Grande }

    BancoEnemigos b;
    Tipo tipo;
    PurgadorIA ia;
    SpriteRenderer sr;
    Sprite[] fIdle, fMover, fAtaque;
    Transform jugador;
    float prevX, t, vxSuave;
    float fpsIdle = 4f, fpsMover = 9f, fpsAtaque = 10f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Registrar() { SceneManager.sceneLoaded += (e, m) => Instalar(); }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Instalar()
    {
        foreach (var p in FindObjectsByType<PurgadorIA>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (p.GetComponent<AnimadorEnemigo>() == null) p.gameObject.AddComponent<AnimadorEnemigo>();
    }

    void Awake()
    {
        b = Resources.Load<BancoEnemigos>("BancoEnemigos");
        ia = GetComponent<PurgadorIA>();
        sr = GetComponentInChildren<SpriteRenderer>();
        if (b == null || ia == null || sr == null) { enabled = false; return; }

        string n = name;
        if (n.Contains("N2_D")) tipo = Tipo.Grande;
        else if (n.Contains("N2_B") || n.Contains("N2_C")) tipo = Tipo.Roja;
        else tipo = Tipo.Hazmat;

        Texture2D ti, tm, ta;
        float ppu, bajo; Vector2 colTam;
        if (tipo == Tipo.Hazmat)
        { ti = b.hazmatIdle; tm = b.hazmatCaminar; ta = b.hazmatAtaque; ppu = 115f; bajo = 5f; colTam = new Vector2(1.0f, 1.9f); }
        else if (tipo == Tipo.Roja)
        { ti = b.rojaIdle; tm = b.rojaCaminar; ta = b.rojaAtaque; ppu = 115f; bajo = 5f; colTam = new Vector2(1.9f, 1.7f); fpsMover = 7f; }
        else
        { ti = b.rojaIdle; tm = b.rojaCaminar; ta = b.rojaAtaque; ppu = 80f; bajo = 5f; colTam = new Vector2(2.7f, 2.4f); fpsMover = 6f; }

        if (ti == null) { enabled = false; return; }
        fIdle   = Cortar(ti, ppu, bajo);
        fMover  = Cortar(tm, ppu, bajo);
        fAtaque = Cortar(ta, ppu, bajo);
        if (fIdle != null && fIdle.Length > 0) sr.sprite = fIdle[0];

        // Escala normal y collider a la medida (pies 0.5 unidades bajo el centro del enemigo)
        transform.localScale = Vector3.one;
        sr.color = Color.white;
        var col = GetComponent<BoxCollider2D>();
        if (col != null)
        {
            col.size   = colTam;
            col.offset = new Vector2(0f, colTam.y * 0.5f - 0.5f);
        }
        prevX = transform.position.x;
    }

    Sprite[] Cortar(Texture2D tex, float ppu, float bajo)
    {
        if (tex == null) return null;
        tex.filterMode = FilterMode.Point;
        int celda = b.celda;
        int n = Mathf.Max(1, tex.width / celda);
        var arr = new Sprite[n];
        float pivY = Mathf.Clamp01((0.5f * ppu + bajo) / celda);
        for (int i = 0; i < n; i++)
            arr[i] = Sprite.Create(tex, new Rect(i * celda, 0, celda, celda),
                                   new Vector2(0.5f, pivY), ppu, 0, SpriteMeshType.FullRect);
        return arr;
    }

    void LateUpdate()
    {
        if (b == null || fIdle == null) return;
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

        bool ataca = ia.EstadoActual == PurgadorIA.Estado.Perseguir && jugador != null &&
                     Vector2.Distance(jugador.position, transform.position) < 1.6f;
        bool mueve = Mathf.Abs(vxSuave) > 0.15f;

        Sprite[] f; float fps;
        if (ataca && fAtaque != null && fAtaque.Length > 0) { f = fAtaque; fps = fpsAtaque; }
        else if (mueve && fMover != null && fMover.Length > 0)
        { f = fMover; fps = fpsMover * (ia.EstadoActual == PurgadorIA.Estado.Perseguir ? 1.5f : 1f); }
        else { f = fIdle; fps = fpsIdle; }
        if (f == null || f.Length == 0) return;

        sr.sprite = f[Mathf.FloorToInt(t * fps) % f.Length];
    }
}
