using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Controla la secuencia de la escena final de Hemorragia.
/// Fade de entrada → texto aparece línea por línea → espera → botón menú.
/// </summary>
public class ControlEscenaFinal : MonoBehaviour
{
    [Header("Textos")]
    public Text textoFinal;
    public Text textoCreditos;

    [Header("Botón")]
    public Button botonMenu;

    [Header("Tiempos")]
    public float tiempoFadeEntrada = 2.0f;
    public float tiempoEntreLineas = 1.8f;
    public float tiempoAntesBoton  = 1.2f;

    static readonly string[] Lineas = {
        "Jeremy abrió los ojos.",
        "Tosió.  Olía a escoria.",
        "La mascarilla seguía en el suelo."
    };

    void Start()
    {
        // Esconder todo al inicio
        if (textoFinal    != null) { textoFinal.text    = ""; textoFinal.color    = new Color(textoFinal.color.r, textoFinal.color.g, textoFinal.color.b, 0f); }
        if (textoCreditos != null) { textoCreditos.color = new Color(textoCreditos.color.r, textoCreditos.color.g, textoCreditos.color.b, 0f); }
        if (botonMenu     != null)   botonMenu.gameObject.SetActive(false);

        StartCoroutine(SecuenciaFinal());
    }

    IEnumerator SecuenciaFinal()
    {
        // Fade de entrada (luz global de 0 a su valor)
        yield return StartCoroutine(FadeEntrada());

        // Mostrar líneas una a una
        string acumulado = "";
        for (int i = 0; i < Lineas.Length; i++)
        {
            acumulado += (i > 0 ? "\n" : "") + Lineas[i];
            if (textoFinal != null) textoFinal.text = acumulado;

            // Fade de opacidad del texto
            yield return StartCoroutine(FadeTexto(textoFinal, 0f, 1f, 0.8f));
            yield return new WaitForSeconds(tiempoEntreLineas);
        }

        // Fade de créditos
        yield return StartCoroutine(FadeTexto(textoCreditos, 0f, 1f, 1.2f));
        yield return new WaitForSeconds(tiempoAntesBoton);

        // Mostrar botón
        if (botonMenu != null) botonMenu.gameObject.SetActive(true);
    }

    IEnumerator FadeEntrada()
    {
        Light2D luzGlobal = null;
        float   intensidadFin = 0f;

        foreach (var l in FindObjectsByType<Light2D>(FindObjectsSortMode.None))
        {
            if (l.lightType == Light2D.LightType.Global)
            {
                luzGlobal    = l;
                intensidadFin = l.intensity;
                l.intensity   = 0f;
                break;
            }
        }

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / tiempoFadeEntrada;
            if (luzGlobal != null)
                luzGlobal.intensity = Mathf.Lerp(0f, intensidadFin, Mathf.SmoothStep(0f, 1f, t));
            yield return null;
        }
        if (luzGlobal != null) luzGlobal.intensity = intensidadFin;
    }

    IEnumerator FadeTexto(Text txt, float desde, float hasta, float duracion)
    {
        if (txt == null) yield break;
        float t = 0f;
        Color c = txt.color;
        while (t < 1f)
        {
            t += Time.deltaTime / duracion;
            c.a = Mathf.Lerp(desde, hasta, Mathf.SmoothStep(0f, 1f, t));
            txt.color = c;
            yield return null;
        }
        c.a = hasta;
        txt.color = c;
    }

    // Llamado por el botón en el inspector
    public void IrAlMenu()
    {
        if (GestorJuego.Instancia != null)
            GestorJuego.Instancia.CargarEscena("MenuPrincipal");
        else
            UnityEngine.SceneManagement.SceneManager.LoadScene("MenuPrincipal");
    }
}
