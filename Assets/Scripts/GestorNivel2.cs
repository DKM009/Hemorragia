using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Gestor del Nivel 2 (Tuberias y Calderas). Tres zonas: 0 = sala de llegada,
/// 1 = corredor de tuberias, 2 = calderas. Mismas reglas que el Nivel 1:
/// Purgadores que patrullan, La Fosa dormida (zona 0) que se despierta con el ruido,
/// escondites, latas, vendas, fusible y tubos que cauterizan.
/// Lo conecta la herramienta Tools > Hemorragia > Nivel 2.
/// </summary>
public class GestorNivel2 : MonoBehaviour
{
    [Header("Zonas")]
    public Transform spawnA;
    public Transform spawnB;
    public Transform spawnC;
    public Collider2D limitesA;
    public Collider2D limitesB;
    public Collider2D limitesC;

    [Header("Enemigos")]
    public PurgadorIA[] purgadoresZonaB;
    public PurgadorIA[] purgadoresZonaC;
    public LaFosaIA laFosa;

    public int zonaActual = 0;

    CamaraSeguimiento camara;
    readonly Dictionary<Transform, Vector3> posIniciales = new Dictionary<Transform, Vector3>();

    void Awake()
    {
        camara = Camera.main != null ? Camera.main.GetComponent<CamaraSeguimiento>() : null;
    }

    void Start()
    {
        Registrar(purgadoresZonaB);
        Registrar(purgadoresZonaC);
        if (laFosa != null) posIniciales[laFosa.transform] = laFosa.transform.position;

        // Enemigos dormidos hasta entrar a su zona
        Activar(purgadoresZonaB, false);
        Activar(purgadoresZonaC, false);

        if (camara != null && limitesA != null) camara.limites = limitesA;
        if (GestorJuego.Instancia != null && spawnA != null)
            GestorJuego.Instancia.GuardarCheckpoint(spawnA.position);

        if (camara != null) camara.AjustarIntensidad(1f);
    }

    void Registrar(PurgadorIA[] lista)
    {
        if (lista == null) return;
        foreach (var p in lista)
            if (p != null) posIniciales[p.transform] = p.transform.position;
    }

    void Activar(PurgadorIA[] lista, bool activo)
    {
        if (lista == null) return;
        foreach (var p in lista)
            if (p != null) p.gameObject.SetActive(activo);
    }

    // ── Cambio de zona ───────────────────────────────────────────────────────
    public void IrAZona(int zona)
    {
        if (zona == zonaActual) return;
        StartCoroutine(RutinaZona(zona));
    }

    IEnumerator RutinaZona(int zona)
    {
        yield return new WaitForSeconds(0.05f);

        Transform spawn = zona == 1 ? spawnB : zona == 2 ? spawnC : spawnA;
        var jugador = GameObject.FindGameObjectWithTag("Player");
        if (jugador != null && spawn != null)
        {
            jugador.transform.position = spawn.position;
            var rb = jugador.GetComponent<Rigidbody2D>();
            if (rb != null) rb.linearVelocity = Vector2.zero;
        }

        Collider2D lim = zona == 1 ? limitesB : zona == 2 ? limitesC : limitesA;
        if (camara != null && lim != null) camara.limites = lim;

        zonaActual = zona;
        ActivarEnemigosDeZona();

        if (GestorJuego.Instancia != null && spawn != null)
            GestorJuego.Instancia.GuardarCheckpoint(spawn.position);

        if (zona == 1)
            UIMensaje.Mostrar("Corredor de tuberias. Los Purgadores patrullan entre los muros. Sube por los bloques.", 5f);
        else if (zona == 2)
        {
            UIMensaje.Mostrar("Las calderas. Sube hasta la compuerta de la caldera derecha.", 5f);
            if (camara != null) camara.AjustarIntensidad(2.5f);
        }
    }

    void ActivarEnemigosDeZona()
    {
        Activar(purgadoresZonaB, zonaActual == 1);
        Activar(purgadoresZonaC, zonaActual == 2);
        if (laFosa != null) laFosa.gameObject.SetActive(zonaActual == 0);
    }

    // ── Reinicio tras morir ──────────────────────────────────────────────────
    public void ReiniciarTrasMuerte()
    {
        foreach (var kv in posIniciales)
        {
            if (kv.Key == null) continue;
            kv.Key.position = kv.Value;
            var rb = kv.Key.GetComponent<Rigidbody2D>();
            if (rb != null) rb.linearVelocity = Vector2.zero;
            var p = kv.Key.GetComponent<PurgadorIA>();
            if (p != null) p.Reiniciar();
            var f = kv.Key.GetComponent<LaFosaIA>();
            if (f != null) f.Reiniciar();
        }
        ActivarEnemigosDeZona();

        foreach (var t in FindObjectsByType<TransicionZona>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            t.Reiniciar();
    }
}
