using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Genera el greybox del Nivel 4 — ALTAR DE DESECHOS.
/// El climax del juego. Estructura:
///   N4-A: Pasillo Sagrado (16 x 8 u) — x=0, pasilloominoso, una pared de cabezas
///   N4-B: El Altar (24 x 14 u) — x=22, sala mas grande del juego, boss final (La Fosa)
///   N4-C: Escape (10 x 6 u) — x=50, corredor de huida hacia la caldera
///   (La caida a la caldera activa la escena final)
/// Uso: Tools > Hemorragia > Crear Nivel 4 (Altar de Desechos)
/// </summary>
public static class CrearGreyboxNivel4
{
    const string RutaSprite = "Assets/Sprites/Greybox/Cuadrado.png";

    [MenuItem("Tools/Hemorragia/Crear Nivel 4 (Altar de Desechos)")]
    public static void CrearNivel4()
    {
        if (GameObject.Find("Nivel4_Raiz") != null)
        {
            EditorUtility.DisplayDialog("Nivel 4", "Ya existe Nivel4_Raiz en la escena.", "OK");
            return;
        }

        Sprite sp = ObtenerSprite();
        if (sp == null) { Debug.LogError("No se pudo obtener sprite greybox."); return; }

        var raiz = new GameObject("Nivel4_Raiz");
        Undo.RegisterCreatedObjectUndo(raiz, "Crear Nivel 4");

        var n4a = Hijo(raiz.transform, "N4A_PasilloSagrado"); n4a.localPosition = Vector3.zero;
        var n4b = Hijo(raiz.transform, "N4B_Altar");          n4b.localPosition = new Vector3(22f, 0, 0);
        var n4c = Hijo(raiz.transform, "N4C_Escape");         n4c.localPosition = new Vector3(52f, 0, 0);

        GenN4A(n4a, sp);
        GenN4B(n4b, sp);
        GenN4C(n4c, sp);

        // Luz global: practicamente ninguna, el altar ilumina solo
        var luzGO = new GameObject("Luz_Global_N4");
        luzGO.transform.SetParent(raiz.transform, false);
        var lg = luzGO.AddComponent<Light2D>();
        lg.lightType = Light2D.LightType.Global;
        lg.color     = new Color(0.03f, 0.02f, 0.04f);
        lg.intensity = 0.03f;

        Camera cam = Camera.main;
        if (cam != null)
        {
            Undo.RecordObject(cam, "Configurar camara N4");
            cam.orthographic     = true;
            cam.orthographicSize = 5f; // la sala del altar es grande
            cam.transform.position = new Vector3(0, 0, -10);
        }

        raiz.transform.position = Vector3.zero;
        Selection.activeGameObject = raiz;
        EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        Debug.Log("Nivel 4 (Altar de Desechos) creado.\n" +
                  "N4A x=0 | N4B x=22 | N4C x=52\n" +
                  "N4B: agrega LaFosaIA al LaFosa_Final. El Altar es el punto mas alto.\n" +
                  "N4C: Trigger_CaidaCaldera activa la escena final via CambiarNivel (EscenaFinal).");
    }

