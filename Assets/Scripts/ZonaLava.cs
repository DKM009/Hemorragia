using System.Collections;
using UnityEngine;

/// <summary>
/// Lava (o magma) mortal: el jugador se hunde, se quema (se vuelve naranja y se desvanece)
/// y muere. Despues se restaura al reaparecer en el checkpoint.
/// Pon este script en un trigger sobre la lava.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class ZonaLava : MonoBehaviour
{
    public float duracionHundimiento = 1.1f;
    public float profundidad = 0.9f;

    bool activa;

    void Awake()
    {
        GetComponent<Collider2D>().isTrigger = true;
    }

    void OnTriggerEnter2D(Collider2D otro)
    {
        if (activa || !otro.CompareTag("Player")) return;
        StartCoroutine(Quemar(otro.transform));
    }

    IEnumerator Quemar(Transform j)
    {
        activa = true;

        var mov = j.GetComponent<MovimientoJugador>();
        if (mov != null) mov.enabled = false;
        var rb = j.GetComponent<Rigidbody2D>();
        if (rb != null) { rb.linearVelocity = Vector2.zero; rb.bodyType = RigidbodyType2D.Kinematic; }
        var cols = j.GetComponents<Collider2D>();
        foreach (var c in cols) c.enabled = false;

        var sr = j.GetComponentInChildren<SpriteRenderer>();
        Color original = sr != null ? sr.color : Color.white;

        UIMensaje.Mostrar("La lava se lo traga...", 2.5f);

        Vector3 ini = j.position;
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / duracionHundimiento;
            j.position = ini + Vector3.down * (profundidad * t);
            if (sr != null)
                sr.color = Color.Lerp(original, new Color(1f, 0.35f, 0f, 0f), t);
            yield return null;
        }

        var sangre = j.GetComponent<SistemaSangre>();
        if (sangre != null) sangre.RecibirDano(9999f);
        else if (GestorJuego.Instancia != null) GestorJuego.Instancia.GameOver();

        // Esperar a que termine el game over (reaparece en el checkpoint) y restaurar al jugador
        yield return new WaitForSeconds(0.3f);
        while (GestorJuego.Instancia != null && GestorJuego.Instancia.EstaEnGameOver)
            yield return null;

        if (rb != null) rb.bodyType = RigidbodyType2D.Dynamic;
        foreach (var c in cols) c.enabled = true;
        if (sr != null) sr.color = original;
        if (mov != null) mov.enabled = true;
        activa = false;
    }
}
