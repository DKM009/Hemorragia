using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Genera el greybox completo del Nivel 2 — TUBERIAS Y CALDERAS.
/// Estructura vertical: se desciende hacia el subsuelo de la fabrica.
/// Cuartos:
///   N2-A: Sala de Llegada (12 x 8 u) — x=0, entrada desde Nivel 1
///   N2-B: Corredor de Tuberias (35 x 5 u) — x=22, laberinto horizontal
///   N2-C: Sala de Calderas (18 x 12 u) — x=70, gran espacio vertical
/// Uso: Tools > Hemorragia > Crear Nivel 2 (Tuberias y Calderas)
/// El Nivel 2 debe estar en su propia escena Unity.
/// </summary>
public static class CrearGreyboxNivel2
{
    const string RutaSprite = "Assets/Sprites/Greybox/Cuadrado.png";

    [MenuItem("Tools/Hemorragia/Crear Nivel 2 (Tuberias y Calderas)")]
    public static void CrearNivel2()
    {
        if (GameObject.Find("Nivel2_Raiz") != null)
        {
            EditorUtility.DisplayDialog("Nivel 2", "Ya existe Nivel2_Raiz en la escena.", "OK");
            return;
        }

        Sprite cuadrado = ObtenerSprite();
        if (cuadrado == null) { Debug.LogError("No se pudo obtener sprite greybox."); return; }

        var raiz = new GameObject("Nivel2_Raiz");
        Undo.RegisterCreatedObjectUndo(raiz, "Crear Nivel 2");

        // ============================================================
        //  N2-A: SALA DE LLEGADA  (12 x 8)
        // ============================================================
        var n2a = Hijo(raiz.transform, "N2A_SalLlegada");
        n2a.localPosition = Vector3.zero;
        GenN2A(n2a, cuadrado);

        // ============================================================
        //  N2-B: CORREDOR DE TUBERIAS (35 x 5)
        // ============================================================
        var n2b = Hijo(raiz.transform, "N2B_CorredorTuberias");
        n2b.localPosition = new Vector3(23.5f, 0, 0);
        GenN2B(n2b, cuadrado);

        // ============================================================
        //  N2-C: SALA DE CALDERAS (18 x 12)
        // ============================================================
        var n2c = Hijo(raiz.transform, "N2C_SalCalderas");
        n2c.localPosition = new Vector3(75f, 0, 0);
        GenN2C(n2c, cuadrado);

        // Luz ambiental global del nivel
        var luzGO = new GameObject("Luz_Global_N2");
        luzGO.transform.SetParent(raiz.transform, false);
        var lg = luzGO.AddComponent<Light2D>();
        lg.lightType = Light2D.LightType.Global;
        lg.color     = new Color(0.05f, 0.05f, 0.08f);
        lg.intensity = 0.07f;

        // Camara
        Camera cam = Camera.main;
        if (cam != null)
        {
            Undo.RecordObject(cam, "Configurar camara N2");
            cam.orthographic     = true;
            cam.orthographicSize = 4f;
            cam.transform.position = new Vector3(0, 0, -10);
        }

        raiz.transform.position = Vector3.zero;
        Selection.activeGameObject = raiz;
        EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        Debug.Log("Nivel 2 creado.\n" +
                  "• N2A en x=0, N2B en x=23.5, N2C en x=75.\n" +
                  "• Conecta las puertas con Interactuable (TeletransporteCuarto).\n" +
                  "• El punto de spawn de entrada esta en N2A: Spawn_Entrada.\n" +
                  "• La caldera final en N2C es el punto de caida (escena final).");
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  N2-A — SALA DE LLEGADA
    // ─────────────────────────────────────────────────────────────────────────
    static void GenN2A(Transform padre, Sprite sp)
    {
        Color c  = new Color(0.22f, 0.20f, 0.20f);
        Color m  = new Color(0.30f, 0.30f, 0.38f);
        Color ox = new Color(0.38f, 0.22f, 0.08f);
        Color calor = new Color(0.9f, 0.4f, 0.1f);

        var e = Hijo(padre, "Estructura");
        var p = Hijo(padre, "Props");
        var l = Hijo(padre, "Luces");
        var t = Hijo(padre, "Triggers");

        // Suelo y techo
        Caja("Suelo", e, sp, new Vector2(0, -4.5f), new Vector2(12, 1),  c, true);
        Caja("Techo", e, sp, new Vector2(0,  4.5f), new Vector2(12, 1),  c, true);
        Caja("Pared_Izq", e, sp, new Vector2(-6.5f, 0), new Vector2(1, 10), m, true);
        Caja("Pared_Der", e, sp, new Vector2( 6.5f, 0), new Vector2(1, 10), m, true);

        // Puerta de entrada (desde Nivel 1)
        var pi = Caja("Puerta_EntradaN1", e, sp, new Vector2(-5.5f, -1.5f), new Vector2(1, 3), new Color(0.5f,0.5f,0.1f), true, 3);
        pi.tag = "Puerta";

        // Spawn point (Transform vacio)
        var spawn = new GameObject("Spawn_Entrada");
        spawn.transform.SetParent(padre, false);
        spawn.transform.localPosition = new Vector3(-4f, -3f, 0);

        // Tuberias en el suelo (obstaculos y calor)
        Caja("Tubo_Suelo_A", e, sp, new Vector2(-2f, -3.5f), new Vector2(1.5f, 0.5f), calor, true, 2);
        Caja("Tubo_Suelo_B", e, sp, new Vector2( 3f, -3.5f), new Vector2(1.5f, 0.5f), calor, true, 2);

        // Cauterizacion en ambos tubos del suelo
        for (int i = 0; i < 2; i++)
        {
            var tr = new GameObject($"Trigger_Cauteriz_N2A_{i}");
            tr.transform.SetParent(t, false);
            tr.transform.localPosition = new Vector3(i == 0 ? -2f : 3f, -3.1f, 0);
            var bc = tr.AddComponent<BoxCollider2D>(); bc.size = new Vector2(2f, 0.8f); bc.isTrigger = true;
            tr.tag = "ZonaCauterizacion";
        }

        // Plataforma elevada con valvula
        Caja("Plataforma_Valv", e, sp, new Vector2(2f, 0.5f), new Vector2(4, 0.25f), m, true, 1);
        Caja("Valvula", p, sp, new Vector2(2.5f, 0.9f), new Vector2(0.5f, 0.5f), ox, false, 3);

        // Puerta de salida a N2B
        var pd = Caja("Puerta_SalidaN2B", e, sp, new Vector2(5.5f, -1.5f), new Vector2(1, 3), new Color(0.12f,0.4f,0.18f), true, 3);
        pd.tag = "Puerta";

        // Trigger de salida
        var ts = new GameObject("Trigger_SalidaN2B");
        ts.transform.SetParent(t, false);
        ts.transform.localPosition = new Vector3(5f, -1.5f, 0);
        var bcs = ts.AddComponent<BoxCollider2D>(); bcs.size = new Vector2(1.5f, 3f); bcs.isTrigger = true;
        ts.tag = "SalidaNivel";

        // Limites
        var lim = new GameObject("Limites_N2A"); lim.transform.SetParent(padre, false);
        var bcl = lim.AddComponent<BoxCollider2D>(); bcl.size = new Vector2(12, 8); bcl.isTrigger = true;

        // Luces
        Luz("Luz_N2A_Punto_A", l, Light2D.LightType.Point, new Vector2(-3f, 2f),  new Color(0.8f,0.7f,0.5f), 0.7f, 6f);
        Luz("Luz_N2A_Calor_A", l, Light2D.LightType.Point, new Vector2(-2f,-3.3f),new Color(1f,0.4f,0.1f),   0.6f, 2.5f);
        Luz("Luz_N2A_Calor_B", l, Light2D.LightType.Point, new Vector2( 3f,-3.3f),new Color(1f,0.4f,0.1f),   0.6f, 2.5f);

        var tipoParpadeo = TypeCache.GetTypesDerivedFrom<MonoBehaviour>().FirstOrDefault(x => x.Name == "LuzParpadeante");
        var luzA = GameObject.Find("Luz_N2A_Punto_A");
        if (tipoParpadeo != null && luzA != null) luzA.AddComponent(tipoParpadeo);
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  N2-B — CORREDOR DE TUBERIAS (35 x 5)
    // ─────────────────────────────────────────────────────────────────────────
    static void GenN2B(Transform padre, Sprite sp)
    {
        Color c   = new Color(0.18f, 0.16f, 0.16f);
        Color m   = new Color(0.28f, 0.28f, 0.33f);
        Color cal = new Color(0.88f, 0.38f, 0.08f);
        Color az  = new Color(0.2f, 0.4f, 0.8f); // tuberia de agua fria

        var e = Hijo(padre, "Estructura");
        var p = Hijo(padre, "Props");
        var l = Hijo(padre, "Luces");
        var t = Hijo(padre, "Triggers");

        // Suelo, techo, paredes
        Caja("Suelo",     e, sp, new Vector2(0,    -3f),   new Vector2(35, 1),  c, true);
        Caja("Techo",     e, sp, new Vector2(0,     2.5f), new Vector2(35, 1),  c, true);
        Caja("Pared_Izq", e, sp, new Vector2(-18f, 0),    new Vector2(1, 6),   m, true);
        Caja("Pared_Der", e, sp, new Vector2( 18f, 0),    new Vector2(1, 6),   m, true);

        // 4 tuberias calientes horizontales que el jugador debe saltar o esquivar
        // Alternan altura para crear ritmo de plataformas
        float[] xTub  = {-12f, -4f, 5f, 13f};
        float[] yTub  = {-1.5f, -0.5f, -1.5f, -0.5f};
        float[] wTub  = {5f, 4f, 4f, 5f};
        for (int i = 0; i < xTub.Length; i++)
        {
            Caja($"Tubo_Cal_{i}", e, sp, new Vector2(xTub[i], yTub[i]), new Vector2(wTub[i], 0.25f), cal, true, 2);
            // Trigger de cauterizacion sobre cada tubo
            var tr = new GameObject($"Cauteriz_{i}");
            tr.transform.SetParent(t, false);
            tr.transform.localPosition = new Vector3(xTub[i], yTub[i] + 0.4f, 0);
            var bc = tr.AddComponent<BoxCollider2D>(); bc.size = new Vector2(wTub[i]+0.2f, 0.8f); bc.isTrigger = true;
            tr.tag = "ZonaCauterizacion";
        }

        // 2 tuberias de agua fria — no queman pero bloquean paso si caes encima
        Caja("Tubo_Frio_A", e, sp, new Vector2(-7f, -2f), new Vector2(2f, 0.3f), az, true, 1);
        Caja("Tubo_Frio_B", e, sp, new Vector2( 9f, -2f), new Vector2(2f, 0.3f), az, true, 1);

        // Muros intermedios (fuerzan a pasar encima/debajo de las tuberias)
        Caja("Muro_B_A", e, sp, new Vector2(-9f, -0.5f),  new Vector2(0.4f, 3f), c*0.9f, true);
        Caja("Muro_B_B", e, sp, new Vector2( 2f, -0.5f),  new Vector2(0.4f, 3f), c*0.9f, true);
        Caja("Muro_B_C", e, sp, new Vector2(10f, -0.5f),  new Vector2(0.4f, 3f), c*0.9f, true);

        // Puertas
        var pi = Caja("Puerta_Entrada_N2A", e, sp, new Vector2(-17f, -0.5f), new Vector2(1, 3), new Color(0.12f,0.4f,0.18f), true, 3);
        pi.tag = "Puerta";
        var pd = Caja("Puerta_Salida_N2C", e, sp, new Vector2(17f, -0.5f), new Vector2(1, 3), new Color(0.12f,0.4f,0.18f), true, 3);
        pd.tag = "Puerta";

        // Trigger salida
        var ts = new GameObject("Trigger_SalidaN2C");
        ts.transform.SetParent(t, false);
        ts.transform.localPosition = new Vector3(16.5f, -0.5f, 0);
        var bcts = ts.AddComponent<BoxCollider2D>(); bcts.size = new Vector2(1.5f, 3f); bcts.isTrigger = true;
        ts.tag = "SalidaNivel";

        // Escondite: ranura bajo tuberia fria A
        var goEsc = Caja("Escondite_RanuraFria", p, sp, new Vector2(-7f, -2.5f), new Vector2(2f, 0.8f), m*0.7f, false, -1);
        var bcEsc = goEsc.AddComponent<BoxCollider2D>(); bcEsc.isTrigger = true;
        goEsc.AddComponent<Escondite>();

        // Nota 5
        Caja("Nota_5", p, sp, new Vector2(-14f, -2.2f), new Vector2(0.4f, 0.25f), Color.white, false, 4);

        // Limites
        var lim = new GameObject("Limites_N2B"); lim.transform.SetParent(padre, false);
        var bcl = lim.AddComponent<BoxCollider2D>(); bcl.size = new Vector2(35, 5); bcl.isTrigger = true;

        // Luces — escasas, solo sobre cada tuberia
        for (int i = 0; i < xTub.Length; i++)
            Luz($"Luz_Tubo_{i}", l, Light2D.LightType.Point,
                new Vector2(xTub[i], yTub[i]+0.5f), new Color(1f,0.42f,0.1f), 0.6f, 3.5f);
        Luz("Luz_Entrada_N2B", l, Light2D.LightType.Point, new Vector2(-16f, 0f), new Color(0.7f,0.65f,0.5f), 0.5f, 4f);
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  N2-C — SALA DE CALDERAS (18 x 12)
    // ─────────────────────────────────────────────────────────────────────────
    static void GenN2C(Transform padre, Sprite sp)
    {
        Color c    = new Color(0.15f, 0.13f, 0.13f);
        Color m    = new Color(0.25f, 0.25f, 0.30f);
        Color cal  = new Color(0.92f, 0.42f, 0.08f);
        Color rojo = new Color(0.7f, 0.05f, 0.05f);

        var e = Hijo(padre, "Estructura");
        var p = Hijo(padre, "Props");
        var l = Hijo(padre, "Luces");
        var t = Hijo(padre, "Triggers");

        // Suelo y techo: sala alta
        Caja("Suelo",     e, sp, new Vector2(0,   -6.5f), new Vector2(18, 1),  c, true);
        Caja("Techo",     e, sp, new Vector2(0,    6.5f), new Vector2(18, 1),  c, true);
        Caja("Pared_Izq", e, sp, new Vector2(-9.5f,0),   new Vector2(1, 14), m, true);
        Caja("Pared_Der", e, sp, new Vector2( 9.5f,0),   new Vector2(1, 14), m, true);

        // CALDERAS — 2 grandes calderas cilindricas (plataformas redondas aprox)
        // Caldera izquierda
        Caja("Caldera_Izq_Cuerpo", e, sp, new Vector2(-5f, -3f), new Vector2(4, 6), new Color(0.30f,0.18f,0.08f), true, 2);
        Caja("Caldera_Izq_Tapa",   e, sp, new Vector2(-5f,  0f), new Vector2(4.4f,0.3f), cal, true, 3);
        Caja("Caldera_Izq_Fondo",  e, sp, new Vector2(-5f, -6f), new Vector2(4f,0.3f), cal, false, 3);

        // Caldera derecha (mas alta, es la caldera final donde cae Jeremy)
        Caja("Caldera_Der_Cuerpo", e, sp, new Vector2(5f, -2f), new Vector2(4, 8), new Color(0.28f,0.15f,0.06f), true, 2);
        Caja("Caldera_Der_Tapa",   e, sp, new Vector2(5f,  2f), new Vector2(4.4f,0.3f), cal, true, 3);
        // AGUJERO en la tapa de la caldera derecha (la que cae) - sin collider
        Caja("Caldera_Der_Agujero", p, sp, new Vector2(5.5f, 2.1f), new Vector2(1.2f, 0.4f), new Color(0.05f,0.05f,0.05f), false, 5);

        // Plataformas de escalada para llegar arriba de las calderas
        Caja("Plat_Escalada_A", e, sp, new Vector2(-2f, -2.5f), new Vector2(2, 0.25f), m, true, 1);
        Caja("Plat_Escalada_B", e, sp, new Vector2( 2f,  0.5f), new Vector2(2, 0.25f), m, true, 1);
        Caja("Plat_Escalada_C", e, sp, new Vector2(-1f,  3.0f), new Vector2(2, 0.25f), m, true, 1);

        // Tuberia de vapor diagonal (decorativa)
        Caja("Tuberia_Vapor_A", p, sp, new Vector2(-7f, 3f), new Vector2(6, 0.25f), cal, false, -1);
        Caja("Tuberia_Vapor_B", p, sp, new Vector2( 7f, -1f), new Vector2(0.25f, 8), cal, false, -1);

        // Zona caliente bajo las calderas
        Caja("Suelo_Magma", p, sp, new Vector2(0, -6.2f), new Vector2(16, 0.15f), rojo, false, 3);

        // Cauterizacion — encima de ambas tapas
        foreach (var (pos, nom) in new[]{(new Vector2(-5f, 0.3f), "Cauteriz_Izq"), (new Vector2(5f, 2.3f), "Cauteriz_Der")})
        {
            var tr = new GameObject(nom); tr.transform.SetParent(t, false);
            tr.transform.localPosition = new Vector3(pos.x, pos.y, 0);
            var bc = tr.AddComponent<BoxCollider2D>(); bc.size = new Vector2(4.5f, 0.8f); bc.isTrigger = true;
            tr.tag = "ZonaCauterizacion";
        }

        // Trigger de caida final (el agujero de la caldera derecha)
        var tcaida = new GameObject("Trigger_CaidaFinal");
        tcaida.transform.SetParent(t, false);
        tcaida.transform.localPosition = new Vector3(5.5f, 2f, 0);
        var bccaida = tcaida.AddComponent<BoxCollider2D>(); bccaida.size = new Vector2(1.3f, 0.6f); bccaida.isTrigger = true;
        tcaida.tag = "CaidaFinal";

        // Puerta de entrada desde N2B
        var pi = Caja("Puerta_Entrada_N2B", e, sp, new Vector2(-8.5f, -3.5f), new Vector2(1, 4), new Color(0.12f,0.4f,0.18f), true, 3);
        pi.tag = "Puerta";

        // Nota 6 — en la plataforma C (mas alta, recompensa de explorar)
        Caja("Nota_6", p, sp, new Vector2(-1.2f, 3.4f), new Vector2(0.4f, 0.25f), Color.white, false, 4);

        // Limites (alto: 12 u)
        var lim = new GameObject("Limites_N2C"); lim.transform.SetParent(padre, false);
        var bcl = lim.AddComponent<BoxCollider2D>(); bcl.size = new Vector2(18, 12); bcl.isTrigger = true;

        // Luces — infernales, naranja/rojo
        Luz("Luz_Caldera_Izq", l, Light2D.LightType.Point, new Vector2(-5f, 0f),  new Color(1f,0.4f,0.1f), 1.2f, 7f);
        Luz("Luz_Caldera_Der", l, Light2D.LightType.Point, new Vector2(5f, 2f),   new Color(1f,0.3f,0.05f),1.4f, 7f);
        Luz("Luz_Suelo_Magma", l, Light2D.LightType.Point, new Vector2(0f, -5.5f),new Color(1f,0.1f,0.0f), 0.8f, 8f);
        Luz("Luz_Azul_Entrada", l, Light2D.LightType.Point,new Vector2(-8f,-3.5f),new Color(0.5f,0.6f,1f), 0.5f, 4f);

        // Parpadeo caldera derecha (la mas dramática)
        var tipoParpadeo = TypeCache.GetTypesDerivedFrom<MonoBehaviour>().FirstOrDefault(x => x.Name == "LuzParpadeante");
        var luzDer = GameObject.Find("Luz_Caldera_Der");
        if (tipoParpadeo != null && luzDer != null) luzDer.AddComponent(tipoParpadeo);
    }

    // ── Utilidades ────────────────────────────────────────────────────────────

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
