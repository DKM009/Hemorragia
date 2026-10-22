using UnityEngine;

/// <summary>
/// Contenedor de texto de una nota de lore en Hemorragia.
/// Ponlo en el mismo GameObject que el Interactuable de la nota (o como componente aparte).
/// Al interactuar (E / F cerca), muestra el texto en pantalla via Debug.Log o un HUD futuro.
///
/// NOTAS CANONICAS DEL NIVEL 1:
///   Nota 0 — Patio (junto al charco de sangre, prop "Nota_0")
///   Nota 1 — Pasillo, pared izquierda (prop "Nota_1")
///   Nota 2 — Pasillo, encima de la tuberia caliente (prop "Nota_2")
/// </summary>
public class NotaLore : MonoBehaviour
{
    [Header("Contenido")]
    [TextArea(3, 6)]
    public string titulo  = "Nota sin titulo";
    [TextArea(4, 10)]
    public string cuerpo  = "";
    [Tooltip("Numero de nota para ordenarlas en el Diario del jugador.")]
    public int numeroNota = 0;

    [Header("Indicador visual (opcional)")]
    [Tooltip("GameObject con el icono/texto '[ E ] Leer'. Se activa al acercarse.")]
    public GameObject indicadorUI;

    bool jugadorCerca;
    bool leida;

    void Start()
    {
        if (indicadorUI != null) indicadorUI.SetActive(false);
    }

    void Update()
    {
        if (!jugadorCerca) return;
        if (PulsarInteraccion()) MostrarNota();
    }

    void OnTriggerEnter2D(Collider2D otro)
    {
        if (!otro.CompareTag("Player")) return;
        jugadorCerca = true;
        if (indicadorUI != null) indicadorUI.SetActive(true);
    }

    void OnTriggerExit2D(Collider2D otro)
    {
        if (!otro.CompareTag("Player")) return;
        jugadorCerca = false;
        if (indicadorUI != null) indicadorUI.SetActive(false);
    }

    void MostrarNota()
    {
        leida = true;
        // TODO: en el siguiente sprint, enviar a un UIManager real
        Debug.Log($"[NOTA {numeroNota}] {titulo}\n{cuerpo}");
    }

    bool PulsarInteraccion()
    {
#if ENABLE_INPUT_SYSTEM
        var k = UnityEngine.InputSystem.Keyboard.current;
        return k != null && (k.eKey.wasPressedThisFrame || k.fKey.wasPressedThisFrame);
#else
        return Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.F);
#endif
    }
}
