using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Limpia la Bodega del Nivel 1: quita el muro que colgaba del techo en medio (no servia para nada).
/// Para devolverlo, borra este script o reactiva Muro_Central_Alto / Arte_Muro_Central_Alto en la escena.
/// </summary>
public static class OrdenBodega
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Registrar() { SceneManager.sceneLoaded += (e, md) => Iniciar(); }

    static void Iniciar()
    {
        if (SceneManager.GetActiveScene().name != "Nivel1") return;
        foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if ((t.name == "Muro_Central_Alto" || t.name == "Arte_Muro_Central_Alto") && EsDeBodega(t))
                t.gameObject.SetActive(false);
    }

    static bool EsDeBodega(Transform t)
    {
        for (var p = t; p != null; p = p.parent) if (p.name == "Cuarto3_Bodega") return true;
        return false;
    }
}
