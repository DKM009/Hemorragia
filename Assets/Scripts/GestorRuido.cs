using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Gestor global de ruido para Hemorragia.
/// Cualquier objeto de la escena puede emitir ruido con GestorRuido.Emitir().
/// La Fosa escucha el ruido acumulado.
///
/// Fuentes de ruido predefinidas:
///   Correr         → 2.5 / s  (continuo, lo maneja SistemaSangre/MovimientoJugador)
///   Saltar         → 4.0      (impulso)
///   Aterrizar      → 3.0      (impulso)
///   Puerta abierta → 8.0      (impulso)
///   Objeto caer    → 6.0      (impulso)
///   Golpe de Fosa  → 20.0     (no aplica al jugador: es La Fosa golpeando la pared)
/// </summary>
public class GestorRuido : MonoBehaviour
{
    // ── Singleton ─────────────────────────────────────────────────────────────
    public static GestorRuido Instancia { get; private set; }

    // ── Parametros ────────────────────────────────────────────────────────────
    [Header("Constantes de ruido")]
    public float ruidoCorrer        = 2.5f;  // por segundo
    public float ruidoSalto         = 4.0f;
    public float ruidoAterrizar     = 3.0f;
    public float ruidoPuerta        = 8.0f;
    public float ruidoObjetoCae     = 6.0f;

    [Header("Indicador visual (radio de ruido en escena)")]
    [Tooltip("Si es true, dibuja gizmos de cada evento de ruido.")]
    public bool mostrarGizmos       = true;

    // ── Lista de escuchas ─────────────────────────────────────────────────────
    readonly List<LaFosaIA> escuchas = new List<LaFosaIA>();

    // Para gizmos de depuracion
    struct EventoGizmo { public Vector3 pos; public float radio; public float tiempo; }
    readonly List<EventoGizmo> gizmosActivos = new List<EventoGizmo>();

    // ─────────────────────────────────────────────────────────────────────────

    void Awake()
    {
        if (Instancia != null && Instancia != this) { Destroy(gameObject); return; }
        Instancia = this;
    }

    void Start()
    {
        // Registrar La Fosa automaticamente
        foreach (var fosa in FindObjectsByType<LaFosaIA>(FindObjectsSortMode.None))
            Registrar(fosa);

        // Registrar en el jugador los eventos de salto y aterrizaje
        var movimiento = FindFirstObjectByType<MovimientoJugador>();
        if (movimiento != null)
            StartCoroutine(MonitorearJugador(movimiento));
    }

    void Update()
    {
        // Limpiar gizmos viejos
        gizmosActivos.RemoveAll(g => Time.time - g.tiempo > 1f);
    }

    // ── API publica ───────────────────────────────────────────────────────────

    /// <summary>Registra una IA de La Fosa para que reciba eventos de ruido.</summary>
    public void Registrar(LaFosaIA fosa)
    {
        if (!escuchas.Contains(fosa)) escuchas.Add(fosa);
    }

    /// <summary>
    /// Emite un evento de ruido desde una posicion.
    /// Todas las IA de La Fosa registradas reciben el evento (atenuado por distancia dentro de LaFosaIA).
    /// </summary>
    public static void Emitir(float cantidad, Vector3 origen)
    {
        if (Instancia == null) return;
        foreach (var fosa in Instancia.escuchas)
            fosa.RecibirRuido(cantidad, origen);

        if (Instancia.mostrarGizmos)
            Instancia.gizmosActivos.Add(new EventoGizmo
                { pos = origen, radio = cantidad * 0.5f, tiempo = Time.time });
    }

    // Atajo para ruido continuo (correr) — llamado desde el exterior cada frame
    public static void EmitirContinuo(float cantidadPorSegundo, Vector3 origen)
    {
        Emitir(cantidadPorSegundo * Time.deltaTime, origen);
    }

    // ── Monitor de jugador ────────────────────────────────────────────────────
    System.Collections.IEnumerator MonitorearJugador(MovimientoJugador mov)
    {
        bool estabaEnSuelo = true;

        while (mov != null)
        {
            bool enSuelo = mov.EstaEnSuelo;

            // Salto
            if (estabaEnSuelo && !enSuelo && mov.gameObject.GetComponent<Rigidbody2D>()?.linearVelocity.y > 0.5f)
                Emitir(ruidoSalto, mov.transform.position);

            // Aterrizaje
            if (!estabaEnSuelo && enSuelo)
                Emitir(ruidoAterrizar, mov.transform.position);

            // Correr (continuo)
            if (mov.EstaCorriendo)
                EmitirContinuo(ruidoCorrer, mov.transform.position);

            estabaEnSuelo = enSuelo;
            yield return null;
        }
    }

    // ── Gizmos ────────────────────────────────────────────────────────────────
    void OnDrawGizmos()
    {
        if (!mostrarGizmos) return;
        Gizmos.color = new Color(1f, 0.8f, 0f, 0.3f);
        foreach (var g in gizmosActivos)
            Gizmos.DrawWireSphere(g.pos, g.radio);
    }
}
