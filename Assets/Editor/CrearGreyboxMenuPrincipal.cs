using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

/// <summary>
/// Crea el greybox del Menu Principal de Hemorragia.
/// Genera:
///   - Fondo oscuro con luces URP (atmosfera de horror)
///   - Canvas con botones Jugar y Salir
///   - Titulo del juego
///   - GestorJuego y AudioManager en la escena
/// Uso: Tools > Hemorragia > Crear Menu Principal
/// </summary>
public static class CrearGreyboxMenuPrincipal
{
    const string RutaSprite = "Assets/Sprites/Greybox/Cuadrado.png";

    [MenuItem("Tools/Hemorragia/Crear Menu Principal")]
    public static void CrearMenu()
    {
        if (GameObject.Find("Menu_Raiz") != null)
        {
            EditorUtility.DisplayDialog("Menu", "Ya existe Menu_Raiz en la escena.", "OK");
            return;
        }

        Sprite sp = ObtenerSprite();
        if (sp == null) { Debug.LogError("No se pudo obtener sprite greybox."); return; }

        // ── Raiz ──────────────────────────────────────────────────────────────
        var raiz = new GameObject("Menu_Raiz");
        Undo.RegisterCreatedObjectUndo(raiz, "Crear Menu Principal");

        // ── Fondo ─────────────────────────────────────────────────────────────
        var fondo = Hijo(raiz.transform, "Fondo");
        // Piso oscuro
        Caja("Piso",    fondo, sp, new Vector2(0, -4f),  new Vector2(20, 1),   new Color(0.08f, 0.07f, 0.10f), true);
        // Paredes laterales
        Caja("Pared_Izq", fondo, sp, new Vector2(-10.5f, 0), new Vector2(1, 12), new Color(0.12f, 0.10f, 0.14f), true);
        Caja("Pared_Der", fondo, sp, new Vector2( 10.5f, 0), new Vector2(1, 12), new Color(0.12f, 0.10f, 0.14f), true);
        // Techo
        Caja("Techo",   fondo, sp, new Vector2(0,  4f),  new Vector2(20, 1),   new Color(0.08f, 0.07f, 0.10f), true);

        // Tuberia decorativa con brillo (da profundidad)
        Caja("Tuberia_Fondo_A", fondo, sp, new Vector2(-5f, 0.5f), new Vector2(8, 0.3f), new Color(0.35f, 0.20f, 0.08f), false, -2);
        Caja("Tuberia_Fondo_B", fondo, sp, new Vector2( 5f, 1.5f), new Vector2(6, 0.25f),new Color(0.30f, 0.18f, 0.08f), false, -2);

        // Silueta de maniqui (el jugador en la escena del menu)
        Caja("Silueta_Jeremy", fondo, sp, new Vector2(-6f, -2.5f), new Vector2(0.8f, 1.8f), new Color(0.05f, 0.04f, 0.06f), false, 1);

        // ── Luces ─────────────────────────────────────────────────────────────
        var luces = Hijo(raiz.transform, "Luces");
        // Global muy oscuro
        var lg = new GameObject("Luz_Global_Menu"); lg.transform.SetParent(luces, false);
        var lgg = lg.AddComponent<Light2D>();
        lgg.lightType = Light2D.LightType.Global;
        lgg.color     = new Color(0.04f, 0.03f, 0.06f);
        lgg.intensity = 0.06f;

        // Luz central roja — ilumina el titulo
        var lc = new GameObject("Luz_Titulo"); lc.transform.SetParent(luces, false);
        lc.transform.localPosition = new Vector3(0, 1f, 0);
        var lgc = lc.AddComponent<Light2D>();
        lgc.lightType = Light2D.LightType.Point;
        lgc.color     = new Color(0.8f, 0.05f, 0.05f);
        lgc.intensity = 0.9f;
        lgc.pointLightOuterRadius = 7f;

        // Parpadeo en el titulo
        var tipoParpadeo = TypeCache.GetTypesDerivedFrom<MonoBehaviour>().FirstOrDefault(x => x.Name == "LuzParpadeante");
        if (tipoParpadeo != null) lc.AddComponent(tipoParpadeo);

        // Luz de tuberia caliente
        var lt = new GameObject("Luz_Tuberia_Menu"); lt.transform.SetParent(luces, false);
        lt.transform.localPosition = new Vector3(-3f, 0.5f, 0);
        var lgt = lt.AddComponent<Light2D>();
        lgt.lightType = Light2D.LightType.Point;
        lgt.color = new Color(1f, 0.4f, 0.1f);
        lgt.intensity = 0.6f;
        lgt.pointLightOuterRadius = 4f;

        // Luz de silueta (tenue, de atras)
        var ls = new GameObject("Luz_Silueta"); ls.transform.SetParent(luces, false);
        ls.transform.localPosition = new Vector3(-6f, 1f, 0);
        var lgs = ls.AddComponent<Light2D>();
        lgs.lightType = Light2D.LightType.Point;
        lgs.color = new Color(0.2f, 0.15f, 0.4f);
        lgs.intensity = 0.5f;
        lgs.pointLightOuterRadius = 4f;

        // ── Canvas UI ─────────────────────────────────────────────────────────
        var canvasGO = new GameObject("Canvas_Menu");
        canvasGO.transform.SetParent(raiz.transform, false);
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 50;
        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        canvasGO.AddComponent<GraphicRaycaster>();

        // Titulo
        var tituloGO = new GameObject("Texto_Titulo");
        tituloGO.transform.SetParent(canvasGO.transform, false);
        var rt = tituloGO.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.7f); rt.anchorMax = new Vector2(0.5f, 0.7f);
        rt.sizeDelta = new Vector2(600, 100); rt.anchoredPosition = Vector2.zero;
        var txtTitulo = tituloGO.AddComponent<Text>();
        txtTitulo.text      = "HEMORRAGIA";
        txtTitulo.fontSize  = 72;
        txtTitulo.fontStyle = FontStyle.Bold;
        txtTitulo.alignment = TextAnchor.MiddleCenter;
        txtTitulo.color     = new Color(0.85f, 0.05f, 0.05f);

