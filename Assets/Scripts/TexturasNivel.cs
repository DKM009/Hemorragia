using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Pone las texturas de Pedro en los niveles SIN tocar escenas:
///  - Pisos, techos, paredes y plataformas: tiles que se repiten.
///  - Puertas, consola, valvula, tanques, contenedor: su dibujo en vez del cuadro de color.
///  - Decorado de fondo (columnas, maquinas, carne, tentaculos...) detras de todo, sin colisiones.
/// F8 alterna entre texturas y cuadros de color para comparar.
/// Requiere correr Tools > Hemorragia > Texturas - Configurar una vez.
/// </summary>
public class TexturasNivel : MonoBehaviour
{
    static BancoTexturas banco;
    static readonly Dictionary<string, Sprite> cacheSpr = new Dictionary<string, Sprite>();
    public static bool Activo = true;

    readonly List<SpriteRenderer> originales = new List<SpriteRenderer>();
    readonly List<GameObject> nuevos = new List<GameObject>();

    class Marca : MonoBehaviour { }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Registrar() { SceneManager.sceneLoaded += (e, m) => Aplicar(); }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Aplicar()
    {
        string esc = SceneManager.GetActiveScene().name;
        if (!esc.StartsWith("Nivel")) return;
        if (banco == null) banco = Resources.Load<BancoTexturas>("BancoTexturas");
        if (banco == null) { Debug.LogWarning("[Texturas] Falta correr Tools > Hemorragia > Texturas - Configurar"); return; }
        if (GameObject.Find("Arte_Escena") != null) return;   // texturas ya puestas en la escena (Texturas movibles)
        if (FindFirstObjectByType<TexturasNivel>() != null) return;
        var go = new GameObject("TexturasNivel");
        var tn = go.AddComponent<TexturasNivel>();
        tn.Construir(esc);
    }

    void Update()
    {
        if (!TeclaF8()) return;
        Activo = !Activo;
        foreach (var o in originales) if (o != null) o.enabled = !Activo;
        foreach (var n in nuevos) if (n != null) n.SetActive(Activo);
    }

