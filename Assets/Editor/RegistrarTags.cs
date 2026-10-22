using UnityEditor;
using UnityEngine;

/// <summary>
/// Registra todos los tags personalizados que usa Hemorragia.
/// Menú: Tools > Hemorragia > Registrar Tags del Proyecto
/// Ejecutar UNA VEZ al inicio del proyecto o cuando aparezca
/// el error "Tag: X is not defined".
/// </summary>
public static class RegistrarTags
{
    static readonly string[] TagsNecesarios = {
        "Player",
        "ZonaCauterizacion",
        "SalidaNivel",
        "Puerta",
        "Checkpoint",
        "Escondite",
        "Ruido"
    };

    [MenuItem("Tools/Hemorragia/Registrar Tags del Proyecto")]
    public static void Registrar()
    {
        var tagManager = new SerializedObject(
            AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);

        SerializedProperty tags = tagManager.FindProperty("tags");

        int agregados = 0;
        foreach (string tag in TagsNecesarios)
        {
            if (!TagExiste(tags, tag))
            {
                int idx = tags.arraySize;
                tags.InsertArrayElementAtIndex(idx);
                tags.GetArrayElementAtIndex(idx).stringValue = tag;
                agregados++;
                Debug.Log($"[Tags] ✅ Tag registrado: '{tag}'");
            }
            else
            {
                Debug.Log($"[Tags] ✔ Ya existía: '{tag}'");
            }
        }

        tagManager.ApplyModifiedProperties();

        EditorUtility.DisplayDialog("Tags Hemorragia",
            agregados > 0
                ? $"✅ {agregados} tag(s) nuevo(s) registrado(s).\n\nTags: {string.Join(", ", TagsNecesarios)}"
                : "✔ Todos los tags ya estaban registrados. No se hizo nada.",
            "OK");
    }

    static bool TagExiste(SerializedProperty tags, string tag)
    {
        for (int i = 0; i < tags.arraySize; i++)
            if (tags.GetArrayElementAtIndex(i).stringValue == tag) return true;
        return false;
    }

    // ── Bonus: botón para subir la luz global (modo debug) ────────────────────

    [MenuItem("Tools/Hemorragia/Luz DEBUG (subir brillo para testear)")]
    public static void LuzDebug()
    {
        var luces = Object.FindObjectsByType<UnityEngine.Rendering.Universal.Light2D>(FindObjectsInactive.Include);
        int cambiadas = 0;
        foreach (var l in luces)
        {
            if (l.lightType == UnityEngine.Rendering.Universal.Light2D.LightType.Global)
            {
                Undo.RecordObject(l, "Luz Debug");
                l.intensity = 0.6f;
                cambiadas++;
            }
        }
        if (cambiadas == 0)
            Debug.LogWarning("[LuzDebug] No se encontró luz global en la escena.");
        else
            Debug.Log($"[LuzDebug] Luz global subida a 0.6 para testear. Recuerda bajarla después.");
    }

    [MenuItem("Tools/Hemorragia/Luz NORMAL (restaurar oscuridad de horror)")]
    public static void LuzNormal()
    {
        var luces = Object.FindObjectsByType<UnityEngine.Rendering.Universal.Light2D>(FindObjectsInactive.Include);
        foreach (var l in luces)
        {
            if (l.lightType == UnityEngine.Rendering.Universal.Light2D.LightType.Global)
            {
                Undo.RecordObject(l, "Luz Normal");
                l.intensity = 0.08f;
            }
        }
        Debug.Log("[LuzNormal] Oscuridad restaurada.");
    }
}
