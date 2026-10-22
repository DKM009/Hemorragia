using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Genera el greybox del Cuarto 3 — BODEGA del Nivel 1.
/// Medidas: 20 x 8 u. Techo medio. Primera zona con Purgadores.
/// Uso: Tools > Hemorragia > Crear Cuarto 3 (Bodega)
/// Se crea en x = 80 (Cuarto1=0, Cuarto2=40, Cuarto3=80).
/// </summary>
public static class CrearGreyboxCuarto3
{
    const string RutaSprite = "Assets/Sprites/Greybox/Cuadrado.png";

    [MenuItem("Tools/Hemorragia/Crear Cuarto 3 (Bodega)")]
    public static void CrearCuarto3()
    {
        if (GameObject.Find("Cuarto3_Bodega") != null)
        {
            EditorUtility.DisplayDialog("Cuarto 3", "Ya existe Cuarto3_Bodega.", "OK");
            return;
        }

        Sprite cuadrado = ObtenerSprite();
        if (cuadrado == null) { Debug.LogError("No se pudo obtener sprite greybox."); return; }

        var raiz = new GameObject("Cuarto3_Bodega");
        Undo.RegisterCreatedObjectUndo(raiz, "Crear Cuarto 3");

        Transform estructura = Hijo(raiz.transform, "Estructura");
        Transform props      = Hijo(raiz.transform, "Props");
        Transform enemigos   = Hijo(raiz.transform, "Enemigos");
        Transform triggers   = Hijo(raiz.transform, "Triggers");
        Transform luces      = Hijo(raiz.transform, "Luces");

        Color concreto  = new Color(0.25f, 0.22f, 0.22f);
        Color metal     = new Color(0.30f, 0.30f, 0.35f);
        Color madera    = new Color(0.40f, 0.28f, 0.15f);
        Color herrumbre = new Color(0.38f, 0.22f, 0.10f);

        // ESTRUCTURA
        Caja("Suelo",      estructura, cuadrado, new Vector2(0,    -4.5f), new Vector2(20, 1),    concreto, true);
        Caja("Techo",      estructura, cuadrado, new Vector2(0,     4.5f), new Vector2(20, 1),    concreto, true);
        Caja("Pared_Izq",  estructura, cuadrado, new Vector2(-10.5f, 0),  new Vector2(1,  10),   metal,    true);
        Caja("Pared_Der",  estructura, cuadrado, new Vector2( 10.5f, 0),  new Vector2(1,  10),   metal,    true);

        // Estanterias como plataformas (3 niveles)
        // Estanteria izquierda
        Caja("Estante_Izq_A", estructura, cuadrado, new Vector2(-7.5f, -2f),   new Vector2(3, 0.2f), madera, true, 1);
        Caja("Estante_Izq_B", estructura, cuadrado, new Vector2(-7.5f,  0.5f), new Vector2(3, 0.2f), madera, true, 1);
        Caja("Estante_Izq_C", estructura, cuadrado, new Vector2(-7.5f,  3f),   new Vector2(3, 0.2f), madera, true, 1);
        // Soporte estanteria izq
        Caja("Soporte_Izq_A", estructura, cuadrado, new Vector2(-9,    1f),    new Vector2(0.2f, 6), herrumbre, false, -1);
        Caja("Soporte_Izq_B", estructura, cuadrado, new Vector2(-6,    1f),    new Vector2(0.2f, 6), herrumbre, false, -1);

        // Estanteria derecha
        Caja("Estante_Der_A", estructura, cuadrado, new Vector2(7.5f, -2f),    new Vector2(3, 0.2f), madera, true, 1);
        Caja("Estante_Der_B", estructura, cuadrado, new Vector2(7.5f,  0.5f),  new Vector2(3, 0.2f), madera, true, 1);
        // Soporte derecha
        Caja("Soporte_Der_A", estructura, cuadrado, new Vector2(9,     -0.75f), new Vector2(0.2f, 4), herrumbre, false, -1);
        Caja("Soporte_Der_B", estructura, cuadrado, new Vector2(6,     -0.75f), new Vector2(0.2f, 4), herrumbre, false, -1);

        // Muro central con hueco (fuerza al jugador a pasar por el medio)
        Caja("Muro_Central_Alto", estructura, cuadrado, new Vector2(0, 1.5f), new Vector2(1.5f, 5), concreto * 0.9f, true);
        // (El hueco queda entre y=-4 y y=-1, suficiente para pasar agachado)

        // Puertas
        Color puertaC2  = new Color(0.12f, 0.40f, 0.18f);
        Color puertaC4  = new Color(0.55f, 0.12f, 0.12f);
        var goPI = Caja("Puerta_Entrada_C2", estructura, cuadrado, new Vector2(-9.5f, -2f), new Vector2(1, 4), puertaC2, true, 3);
        goPI.tag = "Puerta";
        var goPD = Caja("Puerta_Salida_C4",  estructura, cuadrado, new Vector2( 9.5f, -2f), new Vector2(1, 4), puertaC4, true, 3);
        goPD.tag = "Puerta";

        // PROPS
        // Cajas y pales en estantes
        Caja("Caja_Est_IzqA1", props, cuadrado, new Vector2(-8.5f, -1.7f), new Vector2(0.7f, 0.5f), madera * 1.2f, false, 2);
        Caja("Caja_Est_IzqA2", props, cuadrado, new Vector2(-7.2f, -1.7f), new Vector2(0.5f, 0.5f), new Color(0.7f,0.1f,0.1f), false, 2);
        Caja("Caja_Est_IzqB1", props, cuadrado, new Vector2(-8.0f,  0.8f), new Vector2(1.2f, 0.5f), madera * 1.1f, false, 2);
        Caja("Caja_Est_DerA1", props, cuadrado, new Vector2( 7.0f, -1.7f), new Vector2(0.8f, 0.5f), madera, false, 2);
        Caja("Caja_Est_DerB1", props, cuadrado, new Vector2( 8.2f,  0.8f), new Vector2(0.6f, 0.5f), new Color(0.2f,0.5f,0.5f), false, 2);

        // Barril caido
        Caja("Barril_Suelo", props, cuadrado, new Vector2(-2f, -3.9f), new Vector2(0.8f, 0.7f), new Color(0.35f,0.25f,0.1f), false, 1);

        // Charco de aceite / fluido oscuro (escondite visual)
        Caja("Fluido_Suelo", props, cuadrado, new Vector2(3f, -3.95f), new Vector2(2f, 0.1f), new Color(0.05f,0.05f,0.08f), false, 1);

        // Nota 3 (en estante izquierdo nivel C)
        Caja("Nota_3", props, cuadrado, new Vector2(-7.5f, 3.3f), new Vector2(0.4f, 0.25f), Color.white, false, 4);

        // Escondite: caja grande hueca (Cuarto 3 tiene el primer escondite)
        var goEsc = Caja("Escondite_CajaGrande", props, cuadrado, new Vector2(2f, -3f), new Vector2(1.8f, 2f), new Color(0.4f, 0.3f, 0.15f), false, -1);
        var bcEsc = goEsc.AddComponent<BoxCollider2D>();
        bcEsc.isTrigger = true;
        bcEsc.size      = new Vector2(1.8f, 2f) / goEsc.transform.localScale; // ajustar al scale
        goEsc.AddComponent<Escondite>();

        // ENEMIGOS — marcadores de Purgador (sin script de IA aun, solo posicion)
        Caja("Purgador_Marcador_A", enemigos, cuadrado, new Vector2(-5f, -3.5f), new Vector2(0.5f, 1f), new Color(0.8f,0.15f,0.5f), false, 5);
        Caja("Purgador_Marcador_B", enemigos, cuadrado, new Vector2( 4f, -3.5f), new Vector2(0.5f, 1f), new Color(0.8f,0.15f,0.5f), false, 5);

        // TRIGGERS
        {   // Salida hacia Cuarto 4
            var t = new GameObject("Trigger_SalidaC4");
            t.transform.SetParent(triggers, false);
            t.transform.localPosition = new Vector3(9f, -2f, 0);
            var bc = t.AddComponent<BoxCollider2D>();
            bc.size = new Vector2(1.2f, 4f); bc.isTrigger = true;
            t.tag = "SalidaNivel";
        }
        {   // Zona de emboscada (activa Purgadores cuando el jugador llega al centro)
            var t = new GameObject("Trigger_Emboscada");
            t.transform.SetParent(triggers, false);
            t.transform.localPosition = new Vector3(0, -2f, 0);
            var bc = t.AddComponent<BoxCollider2D>();
            bc.size = new Vector2(4f, 6f); bc.isTrigger = true;
            t.tag = "ZonaEmboscada";
        }

        // LUCES
        Luz("Luz_Ambiental", luces, Light2D.LightType.Global, Vector2.zero, new Color(0.04f,0.04f,0.08f), 0.08f);
        Luz("Luz_Colgante_Izq", luces, Light2D.LightType.Point, new Vector2(-6f, 2f),  new Color(0.9f,0.85f,0.7f), 0.9f, 5f);
        Luz("Luz_Colgante_Der", luces, Light2D.LightType.Point, new Vector2( 6f, 2f),  new Color(0.9f,0.85f,0.7f), 0.7f, 5f);
        Luz("Luz_Entrada",      luces, Light2D.LightType.Point, new Vector2(-9f, 0f),  new Color(0.8f,0.7f, 0.5f), 0.5f, 4f);

        // Parpadeo a las dos luces colgantes
        var tipoParpadeo = TypeCache.GetTypesDerivedFrom<MonoBehaviour>().FirstOrDefault(t => t.Name == "LuzParpadeante");
        foreach (var nombre in new[] { "Luz_Colgante_Izq", "Luz_Colgante_Der" })
        {
            var go = GameObject.Find(nombre);
            if (tipoParpadeo != null && go != null) go.AddComponent(tipoParpadeo);
        }

        // LIMITES
        var limGO = new GameObject("Limites_Cuarto3");
        limGO.transform.SetParent(raiz.transform, false);
        var limBC = limGO.AddComponent<BoxCollider2D>();
        limBC.size = new Vector2(20, 8); limBC.isTrigger = true;

        // Posicion global
        raiz.transform.position = new Vector3(80f, 0f, 0f);

        Selection.activeGameObject = raiz;
        EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        Debug.Log("Cuarto 3 (Bodega) creado en x=80.\n" +
                  "• Agrega PurgadorIA.cs a los Purgador_Marcador_A y B.\n" +
                  "• El Trigger_Emboscada debe activar los Purgadores (GestorNivel1 o script ad-hoc).\n" +
                  "• Escondite_CajaGrande ya tiene Escondite.cs.");
    }

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
        if (tipo == Light2D.LightType.Point)
        { l.pointLightInnerRadius = 0f; l.pointLightOuterRadius = radio; }
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
