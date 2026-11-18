using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Nivel 2, zona de las calderas: las tapas naranjas eran rectangulos sin textura que sobresalian
/// del cuerpo de la caldera. Aqui se ajustan al ancho real de la caldera, se visten con una rejilla
/// de metal con rendijas (como piso de escalon) y se quita la barra naranja suelta del fondo de la lava.
/// Si no llegas a caer arriba de la tapa, caes a la lava. Ajusta los anchos aqui abajo.
/// </summary>
public static class CalderasN2
{
    const float AnchoTapaIzq = 3.0f;
    const float AnchoTapaDer = 3.2f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Registrar() { SceneManager.sceneLoaded += (e, md) => Iniciar(); }

    static void Iniciar()
    {
        if (SceneManager.GetActiveScene().name != "Nivel2") return;
        foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (t.name == "Caldera_Izq_Tapa") Tapa(t, AnchoTapaIzq);
            else if (t.name == "Caldera_Der_Tapa") Tapa(t, AnchoTapaDer);
            else if (t.name == "Caldera_Izq_Fondo") t.gameObject.SetActive(false);    // barra naranja sin sentido
        }
        AcomodarPisoLargo();
    }

    // Piso largo del inicio de la zona de calderas: camina el jugador (y los monstruos) del inicio hasta donde
    // empiezan las plataformas. De ahi en adelante es lava: si caes de las plataformas flotantes, caes a la lava.
    // Se quita el cuerpo solido de la caldera que bloqueaba el paso y el piso se dibuja detras del personaje.
    const float FinPiso = 71.8f;

    static void AcomodarPisoLargo()
    {
        Transform piso = null, arte = null;
        foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (t.name == "Escalon_C_1") piso = t;
            else if (t.name == "Arte_Escalon_C_1") arte = t;
            else if (t.name == "Caldera_Izq_Cuerpo")
                foreach (var c in t.GetComponents<Collider2D>()) c.enabled = false;      // ya no bloquea el piso largo
        }
        if (piso != null) Recortar(piso);
        if (arte != null && (piso == null || !arte.IsChildOf(piso)))
        {
            Recortar(arte);
            var jug = GameObject.FindGameObjectWithTag("Player");
            var rj = jug != null ? jug.GetComponentInChildren<SpriteRenderer>() : null;
            var ra = arte.GetComponentInChildren<SpriteRenderer>();
            if (rj != null && ra != null && ra.sortingLayerID == rj.sortingLayerID && ra.sortingOrder >= rj.sortingOrder)
                ra.sortingOrder = rj.sortingOrder - 1;                                  // el personaje se ve sobre el piso
        }
        Physics2D.SyncTransforms();
    }

    // Deja el piso desde su borde izquierdo hasta FinPiso
    static void Recortar(Transform t)
    {
        Bounds b;
        var col = t.GetComponent<Collider2D>();
        var rend = t.GetComponentInChildren<Renderer>();
        if (col != null) b = col.bounds; else if (rend != null) b = rend.bounds; else return;
        if (b.max.x <= FinPiso + 0.05f) return;
        float ancho = FinPiso - b.min.x;
        if (ancho < 0.5f) return;
        var s = t.localScale; s.x *= ancho / b.size.x; t.localScale = s;
        var p = t.position; p.x += (b.min.x + ancho * 0.5f) - b.center.x; t.position = p;
    }

    static void Tapa(Transform t, float ancho)
    {
        var sr = t.GetComponent<SpriteRenderer>();
        if (sr == null || t.Find("Rejilla") != null) return;
        float altoMundo = t.lossyScale.y;
        var s = t.localScale; s.x = ancho; t.localScale = s;           // el collider (1x1) se encoge junto con el

        var go = new GameObject("Rejilla");
        go.transform.SetParent(t, false);
        go.transform.localScale = new Vector3(1f / ancho, 1f / Mathf.Max(0.01f, t.localScale.y), 1f);
        var r = go.AddComponent<SpriteRenderer>();
        r.sprite = SpriteRejilla();
        r.drawMode = SpriteDrawMode.Tiled;
        r.tileMode = SpriteTileMode.Continuous;
        r.size = new Vector2(ancho, Mathf.Max(0.3f, altoMundo));
        r.color = new Color(0.85f, 0.7f, 0.62f, 1f);                   // un poco de calor de la lava
        r.sortingLayerID = sr.sortingLayerID;
        r.sortingOrder = sr.sortingOrder;
        sr.enabled = false;
    }

    static Sprite rejilla;
    static Sprite SpriteRejilla()
    {
        if (rejilla != null) return rejilla;
        const int W = 32, H = 10;
        var tex = new Texture2D(W, H, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Repeat };
        var px = new Color32[W * H];
        for (int y = 0; y < H; y++)
            for (int x = 0; x < W; x++)
            {
                Color c = new Color(0.30f, 0.28f, 0.29f);                                  // acero
                if (y == H - 1) c = new Color(0.55f, 0.52f, 0.50f);                        // borde de arriba claro
                else if (y == 0) c = new Color(0.12f, 0.11f, 0.12f);                       // borde de abajo oscuro
                else if (y >= 3 && y <= 6 && (x % 8) >= 2 && (x % 8) <= 5) c = new Color(0.06f, 0.05f, 0.06f);   // rendijas
                else if ((x % 8) == 0) c = new Color(0.22f, 0.21f, 0.22f);
                px[y * W + x] = c;
            }
        tex.SetPixels32(px); tex.Apply();
        rejilla = Sprite.Create(tex, new Rect(0, 0, W, H), new Vector2(0.5f, 0.5f), 32f, 0, SpriteMeshType.FullRect);
        return rejilla;
    }
}
