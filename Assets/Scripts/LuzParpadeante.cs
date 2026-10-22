using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Parpadeo organico para luces URP 2D (fluorescentes, antorchas, etc.)
/// Agrega al mismo GameObject que el Light2D.
/// </summary>
[RequireComponent(typeof(Light2D))]
public class LuzParpadeante : MonoBehaviour
{
    [Header("Intensidad")]
    public float intensidadBase   = 1.0f;
    public float intensidadMin    = 0.05f;
    [Tooltip("Rango de variacion suave (ruido Perlin).")]
    public float amplitudRuido    = 0.15f;
    [Tooltip("Velocidad del ruido Perlin. Mas alto = mas nervioso.")]
    public float velocidadRuido   = 1.8f;

    [Header("Cortes bruscos (fluorescente)")]
    [Tooltip("Probabilidad por segundo de un corte brusco.")]
    public float probCorte        = 0.08f;
    [Tooltip("Duracion min/max del corte en segundos.")]
    public float corteMin         = 0.04f;
    public float corteMax         = 0.18f;

    [Header("Rafagas (opcional)")]
    [Tooltip("Probabilidad por segundo de una rafaga de parpadeos rapidos.")]
    public float probRafaga       = 0.02f;
    public int   ciclosRafaga     = 5;
    public float duracionCiclo    = 0.05f;

    Light2D luz;
    float semilla;
    bool enCorte;

    void Awake()
    {
        luz    = GetComponent<Light2D>();
        semilla = Random.Range(0f, 100f);
    }

    void OnEnable()
    {
        StartCoroutine(BucleParpade());
    }

    void OnDisable()
    {
        StopAllCoroutines();
        if (luz != null) luz.intensity = intensidadBase;
    }

    IEnumerator BucleParpade()
    {
        while (true)
        {
            if (!enCorte)
            {
                float t = Time.time * velocidadRuido + semilla;
                float ruido = Mathf.PerlinNoise(t, semilla) * 2f - 1f; // -1..1
                luz.intensity = Mathf.Max(intensidadMin, intensidadBase + ruido * amplitudRuido);

                // Corte brusco
                if (Random.value < probCorte * Time.deltaTime)
                    StartCoroutine(Corte());

                // Rafaga
                if (Random.value < probRafaga * Time.deltaTime)
                    StartCoroutine(Rafaga());
            }
            yield return null;
        }
    }

    IEnumerator Corte()
    {
        enCorte = true;
        luz.intensity = intensidadMin;
        yield return new WaitForSeconds(Random.Range(corteMin, corteMax));
        enCorte = false;
    }

    IEnumerator Rafaga()
    {
        for (int i = 0; i < ciclosRafaga; i++)
        {
            luz.intensity = intensidadMin;
            yield return new WaitForSeconds(duracionCiclo);
            luz.intensity = intensidadBase;
            yield return new WaitForSeconds(duracionCiclo);
        }
    }
}
