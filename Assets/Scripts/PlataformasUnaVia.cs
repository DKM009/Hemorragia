using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Convierte las tuberias y estantes delgados en plataformas "de una via": Jeremy las atraviesa desde abajo
/// (o si aparece debajo de una) y se para encima al caer sobre ellas. Evita que se atore bajo los tubos
/// cuando el espacio es menor que su altura. No toca escenas: se aplica solo al cargar un nivel.
/// </summary>
public static class PlataformasUnaVia
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Registrar() { SceneManager.sceneLoaded += (e, m) => Aplicar(e); }

    static bool EsLosa(string n)
    {
        return n.StartsWith("Tubo_Cal") || n.StartsWith("Tubo_Frio") || n.StartsWith("Tuberia_Caliente")
            || n.StartsWith("Tuberia_Emergencia") || n.StartsWith("Plataforma") || n.StartsWith("Plat_")
            || n.StartsWith("Estante");
    }

    static void Aplicar(Scene esc)
    {
        if (!esc.name.StartsWith("Nivel")) return;
        int cuenta = 0;
        foreach (var c in Object.FindObjectsByType<BoxCollider2D>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (c == null || c.isTrigger || c.usedByComposite) continue;
            if (c.GetComponent<Rigidbody2D>() != null) continue;
            if (!EsLosa(c.gameObject.name)) continue;
            Vector3 ls = c.transform.lossyScale;
            float alto = Mathf.Abs(c.size.y * ls.y), ancho = Mathf.Abs(c.size.x * ls.x);
            if (alto > 0.6f || ancho < 1f) continue;
            float z = c.transform.eulerAngles.z;
            if (Mathf.Abs(z) > 1f && Mathf.Abs(z) < 359f) continue;

            c.usedByEffector = true;
            var ef = c.GetComponent<PlatformEffector2D>();
            if (ef == null) ef = c.gameObject.AddComponent<PlatformEffector2D>();
            ef.useOneWay = true;
            ef.useOneWayGrouping = false;
            ef.surfaceArc = 160f;
            ef.useSideFriction = false;
            ef.useSideBounce = false;
            cuenta++;
        }
        Debug.Log("[Plataformas] " + cuenta + " tuberias/estantes ahora se pueden atravesar desde abajo.");
    }
}
