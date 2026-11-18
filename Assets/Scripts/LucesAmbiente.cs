using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

/// <summary>
/// Ambiente de luces sin tocar la escena:
/// - Hace que algunas luces ya existentes parpadeen (fluorescentes / focos viejos).
/// - Nivel 2: agrega focos calidos a lo largo del recorrido (inicio, corredor, despues de la puerta del fusible).
/// Para ajustar: cambia las listas de abajo.
/// </summary>
public static class LucesAmbiente
{
    // Nivel 2: x, y, intensidad, radio, parpadea
    static readonly float[][] ExtrasNivel2 =
    {
        new[] { -4.0f, -1.0f, 0.9f, 8f, 0f },
        new[] {  0.5f, -1.2f, 0.9f, 8f, 1f },
        new[] {  4.0f, -1.0f, 0.8f, 8f, 0f },
        new[] { 12.0f, -1.2f, 0.9f, 8f, 1f },
        new[] { 22.0f, -1.2f, 0.9f, 8f, 0f },
        new[] { 33.0f, -1.2f, 0.9f, 8f, 1f },
        new[] { 44.0f, -1.0f, 1.0f, 9f, 0f },
        new[] { 50.5f, -1.0f, 1.0f, 9f, 1f },
        new[] { 57.0f, -1.2f, 1.0f, 9f, 0f },
        new[] { 62.0f, -2.0f, 1.0f, 9f, 1f },
    };

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Registrar() { SceneManager.sceneLoaded += (e, m) => Aplicar(); }

    static void Aplicar()
    {
        string esc = SceneManager.GetActiveScene().name;
        if (esc != "Nivel1" && esc != "Nivel2") return;

        int n = 0;
        foreach (var l in Object.FindObjectsByType<Light2D>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (l.lightType != Light2D.LightType.Point) continue;
            string nom = l.name;
            if (nom.Contains("Lava") || nom.Contains("Magma") || nom.Contains("Caldera") || nom.Contains("Jefe") || nom.StartsWith("Luz_Nota")) continue;
            if (l.GetComponent<LuzParpadeante>() != null) continue;
            n++;
            // una de cada tres luces + las de tubo / entrada
            if (!(n % 3 == 0 || nom.StartsWith("Luz_Tubo") || nom.StartsWith("Luz_Entrada") || nom.StartsWith("Luz_Azul"))) continue;
            Parpadear(l);
        }

        if (esc != "Nivel2" || GameObject.Find("LuzExtra_0") != null) return;
        for (int i = 0; i < ExtrasNivel2.Length; i++)
        {
            var d = ExtrasNivel2[i];
            var go = new GameObject("LuzExtra_" + i);
            go.transform.position = new Vector3(d[0], d[1], 0f);
            var l = go.AddComponent<Light2D>();
            l.lightType = Light2D.LightType.Point;
            l.color = new Color(1f, 0.66f, 0.38f);
            l.intensity = d[2];
            l.pointLightInnerRadius = 0.5f;
            l.pointLightOuterRadius = d[3];
            l.falloffIntensity = 0.6f;
            if (d[4] > 0f) Parpadear(l);
        }
    }

    static void Parpadear(Light2D l)
    {
        float baseI = l.intensity;
        var p = l.gameObject.AddComponent<LuzParpadeante>();
        p.enabled = false;
        p.intensidadBase = baseI;
        p.intensidadMin = baseI * 0.12f;
        p.amplitudRuido = baseI * 0.15f;
        p.probCorte = Random.Range(0.12f, 0.3f);
        p.probRafaga = Random.Range(0.03f, 0.08f);
        p.enabled = true;
    }
}
