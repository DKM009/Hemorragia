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