    // ── N4-A: PASILLO SAGRADO (16 x 8) ────────────────────────────────────────
    static void GenN4A(Transform padre, Sprite sp)
    {
        Color c    = new Color(0.12f, 0.10f, 0.12f);
        Color m    = new Color(0.20f, 0.18f, 0.25f);
        Color purp = new Color(0.25f, 0.08f, 0.30f); // purpura ominoso
        Color cal  = new Color(0.9f, 0.42f, 0.08f);

        var e = Hijo(padre, "Estructura");
        var p = Hijo(padre, "Props");
        var l = Hijo(padre, "Luces");
        var t = Hijo(padre, "Triggers");

        Caja("Suelo",     e, sp, new Vector2(0,   -4.5f), new Vector2(16, 1),   c,    true);
        Caja("Techo",     e, sp, new Vector2(0,    4f),   new Vector2(16, 1),   c,    true);
        Caja("Pared_Izq", e, sp, new Vector2(-8.5f, 0),  new Vector2(1, 10),   m,    true);
        Caja("Pared_Der", e, sp, new Vector2( 8.5f, 0),  new Vector2(1, 10),   m,    true);

        // La pared de cabezas: cubos dispuestos en mosaico en la pared izquierda (visual)
        for (int row = 0; row < 4; row++)
        {
            for (int col = 0; col < 2; col++)
            {
                float x = -7.5f + col * 0.7f;
                float y = -3.5f + row * 1.5f;
                Color colorCabeza = Color.Lerp(new Color(0.4f, 0.2f, 0.1f), new Color(0.6f, 0.1f, 0.1f), UnityEngine.Random.value);
                Caja($"Cabeza_{row}_{col}", p, sp, new Vector2(x, y), new Vector2(0.55f, 0.55f), colorCabeza, false, 3);
            }
        }

        // Tuberia de cauterizacion (ULTIMA oportunidad antes del altar)
        Caja("Tuberia_Ultima_Cal", e, sp, new Vector2(3f, -2f), new Vector2(5, 0.35f), cal, true, 2);
        var tCal = new GameObject("Cauteriz_N4A_Ultima");
        tCal.transform.SetParent(t, false);
        tCal.transform.localPosition = new Vector3(3f, -1.6f, 0);
        var bcCal = tCal.AddComponent<BoxCollider2D>(); bcCal.size = new Vector2(5.5f, 0.8f); bcCal.isTrigger = true;
        tCal.tag = "ZonaCauterizacion";
        Luz("Luz_Cal_N4A", l, Light2D.LightType.Point, new Vector2(3f, -2f), new Color(1f, 0.4f, 0.1f), 0.7f, 3.5f);

        // Nota 13 — ultimo texto de lore, revela que Jeremy aun esta dormido
        Caja("Nota_13_Final", p, sp, new Vector2(-3f, -4f), new Vector2(0.4f, 0.25f), Color.white, false, 4);

        // Spawn point (entrada desde N3)
        var spawn = new GameObject("Spawn_N4A");
        spawn.transform.SetParent(padre, false);
        spawn.transform.localPosition = new Vector3(-7f, -3.5f, 0);

        // Checkpoint (se guarda al entrar al N4, antes del boss)
        var ckGO = new GameObject("Checkpoint_N4A");
        ckGO.transform.SetParent(padre, false);
        ckGO.transform.localPosition = new Vector3(-6f, -3.5f, 0);
        var ckBC = ckGO.AddComponent<BoxCollider2D>(); ckBC.isTrigger = true; ckBC.size = new Vector2(2f, 3f);
        ckGO.AddComponent<Checkpoint>();

        // Puerta entrada desde N3
        var pi = Caja("Puerta_EntN3", e, sp, new Vector2(-7.5f, -1.5f), new Vector2(1, 4), new Color(0.5f, 0.5f, 0.1f), true, 3);
        pi.tag = "Puerta";

        // Apertura hacia el Altar (arco ominoso, sin puerta solida)
        Caja("Arco_Superior", e, sp, new Vector2(7f, 2.5f), new Vector2(3, 1.5f), purp, true, 3);
        // Dos pilares del arco
        Caja("Arco_Pilar_Izq", e, sp, new Vector2(5.5f, 0f), new Vector2(0.5f, 5), purp, true, 3);
        Caja("Arco_Pilar_Der", e, sp, new Vector2(8.5f, 0f), new Vector2(0.5f, 5), purp, true, 3);

        // Limites
        var lim = new GameObject("Limites_N4A"); lim.transform.SetParent(padre, false);
        var bcL = lim.AddComponent<BoxCollider2D>(); bcL.size = new Vector2(16, 8); bcL.isTrigger = true;

        // Luces
        Luz("Luz_Purp_N4A",  l, Light2D.LightType.Point, new Vector2(0, 2f),   purp,                            0.5f, 8f);
        Luz("Luz_Arco_N4A",  l, Light2D.LightType.Point, new Vector2(7f, 3f),  new Color(0.7f, 0.3f, 0.9f),    0.8f, 5f);
        Luz("Luz_Ent_N4A",   l, Light2D.LightType.Point, new Vector2(-6f, 0f), new Color(0.6f, 0.55f, 0.4f),   0.4f, 4f);

        var tipoParpadeo = TypeCache.GetTypesDerivedFrom<MonoBehaviour>().FirstOrDefault(x => x.Name == "LuzParpadeante");
        var luzPurp = GameObject.Find("Luz_Purp_N4A");
        if (tipoParpadeo != null && luzPurp != null) luzPurp.AddComponent(tipoParpadeo);
    }

