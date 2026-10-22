using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Crea la escena final de Hemorragia (secuencia de desenlace: Jeremy despierta con tos).
/// Menú: Tools > Hemorragia > Crear Escena Final
/// </summary>
public static class CrearEscenaFinal
{
    const string RutaSprite = "Assets/Sprites/Greybox/Cuadrado.png";

    [MenuItem("Tools/Hemorragia/Crear Escena Final")]
    public static void Crear()
    {
        if (GameObject.Find("EscenaFinal_Raiz") != null)
        {
            EditorUtility.DisplayDialog("Escena Final",
                "Ya existe EscenaFinal_Raiz en la escena.", "OK");
            return;
        }

        Sprite sp = ObtenerSprite();
        if (sp == null) { Debug.LogError("No se pudo obtener sprite."); return; }

        var raiz = new GameObject("EscenaFinal_Raiz");
        Undo.RegisterCreatedObjectUndo(raiz, "Crear Escena Final");

        Transform fondo  = Hijo(raiz.transform, "Fondo");
        Transform luces  = Hijo(raiz.transform, "Luces");
        Transform canvas = raiz.transform; // canvas va en raiz directo

        // ── Habitación: caseta de vigilancia ─────────────────────────────────
        Color suelo  = new Color(0.22f, 0.20f, 0.18f);
        Color pared  = new Color(0.18f, 0.17f, 0.16f);
        Color sillon = new Color(0.30f, 0.20f, 0.12f);

        Caja("Suelo",     fondo, sp, new Vector2(0, -4f),   new Vector2(14, 1),    suelo, true);
        Caja("Techo",     fondo, sp, new Vector2(0,  4f),   new Vector2(14, 1),    pared, true);
        Caja("Pared_Izq", fondo, sp, new Vector2(-7f, 0),   new Vector2(1, 10),    pared, true);
        Caja("Pared_Der", fondo, sp, new Vector2( 7f, 0),   new Vector2(1, 10),    pared, true);

        // Sillón/silla del vigilante
        Caja("Sillon_Base", fondo, sp, new Vector2(-2f, -3.2f), new Vector2(1.5f, 0.6f), sillon, false, 1);
        Caja("Sillon_Resp", fondo, sp, new Vector2(-2f, -2.3f), new Vector2(1.5f, 1.5f), sillon * 0.9f, false, 1);

        // Mesa con monitor
        Caja("Mesa",    fondo, sp, new Vector2(2f, -3.0f),  new Vector2(2.5f, 0.5f), new Color(0.28f, 0.22f, 0.16f), false, 1);
        Caja("Monitor", fondo, sp, new Vector2(2f, -2.2f),  new Vector2(1.2f, 0.9f), new Color(0.05f, 0.05f, 0.08f), false, 2);
        Caja("Pantalla",fondo, sp, new Vector2(2f, -2.2f),  new Vector2(1.0f, 0.7f), new Color(0.1f, 0.3f, 0.5f),    false, 3);

        // Mascarilla en el suelo
        Caja("Mascarilla", fondo, sp, new Vector2(-0.5f, -3.65f), new Vector2(0.5f, 0.25f),
             new Color(0.8f, 0.8f, 0.8f), false, 2);

        // ── Luces ─────────────────────────────────────────────────────────────
        // Luz global tenue: amanecer gris
        var lg = new GameObject("Luz_Global_Final");
        lg.transform.SetParent(luces, false);
        var lgg = lg.AddComponent<Light2D>();
        lgg.lightType = Light2D.LightType.Global;
        lgg.color     = new Color(0.55f, 0.50f, 0.45f);
        lgg.intensity = 0.05f; // empieza muy oscuro, el código lo sube

        // Luz del monitor (parpadeo azul)
        var lm = new GameObject("Luz_Monitor");
        lm.transform.SetParent(luces, false);
        lm.transform.localPosition = new Vector3(2f, -2f, 0);
        var lgm = lm.AddComponent<Light2D>();
        lgm.lightType = Light2D.LightType.Point;
        lgm.color     = new Color(0.2f, 0.5f, 0.9f);
        lgm.intensity = 0.6f;
        lgm.pointLightOuterRadius = 4f;

        // Ventana (luz exterior débil)
        var lv = new GameObject("Luz_Ventana");
        lv.transform.SetParent(luces, false);
        lv.transform.localPosition = new Vector3(5f, 1f, 0);
        var lgv = lv.AddComponent<Light2D>();
        lgv.lightType = Light2D.LightType.Point;
        lgv.color     = new Color(0.70f, 0.75f, 0.85f);
        lgv.intensity = 0.4f;
        lgv.pointLightOuterRadius = 5f;

        // ── Canvas: texto de cierre ───────────────────────────────────────────
        var canvasGO = new GameObject("Canvas_Final");
        canvasGO.transform.SetParent(raiz.transform, false);
        var cv = canvasGO.AddComponent<Canvas>();
        cv.renderMode = RenderMode.ScreenSpaceOverlay;
        cv.sortingOrder = 100;
        canvasGO.AddComponent<UnityEngine.UI.CanvasScaler>().uiScaleMode =
            UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasGO.AddComponent<UnityEngine.UI.GraphicRaycaster>();

        // Texto de cierre
        var txtGO = new GameObject("Texto_Final");
        txtGO.transform.SetParent(canvasGO.transform, false);
        var txtRT = txtGO.AddComponent<RectTransform>();
        txtRT.anchorMin = new Vector2(0.5f, 0.5f); txtRT.anchorMax = new Vector2(0.5f, 0.5f);
        txtRT.sizeDelta = new Vector2(700, 200); txtRT.anchoredPosition = new Vector2(0, 80);
        var txt = txtGO.AddComponent<UnityEngine.UI.Text>();
        txt.text      = "Jeremy abrió los ojos.\nTosió. Olía a escoria.\nLa mascarilla seguía en el suelo.";
        txt.fontSize  = 26;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.color     = new Color(0.85f, 0.80f, 0.75f);
        txt.fontStyle = FontStyle.Italic;

        // Créditos
        var credGO = new GameObject("Texto_Creditos");
        credGO.transform.SetParent(canvasGO.transform, false);
        var credRT = credGO.AddComponent<RectTransform>();
        credRT.anchorMin = new Vector2(0.5f, 0.2f); credRT.anchorMax = new Vector2(0.5f, 0.2f);
        credRT.sizeDelta = new Vector2(600, 60); credRT.anchoredPosition = Vector2.zero;
        var cred = credGO.AddComponent<UnityEngine.UI.Text>();
        cred.text      = "HEMORRAGIA";
        cred.fontSize  = 36;
        cred.fontStyle = FontStyle.Bold;
        cred.alignment = TextAnchor.MiddleCenter;
        cred.color     = new Color(0.70f, 0.05f, 0.05f);

        // Boton Menú
        var bMenuGO = new GameObject("BotonMenu");
        bMenuGO.transform.SetParent(canvasGO.transform, false);
        var bRT = bMenuGO.AddComponent<RectTransform>();
        bRT.anchorMin = new Vector2(0.5f, 0.1f); bRT.anchorMax = new Vector2(0.5f, 0.1f);
        bRT.sizeDelta = new Vector2(200, 45); bRT.anchoredPosition = Vector2.zero;
        var bImg = bMenuGO.AddComponent<UnityEngine.UI.Image>();
        bImg.color = new Color(0.20f, 0.05f, 0.05f);
        var btn = bMenuGO.AddComponent<UnityEngine.UI.Button>();
        btn.onClick.AddListener(() => {
            if (GestorJuego.Instancia != null) GestorJuego.Instancia.CargarEscena("MenuPrincipal");
            else UnityEngine.SceneManagement.SceneManager.LoadScene("MenuPrincipal");
        });
        var btnTxtGO = new GameObject("Texto");
        btnTxtGO.transform.SetParent(bMenuGO.transform, false);
        var btnTxtRT = btnTxtGO.AddComponent<RectTransform>();
        btnTxtRT.anchorMin = Vector2.zero; btnTxtRT.anchorMax = Vector2.one;
        btnTxtRT.sizeDelta = Vector2.zero;
        var btnTxt = btnTxtGO.AddComponent<UnityEngine.UI.Text>();
        btnTxt.text = "MENÚ PRINCIPAL";
        btnTxt.fontSize = 16;
        btnTxt.fontStyle = FontStyle.Bold;
        btnTxt.alignment = TextAnchor.MiddleCenter;
        btnTxt.color = new Color(0.9f, 0.85f, 0.85f);

        // Script de control de la escena final
        canvasGO.AddComponent<ControlEscenaFinal>();

        // ── EventSystem ───────────────────────────────────────────────────────
        if (Object.FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            var esGO = new GameObject("EventSystem");
            Undo.RegisterCreatedObjectUndo(esGO, "Crear EventSystem");
            esGO.AddComponent<UnityEngine.EventSystems.EventSystem>();
            esGO.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }

        // ── Cámara ────────────────────────────────────────────────────────────
        Camera cam = Camera.main;
        if (cam != null)
        {
            Undo.RecordObject(cam, "Config camara final");
            cam.orthographic      = true;
            cam.orthographicSize  = 4f;
            cam.backgroundColor   = new Color(0.02f, 0.02f, 0.03f);
            cam.transform.position = new Vector3(0, 0, -10);
        }

        Selection.activeGameObject = raiz;
        EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        Debug.Log("✅ Escena Final creada.\n" +
                  "Agrega esta escena a Build Settings DESPUÉS de Nivel4.\n" +
                  "Nombre de escena: 'EscenaFinal'.");
    }

    static Transform Hijo(Transform padre, string nombre)
    {
        var go = new GameObject(nombre); go.transform.SetParent(padre, false); return go.transform;
    }

    static void Caja(string nombre, Transform padre, Sprite sprite, Vector2 pos, Vector2 tam,
                     Color color, bool solido, int orden = 0)
    {
        var go = new GameObject(nombre);
        go.transform.SetParent(padre, false);
        go.transform.localPosition = new Vector3(pos.x, pos.y, 0);
        go.transform.localScale    = new Vector3(tam.x, tam.y, 1);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite; sr.color = color; sr.sortingOrder = orden;
        if (solido) go.AddComponent<BoxCollider2D>();
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
