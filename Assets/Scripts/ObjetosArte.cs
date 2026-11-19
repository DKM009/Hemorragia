using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Da diseno y tamano a los objetos que se recogen (lata, venda, fusible) SIN tocar escenas.
/// Antes eran cuadritos pensados para un Jeremy mas chico; ahora se ven del tamano del personaje.
/// </summary>
public static class ObjetosArte
{
    static Sprite lata, venda, fusible;

    // Cuanto se agrandan los objetos del nivel (1 = igual que antes). Cambia estos numeros a tu gusto.
    public static float factorEscondite    = 1.5f;
    public static float factorNota         = 2.0f;
    public static float factorInteractuable = 1.7f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Registrar() { SceneManager.sceneLoaded += (e, m) => Aplicar(); }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Aplicar()
    {
        AgrandarObjetos();
        PonerPapelAlasNotas();
        foreach (var r in Object.FindObjectsByType<Recolectable>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (r.GetComponent<ArteAplicado>() != null) continue;
            r.gameObject.AddComponent<ArteAplicado>();

            var sr = r.GetComponentInChildren<SpriteRenderer>();
            if (sr == null) sr = r.gameObject.AddComponent<SpriteRenderer>();

            float escala; float subir;
            switch (r.tipo)
            {
                case Recolectable.Tipo.Lata:    sr.sprite = Lata();    escala = 1.0f;  subir = 0.45f; break;
                case Recolectable.Tipo.Fusible: sr.sprite = Fusible(); escala = 1.1f;  subir = 0.55f; break;
                default:                        sr.sprite = Venda();   escala = 0.6f;  subir = 0.35f; break;
            }
            sr.color = Color.white;
            sr.sortingOrder = 5;
            sr.transform.localScale = new Vector3(escala, escala, 1f);
            if (sr.transform != r.transform) r.transform.localScale = Vector3.one;
            r.transform.position += Vector3.up * subir;
            r.radio = Mathf.Max(r.radio, 1.3f);
            Asentar(r.transform);
        }
        foreach (var n in Object.FindObjectsByType<NotaLore>(FindObjectsInactive.Include, FindObjectsSortMode.None)) Asentar(n.transform);
    }

    class ArteAplicado : MonoBehaviour { }

    static Sprite papel;
    public static Sprite Papel()
    {
        if (papel != null) return papel;
        var imgNota = Resources.Load<Texture2D>("Nota");
        if (imgNota != null)
        {
            imgNota.filterMode = FilterMode.Bilinear;
            papel = Sprite.Create(imgNota, new Rect(0, 0, imgNota.width, imgNota.height), new Vector2(0.5f, 0.5f), imgNota.width / 0.6f);
            return papel;
        }
        const int W = 12, H = 16;
        var tex = Nueva(W, H);
        for (int y = 0; y < H; y++)
            for (int x = 0; x < W; x++)
            {
                bool borde = x == 0 || y == 0 || x == W - 1 || y == H - 1;
                bool linea = x >= 2 && x <= W - 3 && y >= 3 && y <= H - 4 && y % 3 == 0;
                bool mancha = (x - 8) * (x - 8) + (y - 4) * (y - 4) <= 3;
                Color c = borde ? new Color(0.32f, 0.24f, 0.16f)
                        : mancha ? new Color(0.55f, 0.06f, 0.06f)
                        : linea ? new Color(0.42f, 0.36f, 0.30f)
                        : new Color(0.86f, 0.80f, 0.64f);
                tex.SetPixel(x, y, c);
            }
        tex.Apply();
        papel = Sprite.Create(tex, new Rect(0, 0, W, H), new Vector2(0.5f, 0.5f), 24f);
        return papel;
    }

    static void PonerPapelAlasNotas()
    {
        foreach (var n in Object.FindObjectsByType<NotaLore>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (n.transform.Find("Papel") != null) continue;
            var sr = n.GetComponent<SpriteRenderer>();
            if (sr == null) sr = n.GetComponentInChildren<SpriteRenderer>();
            int orden = 4; int capa = 0;
            if (sr != null) { orden = sr.sortingOrder + 1; capa = sr.sortingLayerID; sr.enabled = false; }
            var go = new GameObject("Papel");
            go.transform.SetParent(n.transform, false);
            Vector3 ls = n.transform.lossyScale;
            float k = 1.3f;
            go.transform.localScale = new Vector3(k / Mathf.Max(0.01f, Mathf.Abs(ls.x)), k / Mathf.Max(0.01f, Mathf.Abs(ls.y)), 1f);
            var r = go.AddComponent<SpriteRenderer>();
            r.sprite = Papel(); r.sortingOrder = orden; r.sortingLayerID = capa;
        }
    }

    /// <summary>Agranda un objeto desde su base: los pies siguen apoyados en el piso.</summary>
    static void AgrandarDesdeBase(Transform t, float k)
    {
        if (t == null || Mathf.Approximately(k, 1f) || t.GetComponent<ArteAplicado>() != null) return;
        t.gameObject.AddComponent<ArteAplicado>();
        float ext = -1f;
        var r = t.GetComponentInChildren<Renderer>();
        if (r != null) ext = r.bounds.extents.y;
        else
        {
            var c = t.GetComponentInChildren<Collider2D>();
            if (c != null) ext = c.bounds.extents.y;
        }
        if (ext <= 0f) return;
        t.localScale = new Vector3(t.localScale.x * k, t.localScale.y * k, t.localScale.z);
        t.position += Vector3.up * ((k - 1f) * ext);
    }