    // ── N4-B: EL ALTAR (24 x 14) ──────────────────────────────────────────────
    static void GenN4B(Transform padre, Sprite sp)
    {
        Color c     = new Color(0.10f, 0.08f, 0.12f);
        Color m     = new Color(0.18f, 0.15f, 0.22f);
        Color purp  = new Color(0.22f, 0.06f, 0.28f);
        Color altar = new Color(0.45f, 0.08f, 0.10f); // rojo sangre del altar
        Color oro   = new Color(0.7f, 0.55f, 0.08f);

        var e  = Hijo(padre, "Estructura");
        var p  = Hijo(padre, "Props");
        var l  = Hijo(padre, "Luces");
        var t  = Hijo(padre, "Triggers");
        var en = Hijo(padre, "Enemigos");

        // Gran sala: 24 x 14
        Caja("Suelo",     e, sp, new Vector2(0,   -7.5f), new Vector2(24, 1),   c, true);
        Caja("Techo",     e, sp, new Vector2(0,    7f),   new Vector2(24, 1),   c, true);
        Caja("Pared_Izq", e, sp, new Vector2(-12.5f, 0), new Vector2(1, 16),   m, true);
        Caja("Pared_Der", e, sp, new Vector2( 12.5f, 0), new Vector2(1, 16),   m, true);

        // El Altar propiamente dicho: estructura central elevada con cabezas
        Caja("Altar_Base",     e, sp, new Vector2(0,  -4f),  new Vector2(8, 1),     altar, true,  3);
        Caja("Altar_Columna",  e, sp, new Vector2(0,  -1f),  new Vector2(1.5f, 6),  altar, true,  3);
        Caja("Altar_Tope",     e, sp, new Vector2(0,   2.5f),new Vector2(6, 0.8f),  oro,   true,  4);

        // Decoraciones del altar: cabezas en mosaico
        for (int i = -3; i <= 3; i++)
        {
            float y = 3.2f + (i % 2 == 0 ? 0f : 0.3f);
            Color col = Color.Lerp(altar, new Color(0.55f, 0.05f, 0.05f), Mathf.Abs(i) / 3f);
            Caja($"Cabeza_Altar_{i}", p, sp, new Vector2(i * 0.7f, y), new Vector2(0.55f, 0.55f), col, false, 5);
        }

        // Plataformas de combate / evasion alrededor del altar
        Caja("Plat_Altar_Izq_A", e, sp, new Vector2(-7f,  -4f), new Vector2(3, 0.25f), m, true, 1);
        Caja("Plat_Altar_Izq_B", e, sp, new Vector2(-8f,  -1f), new Vector2(3, 0.25f), m, true, 1);
        Caja("Plat_Altar_Der_A", e, sp, new Vector2( 7f,  -4f), new Vector2(3, 0.25f), m, true, 1);
        Caja("Plat_Altar_Der_B", e, sp, new Vector2( 8f,  -1f), new Vector2(3, 0.25f), m, true, 1);

        // Nicho de escape (escondite de emergencia)
        var goEsc = Caja("Escondite_Nicho_Altar", p, sp, new Vector2(-11f, -5.5f), new Vector2(2f, 2.5f), m * 0.5f, false, -1);
        var bcEsc = goEsc.AddComponent<BoxCollider2D>(); bcEsc.isTrigger = true;
        goEsc.AddComponent<Escondite>();

        // LA FOSA FINAL — marcador
        var goFosa = Caja("LaFosa_Final", en, sp, new Vector2(8f, -6f), new Vector2(2f, 4f), new Color(0.4f, 0.05f, 0.05f), false, 5);
        // Agrega LaFosaIA en el inspector

        // Trigger de climax (activa La Fosa al entrar al altar)
        var tClimax = new GameObject("Trigger_CliMaxAltar");
        tClimax.transform.SetParent(t, false);
        tClimax.transform.localPosition = new Vector3(-5f, -3f, 0);
        var bcCl = tClimax.AddComponent<BoxCollider2D>(); bcCl.size = new Vector2(5f, 10f); bcCl.isTrigger = true;
        tClimax.tag = "ZonaClimax";

        // Apertura hacia N4-C (escape)
        Caja("Marco_Escape_Superior", e, sp, new Vector2(11.5f, 3.5f), new Vector2(2, 1), purp, true, 3);
        Caja("Marco_Escape_Pilar_U", e, sp, new Vector2(10.5f, 1f), new Vector2(0.4f, 4), purp, true, 3);

        // Limites
        var lim = new GameObject("Limites_N4B"); lim.transform.SetParent(padre, false);
        var bcL = lim.AddComponent<BoxCollider2D>(); bcL.size = new Vector2(24, 14); bcL.isTrigger = true;

        // Luces: el altar emite luz roja-morada intensa
        Luz("Luz_Altar_Central",  l, Light2D.LightType.Point, new Vector2(0,  2.5f), altar,                       1.5f, 12f);
        Luz("Luz_Altar_Superior", l, Light2D.LightType.Point, new Vector2(0,  4.5f), new Color(0.7f, 0.3f, 0.9f), 1.2f, 8f);
        Luz("Luz_Suelo_Altar",    l, Light2D.LightType.Point, new Vector2(0, -6.5f), new Color(0.5f, 0.05f, 0.05f),0.5f, 8f);
        Luz("Luz_Izq_Altar",      l, Light2D.LightType.Point, new Vector2(-9f, -2f), new Color(0.3f, 0.1f, 0.5f), 0.4f, 6f);
        Luz("Luz_Der_Altar",      l, Light2D.LightType.Point, new Vector2( 9f, -2f), new Color(0.3f, 0.1f, 0.5f), 0.4f, 6f);

        var tipoParpadeo = TypeCache.GetTypesDerivedFrom<MonoBehaviour>().FirstOrDefault(x => x.Name == "LuzParpadeante");
        foreach (var nombre in new[] { "Luz_Altar_Central", "Luz_Altar_Superior" })
        {
            var go = GameObject.Find(nombre);
            if (tipoParpadeo != null && go != null)
            {
                var lp = (LuzParpadeante)go.AddComponent(tipoParpadeo);
                lp.probCorte = 0.35f;
            }
        }
    }

