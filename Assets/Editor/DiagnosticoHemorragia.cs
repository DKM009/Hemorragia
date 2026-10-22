using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Diagnostica y repara los problemas más comunes del proyecto Hemorragia.
/// Menú: Tools > Hemorragia > Diagnosticar y Reparar Escena
/// </summary>
public static class DiagnosticoHemorragia
{
    [MenuItem("Tools/Hemorragia/Diagnosticar y Reparar Escena")]
    public static void Diagnosticar()
    {
        int fixes = 0;
        var log = new System.Text.StringBuilder();
        log.AppendLine("=== DIAGNÓSTICO HEMORRAGIA ===\n");

        // 1. EventSystem
        var es = Object.FindAnyObjectByType<EventSystem>();
        if (es == null)
        {
            var esGO = new GameObject("EventSystem");
            Undo.RegisterCreatedObjectUndo(esGO, "Fix EventSystem");
            esGO.AddComponent<EventSystem>();
            esGO.AddComponent<StandaloneInputModule>();
            log.AppendLine("✅ EventSystem creado (era necesario para botones y UI).");
            fixes++;
        }
        else
        {
            log.AppendLine($"✔ EventSystem OK ({es.gameObject.name}).");
        }

        // 2. Player con tag correcto
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            log.AppendLine("⚠️  No hay ningún GameObject con tag 'Player'.");
            log.AppendLine("   → Ejecuta: Tools > Hemorragia > Crear Player (Maniqui)");
        }
        else
        {
            log.AppendLine($"✔ Player encontrado: '{player.name}'.");

            // Verificar componentes del jugador
            if (player.GetComponent<MovimientoJugador>() == null)
            {
                player.AddComponent<MovimientoJugador>();
                log.AppendLine("  ✅ MovimientoJugador añadido al Player.");
                fixes++;
            }
            if (player.GetComponent<SistemaSangre>() == null)
            {
                player.AddComponent<SistemaSangre>();
                log.AppendLine("  ✅ SistemaSangre añadido al Player.");
                fixes++;
            }
            if (player.GetComponent<InventarioJugador>() == null)
            {
                player.AddComponent<InventarioJugador>();
                log.AppendLine("  ✅ InventarioJugador añadido al Player.");
                fixes++;
            }
            var rb = player.GetComponent<Rigidbody2D>();
            if (rb == null)
            {
                rb = player.AddComponent<Rigidbody2D>();
                rb.constraints = RigidbodyConstraints2D.FreezeRotation;
                rb.gravityScale = 3.5f;
                log.AppendLine("  ✅ Rigidbody2D añadido al Player.");
                fixes++;
            }
            if (player.GetComponent<Collider2D>() == null)
            {
                var bc = player.AddComponent<BoxCollider2D>();
                bc.size = new Vector2(0.85f, 0.95f);
                log.AppendLine("  ✅ BoxCollider2D añadido al Player.");
                fixes++;
            }
        }

        // 3. Cámara
        var cam = Camera.main;
        if (cam == null)
        {
            log.AppendLine("⚠️  No hay Main Camera en la escena.");
        }
        else
        {
            var camSeg = cam.GetComponent<CamaraSeguimiento>();
            if (camSeg == null)
            {
                cam.gameObject.AddComponent<CamaraSeguimiento>();
                log.AppendLine("✅ CamaraSeguimiento añadida a la Main Camera.");
                fixes++;
            }
            else if (camSeg.objetivo == null && player != null)
            {
                camSeg.objetivo = player.transform;
                log.AppendLine("✅ CamaraSeguimiento.objetivo asignado al Player.");
                fixes++;
            }
            log.AppendLine($"✔ Cámara OK.");
        }

        // 4. Luces globales duplicadas
        var luces = Object.FindObjectsByType<UnityEngine.Rendering.Universal.Light2D>(FindObjectsInactive.Include);
        int globales = 0;
        foreach (var l in luces)
            if (l.lightType == UnityEngine.Rendering.Universal.Light2D.LightType.Global) globales++;
        if (globales > 1)
        {
            log.AppendLine($"⚠️  Hay {globales} luces globales. Solo debería haber 1.");
            log.AppendLine("   → Ejecuta: Tools > Hemorragia > Limpiar Luces Globales Duplicadas");
        }
        else if (globales == 0)
        {
            log.AppendLine("⚠️  No hay luz global. El juego se verá completamente negro.");
            log.AppendLine("   → Los scripts de creación de cuartos la agregan automáticamente.");
        }
        else
        {
            log.AppendLine("✔ Luz global OK (1 encontrada).");
        }

        // 5. GestorJuego
        var gestor = Object.FindAnyObjectByType<GestorJuego>();
        if (gestor == null)
        {
            log.AppendLine("⚠️  GestorJuego no encontrado.");
            log.AppendLine("   → Ejecuta: Tools > Hemorragia > Crear Menu Principal para crearlo.");
        }
        else
        {
            log.AppendLine($"✔ GestorJuego OK ({gestor.gameObject.name}).");
        }

        log.AppendLine($"\n=== {fixes} reparaciones aplicadas. ===");
        EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        bool abrir = EditorUtility.DisplayDialog("Diagnóstico Hemorragia",
            log.ToString(), "OK y guardar escena", "Solo ver");
        if (abrir)
        {
            EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
        }

        Debug.Log(log.ToString());
    }
}
