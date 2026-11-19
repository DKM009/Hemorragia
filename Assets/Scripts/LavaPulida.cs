using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Lava del Nivel 2 (zona de las calderas) mas bonita: superficie con olas que se mueven, textura con su proporcion
/// (sin deformarse), orillas oscuras y brillo en la superficie. Se instala sola, sin tocar la escena.
/// Para ajustar: cambia los numeros de abajo.
/// </summary>
public class LavaViva : MonoBehaviour
{
    const float Amplitud   = 0.06f;   // altura de las olas
    const float VelCapa1   = 0.30f;   // velocidad de la textura (unidades por segundo)
    const float VelCapa2   = -0.18f;
    const int   Cuadros    = 8;

    Transform capa1, capa2;
    SpriteMask mascara;
    SpriteRenderer tira;
    Sprite[] frMasc, frTira;
    float anchoTile, baseX;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Registrar()
    {
        SceneManager.sceneLoaded += (e, m) =>
        {
            if (e.name != "Nivel2" || FindFirstObjectByType<LavaViva>() != null) return;
            new GameObject("LavaViva").AddComponent<LavaViva>();
        };
    }

    IEnumerator Start()
    {
        yield return null; yield return null;   // espera a que otros scripts terminen de poner arte
        var lava = GameObject.Find("Lava_N2C");
        var bc = lava != null ? lava.GetComponent<BoxCollider2D>() : null;
        if (bc == null) { Destroy(gameObject); yield break; }

        // ocultar el dibujo anterior (estirado y cortado)
        var sr0 = lava.GetComponent<SpriteRenderer>(); if (sr0 != null) sr0.enabled = false;
        foreach (var n in new[] { "Arte_Lava_N2C", "Borde_Brillante" })
        {
            var o = GameObject.Find(n); if (o != null) o.SetActive(false);
        }
        foreach (var t in lava.GetComponentsInChildren<Transform>(true))
            if (t != lava.transform && (t.name.StartsWith("Tex_") || t.name == "Borde_Brillante")) t.gameObject.SetActive(false);

        Bounds b = bc.bounds;
        float w = b.size.x + 0.3f, h = Mathf.Max(0.4f, b.size.y);
        baseX = b.center.x;

        var unlit = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
        Material mat = unlit != null ? new Material(unlit) : null;

        // olas: cuadros de la mascara y de la tira brillante
        float alto = h + 0.2f;
        const int PW = 256, PH = 64;
        float pxu = PH / alto;                         // pixeles por unidad (vertical)
        float ppuX = PW / w;                           // pixeles por unidad (horizontal) para que mida w
        frMasc = new Sprite[Cuadros]; frTira = new Sprite[Cuadros];
        for (int f = 0; f < Cuadros; f++)
        {
            float ph = f * Mathf.PI * 2f / Cuadros;
            var tm = new Texture2D(PW, PH, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            var tt = new Texture2D(PW, 24, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            for (int x = 0; x < PW; x++)
            {
                float wx = x / ppuX;
                float ola = (Mathf.Sin(wx * 6.2832f / 1.6f + ph) + 0.6f * Mathf.Sin(wx * 6.2832f / 0.9f - 2f * ph)) / 1.6f;
                float sup = (h - 0.04f) * pxu + ola * Amplitud * pxu;       // fila de la superficie
                for (int y = 0; y < PH; y++) tm.SetPixel(x, y, new Color(1, 1, 1, y <= sup ? 1f : 0f));
                // tira brillante: ventana de 24 filas alrededor de la superficie nominal (sigue la ola)
                float nominal = (h - 0.04f) * pxu;
                for (int y = 0; y < 24; y++)
                {
                    float fila = nominal - 16f + y;
                    float dist = fila - sup;            // negativo = dentro de la lava
                    float a = dist > 0f ? 0f : Mathf.Clamp01(1f + dist / 9f);
                    var c = Color.Lerp(new Color(1f, 0.45f, 0.08f), new Color(1f, 0.92f, 0.45f), Mathf.Clamp01(1f + dist / 5f));
                    tt.SetPixel(x, y, new Color(c.r, c.g, c.b, a * 0.9f));
                }
            }
            tm.Apply(); tt.Apply();
            frMasc[f] = Sprite.Create(tm, new Rect(0, 0, PW, PH), new Vector2(0.5f, 0f), ppuX, 0, SpriteMeshType.FullRect);
            // la tira se coloca para que su fila 16 (superficie) coincida: el pivote queda en la base de la tira
            frTira[f] = Sprite.Create(tt, new Rect(0, 0, PW, 24), new Vector2(0.5f, 0f), ppuX, 0, SpriteMeshType.FullRect);
        }
        // escala vertical: la mascara usa ppuX horizontal; ajusta con escala Y para mantener las unidades
        float escY = (ppuX / pxu);   // el sprite mide PH/ppuX de alto; queremos 'alto'

        var raiz = new GameObject("Lava_Viva").transform;
        raiz.SetParent(lava.transform.parent, true);
        raiz.position = new Vector3(b.center.x, b.min.y, 0f);

        var gm = new GameObject("Mascara"); gm.transform.SetParent(raiz, false);
        mascara = gm.AddComponent<SpriteMask>();
        mascara.sprite = frMasc[0];
        gm.transform.localScale = new Vector3(1f, escY, 1f);

        // textura de lava con su proporcion
        Texture2D tex = null;
        var banco = Resources.Load<BancoTexturas>("BancoTexturas");
        if (banco != null) tex = banco.Buscar("n2_lava_b");
        Sprite spL = null;
        if (tex != null)
        {
            tex.filterMode = FilterMode.Bilinear; tex.wrapMode = TextureWrapMode.Repeat;
            float altoTex = h * 1.5f;
            spL = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0f), tex.height / altoTex, 0, SpriteMeshType.FullRect);
            anchoTile = spL.bounds.size.x;
        }
        capa1 = CrearCapa(raiz, "Capa1", spL, mat, w, h, 20, false, new Color(1f, 0.82f, 0.62f, 1f));
        capa2 = CrearCapa(raiz, "Capa2", spL, mat, w, h, 21, true, new Color(1f, 0.55f, 0.25f, 0.55f));
        capa2.localPosition += new Vector3(0f, 0.12f, 0f);

        // orillas oscuras (sombra) a los lados, dentro de la lava
        var grad = new Texture2D(32, 1, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        for (int x = 0; x < 32; x++) grad.SetPixel(x, 0, new Color(0.12f, 0.02f, 0f, Mathf.Pow(1f - x / 31f, 1.6f) * 0.85f));
        grad.Apply();
        var spG = Sprite.Create(grad, new Rect(0, 0, 32, 1), new Vector2(0f, 0f), 32f / 0.7f);
        for (int lado = 0; lado < 2; lado++)
        {
            var g = new GameObject(lado == 0 ? "OrillaIzq" : "OrillaDer"); g.transform.SetParent(raiz, false);
            var r = g.AddComponent<SpriteRenderer>();
            r.sprite = spG; r.sortingOrder = 22; r.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
            if (mat != null) r.sharedMaterial = mat;
            g.transform.localScale = new Vector3(lado == 0 ? 1f : -1f, h * 1.6f, 1f);
            g.transform.localPosition = new Vector3(lado == 0 ? -w * 0.5f : w * 0.5f, 0f, 0f);
        }

        // tira brillante en la superficie
        var gt = new GameObject("Brillo"); gt.transform.SetParent(raiz, false);
        tira = gt.AddComponent<SpriteRenderer>();
        tira.sprite = frTira[0]; tira.sortingOrder = 23;
        if (mat != null) tira.sharedMaterial = mat;
        gt.transform.localScale = new Vector3(1f, escY, 1f);
        gt.transform.localPosition = new Vector3(0f, (h - 0.04f) - 16f / pxu, 0f);

        // el brillo (luz) que ya tenia se conserva
        enabled = true;
        hecho = true;
    }
    bool hecho;

    Transform CrearCapa(Transform raiz, string nombre, Sprite sp, Material mat, float w, float h, int orden, bool voltear, Color col)
    {
        var g = new GameObject(nombre); g.transform.SetParent(raiz, false);
        var r = g.AddComponent<SpriteRenderer>();
        r.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
        r.sortingOrder = orden;
        if (mat != null) r.sharedMaterial = mat;
        if (sp != null)
        {
            r.sprite = sp; r.drawMode = SpriteDrawMode.Tiled; r.tileMode = SpriteTileMode.Continuous;
            r.size = new Vector2(w + anchoTile * 2f, h * 1.5f);
            r.flipX = voltear;
            r.color = col;
        }
        else
        {
            r.sprite = JugadorRef.SpriteBlanco(); r.color = new Color(1f, 0.4f, 0.05f, col.a);
            g.transform.localScale = new Vector3(w, h, 1f);
        }
        g.transform.localPosition = new Vector3(0f, -0.1f, 0f);
        return g.transform;
    }

    void Update()
    {
        if (!hecho || mascara == null) return;
        int f = Mathf.FloorToInt(Time.time * 8f) % Cuadros;
        mascara.sprite = frMasc[f];
        tira.sprite = frTira[f];
        if (anchoTile > 0.01f)
        {
            float o1 = Mathf.Repeat(Time.time * VelCapa1, anchoTile);
            float o2 = Mathf.Repeat(Time.time * VelCapa2, anchoTile);
            capa1.localPosition = new Vector3(-0f + o1 - anchoTile * 0.0f, capa1.localPosition.y, 0f);
            capa2.localPosition = new Vector3(o2, capa2.localPosition.y, 0f);
        }
    }
}
