using UnityEditor;
using UnityEngine;

/// <summary>
/// Configura automaticamente el GestorNivel1 con un solo clic:
///   1. Crea Spawn_Cuarto2/3/4 donde no existan.
///   2. Añade TriggerCuarto a los triggers de salida de cada cuarto.
///   3. Asigna todos los campos del GestorNivel1 en el Inspector.
///
/// Menu: Tools > Hemorragia > Configurar GestorNivel1 (Auto-Setup)
/// </summary>
public static class ConfigurarGestorNivel1
{
    [MenuItem("Tools/Hemorragia/Configurar GestorNivel1 (Auto-Setup)")]
    public static void Configurar()
    {
        var gestor = Object.FindFirstObjectByType<GestorNivel1>();
        if (gestor == null)
        {
            EditorUtility.DisplayDialog("Error",
                "No se encontró GestorNivel1 en la escena.\n" +
                "Asegúrate de tener el GameObject con ese script.", "OK");
            return;
        }

        // ── Buscar cuartos ────────────────────────────────────────────────────
        var c1 = GameObject.Find("Cuarto1_Patio");
        var c2 = GameObject.Find("Cuarto2_Pasillo");
        var c3 = GameObject.Find("Cuarto3_Bodega");
        var c4 = GameObject.Find("Cuarto4_Emboscada");

        // ── Asignar cuartos al gestor ─────────────────────────────────────────
        Undo.RecordObject(gestor, "Auto-Setup GestorNivel1");

        if (c1 != null) gestor.cuarto1_Patio     = c1;
        if (c2 != null) gestor.cuarto2_Pasillo   = c2;
        if (c3 != null) gestor.cuarto3_Bodega    = c3;
        if (c4 != null) gestor.cuarto4_Emboscada = c4;

        // ── Crear spawns faltantes y asignarlos ───────────────────────────────
        gestor.spawn1 = ObtenerOCrearSpawn(c1, "Spawn_Cuarto1", new Vector3(-12f, -3f,  0f));
        gestor.spawn2 = ObtenerOCrearSpawn(c2, "Spawn_Cuarto2", new Vector3(-13f, -2f,  0f));
        gestor.spawn3 = ObtenerOCrearSpawn(c3, "Spawn_Cuarto3", new Vector3(-14f, -2f,  0f));
        gestor.spawn4 = ObtenerOCrearSpawn(c4, "Spawn_Cuarto4", new Vector3(-10f, -2f,  0f));

        // ── Asignar límites de cámara ─────────────────────────────────────────
        gestor.limites1 = BuscarLimites("Limites_Cuarto1");
        gestor.limites2 = BuscarLimites("Limites_Cuarto2");
        gestor.limites3 = BuscarLimites("Limites_Cuarto3");
        gestor.limites4 = BuscarLimites("Limites_Cuarto4");

        // ── Configurar triggers de salida ─────────────────────────────────────
        // Cuarto 1 → Cuarto 2
        ConfigurarTriggerSalida("Trigger_SalidaC2", 1);
        // Cuarto 2 → Cuarto 3
        ConfigurarTriggerSalida("Trigger_SalidaC3", 2);
        // Cuarto 3 → Cuarto 4
        ConfigurarTriggerSalida("Trigger_SalidaC4", 3);

        EditorUtility.SetDirty(gestor);
        UnityEditor.SceneManagement.EditorSceneManager
            .MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        // ── Reporte final ─────────────────────────────────────────────────────
        string reporte =
            $"✅ GestorNivel1 configurado:\n\n" +
            $"Cuartos: {(c1?c1.name:"❌")} / {(c2?c2.name:"❌")} / {(c3?c3.name:"❌")} / {(c4?c4.name:"❌")}\n" +
            $"Spawn1: {(gestor.spawn1 ? gestor.spawn1.name : "❌")}\n" +
            $"Spawn2: {(gestor.spawn2 ? gestor.spawn2.name : "❌")}\n" +
            $"Spawn3: {(gestor.spawn3 ? gestor.spawn3.name : "❌")}\n" +
            $"Spawn4: {(gestor.spawn4 ? gestor.spawn4.name : "❌")}\n" +
            $"Límites1: {(gestor.limites1 ? "✅" : "❌ no encontrado")}\n" +
            $"Límites2: {(gestor.limites2 ? "✅" : "❌ no encontrado")}\n" +
            $"Límites3: {(gestor.limites3 ? "✅" : "❌ no encontrado")}\n" +
            $"Límites4: {(gestor.limites4 ? "✅" : "❌ no encontrado")}\n\n" +
            "Guarda la escena con Ctrl+S.";

        Debug.Log("[ConfigurarGestorNivel1] " + reporte);
        EditorUtility.DisplayDialog("Auto-Setup completado", reporte, "OK");
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Busca un spawn por nombre en toda la escena; si no existe lo crea
    /// como hijo del cuarto indicado en la posicion local dada.
    /// </summary>
    static Transform ObtenerOCrearSpawn(GameObject cuarto, string nombre, Vector3 posLocal)
    {
        // Buscar en toda la escena primero
        var existente = GameObject.Find(nombre);
        if (existente != null) return existente.transform;

        // No existe → crear
        if (cuarto == null)
        {
            Debug.LogWarning($"[ConfigurarGestorNivel1] No se pudo crear '{nombre}': cuarto no encontrado.");
            return null;
        }

        var go = new GameObject(nombre);
        Undo.RegisterCreatedObjectUndo(go, $"Crear {nombre}");
        go.transform.SetParent(cuarto.transform, false);
        go.transform.localPosition = posLocal;
        Debug.Log($"[ConfigurarGestorNivel1] ✅ Creado: {nombre} en {cuarto.name}");
        return go.transform;
    }

    /// <summary>
    /// Busca un Limites_CuartoX en la escena y devuelve su Collider2D.
    /// </summary>
    static Collider2D BuscarLimites(string nombre)
    {
        var go = GameObject.Find(nombre);
        if (go == null)
        {
            Debug.LogWarning($"[ConfigurarGestorNivel1] ⚠ No se encontró '{nombre}'.");
            return null;
        }
        var col = go.GetComponent<Collider2D>();
        if (col == null)
            Debug.LogWarning($"[ConfigurarGestorNivel1] ⚠ '{nombre}' no tiene Collider2D.");
        return col;
    }

    /// <summary>
    /// Busca el trigger de salida por nombre y le asigna/configura TriggerCuarto
    /// apuntando al cuarto destino indicado.
    /// </summary>
    static void ConfigurarTriggerSalida(string nombre, int indiceCuartoDestino)
    {
        var go = GameObject.Find(nombre);
        if (go == null)
        {
            Debug.LogWarning($"[ConfigurarGestorNivel1] ⚠ No se encontró trigger '{nombre}'.");
            return;
        }

        var tc = go.GetComponent<TriggerCuarto>();
        if (tc == null)
            tc = Undo.AddComponent<TriggerCuarto>(go);

        Undo.RecordObject(tc, $"Configurar {nombre}");
        tc.tipo                 = TriggerCuarto.TipoTrigger.TransicionCuarto;
        tc.indiceCuartoDestino  = indiceCuartoDestino;
        tc.soloUnaVez           = false; // permitir volver a activar si el jugador regresa
        EditorUtility.SetDirty(tc);

        Debug.Log($"[ConfigurarGestorNivel1] ✅ {nombre} → Cuarto {indiceCuartoDestino}");
    }
}