    static bool TeclaF8()
    {
#if ENABLE_INPUT_SYSTEM
        var k = UnityEngine.InputSystem.Keyboard.current;
        return k != null && k.f8Key.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.F8);
#endif
    }

    // ── Sprites ──────────────────────────────────────────────────────────────
    static Sprite Spr(string nombre, float ppu = 100f)
    {
        string clave = nombre + "@" + ppu;
        if (cacheSpr.TryGetValue(clave, out var s) && s != null) return s;
        var t = banco.Buscar(nombre);
        if (t == null) return null;
        t.filterMode = nombre.StartsWith("n2_") ? FilterMode.Bilinear : FilterMode.Point;
        s = Sprite.Create(t, new Rect(0, 0, t.width, t.height), new Vector2(0.5f, 0.5f), ppu, 0, SpriteMeshType.FullRect);
        cacheSpr[clave] = s;
        return s;
    }

    static bool Cuadro(SpriteRenderer sr, out float w, out float h)
    {
        w = h = 0f;
        if (sr == null || sr.sprite == null) return false;
        var t = sr.transform;
        if (Mathf.Abs(t.eulerAngles.z) > 0.5f && Mathf.Abs(Mathf.Abs(t.eulerAngles.z) - 360f) > 0.5f) return false;
        Vector3 ls = t.lossyScale;
        if (Mathf.Abs(ls.x) < 0.001f || Mathf.Abs(ls.y) < 0.001f) return false;
        w = Mathf.Abs(ls.x * sr.sprite.bounds.size.x);
        h = Mathf.Abs(ls.y * sr.sprite.bounds.size.y);
        return true;
    }

    Color Tinte(Color orig, float mezcla, float base0)
    {
        Color b = new Color(base0, base0 * 0.96f, base0 * 0.94f, 1f);
        return Color.Lerp(b, orig, mezcla);
    }

    /// <summary>Rellena el cuadro con un tile que se repite.</summary>
    void Teselar(SpriteRenderer sr, string tile, float ppu, float base0)
    {
        if (!Cuadro(sr, out float w, out float h)) return;
        var sp = Spr(tile, ppu); if (sp == null) return;
        var go = new GameObject("Tex_" + tile);
        go.transform.SetParent(sr.transform, false);
        Vector3 ls = sr.transform.lossyScale;
        go.transform.localScale = new Vector3(1f / ls.x, 1f / ls.y, 1f);
        var r = go.AddComponent<SpriteRenderer>();
        r.sprite = sp; r.drawMode = SpriteDrawMode.Tiled; r.tileMode = SpriteTileMode.Continuous;
        r.size = new Vector2(w, h);
        r.color = Tinte(sr.color, 0.3f, base0);
        r.sortingLayerID = sr.sortingLayerID; r.sortingOrder = sr.sortingOrder;
        sr.enabled = false; originales.Add(sr); nuevos.Add(go);
        sr.gameObject.AddComponent<Marca>();
    }

    /// <summary>Pone un dibujo ajustado al cuadro (estirado o con su proporcion).</summary>
    void Ajustar(SpriteRenderer sr, string nombre, bool estirar, float mezcla)
    {
        if (!Cuadro(sr, out float w, out float h)) return;
        var sp = Spr(nombre); if (sp == null) return;
        float sw = sp.bounds.size.x, sh = sp.bounds.size.y;
        float fw = w, fh = h;
        if (!estirar)
        {
            float k = h / sh; fw = sw * k; fh = h;
            if (fw > w * 1.6f) { k = w * 1.6f / sw; fw = sw * k; fh = sh * k; }
        }
        var go = new GameObject("Tex_" + nombre);
        go.transform.SetParent(sr.transform, false);
        Vector3 ls = sr.transform.lossyScale;
        go.transform.localScale = new Vector3(fw / sw / Mathf.Abs(ls.x), fh / sh / Mathf.Abs(ls.y), 1f);
        // base apoyada en la base del cuadro
        go.transform.position = new Vector3(sr.bounds.center.x, sr.bounds.min.y + fh * 0.5f, sr.transform.position.z);
        var r = go.AddComponent<SpriteRenderer>();
        r.sprite = sp; r.color = Color.Lerp(Color.white, sr.color, mezcla);
        r.sortingLayerID = sr.sortingLayerID; r.sortingOrder = sr.sortingOrder;
        sr.enabled = false; originales.Add(sr); nuevos.Add(go);
        sr.gameObject.AddComponent<Marca>();
    }

    // ── Construccion ─────────────────────────────────────────────────────────
    void Construir(string escena)
    {
        if (escena == "Nivel1") Nivel1Arreglos();
        foreach (var sr in FindObjectsByType<SpriteRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (sr == null || !sr.enabled || sr.GetComponent<Marca>() != null) continue;
            string n = sr.gameObject.name;
            if (n.StartsWith("Tex_") || n.StartsWith("Gemini") || n == "Patio" || n == "Sprite" || n == "Player") continue;
            if (escena.Contains("2") && Nivel2Arte(sr, n)) continue;
            if (escena == "Nivel1" && Nivel1Arte(sr, n)) continue;
            if (n.Contains("Magma") || n.Contains("Lava") || n.Contains("Fluido") || n.Contains("Mancha") || n.Contains("Brillante")) continue;
            if (n.StartsWith("Purgador") || n.StartsWith("LaFosa") || n.StartsWith("Nota") || n.StartsWith("Barrera")) continue;

            if (n.StartsWith("Puerta_")) { PuertaDe(sr, n); continue; }
            if (n == "Panel_Control") { Ajustar(sr, "consola", false, 0.2f); continue; }
            if (n == "Valvula") { Ajustar(sr, "valvula_rueda", false, 0.0f); continue; }
            if (n.StartsWith("Caldera_Der_Cuerpo")) { Ajustar(sr, "tanque_1", true, 0.1f); continue; }
            if (n.StartsWith("Caldera_Izq_Cuerpo")) { Teselar(sr, "Tile_Plat", 200f, 0.75f); continue; }
            if (n == "Escondite_CajaGrande") { Ajustar(sr, "contenedor_madera_grande", true, 0.15f); continue; }
            if (n.StartsWith("Caja_Est") || n.StartsWith("Caja_Metal")) { Ajustar(sr, "caja_carton", false, 0.25f); continue; }

            if (n.StartsWith("Suelo")) Teselar(sr, "Tile_Suelo", 200f, 0.8f);
            else if (n.StartsWith("Techo")) Teselar(sr, "Tile_Techo", 200f, 0.6f);
            else if (n.StartsWith("Pared") || n.StartsWith("Muro") || n.StartsWith("Relleno")) Teselar(sr, "Tile_Pared", 200f, 0.7f);
            else if (n.StartsWith("Plataforma") || n.StartsWith("Plat_") || n.StartsWith("Estante") || n.StartsWith("Escalon") || n.StartsWith("Camion")
                     || n.StartsWith("Escombro") || n.StartsWith("Barril") || n.StartsWith("Escondite")) Teselar(sr, "Tile_Plat", 200f, 0.85f);
        }
        if (escena.Contains("2")) FondosN2();
        Decorar(escena);
    }

    // ── Nivel 1: el camion del patio quedaba flotando; se baja al piso y se agrega un tambo de escalon ──
    void Nivel1Arreglos()
    {
        const float suelo = -4.0f;   // parte alta del suelo principal
        Bajar("Camion_Base", suelo);
        Bajar("Camion_Cabina", suelo);
        var cab = GameObject.Find("Camion_Cabina"); var techo = GameObject.Find("Camion_Techo");
        if (cab != null && techo != null)
        {
            var sr = cab.GetComponent<SpriteRenderer>();
            float cima = sr != null ? sr.bounds.max.y : suelo + 1.8f;
            var tp = techo.transform.position; techo.transform.position = new Vector3(tp.x, cima - 0.1f, tp.z);
        }
        if (GameObject.Find("Tambo_Escalon") != null || banco.Buscar("n2_barril_a") == null) return;
        var sp = Spr("n2_barril_a"); if (sp == null) return;
        var go = new GameObject("Tambo_Escalon");
        float alto = 1.1f;
        float k = alto / sp.bounds.size.y;
        go.transform.localScale = new Vector3(k, k, 1f);
        go.transform.position = new Vector3(0.3f, suelo + alto * 0.5f, 0f);
        var r = go.AddComponent<SpriteRenderer>();
        r.sprite = sp; r.sortingOrder = 1;
        go.AddComponent<BoxCollider2D>();
    }

    static void Bajar(string nombre, float suelo)
    {
        var go = GameObject.Find(nombre); if (go == null) return;
        var sr = go.GetComponent<SpriteRenderer>(); if (sr == null) return;
        float dy = suelo - sr.bounds.min.y;
        if (dy > -0.01f) return;   // ya esta apoyado (o por debajo)
        go.transform.position += new Vector3(0f, dy, 0f);
    }

    static bool PuertaTieneLlave(SpriteRenderer sr)
    {
        return sr.GetComponent<PuertaConLlave>() != null || sr.GetComponentInParent<PuertaConLlave>() != null;
    }

    bool Nivel1Arte(SpriteRenderer sr, string n)
    {
        if (banco.Buscar("n2_cofre") == null) return false;
        if (n.StartsWith("Barril")) { Ajustar(sr, n.EndsWith("A") ? "n2_barril_a" : "n2_barril_b", false, 0.05f); return true; }
        if (n.StartsWith("Puerta_")) { Ajustar(sr, PuertaTieneLlave(sr) ? "n2_puerta_amarilla_a" : "n2_puerta_verde", true, 0.15f); return true; }
        if (n == "Tuberia_Caliente" || n == "Tuberia_Emergencia") { Ajustar(sr, "n2_tuberia_naranja", true, 0f); return true; }
        if (n == "Tuberia_Izq" || n.StartsWith("Tub_Techo")) { Ajustar(sr, "n2_tuberia_gris", true, 0f); return true; }
        if (n.StartsWith("Soporte_")) { Ajustar(sr, "n2_tuberia_vertical", true, 0f); return true; }
        if (n == "Escalon_Entrada") { Ajustar(sr, "n2_plataforma", true, 0.1f); return true; }
        if (n.StartsWith("Escalon")) { Ajustar(sr, "n2_escalon", true, 0.1f); return true; }
        if (n.StartsWith("Estante") || n == "Plataforma_Izq") { Ajustar(sr, "n2_pasarela", true, 0.1f); return true; }
        if (n.StartsWith("Caja_Metal")) { Ajustar(sr, "n2_cofre", true, 0.1f); return true; }
        if (n == "Camion_Base")   { Ajustar(sr, "n2_cofre", true, 0.1f); return true; }
        if (n == "Camion_Cabina") { Ajustar(sr, "n2_escalon", true, 0.1f); return true; }
        if (n == "Camion_Techo")  { Ajustar(sr, "n2_plataforma", true, 0.1f); return true; }
        return false;
    }

    // ── Nivel 2: arte Gemini (Texturas/Nivel2, prefijo n2_) ──────────────────
    static string[] puertasN2 = { "n2_puerta_verde", "n2_puerta_amarilla_a", "n2_puerta_amarilla_b" };
    bool Nivel2Arte(SpriteRenderer sr, string n)
    {
        if (banco.Buscar("n2_barril_a") == null) return false;   // aun sin configurar
        if (GameObject.Find("N2_Arte") != null) return false;     // texturas ya puestas en la escena a mano
        if (n.StartsWith("Puerta_"))
        {
            Ajustar(sr, PuertaTieneLlave(sr) ? "n2_puerta_amarilla_a" : "n2_puerta_verde", true, 0.15f); return true;
        }
        if (n == "Lava_N2C") { Ajustar(sr, "n2_lava_b", true, 0f); return true; }
        if (n.StartsWith("Caldera_Izq_Cuerpo")) { Ajustar(sr, "n2_caldera_horizontal", true, 0.1f); return true; }
        if (n.StartsWith("Caldera_Der_Cuerpo")) { Ajustar(sr, "n2_caldera_vertical", true, 0.1f); return true; }
        if (n == "Escondite_RanuraFria") { Ajustar(sr, "n2_escondite_frio", false, 0f); return true; }
        if (n == "Escondite_FinalPasillo") { Ajustar(sr, "n2_cofre", true, 0.1f); return true; }
        if (n.StartsWith("Tubo_Cal")) { Ajustar(sr, "n2_tuberia_naranja", true, 0f); return true; }
        if (n.StartsWith("Tubo_Frio")) { Ajustar(sr, "n2_tuberia_hielo", true, 0f); return true; }
        if (n.StartsWith("Tubo_Suelo") || n == "Tuberia_Vapor_A") { Ajustar(sr, "n2_tuberia_gris", true, 0f); return true; }
        if (n == "Tuberia_Vapor_B") { Ajustar(sr, "n2_tuberia_vertical", true, 0f); return true; }
        if (n.StartsWith("Plat_") || n.StartsWith("Plataforma")) { Ajustar(sr, "n2_plataforma", true, 0.1f); return true; }
        if (n.StartsWith("Escalon")) { Ajustar(sr, "n2_escalon", true, 0.1f); return true; }
        return false;
    }

    void FondosN2()
    {
        if (GameObject.Find("N2_Arte") != null) return;
        var a0 = GameObject.Find("Puerta_Entrada_N2A"); var a1 = GameObject.Find("Salida_N2B");
        var c0 = GameObject.Find("Spawn_N2C"); var c1 = GameObject.Find("Msg_N2C_Final");
        if (a0 == null || a1 == null || c0 == null || c1 == null) return;
        PonerFondo("n2_fondo_a", a0.transform.position.x - 4f, a1.transform.position.x + 4f);
        PonerFondo("n2_fondo_b", c0.transform.position.x - 12f, c1.transform.position.x + 12f);
    }

    void PonerFondo(string tex, float x0, float x1)
    {
        var sp = Spr(tex, 2048f / 26f); if (sp == null) return;
        float ancho = sp.bounds.size.x; int i = 0;
        for (float x = x0 + ancho * 0.5f; x - ancho * 0.5f < x1; x += ancho, i++)
        {
            var go = new GameObject("Deco_Fondo_" + tex);
            go.transform.position = new Vector3(x, -1.5f, 5f);
            var r = go.AddComponent<SpriteRenderer>();
            r.sprite = sp; r.sortingOrder = -9; r.flipX = (i % 2 == 1);
            r.color = new Color(0.7f, 0.7f, 0.7f, 1f);
            nuevos.Add(go);
        }
    }

    static string[] puertas = { "puerta_madera_marron", "puerta_arco_doble", "puerta_blanca_panel" };
    void PuertaDe(SpriteRenderer sr, string n)
    {
        int h = 0; foreach (char c in n) h = h * 31 + c;
        string tex = puertas[Mathf.Abs(h) % puertas.Length];
        Ajustar(sr, tex, true, 0.45f);   // conserva un tinte del color original (indica la llave)
    }

    // ── Decorado de fondo ────────────────────────────────────────────────────
    struct Prop { public string tex; public float alto; public bool colgado; public bool pared; }

    void Decorar(string escena)
    {
        bool n2 = escena.Contains("2");
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
            if (banco.Buscar("n2_barril_a") != null)
            {
                lista.Add(new Prop { tex = "n2_barril_a", alto = 1.6f });
                lista.Add(new Prop { tex = "n2_barril_b", alto = 1.6f });
                lista.Add(new Prop { tex = "n2_escombros", alto = 1.0f });
                lista.Add(new Prop { tex = "n2_tubo_roto", alto = 1.2f });
            }
        }

        float x0 = n2 ? -16f : -14f, x1 = n2 ? 82f : 14f;
        for (float x = x0; x < x1; x += 3.4f + (float)rng.NextDouble() * 3.2f)
        {
            var p = lista[rng.Next(lista.Count)];
            var sp = Spr(p.tex); if (sp == null) continue;
            float ppu = sp.texture.height / p.alto;
            var sp2 = Spr(p.tex, ppu); if (sp2 == null) continue;

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

            var go = new GameObject("Deco_" + p.tex);
            float desfase = p.pared ? p.alto * 0.5f + 0.6f : 0f;
            float py = p.colgado ? y - p.alto * 0.5f : y + p.alto * 0.5f + desfase;
            go.transform.position = new Vector3(x, py, 0f);
            var r = go.AddComponent<SpriteRenderer>();
            r.sprite = sp2; r.sortingOrder = -5 + rng.Next(0, 3) - 1;
            float d = 0.55f + (float)rng.NextDouble() * 0.15f;
            r.color = new Color(d, d, d * 1.02f, 1f);
            if (rng.NextDouble() < 0.5) r.flipX = true;
            nuevos.Add(go);
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
            if (h.collider.GetComponent<Rigidbody2D>() != null && h.collider.GetComponent<Rigidbody2D>().bodyType == RigidbodyType2D.Dynamic) continue;
            if (h.point.y > -2.0f && !(n2 && x > 60f)) continue;   // ignora estantes y plataformas altas
            y = h.point.y;
            return true;
        }
        return false;
    }
}
