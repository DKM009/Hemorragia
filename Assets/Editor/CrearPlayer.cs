using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Crea el GameObject Player (maniqui) con todos los componentes necesarios
/// ya enlazados: Rigidbody2D, BoxCollider2D, MovimientoJugador, SistemaSangre.
/// Menú: Tools > Hemorragia > Crear Player (Maniqui)
/// </summary>
public static class CrearPlayer
{
    [MenuItem("Tools/Hemorragia/Crear Player (Maniqui)")]
    public static void Crear()
    {
        if (GameObject.FindGameObjectWithTag("Player") != null)
        {
            bool reemplazar = EditorUtility.DisplayDialog("Player",
                "Ya existe un objeto con tag Player en la escena.\n¿Deseas borrarlo y crear uno nuevo?",
                "Sí, reemplazar", "Cancelar");
            if (!reemplazar) return;
            Object.DestroyImmediate(GameObject.FindGameObjectWithTag("Player"));
        }

        // ── Raiz del jugador ──────────────────────────────────────────────────
        var playerGO = new GameObject("Player");
        Undo.RegisterCreatedObjectUndo(playerGO, "Crear Player");

        playerGO.tag = "Player";
        playerGO.layer = 0;

        // Sprite raiz (maniqui 1×2 unidades, blanco claro)
        var sr = playerGO.AddComponent<SpriteRenderer>();
        sr.sprite       = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        sr.color        = new Color(0.85f, 0.80f, 0.75f);
        sr.sortingOrder = 10;
        playerGO.transform.localScale = new Vector3(1f, 2f, 1f);

        // Situar en origen o cerca del spawn del cuarto 1
        var spawn = GameObject.Find("Spawn_Cuarto1");
        playerGO.transform.position = spawn != null
            ? spawn.transform.position
            : new Vector3(-12f, -2f, 0f);

        // ── Fisica ────────────────────────────────────────────────────────────
        var rb = playerGO.AddComponent<Rigidbody2D>();
        rb.constraints   = RigidbodyConstraints2D.FreezeRotation;
        rb.gravityScale  = 3.5f;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        var col = playerGO.AddComponent<BoxCollider2D>();
        col.size   = new Vector2(0.85f, 0.95f);   // en espacio local (el GO ya escala 2 en Y)
        col.offset = new Vector2(0f, -0.025f);

        // ── Scripts de juego ──────────────────────────────────────────────────
        playerGO.AddComponent<MovimientoJugador>();
        playerGO.AddComponent<SistemaSangre>();
        playerGO.AddComponent<InventarioJugador>();

        // ── HUD de sangre (Canvas en World Space, hijo del Player) ────────────
        // Se crea un HUD mínimo funcional en Screen Space
        var canvasGO = new GameObject("HUD_Sangre");
        Undo.RegisterCreatedObjectUndo(canvasGO, "Crear HUD Sangre");
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        canvasGO.AddComponent<UnityEngine.UI.CanvasScaler>().uiScaleMode =
            UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasGO.AddComponent<UnityEngine.UI.GraphicRaycaster>();

        // Fondo de la barra
        var fondoGO = new GameObject("Fondo_Barra");
        fondoGO.transform.SetParent(canvasGO.transform, false);
        var fondoRT = fondoGO.AddComponent<RectTransform>();
        fondoRT.anchorMin = new Vector2(0f, 0f);
        fondoRT.anchorMax = new Vector2(0f, 0f);
        fondoRT.pivot     = new Vector2(0f, 0f);
        fondoRT.anchoredPosition = new Vector2(20f, 20f);
        fondoRT.sizeDelta = new Vector2(200f, 22f);
        var fondoImg = fondoGO.AddComponent<UnityEngine.UI.Image>();
        fondoImg.color = new Color(0.1f, 0.05f, 0.05f, 0.85f);

        // Relleno
        var rellenoGO = new GameObject("Relleno_Barra");
        rellenoGO.transform.SetParent(fondoGO.transform, false);
        var rellenoRT = rellenoGO.AddComponent<RectTransform>();
        rellenoRT.anchorMin = Vector2.zero;
        rellenoRT.anchorMax = Vector2.one;
        rellenoRT.sizeDelta = Vector2.zero;
        rellenoRT.offsetMin = new Vector2(2f,  2f);
        rellenoRT.offsetMax = new Vector2(-2f, -2f);
        var rellenoImg = rellenoGO.AddComponent<UnityEngine.UI.Image>();
        rellenoImg.color      = new Color(0.7f, 0.05f, 0.05f);
        rellenoImg.type       = UnityEngine.UI.Image.Type.Filled;
        rellenoImg.fillMethod = UnityEngine.UI.Image.FillMethod.Horizontal;
        rellenoImg.fillOrigin = 0;

        // Texto "SANGRE"
        var textoGO = new GameObject("Texto_Estado");
        textoGO.transform.SetParent(fondoGO.transform, false);
        var textoRT = textoGO.AddComponent<RectTransform>();
        textoRT.anchorMin = Vector2.zero;
        textoRT.anchorMax = Vector2.one;
        textoRT.sizeDelta = Vector2.zero;
        var txt = textoGO.AddComponent<UnityEngine.UI.Text>();
        txt.text      = "";
        txt.fontSize  = 11;
        txt.fontStyle = FontStyle.Bold;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.color     = new Color(1f, 0.85f, 0.85f);

        // HUDSangre script
        var hud = fondoGO.AddComponent<HUDSangre>();
        hud.rellenoBarra = rellenoImg;
        hud.textoEstado  = txt;

        // Enlazar HUD al SistemaSangre
        var ss = playerGO.GetComponent<SistemaSangre>();
        ss.hud = hud;

        // ── Camara: asignar objetivo ──────────────────────────────────────────
        var camSeg = Camera.main?.GetComponent<CamaraSeguimiento>();
        if (camSeg != null)
        {
            camSeg.objetivo = playerGO.transform;
            // Intentar asignar limites del cuarto 1 si existe
            var limGO = GameObject.Find("Limites_Cuarto1");
            if (limGO != null)
                camSeg.limites = limGO.GetComponent<Collider2D>();
        }

        // ── EventSystem (necesario para que el Canvas/Menu funcione) ──────────
        if (Object.FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            var esGO = new GameObject("EventSystem");
            Undo.RegisterCreatedObjectUndo(esGO, "Crear EventSystem");
            esGO.AddComponent<UnityEngine.EventSystems.EventSystem>();
            esGO.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }

        Selection.activeGameObject = playerGO;
        EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        Debug.Log(
            "[CrearPlayer] ✅ Player creado con: MovimientoJugador, SistemaSangre, InventarioJugador, HUD.\n" +
            "• Controles: A/D mover | Shift correr | Espacio/W saltar\n" +
            "• La cámara ya tiene el objetivo asignado.\n" +
            "• Si el HUD no se ve, verifica que no haya otro Canvas encima.");
    }
}
