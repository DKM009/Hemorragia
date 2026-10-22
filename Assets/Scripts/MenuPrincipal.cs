using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// Menu principal de Hemorragia.
/// Requiere un Canvas con:
///   - BotonJugar   (Button)
///   - BotonSalir   (Button)
///   - TextoTitulo  (Text / TMP)
/// El menu carga la escena "Nivel1" al pulsar Jugar.
/// </summary>
public class MenuPrincipal : MonoBehaviour
{
    [Header("Botones")]
    public Button botonJugar;
    public Button botonSalir;

    [Header("Animacion de entrada")]
    [Tooltip("Segundos de fade de entrada al abrir el menu.")]
    public float duracionFadeEntrada = 1.2f;

    [Header("Escena a cargar")]
    public string nombreEscenaJuego = "Nivel1";

    void Start()
    {
        if (botonJugar != null) botonJugar.onClick.AddListener(Jugar);
        if (botonSalir  != null) botonSalir.onClick.AddListener(Salir);

        // Audio del menu
        if (AudioManager.Instancia != null)
            AudioManager.CambiarMusica(AudioManager.Instancia.musicaMenu);

        StartCoroutine(FadeEntrada());
    }

    public void Jugar()
    {
        if (GestorJuego.Instancia != null)
            GestorJuego.Instancia.CargarEscena(nombreEscenaJuego);
        else
            UnityEngine.SceneManagement.SceneManager.LoadScene(nombreEscenaJuego);
    }

    public void Salir()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    IEnumerator FadeEntrada()
    {
        // Esperar un frame para que las luces esten listas
        yield return null;
        // El fade real lo maneja GestorJuego si existe
        // Aqui solo nos aseguramos que la camara empiece oscura
        var luces = FindObjectsByType<UnityEngine.Rendering.Universal.Light2D>(FindObjectsSortMode.None);
        float[] intensidadesOriginales = new float[luces.Length];
        for (int i = 0; i < luces.Length; i++)
        {
            intensidadesOriginales[i] = luces[i].intensity;
            luces[i].intensity = 0f;
        }

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / duracionFadeEntrada;
            for (int i = 0; i < luces.Length; i++)
                luces[i].intensity = Mathf.Lerp(0f, intensidadesOriginales[i], t);
            yield return null;
        }
    }
}
