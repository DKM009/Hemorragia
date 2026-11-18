using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// HUD de sangre: barra izquierda inferior, color reactivo, pulso en critico.
/// Crea un Canvas con un Image de barra y asigna las referencias aqui.
///
/// Jerarquia sugerida:
///   Canvas (Screen Space - Overlay)
///     HUD_Sangre  <-- este script aqui
///       Fondo_Barra  (Image, color oscuro)
///       Relleno_Barra (Image, type=Filled, FillMethod=Horizontal)
///       Icono_Gota    (Image, sprite gota de sangre)
///       Texto_Estado  (Text - opcional)
/// </summary>
public class HUDSangre : MonoBehaviour
{
    [Header("Componentes UI")]
    public Image   rellenoBarra;
    public Image   iconoGota;
    public Text    textoEstado;  // puede quedar null

    [Header("Colores de estado")]
    public Color colorSano       = new Color(0.60f, 0.08f, 0.08f);
    public Color colorSangrando  = new Color(0.85f, 0.05f, 0.05f);
    public Color colorCritico    = new Color(1.00f, 0.00f, 0.00f);
    public Color colorCauteriz   = new Color(1.00f, 0.45f, 0.10f);

    [Header("Pulso critico")]
    public float velocidadPulso  = 3.0f;
    public float escalaMinPulso  = 0.85f;
    public float escalaMaxPulso  = 1.10f;

    float pctObjetivo;
    float pctActual;
    Color colorObjetivo;
    bool  pulsando;

    void Start()
    {
        var ui = BancoUI.Actual;
        if (ui == null || ui.barraLlena == null || rellenoBarra == null) return;
        var fondo = transform.Find("Fondo_Barra");
        if (fondo != null)
        {
            var fi = fondo.GetComponent<Image>();
            if (fi != null && ui.barraVacia != null) { fi.sprite = ui.barraVacia; fi.type = Image.Type.Simple; fi.color = Color.white; fi.preserveAspect = false; }
            // La barra ocupa su cuadro completo (el arte mide 722x110): sin franjas ni fondo rojo sobrante
            var rtF = fondo.GetComponent<RectTransform>();
            if (rtF != null) { rtF.sizeDelta = new Vector2(328f, 50f); rtF.anchoredPosition = new Vector2(20f, 14f); }
        }
        rellenoBarra.sprite = ui.barraLlena;
        rellenoBarra.type = Image.Type.Filled;
        rellenoBarra.fillMethod = Image.FillMethod.Horizontal;
        rellenoBarra.fillOrigin = 0;
        rellenoBarra.preserveAspect = false;
        var rtR = rellenoBarra.rectTransform;
        rtR.anchorMin = Vector2.zero; rtR.anchorMax = Vector2.one; rtR.offsetMin = Vector2.zero; rtR.offsetMax = Vector2.zero;
        // El texto de estado va ARRIBA de la barra, sin quedar encima del relleno rojo
        if (textoEstado != null)
        {
            var rtT = textoEstado.rectTransform;
            rtT.anchorMin = new Vector2(0f, 1f); rtT.anchorMax = new Vector2(1f, 1f); rtT.pivot = new Vector2(0f, 0f);
            rtT.anchoredPosition = new Vector2(6f, 2f); rtT.sizeDelta = new Vector2(0f, 26f);
            textoEstado.alignment = TextAnchor.LowerLeft;
            textoEstado.fontSize = 18;
            textoEstado.color = new Color(1f, 0.85f, 0.85f);
            if (ui.fuente != null) textoEstado.font = ui.fuente;
        }
        // el arte ya es rojo: los estados lo oscurecen o aclaran
        colorSano = Color.white;
        colorSangrando = new Color(1f, 0.70f, 0.70f);
        colorCritico = new Color(1f, 0.40f, 0.40f);
        colorCauteriz = new Color(1f, 0.65f, 0.25f);
        rellenoBarra.color = Color.white;
    }

    void Awake()
    {
        pctActual    = 1f;
        pctObjetivo  = 1f;
        colorObjetivo = colorSano;
    }

    void Update()
    {
        // Suavizado de la barra
        pctActual = Mathf.Lerp(pctActual, pctObjetivo, Time.deltaTime * 6f);
        if (rellenoBarra != null)
        {
            rellenoBarra.fillAmount = pctActual;
            rellenoBarra.color = Color.Lerp(rellenoBarra.color, colorObjetivo, Time.deltaTime * 5f);
        }

        // Pulso del icono en critico
        if (pulsando && iconoGota != null)
        {
            float s = Mathf.Lerp(escalaMinPulso, escalaMaxPulso,
                        (Mathf.Sin(Time.time * velocidadPulso * Mathf.PI * 2f) + 1f) * 0.5f);
            iconoGota.transform.localScale = Vector3.one * s;
        }
        else if (iconoGota != null)
        {
            iconoGota.transform.localScale = Vector3.Lerp(
                iconoGota.transform.localScale, Vector3.one, Time.deltaTime * 5f);
        }
    }

    /// <summary>Llamado por SistemaSangre cada frame.</summary>
    public void ActualizarBarra(float pct, SistemaSangre.EstadoSangre estado)
    {
        pctObjetivo = pct;
        pulsando    = false;

        switch (estado)
        {
            case SistemaSangre.EstadoSangre.Sano:
                colorObjetivo = colorSano;
                if (textoEstado != null) textoEstado.text = "";
                break;
            case SistemaSangre.EstadoSangre.Sangrando:
                colorObjetivo = colorSangrando;
                if (textoEstado != null) textoEstado.text = "SANGRANDO";
                break;
            case SistemaSangre.EstadoSangre.Critico:
                colorObjetivo = colorCritico;
                pulsando      = true;
                if (textoEstado != null) textoEstado.text = "CRITICO";
                break;
            case SistemaSangre.EstadoSangre.Cauterizando:
                colorObjetivo = colorCauteriz;
                if (textoEstado != null) textoEstado.text = "CAUTERIZANDO...";
                break;
        }
    }
}
