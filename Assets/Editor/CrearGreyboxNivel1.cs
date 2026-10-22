using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Crea el greybox del Cuarto 1 (Patio) del Nivel 1 de Hemorragia.
/// Menú: Tools > Hemorragia > Crear Cuarto 1 (Patio)
/// </summary>
public static class CrearGreyboxNivel1
{
    const string RutaSprite = "Assets/Sprites/Greybox/Cuadrado.png";

    [MenuItem("Tools/Hemorragia/Crear Cuarto 1 (Patio)")]
    public static void CrearCuarto1()
    {
        if (GameObject.Find("Cuarto1_Patio") != null)
        {
            EditorUtility.DisplayDialog("Cuarto 1",
                "Ya existe Cuarto1_Patio en la escena. Borralo antes de volver a generar.", "OK");
            return;
        }

        Sprite cuadrado = ObtenerSprite();
        if (cuadrado == null) { Debug.LogError("No se pudo obtener sprite greybox."); return; }

        var raiz = new GameObject("Cuarto1_Patio");
        Undo.RegisterCreatedObjectUndo(raiz, "Crear Cuarto 1");

        Transform estructura = Hijo(raiz.transform, "Estructura");
        Transform props      = Hijo(raiz.transform, "Props");
        Transform luces      = Hijo(raiz.transform, "Luces");

        Color concreto  = new Color(0.30f, 0.28f, 0.25f);
        Color herrumbre = new Color(0.42f, 0.28f, 0.16f);
        Color metal     = new Color(0.38f, 0.38f, 0.42f);

        // Suelo principal
        Caja("Suelo_Principal",    estructura, cuadrado, new Vector2(0,    -4.5f), new Vector2(32, 1),    concreto,  true);
        // Plataforma elevada izq
        Caja("Plataforma_Izq",     estructura, cuadrado, new Vector2(-10f, -2.5f), new Vector2(6, 0.5f),  concreto,  true);
        // Camion (plataforma alta, 1.4 u sobre suelo base)
        Caja("Camion_Base",        estructura, cuadrado, new Vector2(4f,   -2.6f), new Vector2(4, 1.2f),  herrumbre, true);
        Caja("Camion_Cabina",      estructura, cuadrado, new Vector2(2.2f, -1.6f), new Vector2(2, 1.8f),  metal,     true);
        Caja("Camion_Techo",       props,      cuadrado, new Vector2(2.2f, -0.8f), new Vector2(2.2f, 0.3f), metal,  false, 1);
        // Tope derecho
        Caja("Pared_Der",          estructura, cuadrado, new Vector2(15.5f, -1f),  new Vector2(1, 8),     concreto,  true);
        // Tope izquierdo
        Caja("Pared_Izq",          estructura, cuadrado, new Vector2(-15.5f,-1f),  new Vector2(1, 8),     concreto,  true);
        // Puerta nave (entrada al pasillo)
        Color puerta = new Color(0.50f, 0.12f, 0.12f);
        Caja("Puerta_Nave",        estructura, cuadrado, new Vector2(14.5f,-2f),   new Vector2(1, 4),     puerta,    true, 3);

        // Props deco
        Caja("Barril_A",    props, cuadrado, new Vector2(-6f,  -3.9f), new Vector2(0.7f, 1.0f), herrumbre, false, 1);
        Caja("Barril_B",    props, cuadrado, new Vector2(-5.2f,-3.9f), new Vector2(0.7f, 1.0f), metal,     false, 1);
        Caja("Tuberia_Izq", props, cuadrado, new Vector2(-13f,  1.0f), new Vector2(4f,  0.25f), herrumbre, false,-2);

        // Nota 0 (charco)
        Caja("Nota_0", props, cuadrado, new Vector2(-1f, -4.15f), new Vector2(0.4f, 0.25f), Color.white, false, 4);

        // Luz global (oscuridad de noche)
        Luz("Luz_Global_Patio", luces, Light2D.LightType.Global, Vector2.zero,
            new Color(0.04f, 0.05f, 0.08f), 0.08f);
        // Poste
        Luz("Luz_Poste", luces, Light2D.LightType.Point, new Vector2(-8f, 2f),
            new Color(0.90f, 0.80f, 0.55f), 0.85f, 8f);
        // Horizonte tenue
        Luz("Luz_Horizonte", luces, Light2D.LightType.Point, new Vector2(0f, -1f),
            new Color(0.15f, 0.22f, 0.35f), 0.3f, 18f);

        // Parpadeo en el poste
        var tipoParpadeo = TypeCache.GetTypesDerivedFrom<MonoBehaviour>()
                              .FirstOrDefault(t => t.Name == "LuzParpadeante");
        var goPoste = GameObject.Find("Luz_Poste");
        if (tipoParpadeo != null && goPoste != null)
            goPoste.AddComponent(tipoParpadeo);

        // Limites de camara
        var limGO = new GameObject("Limites_Cuarto1");
        limGO.transform.SetParent(raiz.transform, false);
        var limBC = limGO.AddComponent<BoxCollider2D>();
        limBC.size = new Vector2(32, 10);
        limBC.isTrigger = true;

        // Trigger de salida (lleva al cuarto 2)
        var trigSal = new GameObject("Trigger_SalidaC2");
        trigSal.transform.SetParent(raiz.transform, false);
        trigSal.transform.localPosition = new Vector3(14.8f, -2f, 0f);
        var bc = trigSal.AddComponent<BoxCollider2D>();
        bc.size = new Vector2(0.5f, 4f);
        bc.isTrigger = true;
        trigSal.tag = "SalidaNivel";

        // Spawn point del jugador
        var spawn = new GameObject("Spawn_Cuarto1");
        spawn.transform.SetParent(raiz.transform, false);
        spawn.transform.localPosition = new Vector3(-12f, -3f, 0f);

        // Camara
        Camera cam = Camera.main;
        if (cam != null)
        {
            Undo.RecordObject(cam, "Configurar camara Cuarto 1");
            cam.orthographic      = true;
            cam.orthographicSize  = 5f;
            cam.backgroundColor   = new Color(0.02f, 0.02f, 0.03f);
            cam.transform.position = new Vector3(0, 0, -10);
        }

        Selection.activeGameObject = raiz;
        EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        Debug.Log("Cuarto 1 (Patio) creado.\n" +
                  "• Arrastra 'Limites_Cuarto1' al campo 'Limites' de CamaraSeguimiento.\n" +
                  "• Asigna 'Spawn_Cuarto1' como posicion inicial del Player.");
    }