        // Subtitulo
        var subGO = new GameObject("Texto_Subtitulo");
        subGO.transform.SetParent(canvasGO.transform, false);
        var subRT = subGO.AddComponent<RectTransform>();
        subRT.anchorMin = new Vector2(0.5f, 0.62f); subRT.anchorMax = new Vector2(0.5f, 0.62f);
        subRT.sizeDelta = new Vector2(500, 40); subRT.anchoredPosition = Vector2.zero;
        var txtSub = subGO.AddComponent<Text>();
        txtSub.text      = "no corras. no sangres. no hagas ruido.";
        txtSub.fontSize  = 18;
        txtSub.fontStyle = FontStyle.Italic;
        txtSub.alignment = TextAnchor.MiddleCenter;
        txtSub.color     = new Color(0.6f, 0.4f, 0.4f, 0.9f);

        // Boton Jugar
        var bJugar = CrearBoton(canvasGO.transform, "BotonJugar", "JUGAR",
            new Vector2(0, -20), new Vector2(220, 55));

        // Boton Salir
        var bSalir = CrearBoton(canvasGO.transform, "BotonSalir", "SALIR",
            new Vector2(0, -95), new Vector2(220, 45));

        // Script MenuPrincipal
        var menuScript = canvasGO.AddComponent<MenuPrincipal>();
        menuScript.botonJugar = bJugar;
        menuScript.botonSalir = bSalir;

        // ── Gestores ──────────────────────────────────────────────────────────
        if (GameObject.Find("GestorJuego") == null)
        {
            var gjGO = new GameObject("GestorJuego");
            gjGO.AddComponent<GestorJuego>();
            Debug.Log("GestorJuego creado.");
        }
        if (GameObject.Find("AudioManager") == null)
        {
            var amGO = new GameObject("AudioManager");
            amGO.AddComponent<AudioManager>();
            Debug.Log("AudioManager creado.");
        }

        // ── EventSystem OBLIGATORIO para que los botones funcionen ────────────
        if (Object.FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            var esGO = new GameObject("EventSystem");
            Undo.RegisterCreatedObjectUndo(esGO, "Crear EventSystem");
            esGO.AddComponent<UnityEngine.EventSystems.EventSystem>();
            esGO.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            Debug.Log("[Menu] EventSystem creado — los botones ahora responden al click.");
        }

        // ── Camara ────────────────────────────────────────────────────────────
        Camera cam = Camera.main;
        if (cam != null)
        {
            Undo.RecordObject(cam, "Configurar camara Menu");
            cam.orthographic     = true;
            cam.orthographicSize = 4.5f;
            cam.backgroundColor  = new Color(0.02f, 0.01f, 0.03f);
            cam.transform.position = new Vector3(0, 0, -10);
        }

        Selection.activeGameObject = raiz;
        EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        Debug.Log("✅ Menu Principal creado con EventSystem.\n" +
                  "Build Settings: MenuPrincipal(0) Nivel1(1) Nivel2(2) Nivel3(3) Nivel4(4).\n" +
                  "Asigna musica al AudioManager en el Inspector.");
    }

    // ── Utilidades ─────────────────────────────────────────────────────────────

    static Button CrearBoton(Transform padre, string nombre, string texto, Vector2 posicion, Vector2 tamano)
    {
        var go = new GameObject(nombre);
        go.transform.SetParent(padre, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f); rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = posicion; rt.sizeDelta = tamano;

        var img = go.AddComponent<Image>();
        img.color = new Color(0.25f, 0.05f, 0.05f);

        var btn = go.AddComponent<Button>();
        var cols = btn.colors;
        cols.highlightedColor = new Color(0.55f, 0.08f, 0.08f);
        cols.pressedColor     = new Color(0.75f, 0.1f, 0.1f);
        btn.colors = cols;

        var txtGO = new GameObject("Texto");
        txtGO.transform.SetParent(go.transform, false);
        var txtRt = txtGO.AddComponent<RectTransform>();
        txtRt.anchorMin = Vector2.zero; txtRt.anchorMax = Vector2.one;
        txtRt.sizeDelta = Vector2.zero;
        var txt = txtGO.AddComponent<Text>();
        txt.text      = texto;
        txt.fontSize  = 24;
        txt.fontStyle = FontStyle.Bold;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.color     = new Color(0.9f, 0.85f, 0.85f);

        return btn;
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