    // ── N4-C: ESCAPE / CORREDOR DE HUIDA (10 x 6) ─────────────────────────────
    static void GenN4C(Transform padre, Sprite sp)
    {
        Color c   = new Color(0.10f, 0.08f, 0.10f);
        Color m   = new Color(0.18f, 0.15f, 0.22f);
        Color cal = new Color(0.92f, 0.45f, 0.08f);

        var e = Hijo(padre, "Estructura");
        var p = Hijo(padre, "Props");
        var l = Hijo(padre, "Luces");
        var t = Hijo(padre, "Triggers");

        Caja("Suelo",     e, sp, new Vector2(0,  -3.5f), new Vector2(10, 1),   c, true);
        Caja("Techo",     e, sp, new Vector2(0,   3f),   new Vector2(10, 1),   c, true);
        Caja("Pared_Izq", e, sp, new Vector2(-5.5f, 0), new Vector2(1, 8),    m, true);
        Caja("Pared_Der", e, sp, new Vector2( 5.5f, 0), new Vector2(1, 8),    m, true);

        // LA CALDERA FINAL — el hoyo por el que cae Jeremy
        // No tiene pared en el suelo derecho, el jugador simplemente cae
        Caja("Caldera_Agujero_Visual", p, sp, new Vector2(4f, -2.8f), new Vector2(2.5f, 1), new Color(0.05f, 0.05f, 0.05f), false, 3);
        // Glow rojo-naranja del interior de la caldera
        Luz("Luz_Caldera_Final", l, Light2D.LightType.Point, new Vector2(4f, -3.5f), new Color(1f, 0.3f, 0.0f), 1.5f, 5f);

        // Trigger de caida — activa la escena final
        var tCaida = new GameObject("Trigger_CaidaCaldera_Final");
        tCaida.transform.SetParent(t, false);
        tCaida.transform.localPosition = new Vector3(4f, -2.5f, 0);
        var bcCaida = tCaida.AddComponent<BoxCollider2D>();
        bcCaida.size = new Vector2(2.5f, 1.5f);
        bcCaida.isTrigger = true;
        // CambiarNivel con nombreEscena = "EscenaFinal"
        var cn = tCaida.AddComponent<CambiarNivel>();
        cn.nombreEscena = "EscenaFinal";

        // Tuberia de cauterizacion (antes de la caldera, decision final del jugador)
        Caja("Tuberia_AntesCaldera", e, sp, new Vector2(-1f, -1.5f), new Vector2(4, 0.35f), cal, true, 2);
        var tCal = new GameObject("Cauteriz_AntesCaldera");
        tCal.transform.SetParent(t, false);
        tCal.transform.localPosition = new Vector3(-1f, -1.1f, 0);
        var bcCal = tCal.AddComponent<BoxCollider2D>(); bcCal.size = new Vector2(4.5f, 0.8f); bcCal.isTrigger = true;
        tCal.tag = "ZonaCauterizacion";
        Luz("Luz_Cal_N4C", l, Light2D.LightType.Point, new Vector2(-1f, -1.5f), new Color(1f, 0.4f, 0.1f), 0.8f, 3f);

        // Limites
        var lim = new GameObject("Limites_N4C"); lim.transform.SetParent(padre, false);
        var bcL = lim.AddComponent<BoxCollider2D>(); bcL.size = new Vector2(10, 6); bcL.isTrigger = true;

        // Luz del escape (muy tenue, solo la caldera brilla)
        Luz("Luz_Escape_Entrada", l, Light2D.LightType.Point, new Vector2(-4f, 0f), new Color(0.5f, 0.45f, 0.35f), 0.4f, 4f);
    }

