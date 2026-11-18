using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Agrega al menu principal los botones CONTROLES y CREDITOS (con sus paneles)
/// y hace latir el titulo. Se crea solo al abrir la escena MenuPrincipal.
/// </summary>
public class MenuExtras : MonoBehaviour
{
    GameObject panelControles, panelCreditos;
    Text titulo;
    Color colorTitulo;

    const string TextoControles =
        "CONTROLES\n\n" +
        "A / D  o  flechas  -  mover\n" +
        "Shift  -  correr (sangras mas rapido y haces ruido)\n" +
        "Espacio / W  -  saltar\n" +
        "S / Flecha abajo  -  agacharse\n" +
        "E / F  -  interactuar / esconderse / leer notas\n" +
        "Q  -  lanzar una lata para distraer\n" +
        "Esc  -  pausa\n\n" +
        "CONSEJOS\n" +
        "Los tubos calientes cauterizan tu herida.\n" +
        "Los Purgadores siguen tus charcos de sangre.\n" +
        "El ruido despierta a La Fosa.";

    const string TextoCreditos =
        "¡WAKE UP!\n\n" +
        "Videojuego de terror 2D\n\n" +
        "Equipo\n" +
        "Oscar  -  Alexa  -  Pedro  -  Ivan\n" +
        "Texturas y audio: Pedro\n\n" +
        "Interfaz: Bloodlines Dark UI (Alebardium)\n" +
        "Tipografia: Manufacturing Consent (SIL OFL)\n\n" +
        "Hecho con Unity";

    void Start()
    {
        var go = GameObject.Find("Texto_Titulo");
        if (go != null) { titulo = go.GetComponent<Text>(); if (titulo != null) { titulo.text = "¡WAKE UP!"; colorTitulo = titulo.color; } }

        VestirBotones();
        var canvas = UIFab.CrearCanvas("Canvas_MenuExtras", 60, transform);
        ArmarBotones(canvas);

        panelControles = CrearPanel(canvas.transform, "Panel_Controles", TextoControles, 34, TextAnchor.MiddleLeft);
        panelCreditos  = CrearPanel(canvas.transform, "Panel_Creditos",  TextoCreditos, 40, TextAnchor.MiddleCenter);
    }

    /// <summary>Los cuatro botones (JUGAR, CONTROLES, CREDITOS, SALIR) quedan del mismo tamano, en el mismo canvas y en columna.</summary>
    void ArmarBotones(Canvas canvasExtra)
    {
        var jugar = GameObject.Find("BotonJugar"); var salir = GameObject.Find("BotonSalir");
        RectTransform molde = jugar != null ? jugar.GetComponent<RectTransform>() : null;
        Transform padre = molde != null ? molde.parent : canvasExtra.transform;
        Vector2 tam = new Vector2(260f, 50f);
        Vector2 p0 = molde != null ? molde.anchoredPosition : new Vector2(0f, -20f);
        const float paso = 62f;

        var bc = UIFab.Boton(padre, "BotonControles", "CONTROLES", Vector2.zero, tam, () => Mostrar(panelControles, true));
        var bk = UIFab.Boton(padre, "BotonCreditos", "CREDITOS", Vector2.zero, tam, () => Mostrar(panelCreditos, true));
        Colocar(jugar, molde, p0, tam);
        Colocar(bc.gameObject, molde, p0 + new Vector2(0f, -paso), tam);
        Colocar(bk.gameObject, molde, p0 + new Vector2(0f, -paso * 2f), tam);
        Colocar(salir, molde, p0 + new Vector2(0f, -paso * 3f), tam);
    }

    static void Colocar(GameObject go, RectTransform molde, Vector2 pos, Vector2 tam)
    {
        if (go == null) return;
        var rt = go.GetComponent<RectTransform>(); if (rt == null) return;
        if (molde != null && rt != molde) { rt.anchorMin = molde.anchorMin; rt.anchorMax = molde.anchorMax; rt.pivot = molde.pivot; }
        rt.anchoredPosition = pos; rt.sizeDelta = tam;
        foreach (var t in go.GetComponentsInChildren<Text>(true)) t.fontSize = 24;
    }

    /// <summary>Pone el arte de Bloodlines a los botones que ya estan en la escena (Jugar, Salir).</summary>
    void VestirBotones()
    {
        var ui = BancoUI.Actual; if (ui == null) return;
        foreach (var b in FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            var img = b.GetComponent<Image>(); if (img == null) continue;
            img.sprite = ui.btnNormal; img.type = Image.Type.Sliced; img.color = Color.white;
            b.transition = Selectable.Transition.SpriteSwap;
            var ss = new SpriteState();
            ss.highlightedSprite = ui.btnHover != null ? ui.btnHover : ui.btnNormal;
            ss.selectedSprite = ss.highlightedSprite;
            ss.pressedSprite = ui.btnPresionado != null ? ui.btnPresionado : ui.btnNormal;
            ss.disabledSprite = ui.btnApagado != null ? ui.btnApagado : ui.btnNormal;
            b.spriteState = ss;
            var rt = img.rectTransform;
            rt.sizeDelta = new Vector2(Mathf.Max(rt.sizeDelta.x, 150f), Mathf.Max(rt.sizeDelta.y, 44f));
            foreach (var t in b.GetComponentsInChildren<Text>(true))
                if (ui.fuente != null) { t.font = ui.fuente; t.fontStyle = FontStyle.Normal; }
        }
    }

    GameObject CrearPanel(Transform padre, string nombre, string texto, int tam, TextAnchor alinea)
    {
        var fondo = UIFab.Panel(padre, nombre, new Color(0.02f, 0.01f, 0.03f, 0.96f));
        UIFab.Texto(fondo.transform, "Texto", texto, tam, new Color(0.88f, 0.82f, 0.82f),
                    new Vector2(0, 40), new Vector2(1300, 760), alinea);
        UIFab.Boton(fondo.transform, "BotonVolver", "VOLVER", new Vector2(0, -400), new Vector2(240, 50),
                    () => fondo.gameObject.SetActive(false));
        fondo.gameObject.SetActive(false);
        return fondo.gameObject;
    }

    void Mostrar(GameObject panel, bool v) { if (panel != null) panel.SetActive(v); }

    void Update()
    {
        if (titulo == null) return;
        float p = 0.75f + 0.25f * Mathf.Sin(Time.time * 1.6f);
        titulo.color = new Color(colorTitulo.r * p, colorTitulo.g * p, colorTitulo.b * p, colorTitulo.a);
        if (UIFab.PulsoEscape())
        {
            if (panelControles != null) panelControles.SetActive(false);
            if (panelCreditos != null) panelCreditos.SetActive(false);
        }
    }
}
