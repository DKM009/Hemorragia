using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Pone TODAS las texturas del nivel (pisos, paredes, puertas, tuberias, escalones, decorado y fondos)
/// como objetos reales dentro de la escena, para verlos y moverlos en el editor.
/// Todo se llama "Arte_..." (escribe Arte_ en el buscador de la Hierarchy para verlos todos).
/// Se puede correr varias veces: borra lo anterior y lo vuelve a crear.
/// </summary>
public static class HornearEscena
{
    const string Grupo = "Arte_Escena";
    static readonly Dictionary<string, Sprite> cache = new Dictionary<string, Sprite>();

    [MenuItem("Tools/Hemorragia/Texturas movibles - Nivel 1 (poner en escena)")]
    public static void Nivel1() { Hornear("Nivel1"); }

    [MenuItem("Tools/Hemorragia/Texturas movibles - Nivel 2 (poner en escena)")]
    public static void Nivel2() { Hornear("Nivel2"); }

    [MenuItem("Tools/Hemorragia/Texturas movibles - Quitar de la escena abierta")]
    public static void Quitar()
    {
        var esc = SceneManager.GetActiveScene();
        Limpiar();
        EditorSceneManager.MarkSceneDirty(esc);
        EditorSceneManager.SaveScene(esc);
        EditorUtility.DisplayDialog("Texturas", "Listo: se quitaron las texturas movibles. El juego volvera a ponerlas solo al darle Play.", "OK");
    }

    // ── Proceso principal ────────────────────────────────────────────────────
    static void Hornear(string esc)
    {
        var actual = SceneManager.GetActiveScene();
        if (actual.name != esc)
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            actual = EditorSceneManager.OpenScene("Assets/Scenes/" + esc + ".unity", OpenSceneMode.Single);
        }
        cache.Clear();
        if (Sp("n2_barril_a") == null)
        {
            EditorUtility.DisplayDialog("Texturas", "No encuentro las texturas del Nivel 2 (Assets/Texturas/Nivel2).", "OK");
            return;
        }

        Limpiar();
        bool n1 = esc == "Nivel1", n2 = esc == "Nivel2";
        if (n1) Nivel1Arreglos();

        var raiz = new GameObject(Grupo);
        Undo.RegisterCreatedObjectUndo(raiz, "Arte escena");

        var todos = Object.FindObjectsByType<SpriteRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        int cuenta = 0;
        foreach (var sr in todos)
        {
            if (sr == null || !sr.enabled || sr.sprite == null) continue;
            if (sr.GetComponent<N2Oculto>() != null || sr.GetComponent<ArteHorneado>() != null) continue;
            if (sr.transform.IsChildOf(raiz.transform)) continue;
            if (Procesar(sr, sr.gameObject.name, n1, n2)) cuenta++;
        }

        Physics2D.SyncTransforms();
        Decorar(raiz.transform, esc, n2);
        if (n2) FondosN2(raiz.transform);

