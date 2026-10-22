using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Busca todas las Light2D de tipo Global en la escena y borra las duplicadas,
/// dejando solo la primera. Ejecutar una vez despues de generar los greybox.
/// Tools > Hemorragia > Limpiar Luces Globales Duplicadas
/// </summary>
public static class LimpiarLucesGlobales
{
    [MenuItem("Tools/Hemorragia/Limpiar Luces Globales Duplicadas")]
    public static void Limpiar()
    {
        var todas = Object.FindObjectsByType<Light2D>(FindObjectsInactive.Include);
        Light2D primera = null;
        int borradas = 0;

        foreach (var l in todas)
        {
            if (l.lightType != Light2D.LightType.Global) continue;

            if (primera == null)
            {
                primera = l;
                Debug.Log($"[LucesGlobales] Conservando: {l.gameObject.name}");
            }
            else
            {
                Debug.Log($"[LucesGlobales] Borrando duplicada: {l.gameObject.name}");
                Undo.DestroyObjectImmediate(l.gameObject);
                borradas++;
            }
        }

        if (borradas == 0)
            Debug.Log("[LucesGlobales] No habia duplicadas. Todo bien.");
        else
            Debug.Log($"[LucesGlobales] {borradas} luz(es) global(es) duplicada(s) eliminada(s).");

        UnityEditor.SceneManagement.EditorSceneManager
            .MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
    }
}
