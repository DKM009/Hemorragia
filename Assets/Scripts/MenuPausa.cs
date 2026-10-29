using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Menu de pausa (Esc) en los niveles: continuar, reiniciar nivel, volver al menu.
/// Se crea solo al cargar una escena cuyo nombre empieza con "Nivel".
/// </summary>
public class MenuPausa : MonoBehaviour
{
    GameObject panel;
    bool pausado;

    void Start()
    {
        var canvas = UIFab.CrearCanvas("Canvas_Pausa", 800, transform);
        var fondo = UIFab.Panel(canvas.transform, "Panel_Pausa", new Color(0.02f, 0.01f, 0.03f, 0.88f));
        panel = fondo.gameObject;

        UIFab.Texto(fondo.transform, "Titulo", "PAUSA", 90, new Color(0.8f, 0.06f, 0.06f),
                    new Vector2(0, 260), new Vector2(900, 120), TextAnchor.MiddleCenter, FontStyle.Bold);
        UIFab.Boton(fondo.transform, "Continuar", "CONTINUAR", new Vector2(0, 110), new Vector2(340, 60), Alternar);
        UIFab.Boton(fondo.transform, "Reiniciar", "REINICIAR NIVEL", new Vector2(0, 30), new Vector2(340, 60), Reiniciar);
        UIFab.Boton(fondo.transform, "Menu", "MENU PRINCIPAL", new Vector2(0, -50), new Vector2(340, 60), IrAlMenu);
        UIFab.Texto(fondo.transform, "Ayuda",
                    "A/D mover - Shift correr - Espacio saltar - E interactuar - Q lanzar lata",
                    26, new Color(0.7f, 0.6f, 0.6f), new Vector2(0, -190), new Vector2(1500, 50));
        panel.SetActive(false);
    }

    void Update()
    {
        if (UIFab.PulsoEscape()) Alternar();
    }

    void Alternar()
    {
        if (GestorJuego.Instancia != null && GestorJuego.Instancia.EstaEnGameOver) return;
        pausado = !pausado;
        panel.SetActive(pausado);
        Time.timeScale = pausado ? 0f : 1f;
    }

    void Reiniciar()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    void IrAlMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("MenuPrincipal");
    }

    void OnDestroy()
    {
        Time.timeScale = 1f;
    }
}
