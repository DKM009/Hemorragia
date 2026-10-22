using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Crea el greybox del Cuarto 2 (Pasillo) del Nivel 1 de Hemorragia con un clic.
/// Colocar en: Assets/Editor/CrearGreyboxCuarto2.cs
/// Uso: Tools > Hemorragia > Crear Cuarto 2 (Pasillo)
///
/// MEDIDAS (diseño Nivel 1):
///   Ancho total 30 u, alto jugable 6 u (techo bajo, claustrofobico).
///   El jugador entra por la izquierda (puerta de la nave) y sale por la derecha
///   hacia la Bodega (Cuarto 3).
///   Techo: 2.5 u sobre el suelo (el jugador mide 2 u => margen de 0.5).
///   Tuberia caliente: plataforma angosta a media altura, sirve para cauterizacion.
///   Puerta izquierda (entrada, cerrada con llave inicial) en x=-14.5.
///   Puerta derecha (salida) en x=14.5.
///   Limites de camara: 30 x 6.
/// </summary>
public static class CrearGreyboxCuarto2
{
    const string RutaSprite = "Assets/Sprites/Greybox/Cuadrado.png";

    [MenuItem("Tools/Hemorragia/Crear Cuarto 2 (Pasillo)")]
    public static void CrearCuarto2()
    {
        if (GameObject.Find("Cuarto2_Pasillo") != null)
        {
            EditorUtility.DisplayDialog("Cuarto 2",
                "Ya existe un objeto llamado Cuarto2_Pasillo en la escena. Borralo o renombralo antes de volver a generar.",
                "OK");
            return;
        }

        Sprite cuadrado = ObtenerSprite();
        if (cuadrado == null)
        {
            Debug.LogError("No se pudo crear el sprite base del greybox.");
            return;
        }

        // Raiz y grupos
        var raiz = new GameObject("Cuarto2_Pasillo");
        Undo.RegisterCreatedObjectUndo(raiz, "Crear Cuarto 2");

        Transform estructura = Hijo(raiz.transform, "Estructura");
        Transform props      = Hijo(raiz.transform, "Props");
        Transform triggers   = Hijo(raiz.transform, "Triggers");
        Transform luces      = Hijo(raiz.transform, "Luces");

        // ============================================================
        //  ESTRUCTURA
        // ============================================================
        Color concreto   = new Color(0.28f, 0.27f, 0.30f);   // gris oscuro sucio
        Color herrumbre  = new Color(0.40f, 0.28f, 0.18f);   // oxido
        Color metal      = new Color(0.35f, 0.35f, 0.40f);   // metal liso

        // Suelo: cara superior en y = -3 (centro del cuarto en y = 0)
        // El cuarto tiene 6 u de alto: de y=-3 (suelo) a y=3 (techo).
        Caja("Suelo",         estructura, cuadrado, new Vector2(0,    -3.5f), new Vector2(30,  1),    concreto,  true);
        // Techo bajo: cara inferior en y = 3 (solo 5 u de espacio interior)
        Caja("Techo",         estructura, cuadrado, new Vector2(0,     3.5f), new Vector2(30,  1),    concreto,  true);
        // Paredes laterales
        Caja("Pared_Izq",     estructura, cuadrado, new Vector2(-15.5f, 0),   new Vector2(1,   8),    metal,     true);
        Caja("Pared_Der",     estructura, cuadrado, new Vector2( 15.5f, 0),   new Vector2(1,   8),    metal,     true);

        // Escalon de entrada (al entrar desde el patio el suelo baja un poco)
        Caja("Escalon_Entrada", estructura, cuadrado, new Vector2(-13, -3.25f), new Vector2(2, 0.5f), concreto * 1.2f, true);

        // Tuberia caliente horizontal (plataforma de cauterizacion) - centro del pasillo
        // Ancha para que el jugador tenga tiempo de pararse encima
        Color tuboCal = new Color(0.85f, 0.35f, 0.10f); // naranja caliente
        Caja("Tuberia_Caliente", estructura, cuadrado, new Vector2(0, -0.5f), new Vector2(6, 0.3f), tuboCal, true, 2);

        // Soporte de tuberia (pilares)
        Caja("Soporte_Tub_Izq", estructura, cuadrado, new Vector2(-3, -1.7f), new Vector2(0.25f, 2.5f), herrumbre, false, -1);
        Caja("Soporte_Tub_Der", estructura, cuadrado, new Vector2( 3, -1.7f), new Vector2(0.25f, 2.5f), herrumbre, false, -1);

        // Pared ciega central (crea embudo: forzar pasar bajo la tuberia O saltar encima)
        // Pequeno obstaculo de medio muro a la izquierda
        Caja("Muro_Obstaculo", estructura, cuadrado, new Vector2(-7, -1.5f), new Vector2(0.5f, 3), concreto * 0.9f, true);

        // Tuberias decorativas en el techo (sin collider, solo visual)
        Caja("Tub_Techo_A", props, cuadrado, new Vector2(-8,  2.8f), new Vector2(14, 0.2f), herrumbre,  false, -2);
        Caja("Tub_Techo_B", props, cuadrado, new Vector2( 5,  2.8f), new Vector2( 6, 0.2f), metal,      false, -2);

        // ============================================================
        //  PUERTAS  (marcadores visuales; la logica la maneja Interactuable.cs)
        // ============================================================
        Color puertaCerrada = new Color(0.55f, 0.12f, 0.12f); // rojo oscuro = bloqueada
        Color puertaAbierta = new Color(0.12f, 0.40f, 0.18f); // verde apagado = paso libre

        // Puerta izquierda (entrada desde Cuarto 1 - bloqueada al regresar)
        var goP_Izq = Caja("Puerta_Entrada_C1", estructura, cuadrado,
                            new Vector2(-14.5f, -1.5f), new Vector2(1, 3), puertaCerrada, true, 3);
        goP_Izq.tag = "Puerta";

        // Puerta derecha (salida hacia Cuarto 3)
        var goP_Der = Caja("Puerta_Salida_C3", estructura, cuadrado,
                            new Vector2(14.5f, -1.5f), new Vector2(1, 3), puertaAbierta, true, 3);
        goP_Der.tag = "Puerta";

        // ============================================================
        //  PROPS  (decoracion)
        // ============================================================
        // Nota 1 en la pared izquierda del pasillo
        Caja("Nota_1", props, cuadrado, new Vector2(-11, -2.8f), new Vector2(0.4f, 0.25f), Color.white, false, 4);

        // Nota 2 cerca de la tuberia caliente
        Caja("Nota_2", props, cuadrado, new Vector2(2.5f, -0.25f), new Vector2(0.4f, 0.25f), Color.white, false, 4);

        // Candado/panel de control (para puerta izquierda - visual)
        Caja("Panel_Control", props, cuadrado, new Vector2(-13.8f, -2.2f), new Vector2(0.35f, 0.5f), new Color(0.7f, 0.65f, 0.1f), false, 4);

        // Goteo de aceite en la pared derecha
        Caja("Mancha_Aceite", props, cuadrado, new Vector2(12, -2.5f), new Vector2(0.5f, 1.2f), new Color(0.1f, 0.1f, 0.12f), false, -1);

        // Caja metalica apilada (pequeno obstaculo decorativo)
        Caja("Caja_Metal_A", props, cuadrado, new Vector2(-9, -2.6f), new Vector2(0.8f, 0.8f), metal, false, 1);
        Caja("Caja_Metal_B", props, cuadrado, new Vector2(-8.5f, -1.8f), new Vector2(0.6f, 0.6f), metal * 1.1f, false, 1);

        // ============================================================
        //  TRIGGERS  (zona de cauterizacion + transiciones)
        // ============================================================
        // Zona de cauterizacion: trigger sobre la tuberia caliente
        {
            var goTrig = new GameObject("Trigger_Cauterizacion");
            goTrig.transform.SetParent(triggers, false);
            goTrig.transform.localPosition = new Vector3(0, -0.25f, 0);
            var bc = goTrig.AddComponent<BoxCollider2D>();
            bc.size      = new Vector2(6.5f, 1f);
            bc.isTrigger = true;
            goTrig.tag   = "ZonaCauterizacion";
        }

        // Trigger de teletransporte: zona al llegar a puerta derecha
        {
            var goTrig = new GameObject("Trigger_SalidaC3");
            goTrig.transform.SetParent(triggers, false);
            goTrig.transform.localPosition = new Vector3(14f, -1.5f, 0);
            var bc = goTrig.AddComponent<BoxCollider2D>();
            bc.size      = new Vector2(1.2f, 3f);
            bc.isTrigger = true;
            goTrig.tag   = "SalidaNivel";
        }

        // ============================================================
        //  LUCES 2D
        // ============================================================
        // Oscuridad base - mas apretada que el patio exterior
        Luz("Luz_Ambiental_Pasillo", luces, Light2D.LightType.Global, Vector2.zero, new Color(0.06f, 0.06f, 0.10f), 0.10f);

        // Tubo fluorescente parpadeante sobre la tuberia caliente
        Luz("Luz_Fluorescente_Centro", luces, Light2D.LightType.Point,
            new Vector2(0, 1.5f), new Color(0.85f, 0.95f, 1.00f), 1.0f, 6f);

        // Brillo naranja/rojo de la tuberia caliente
        Luz("Luz_Tuberia_Calor",  luces, Light2D.LightType.Point,
            new Vector2(0, -0.5f), new Color(1f, 0.45f, 0.10f), 0.9f, 4f);

        // Luz tenue de entrada (desde el patio)
        Luz("Luz_Entrada", luces, Light2D.LightType.Point,
            new Vector2(-13, 0f), new Color(1f, 0.85f, 0.6f), 0.5f, 5f);

        // Advertencia: parpadeo del fluorescente
        var tipoParpadeo = UnityEditor.TypeCache.GetTypesDerivedFrom<MonoBehaviour>()
                              .FirstOrDefault(t => t.Name == "LuzParpadeante");
        var luzFluor = GameObject.Find("Luz_Fluorescente_Centro");
        if (tipoParpadeo != null && luzFluor != null)
            luzFluor.AddComponent(tipoParpadeo);
        else
            Debug.LogWarning("No se encontro LuzParpadeante.cs. Agregala manualmente a Luz_Fluorescente_Centro.");

        // ============================================================
        //  LIMITES DE CAMARA
        // ============================================================
        var limGO  = new GameObject("Limites_Cuarto2");
        limGO.transform.SetParent(raiz.transform, false);
        var limBC  = limGO.AddComponent<BoxCollider2D>();
        limBC.size    = new Vector2(30, 6);
        limBC.isTrigger = true;

        // ============================================================
        //  CAMARA: centrar y ajustar tama~no para techo bajo
        // ============================================================
        Camera cam = Camera.main;
        if (cam != null)
        {
            Undo.RecordObject(cam, "Configurar camara Cuarto 2");
            cam.orthographic     = true;
            cam.orthographicSize = 3.5f; // techo bajo => zoom mas cerrado (era 5 en Cuarto 1)
            cam.transform.position = new Vector3(0, 0, -10);
        }

        // Posicionar el cuarto a la derecha del Cuarto 1 (offset de 40 u)
        raiz.transform.position = new Vector3(40f, 0f, 0f);

        Selection.activeGameObject = raiz;
        EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        Debug.Log(
            "Cuarto 2 (Pasillo) creado en x=40.\n" +
            "• Arrastra 'Limites_Cuarto2' al campo 'Limites' de CamaraSeguimiento cuando el jugador entre.\n" +
            "• Asigna el script 'Interactuable.cs' a Puerta_Entrada_C1 y Puerta_Salida_C3.\n" +
            "• Trigger_Cauterizacion necesita SistemaSangre para funcionar (siguiente sprint).\n" +
            "• Agrega 'LuzParpadeante.cs' a Luz_Fluorescente_Centro si aun no existe.");
    }

    // ---------- Utilidades ----------

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
        var l         = go.AddComponent<Light2D>();
        l.lightType   = tipo;
        l.color       = color;
        l.intensity   = intensidad;
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
        imp.textureType          = TextureImporterType.Sprite;
        imp.spriteImportMode     = SpriteImportMode.Single;
        imp.spritePixelsPerUnit  = 4;
        imp.filterMode           = FilterMode.Point;
        imp.textureCompression   = TextureImporterCompression.Uncompressed;
        imp.SaveAndReimport();

        return AssetDatabase.LoadAssetAtPath<Sprite>(RutaSprite);
    }
}