    static void AgrandarObjetos()
    {
        foreach (var e in Object.FindObjectsByType<Escondite>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            AgrandarDesdeBase(e.transform, factorEscondite);
        foreach (var n in Object.FindObjectsByType<NotaLore>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            AgrandarDesdeBase(n.transform, factorNota);
        foreach (var i in Object.FindObjectsByType<Interactuable>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (i.GetComponent<Recolectable>() != null || i.GetComponent<NotaLore>() != null || i.GetComponent<PuertaConLlave>() != null) continue;
            AgrandarDesdeBase(i.transform, factorInteractuable);
        }
    }

    public static Sprite Lata() { if (lata == null) lata = LanzadorLatas.SpriteLata(); return lata; }

    public static Sprite Venda()
    {
        if (venda != null) return venda;
        var img = Resources.Load<Texture2D>("Venda");
        if (img != null)
        {
            img.filterMode = FilterMode.Bilinear;
            // alto de 0.85 unidades (luego se escala igual que antes)
            venda = Sprite.Create(img, new Rect(0, 0, img.width, img.height), new Vector2(0.5f, 0.5f), img.height / 0.85f);
            return venda;
        }
        const int N = 10;
        var tex = Nueva(N, N);
        for (int y = 0; y < N; y++)
            for (int x = 0; x < N; x++)
            {
                bool borde = x == 0 || y == 0 || x == N - 1 || y == N - 1;
                bool cruz = (x >= 4 && x <= 5) || (y >= 4 && y <= 5);
                Color c = borde ? new Color(0.30f, 0.26f, 0.26f) : cruz ? new Color(0.78f, 0.08f, 0.10f) : new Color(0.92f, 0.90f, 0.84f);
                tex.SetPixel(x, y, c);
            }
        tex.Apply();
        venda = Sprite.Create(tex, new Rect(0, 0, N, N), new Vector2(0.5f, 0.5f), 12f);
        return venda;
    }

    /// <summary>Apoya el objeto sobre el piso o plataforma que tenga debajo (para que no flote).</summary>
    public static void Asentar(Transform t)
    {
        if (t == null) return;
        var sr = t.GetComponentInChildren<SpriteRenderer>();
        if (sr == null || sr.sprite == null) return;
        Physics2D.SyncTransforms();
        Bounds b = sr.bounds;
        var hits = Physics2D.RaycastAll(new Vector2(b.center.x, b.min.y + 0.12f), Vector2.down, 9f);
        foreach (var h in hits)
        {
            if (h.collider == null || h.collider.isTrigger) continue;
            if (h.collider.transform.IsChildOf(t)) continue;
            bool propio = false;
            for (var p = h.collider.transform; p != null; p = p.parent) if (p.CompareTag("Player")) { propio = true; break; }
            if (propio) continue;
            var rb = h.collider.attachedRigidbody;
            if (rb != null && rb.bodyType == RigidbodyType2D.Dynamic) continue;
            float suelo = h.fraction <= 0f ? h.collider.bounds.max.y : h.point.y;
            float dy = (suelo + 0.04f) - b.min.y;
            if (Mathf.Abs(dy) > 0.02f) t.position += Vector3.up * dy;
            return;
        }
    }

    /// <summary>Fusible de vidrio ambar con tapas de metal, 8x14 px.</summary>
    public static Sprite Fusible()
    {
        if (fusible != null) return fusible;
        var imgF = Resources.Load<Texture2D>("Fusible");
        if (imgF != null)
        {
            imgF.filterMode = FilterMode.Bilinear;
            fusible = Sprite.Create(imgF, new Rect(0, 0, imgF.width, imgF.height), new Vector2(0.5f, 0.5f), Mathf.Max(imgF.width, imgF.height) / 0.95f);
            return fusible;
        }
        const int W = 8, H = 14;
        var tex = Nueva(W, H);
        Color clear = new Color(0, 0, 0, 0);
        Color contorno = new Color(0.10f, 0.08f, 0.08f);
        for (int y = 0; y < H; y++)
            for (int x = 0; x < W; x++)
            {
                Color c = clear;
                bool tapa = y <= 2 || y >= H - 3;
                if (x >= 1 && x <= W - 2)
                {
                    if (tapa)
                    {
                        c = x == 1 ? new Color(0.82f, 0.80f, 0.76f) : x >= W - 3 ? new Color(0.35f, 0.33f, 0.32f) : new Color(0.60f, 0.58f, 0.55f);
                        if (y == 0 || y == H - 1) c = contorno;
                    }
                    else
                    {
                        // vidrio ambar con brillo a la izquierda y filamento al centro
                        c = x == 2 ? new Color(1f, 0.88f, 0.45f) : x >= W - 3 ? new Color(0.70f, 0.38f, 0.06f) : new Color(0.95f, 0.66f, 0.14f);
                        if (x == 3 || x == 4) if (y >= 5 && y <= 8) c = new Color(0.30f, 0.16f, 0.05f);
                    }
                }
                else if ((x == 0 || x == W - 1) && y >= 1 && y <= H - 2) c = contorno;
                tex.SetPixel(x, y, c);
            }
        tex.Apply();
        fusible = Sprite.Create(tex, new Rect(0, 0, W, H), new Vector2(0.5f, 0.5f), 14f);
        return fusible;
    }

    static Texture2D Nueva(int w, int h)
    {
        var t = new Texture2D(w, h, TextureFormat.RGBA32, false);
        t.filterMode = FilterMode.Point; t.wrapMode = TextureWrapMode.Clamp;
        for (int y = 0; y < h; y++) for (int x = 0; x < w; x++) t.SetPixel(x, y, new Color(0, 0, 0, 0));
        return t;
    }
}
