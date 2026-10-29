using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Utilidades para armar interfaz por codigo (menu, pausa, game over) sin tocar escenas,
/// y arranque automatico: al cargar MenuPrincipal agrega controles/creditos,
/// y al cargar un nivel agrega el menu de pausa (Esc).
/// </summary>
public static class UIFab
{
    static Font Fuente => Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

    // ── Arranque automatico ──────────────────────────────────────────────────
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Registrar()
    {
        SceneManager.sceneLoaded -= AlCargarEscena;
        SceneManager.sceneLoaded += AlCargarEscena;
    }

    static void AlCargarEscena(Scene escena, LoadSceneMode modo)
    {
        Time.timeScale = 1f;
        if (escena.name == "MenuPrincipal")
        {
            if (Object.FindFirstObjectByType<MenuExtras>() == null)
                new GameObject("MenuExtras").AddComponent<MenuExtras>();
        }
        else if (escena.name.StartsWith("Nivel"))
        {
            if (Object.FindFirstObjectByType<MenuPausa>() == null)
                new GameObject("MenuPausa").AddComponent<MenuPausa>();
        }
    }

    // ── Constructores de UI ──────────────────────────────────────────────────
    public static Canvas CrearCanvas(string nombre, int orden, Transform padre = null)
    {
        var go = new GameObject(nombre);
        if (padre != null) go.transform.SetParent(padre, false);
        var c = go.AddComponent<Canvas>();
        c.renderMode = RenderMode.ScreenSpaceOverlay;
        c.sortingOrder = orden;
        var s = go.AddComponent<CanvasScaler>();
        s.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        s.referenceResolution = new Vector2(1920, 1080);
        go.AddComponent<GraphicRaycaster>();
        AsegurarEventSystem();
        return c;
    }

    public static Image Panel(Transform padre, string nombre, Color color)
    {
        var go = new GameObject(nombre);
        go.transform.SetParent(padre, false);
        var img = go.AddComponent<Image>();
        img.color = color;
        var rt = img.rectTransform;
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        return img;
    }

    public static Text Texto(Transform padre, string nombre, string texto, int tam, Color color,
                             Vector2 pos, Vector2 caja, TextAnchor alinea = TextAnchor.MiddleCenter,
                             FontStyle estilo = FontStyle.Normal)
    {
        var go = new GameObject(nombre);
        go.transform.SetParent(padre, false);
        var t = go.AddComponent<Text>();
        t.font = Fuente;
        t.text = texto;
        t.fontSize = tam;
        t.fontStyle = estilo;
        t.alignment = alinea;
        t.color = color;
        t.raycastTarget = false;
        var rt = t.rectTransform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = caja;
        return t;
    }

    public static Button Boton(Transform padre, string nombre, string texto, Vector2 pos, Vector2 tam, UnityAction accion)
    {
        var go = new GameObject(nombre);
        go.transform.SetParent(padre, false);
        var img = go.AddComponent<Image>();
        img.color = new Color(0.25f, 0.05f, 0.05f);
        var rt = img.rectTransform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = tam;
        var b = go.AddComponent<Button>();
        var cols = b.colors;
        cols.highlightedColor = new Color(0.55f, 0.08f, 0.08f);
        cols.pressedColor = new Color(0.75f, 0.1f, 0.1f);
        b.colors = cols;
        if (accion != null) b.onClick.AddListener(accion);
        Texto(go.transform, "Texto", texto, 26, new Color(0.92f, 0.86f, 0.86f), Vector2.zero, tam,
              TextAnchor.MiddleCenter, FontStyle.Bold);
        return b;
    }

    public static void AsegurarEventSystem()
    {
        if (Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() != null) return;
        var es = new GameObject("EventSystem");
        es.AddComponent<UnityEngine.EventSystems.EventSystem>();
#if ENABLE_INPUT_SYSTEM
        es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
        es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
#endif
    }

    public static bool PulsoEscape()
    {
#if ENABLE_INPUT_SYSTEM
        var k = UnityEngine.InputSystem.Keyboard.current;
        return k != null && k.escapeKey.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.Escape);
#endif
    }
}

/// <summary>Pantalla "HAS MUERTO" que se muestra mientras la pantalla esta en negro.</summary>
public static class PantallaMuerte
{
    static GameObject actual;

    public static void Mostrar(bool mostrar)
    {
        if (!mostrar)
        {
            if (actual != null) Object.Destroy(actual);
            actual = null;
            return;
        }
        if (actual != null) return;

        var canvas = UIFab.CrearCanvas("PantallaMuerte", 950);
        actual = canvas.gameObject;
        UIFab.Texto(canvas.transform, "Titulo", "HAS MUERTO", 110, new Color(0.75f, 0.04f, 0.04f),
                    new Vector2(0, 40), new Vector2(1400, 160), TextAnchor.MiddleCenter, FontStyle.Bold);
        UIFab.Texto(canvas.transform, "Sub", "La sangre no perdona. Reapareces en el ultimo punto de control.",
                    30, new Color(0.7f, 0.55f, 0.55f), new Vector2(0, -60), new Vector2(1400, 60),
                    TextAnchor.MiddleCenter, FontStyle.Italic);
    }
}
