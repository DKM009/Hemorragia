using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Acerca la camara para que los personajes se vean mas grandes (sin tocar escenas).
/// factorZoom 0.8 = personajes 25% mas grandes. Cambia el numero si lo quieres mas/menos cerca.
/// </summary>
public static class AjusteCamara
{
    public static float factorZoom = 0.85f;
    static readonly HashSet<Camera> aplicadas = new HashSet<Camera>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Registrar() { SceneManager.sceneLoaded += (e, m) => Aplicar(); }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Aplicar()
    {
        foreach (var c in Object.FindObjectsByType<CamaraSeguimiento>(FindObjectsSortMode.None))
        {
            var cam = c.GetComponent<Camera>();
            if (cam == null || !cam.orthographic) continue;
            if (!aplicadas.Add(cam)) continue;
            cam.orthographicSize *= factorZoom;
        }
    }
}