        EditorSceneManager.MarkSceneDirty(actual);
        EditorSceneManager.SaveScene(actual);
        Selection.activeGameObject = raiz;
        Debug.Log("[Arte] " + esc + ": " + cuenta + " texturas colocadas + decorado. Busca 'Arte_' en la Hierarchy.");
        EditorUtility.DisplayDialog("Texturas " + esc,
            "Listo (" + cuenta + " texturas + decorado).\n\nEn la Hierarchy escribe  Arte_  en el buscador para ver todo. Selecciona uno y muevelo con W (mover), R (escalar) o T (cambiar tamano).", "OK");
    }

    static void Limpiar()
    {
        foreach (var m in Object.FindObjectsByType<N2Oculto>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (m == null) continue;
            var sr = m.GetComponent<SpriteRenderer>();
            if (sr != null) sr.enabled = true;
            Object.DestroyImmediate(m);
        }
        foreach (var a in Object.FindObjectsByType<ArteHorneado>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (a != null) Object.DestroyImmediate(a.gameObject);
        foreach (var nombre in new[] { "N2_Arte", Grupo })
        {
            var g = GameObject.Find(nombre);
            int guardia = 0;
            while (g != null && guardia++ < 10) { Object.DestroyImmediate(g); g = GameObject.Find(nombre); }
        }
    }

    // ── Reglas (igual que el modo automatico del juego) ──────────────────────
    static bool Procesar(SpriteRenderer sr, string n, bool n1, bool n2)
    {
        if (n.StartsWith("Tex_") || n.StartsWith("Gemini") || n == "Patio" || n == "Sprite" || n == "Player") return false;
        if (n2 && ArteNivel2(sr, n)) return true;
        if (n1 && ArteNivel1(sr, n)) return true;
        if (n.Contains("Magma") || n.Contains("Lava") || n.Contains("Fluido") || n.Contains("Mancha") || n.Contains("Brillante")) return false;
        if (n.StartsWith("Purgador") || n.StartsWith("LaFosa") || n.StartsWith("Nota") || n.StartsWith("Barrera")) return false;

        if (n == "Panel_Control") return Ajustar(sr, "consola", false, 0.2f);
        if (n == "Valvula") return Ajustar(sr, "valvula_rueda", false, 0f);
        if (n.StartsWith("Caldera_Der_Cuerpo")) return Ajustar(sr, "tanque_1", true, 0.1f);
        if (n.StartsWith("Caldera_Izq_Cuerpo")) return Teselar(sr, "Tile_Plat", 0.75f);
        if (n == "Escondite_CajaGrande") return Ajustar(sr, "contenedor_madera_grande", true, 0.15f);
        if (n.StartsWith("Caja_Est") || n.StartsWith("Caja_Metal")) return Ajustar(sr, "caja_carton", false, 0.25f);

        if (n.StartsWith("Suelo")) return Teselar(sr, "Tile_Suelo", 0.8f);
        if (n.StartsWith("Techo")) return Teselar(sr, "Tile_Techo", 0.6f);
        if (n.StartsWith("Pared") || n.StartsWith("Muro") || n.StartsWith("Relleno")) return Teselar(sr, "Tile_Pared", 0.7f);
        if (n.StartsWith("Plataforma") || n.StartsWith("Plat_") || n.StartsWith("Estante") || n.StartsWith("Escalon") || n.StartsWith("Camion")
            || n.StartsWith("Escombro") || n.StartsWith("Barril") || n.StartsWith("Escondite")) return Teselar(sr, "Tile_Plat", 0.85f);
        return false;
    }

    static bool PuertaTieneLlave(SpriteRenderer sr)
    {
        return sr.GetComponent<PuertaConLlave>() != null || sr.GetComponentInParent<PuertaConLlave>() != null;
    }

    static bool ArteNivel1(SpriteRenderer sr, string n)
    {
        if (n.StartsWith("Barril")) return Ajustar(sr, n.EndsWith("A") ? "n2_barril_a" : "n2_barril_b", false, 0.05f);
        if (n.StartsWith("Puerta_")) return Ajustar(sr, PuertaTieneLlave(sr) ? "n2_puerta_amarilla_a" : "n2_puerta_verde", true, 0.15f);
        if (n == "Tuberia_Caliente" || n == "Tuberia_Emergencia") return Ajustar(sr, "n2_tuberia_naranja", true, 0f);
        if (n == "Tuberia_Izq" || n.StartsWith("Tub_Techo")) return Ajustar(sr, "n2_tuberia_gris", true, 0f);
        if (n.StartsWith("Soporte_")) return Ajustar(sr, "n2_tuberia_vertical", true, 0f);
        if (n == "Escalon_Entrada") return Ajustar(sr, "n2_plataforma", true, 0.1f);
        if (n.StartsWith("Escalon")) return Ajustar(sr, "n2_escalon", true, 0.1f);
        if (n.StartsWith("Estante") || n == "Plataforma_Izq") return Ajustar(sr, "n2_pasarela", true, 0.1f);
        if (n.StartsWith("Caja_Metal")) return Ajustar(sr, "n2_cofre", true, 0.1f);
        if (n == "Camion_Base") return Ajustar(sr, "n2_cofre", true, 0.1f);
        if (n == "Camion_Cabina") return Ajustar(sr, "n2_escalon", true, 0.1f);
        if (n == "Camion_Techo") return Ajustar(sr, "n2_plataforma", true, 0.1f);
        return false;
    }

    static bool ArteNivel2(SpriteRenderer sr, string n)
    {
        if (n.StartsWith("Puerta_")) return Ajustar(sr, PuertaTieneLlave(sr) ? "n2_puerta_amarilla_a" : "n2_puerta_verde", true, 0.15f);
        if (n == "Lava_N2C") return Ajustar(sr, "n2_lava_b", true, 0f);
        if (n.StartsWith("Caldera_Izq_Cuerpo")) return Ajustar(sr, "n2_caldera_horizontal", true, 0.1f);
        if (n.StartsWith("Caldera_Der_Cuerpo")) return Ajustar(sr, "n2_caldera_vertical", true, 0.1f);
        if (n == "Escondite_RanuraFria") return Ajustar(sr, "n2_escondite_frio", false, 0f);
        if (n == "Escondite_FinalPasillo") return Ajustar(sr, "n2_cofre", true, 0.1f);
        if (n.StartsWith("Tubo_Cal")) return Ajustar(sr, "n2_tuberia_naranja", true, 0f);
        if (n.StartsWith("Tubo_Frio")) return Ajustar(sr, "n2_tuberia_hielo", true, 0f);
        if (n.StartsWith("Tubo_Suelo") || n == "Tuberia_Vapor_A") return Ajustar(sr, "n2_tuberia_gris", true, 0f);
        if (n == "Tuberia_Vapor_B") return Ajustar(sr, "n2_tuberia_vertical", true, 0f);
        if (n.StartsWith("Plat_") || n.StartsWith("Plataforma")) return Ajustar(sr, "n2_plataforma", true, 0.1f);
        if (n.StartsWith("Escalon")) return Ajustar(sr, "n2_escalon", true, 0.1f);
        return false;
    }

    // ── Nivel 1: bajar el camion al piso y poner el tambo-escalon ────────────
    static void Nivel1Arreglos()
    {
        const float suelo = -4.0f;
        Bajar("Camion_Base", suelo);
        Bajar("Camion_Cabina", suelo);
        var cab = GameObject.Find("Camion_Cabina"); var techo = GameObject.Find("Camion_Techo");
        if (cab != null && techo != null)
        {
            var sr = cab.GetComponent<SpriteRenderer>();
            float cima = sr != null ? sr.bounds.max.y : suelo + 1.8f;
            var tp = techo.transform.position;
            Undo.RecordObject(techo.transform, "Techo camion");
            techo.transform.position = new Vector3(tp.x, cima - 0.1f, tp.z);
        }
        if (GameObject.Find("Tambo_Escalon") != null) return;
        var sp = Sp("n2_barril_a"); if (sp == null) return;
        var go = new GameObject("Tambo_Escalon");
        Undo.RegisterCreatedObjectUndo(go, "Tambo");
        var baseCam = GameObject.Find("Camion_Base");
        if (baseCam != null && baseCam.transform.parent != null) go.transform.SetParent(baseCam.transform.parent, false);
        Vector3 ps = Escala(go.transform.parent);
        float alto = 1.1f;
        float k = alto / sp.bounds.size.y;
        go.transform.position = new Vector3(0.3f, suelo + alto * 0.5f, 0f);
        go.transform.localScale = new Vector3(k / Mathf.Abs(ps.x), k / Mathf.Abs(ps.y), 1f);
        var r = go.AddComponent<SpriteRenderer>();
        r.sprite = sp; r.sortingOrder = 1;
        var bc = go.AddComponent<BoxCollider2D>();
        bc.size = sp.bounds.size; bc.offset = sp.bounds.center;
    }

    static void Bajar(string nombre, float suelo)
    {
        var go = GameObject.Find(nombre); if (go == null) return;
        var sr = go.GetComponent<SpriteRenderer>(); if (sr == null) return;
        float dy = suelo - sr.bounds.min.y;
        if (dy > -0.01f) return;
        Undo.RecordObject(go.transform, "Bajar " + nombre);
        go.transform.position += new Vector3(0f, dy, 0f);
    }

    // ── Colocar texturas ─────────────────────────────────────────────────────
    static Vector3 Escala(Transform t) { return t == null ? Vector3.one : t.lossyScale; }

    static bool Medir(SpriteRenderer sr, out Vector2 centro, out Vector2 tam)
    {
        centro = Vector2.zero; tam = Vector2.zero;
        if (sr == null || sr.sprite == null) return false;
        var t = sr.transform;
        float z = t.eulerAngles.z;
        if (Mathf.Abs(z) > 0.5f && Mathf.Abs(Mathf.Abs(z) - 360f) > 0.5f) return false;
        Vector3 ls = t.lossyScale;
        if (Mathf.Abs(ls.x) < 0.001f || Mathf.Abs(ls.y) < 0.001f) return false;
        Vector3 c = t.TransformPoint(sr.sprite.bounds.center);
        centro = new Vector2(c.x, c.y);
        tam = new Vector2(Mathf.Abs(ls.x * sr.sprite.bounds.size.x), Mathf.Abs(ls.y * sr.sprite.bounds.size.y));
        return true;
    }

    static GameObject Hijo(SpriteRenderer sr)
    {
        var go = new GameObject("Arte_" + sr.gameObject.name);
        if (sr.transform.parent != null) go.transform.SetParent(sr.transform.parent, false);
        go.AddComponent<ArteHorneado>();
        Undo.RegisterCreatedObjectUndo(go, "Arte");
        return go;
    }

    static void Ocultar(SpriteRenderer sr)
    {
        sr.enabled = false;
        if (sr.GetComponent<N2Oculto>() == null) sr.gameObject.AddComponent<N2Oculto>();
    }

    static bool Ajustar(SpriteRenderer sr, string tex, bool estirar, float mezcla)
    {
        Vector2 c, tam;
        if (!Medir(sr, out c, out tam)) return false;
        var sp = Sp(tex); if (sp == null) return false;
        float sw = sp.bounds.size.x, sh = sp.bounds.size.y;
        float fw = tam.x, fh = tam.y;
        if (!estirar)
        {
            float k = tam.y / sh; fw = sw * k; fh = tam.y;
            if (fw > tam.x * 1.6f) { k = tam.x * 1.6f / sw; fw = sw * k; fh = sh * k; }
        }
        var go = Hijo(sr);
        Vector3 ps = Escala(go.transform.parent);
        go.transform.position = new Vector3(c.x, c.y - tam.y * 0.5f + fh * 0.5f, sr.transform.position.z);
        go.transform.localScale = new Vector3(fw / sw / Mathf.Abs(ps.x), fh / sh / Mathf.Abs(ps.y), 1f);
        var r = go.AddComponent<SpriteRenderer>();
        r.sprite = sp; r.color = Color.Lerp(Color.white, sr.color, mezcla);
        r.sortingLayerID = sr.sortingLayerID; r.sortingOrder = sr.sortingOrder;
        Ocultar(sr);
        return true;
    }

    static bool Teselar(SpriteRenderer sr, string tile, float base0)
    {
        Vector2 c, tam;
        if (!Medir(sr, out c, out tam)) return false;
        var sp = Sp(tile); if (sp == null) return false;
        var go = Hijo(sr);
        Vector3 ps = Escala(go.transform.parent);
        go.transform.position = new Vector3(c.x, c.y, sr.transform.position.z);
        go.transform.localScale = new Vector3(1f / Mathf.Abs(ps.x), 1f / Mathf.Abs(ps.y), 1f);
        var r = go.AddComponent<SpriteRenderer>();
        r.sprite = sp; r.drawMode = SpriteDrawMode.Tiled; r.tileMode = SpriteTileMode.Continuous;
        r.size = tam;
        Color b = new Color(base0, base0 * 0.96f, base0 * 0.94f, 1f);
        r.color = Color.Lerp(b, sr.color, 0.3f);
        r.sortingLayerID = sr.sortingLayerID; r.sortingOrder = sr.sortingOrder;
        Ocultar(sr);
        return true;
    }

    // ── Decorado de fondo ────────────────────────────────────────────────────
    struct Prop { public string tex; public float alto; public bool colgado; public bool pared; }

    static void Decorar(Transform raiz, string escena, bool n2)
    {
        var rng = new System.Random(escena.GetHashCode());
        var lista = new List<Prop>();
        lista.Add(new Prop { tex = "columna_oxidada", alto = 5.0f });
        lista.Add(new Prop { tex = "maquinas_conjunto", alto = 3.2f });
        lista.Add(new Prop { tex = n2 ? "tanque_2" : "tanque_1", alto = 3.4f });
        lista.Add(new Prop { tex = "consola", alto = 1.8f });
        lista.Add(new Prop { tex = "casillero_gris", alto = 2.2f });
        lista.Add(new Prop { tex = "reja_1", alto = 1.9f });
        if (n2)
        {
            lista.Add(new Prop { tex = "escombros_carne", alto = 1.1f });
            lista.Add(new Prop { tex = "carne_muro_1", alto = 2.6f, pared = true });
            lista.Add(new Prop { tex = "tentaculo", alto = 3.2f, colgado = true });
            lista.Add(new Prop { tex = "tentaculo", alto = 2.4f, colgado = true });
            lista.Add(new Prop { tex = "n2_barril_a", alto = 1.6f });
            lista.Add(new Prop { tex = "n2_barril_b", alto = 1.6f });
            lista.Add(new Prop { tex = "n2_escombros", alto = 1.0f });
            lista.Add(new Prop { tex = "n2_tubo_roto", alto = 1.2f });
        }

        float x0 = n2 ? -16f : -14f, x1 = n2 ? 82f : 14f;
        for (float x = x0; x < x1; x += 3.4f + (float)rng.NextDouble() * 3.2f)
        {
            var p = lista[rng.Next(lista.Count)];
            var sp = Sp(p.tex); if (sp == null) continue;
            float k = p.alto / sp.bounds.size.y;

            float y;
            if (p.colgado)
            {
                var hit = Physics2D.Raycast(new Vector2(x, 0f), Vector2.up, 12f);
                if (hit.collider == null || hit.collider.isTrigger) continue;
                y = hit.point.y;
            }
            else
            {
                if (!BuscarSuelo(x, n2, out y)) continue;
            }

            var go = new GameObject("Arte_Deco_" + p.tex);
            go.transform.SetParent(raiz, false);
            go.AddComponent<ArteHorneado>();
            float desfase = p.pared ? p.alto * 0.5f + 0.6f : 0f;
            float py = p.colgado ? y - p.alto * 0.5f : y + p.alto * 0.5f + desfase;
            go.transform.position = new Vector3(x, py, 0f);
            go.transform.localScale = new Vector3(k, k, 1f);
            var r = go.AddComponent<SpriteRenderer>();
            r.sprite = sp; r.sortingOrder = -5 + rng.Next(0, 3) - 1;
            float d = 0.55f + (float)rng.NextDouble() * 0.15f;
            r.color = new Color(d, d, d * 1.02f, 1f);
            if (rng.NextDouble() < 0.5) r.flipX = true;
        }
    }

    static bool BuscarSuelo(float x, bool n2, out float y)
    {
        y = 0f;
        float tope = (n2 && x > 60f) ? -4f : 1.5f;
        var hits = Physics2D.RaycastAll(new Vector2(x, tope), Vector2.down, 14f);
        foreach (var h in hits)
        {
            if (h.collider == null || h.collider.isTrigger) continue;
            var rb = h.collider.GetComponent<Rigidbody2D>();
            if (rb != null && rb.bodyType == RigidbodyType2D.Dynamic) continue;
            if (h.point.y > -2.0f && !(n2 && x > 60f)) continue;
            y = h.point.y;
            return true;
        }
        return false;
    }

    // ── Fondos del Nivel 2 ───────────────────────────────────────────────────
    static void FondosN2(Transform raiz)
    {
        var a0 = GameObject.Find("Puerta_Entrada_N2A"); var a1 = GameObject.Find("Salida_N2B");
        var c0 = GameObject.Find("Spawn_N2C"); var c1 = GameObject.Find("Msg_N2C_Final");
        if (a0 != null && a1 != null) Fondo(raiz, "n2_fondo_a", a0.transform.position.x - 4f, a1.transform.position.x + 4f);
        if (c0 != null && c1 != null) Fondo(raiz, "n2_fondo_b", c0.transform.position.x - 12f, c1.transform.position.x + 12f);
    }

    static void Fondo(Transform raiz, string tex, float x0, float x1)
    {
        var spr = Sp(tex); if (spr == null) return;
        float ancho = 26f, esc = ancho / spr.bounds.size.x; int i = 0;
        for (float x = x0 + ancho * 0.5f; x - ancho * 0.5f < x1; x += ancho, i++)
        {
            var go = new GameObject("Arte_Fondo_" + tex + "_" + i);
            go.transform.SetParent(raiz, false);
            go.AddComponent<ArteHorneado>();
            go.transform.position = new Vector3(x, -1.5f, 5f);
            go.transform.localScale = new Vector3(esc, esc, 1f);
            var r = go.AddComponent<SpriteRenderer>();
            r.sprite = spr; r.sortingOrder = -9; r.flipX = (i % 2 == 1);
            r.color = new Color(0.7f, 0.7f, 0.7f, 1f);
        }
    }

    // ── Sprites a partir de las texturas ─────────────────────────────────────
    static Sprite Sp(string nombre)
    {
        Sprite s;
        if (cache.TryGetValue(nombre, out s) && s != null) return s;
        string ruta = null;
        foreach (var g in AssetDatabase.FindAssets(nombre + " t:Texture2D", new[] { "Assets/Texturas" }))
        {
            string p = AssetDatabase.GUIDToAssetPath(g);
            if (Path.GetFileNameWithoutExtension(p) == nombre) { ruta = p; break; }
        }
        if (ruta == null) return null;
        Importar(ruta, nombre);
        s = AssetDatabase.LoadAssetAtPath<Sprite>(ruta);
        cache[nombre] = s;
        return s;
    }

    static void Importar(string ruta, string nombre)
    {
        var imp = AssetImporter.GetAtPath(ruta) as TextureImporter;
        if (imp == null) return;
        bool tile = nombre.StartsWith("Tile_");
        float ppu = tile ? 200f : 100f;
        var st = new TextureImporterSettings();
        imp.ReadTextureSettings(st);
        if (imp.textureType == TextureImporterType.Sprite && imp.spriteImportMode == SpriteImportMode.Single
            && Mathf.Approximately(imp.spritePixelsPerUnit, ppu) && st.spriteMeshType == SpriteMeshType.FullRect) return;

        imp.textureType = TextureImporterType.Sprite;
        imp.spriteImportMode = SpriteImportMode.Single;
        imp.spritePixelsPerUnit = ppu;
        imp.mipmapEnabled = false;
        imp.alphaIsTransparency = true;
        imp.textureCompression = TextureImporterCompression.Uncompressed;
        imp.filterMode = nombre.StartsWith("n2_") ? FilterMode.Bilinear : FilterMode.Point;
        if (nombre.StartsWith("n2_") && imp.maxTextureSize < 2048) imp.maxTextureSize = 2048;
        imp.ReadTextureSettings(st);
        st.spriteMeshType = SpriteMeshType.FullRect;
        if (tile) st.wrapMode = TextureWrapMode.Repeat;
        imp.SetTextureSettings(st);
        imp.SaveAndReimport();
    }
}
