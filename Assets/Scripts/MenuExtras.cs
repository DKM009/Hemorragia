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
        "A / D  -  mover\n" +
        "Shift  -  correr (sangras mas rapido y haces ruido)\n" +
        "Espacio  -  saltar\n" +
        "E  -  interactuar / esconderse / leer notas\n" +
        "Q  -  lanzar una lata para distraer\n" +
        "Esc  -  pausa\n\n" +
        "CONSEJOS\n" +
        "Los tubos calientes cauterizan tu herida.\n" +
        "Los Purgadores siguen tus charcos de sangre.\n" +
        "El ruido despierta a La Fosa.";

    const string TextoCreditos =
        "HEMORRAGIA\n\n" +
        "Videojuego de terror 2D\n\n" +
        "Equipo\n" +
        "Oscar  -  Alexa  -  Pedro  -  Ivan\n\n" +
        "Hecho con Unity";

    void Start()
    {
        var go = GameObject.Find("Texto_Titulo");
        if (go != null) { titulo = go.GetComponent<Text>(); if (titulo != null) colorTitulo = titulo.color; }

        var canvas = UIFab.CrearCanvas("Canvas_MenuExtras", 60, transform);
        UIFab.Boton(canvas.transform, "BotonControles", "CONTROLES", new Vector2(0, -165), new Vector2(260, 45), () => Mostrar(panelControles, true));
        UIFab.Boton(canvas.transform, "BotonCreditos", "CREDITOS", new Vector2(0, -225), new Vector2(260, 45), () => Mostrar(panelCreditos, true));

        panelControles = CrearPanel(canvas.transform, "Panel_Controles", TextoControles, 34, TextAnchor.MiddleLeft);
        panelCreditos  = CrearPanel(canvas.transform, "Panel_Creditos",  TextoCreditos, 40, TextAnchor.MiddleCenter);
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