    // ── Utilidades ─────────────────────────────────────────────────────────────
    static Transform Hijo(Transform padre, string nombre)
    {
        var go = new GameObject(nombre); go.transform.SetParent(padre, false); return go.transform;
    }

    static GameObject Caja(string nombre, Transform padre, Sprite sprite, Vector2 pos, Vector2 tam,
                            Color color, bool solido, int orden = 0)
    {
        var go = new GameObject(nombre);
        go.transform.SetParent(padre, false);
        go.transform.localPosition = new Vector3(pos.x, pos.y, 0);
        go.transform.localScale    = new Vector3(tam.x, tam.y, 1);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite; sr.color = color; sr.sortingOrder = orden;
        if (solido) go.AddComponent<BoxCollider2D>();
        return go;
    }

    static Light2D Luz(string nombre, Transform padre, Light2D.LightType tipo,
                       Vector2 pos, Color color, float intensidad, float radio = 0f)
    {
        var go = new GameObject(nombre);
        go.transform.SetParent(padre, false);
        go.transform.localPosition = new Vector3(pos.x, pos.y, 0);
        var l = go.AddComponent<Light2D>();
        l.lightType = tipo; l.color = color; l.intensity = intensidad;
        if (tipo == Light2D.LightType.Point) { l.pointLightInnerRadius = 0f; l.pointLightOuterRadius = radio; }
        return l;
    }

    static Sprite ObtenerSprite()
    {
        var ex = AssetDatabase.LoadAssetAtPath<Sprite>(RutaSprite);
        if (ex != null) return ex;
        Directory.CreateDirectory(Path.GetDirectoryName(RutaSprite));
        var tex = new Texture2D(4, 4, TextureFormat.RGBA32, false);
        tex.SetPixels(Enumerable.Repeat(Color.white, 16).ToArray()); tex.Apply();
        File.WriteAllBytes(RutaSprite, tex.EncodeToPNG()); Object.DestroyImmediate(tex);
        AssetDatabase.ImportAsset(RutaSprite);
        var imp = (TextureImporter)AssetImporter.GetAtPath(RutaSprite);
        imp.textureType = TextureImporterType.Sprite; imp.spriteImportMode = SpriteImportMode.Single;
        imp.spritePixelsPerUnit = 4; imp.filterMode = FilterMode.Point;
        imp.textureCompression = TextureImporterCompression.Uncompressed; imp.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(RutaSprite);
    }
}
