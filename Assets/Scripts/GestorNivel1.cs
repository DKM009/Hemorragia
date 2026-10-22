using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Gestor del Nivel 1 de Hemorragia.
/// Orquesta la transicion entre los 4 cuartos, activa enemigos en triggers
/// y maneja el evento de climax (La Fosa irrumpe en el Cuarto 4).
///
/// Setup en Unity:
///   1. Crea un GameObject vacio llamado "GestorNivel1" y agrega este script.
///   2. En el Inspector arrastra las referencias de cuartos, spawn points, etc.
///   3. El Cuarto activo al inicio es el 0 (Cuarto1_Patio).
/// </summary>
public class GestorNivel1 : MonoBehaviour
{
    // ── Cuartos ───────────────────────────────────────────────────────────────
    [Header("Cuartos (GameObjects raiz)")]
    public GameObject cuarto1_Patio;
    public GameObject cuarto2_Pasillo;
    public GameObject cuarto3_Bodega;
    public GameObject cuarto4_Emboscada;

    // ── Spawn points ──────────────────────────────────────────────────────────
    [Header("Puntos de spawn por cuarto")]
    public Transform spawn1;
    public Transform spawn2;
    public Transform spawn3;
    public Transform spawn4;

    // ── Limites de camara por cuarto ──────────────────────────────────────────
    [Header("Limites de camara")]
    public Collider2D limites1;
    public Collider2D limites2;
    public Collider2D limites3;
    public Collider2D limites4;

    // ── Enemigos ──────────────────────────────────────────────────────────────
    [Header("Enemigos del Cuarto 3 (Bodega)")]
    [Tooltip("Purgadores que se activan en la emboscada de la Bodega.")]
    public PurgadorIA[] purgadoresBodega;

    [Header("Enemigos del Cuarto 4 (Emboscada)")]
    public PurgadorIA[] purgadoresEmboscada;
    [Tooltip("La Fosa irrumpe al llegar al trigger de climax del Cuarto 4.")]
    public LaFosaIA     laFosa;

    // ── Estado ────────────────────────────────────────────────────────────────
    [Header("Estado")]
    public int cuartoActual = 0;

    CamaraSeguimiento camara;
    bool climaxActivado;

    // ─────────────────────────────────────────────────────────────────────────

    void Awake()
    {
        camara = Camera.main?.GetComponent<CamaraSeguimiento>();
        // Solo mostrar el cuarto inicial; los demas se desactivan
        ActivarCuarto(cuartoActual);
    }

    void Start()
    {
        // Guardar checkpoint inicial
        if (GestorJuego.Instancia != null && spawn1 != null)
            GestorJuego.Instancia.GuardarCheckpoint(spawn1.position);

        // Desactivar enemigos hasta que se activen por trigger
        DesactivarEnemigos(purgadoresBodega);
        DesactivarEnemigos(purgadoresEmboscada);
        if (laFosa != null) laFosa.gameObject.SetActive(false);
    }

    // ── Transicion de cuartos ─────────────────────────────────────────────────

    /// <summary>Llama esto desde el trigger de salida de cada cuarto.</summary>
    public void IrAlCuarto(int indice)
    {
        if (indice == cuartoActual) return;
        StartCoroutine(RutinaTransicion(indice));
    }

    IEnumerator RutinaTransicion(int indice)
    {
        yield return new WaitForSeconds(0.05f); // micro-pausa para que el trigger procese

        // Mover jugador al spawn del cuarto destino
        Transform spawn = ObtenerSpawn(indice);
        var jugador = GameObject.FindGameObjectWithTag("Player");
        if (jugador != null && spawn != null)
            jugador.transform.position = spawn.position;

        // Actualizar limites de camara
        if (camara != null)
            camara.limites = ObtenerLimites(indice);

        ActivarCuarto(indice);
        cuartoActual = indice;

        // Guardar checkpoint al llegar a un nuevo cuarto
        if (GestorJuego.Instancia != null && spawn != null)
            GestorJuego.Instancia.GuardarCheckpoint(spawn.position);
    }

    void ActivarCuarto(int indice)
    {
        GameObject[] cuartos = { cuarto1_Patio, cuarto2_Pasillo, cuarto3_Bodega, cuarto4_Emboscada };
        for (int i = 0; i < cuartos.Length; i++)
            if (cuartos[i] != null) cuartos[i].SetActive(i == indice);
    }

    Transform ObtenerSpawn(int indice) => indice switch
    {
        0 => spawn1,
        1 => spawn2,
        2 => spawn3,
        3 => spawn4,
        _ => spawn1
    };

    Collider2D ObtenerLimites(int indice) => indice switch
    {
        0 => limites1,
        1 => limites2,
        2 => limites3,
        3 => limites4,
        _ => limites1
    };

    // ── Activacion de enemigos ─────────────────────────────────────────────────

    /// <summary>Llama desde Trigger_Emboscada del Cuarto 3.</summary>
    public void ActivarEmboscadaBodega()
    {
        ActivarEnemigos(purgadoresBodega);
        Debug.Log("[GestorNivel1] Emboscada activada en la Bodega.");
    }

    /// <summary>Llama desde Trigger_Climax del Cuarto 4.</summary>
    public void ActivarClimax()
    {
        if (climaxActivado) return;
        climaxActivado = true;

        ActivarEnemigos(purgadoresEmboscada);

        if (laFosa != null)
        {
            laFosa.gameObject.SetActive(true);
            Debug.Log("[GestorNivel1] CLIMAX: La Fosa ha irrumpido.");
        }
    }

    // ── Utilidades ────────────────────────────────────────────────────────────

    void ActivarEnemigos(PurgadorIA[] lista)
    {
        if (lista == null) return;
        foreach (var e in lista)
            if (e != null) { e.gameObject.SetActive(true); e.enabled = true; }
    }

    void DesactivarEnemigos(PurgadorIA[] lista)
    {
        if (lista == null) return;
        foreach (var e in lista)
            if (e != null) e.gameObject.SetActive(false);
    }
}