    // ── Utilidades ─────────────────────────────────────────────────────────────
    static Transform Hijo(Transform padre, string nombre)
    {
        var go = new GameObject(nombre);
        go.transform.SetParent(padre, false);
        return go.transform;
    }

    static GameObject Caja(string nombre, Transform padre, Sprite sprite, Vector2 pos, Vector2 tam,
                            Color color, bool solido, int orden = 0)
    {
        var go = new GameObject(nombre);
        go.transform.SetParent(padre, false);
        go.transform.localPosition = new Vector3(pos.x, pos.y, 0);
        go.transform.localScale    = new Vector3(tam.x, tam.y, 1);
        var sr        = go.AddComponent<SpriteRenderer>();
        sr.sprite      = sprite;
        sr.color       = color;
        sr.sortingOrder = orden;
        if (solido) go.AddComponent<BoxCollider2D>();
        return go;
    }

    static Light2D Luz(string nombre, Transform padre, Light2D.LightType tipo,
                       Vector2 pos, Color color, float intensidad, float radio = 0f)
    {
        var go = new GameObject(nombre);
        go.transform.SetParent(padre, false);
        go.transform.localPosition = new Vector3(pos.x, pos.y, 0);
        var l       = go.AddComponent<Light2D>();
        l.lightType = tipo;
        l.color     = color;
        l.intensity = intensidad;
        if (tipo == Light2D.LightType.Point)
        {
            l.pointLightInnerRadius = 0f;
            l.pointLightOuterRadius = radio;
        }
        return l;
    }

    static Sprite ObtenerSprite()
    {
        var existente = AssetDatabase.LoadAssetAtPath<Sprite>(RutaSprite);
        if (existente != null) return existente;
        Directory.CreateDirectory(Path.GetDirectoryName(RutaSprite));
        var tex = new Texture2D(4, 4, TextureFormat.RGBA32, false);
        tex.SetPixels(Enumerable.Repeat(Color.white, 16).ToArray());
        tex.Apply();
        File.WriteAllBytes(RutaSprite, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
        AssetDatabase.ImportAsset(RutaSprite);
        var imp = (TextureImporter)AssetImporter.GetAtPath(RutaSprite);
        imp.textureType         = TextureImporterType.Sprite;
        imp.spriteImportMode    = SpriteImportMode.Single;
        imp.spritePixelsPerUnit = 4;
        imp.filterMode          = FilterMode.Point;
        imp.textureCompression  = TextureImporterCompression.Uncompressed;
        imp.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(RutaSprite);
    }
}
