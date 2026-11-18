using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Pone el fondo nuevo (Resources/Fondo_Emboscada.jpg: bodega con la puerta rota y la alarma roja)
/// detras del cuarto de la emboscada del Nivel 1, en lugar del fondo anterior que no le quedaba.
/// El piso del dibujo queda a la altura del piso del cuarto. Para ajustarlo, cambia los valores de abajo.
/// </summary>
public static class FondoEmboscada
{
    const float Ancho = 17f;        // ancho en unidades (el cuarto mide 16)
    const float CentroY = -0.745f;  // sube o baja el dibujo para alinear el piso

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Registrar() { SceneManager.sceneLoaded += (e, md) => Iniciar(); }

    static void Iniciar()
    {
        if (SceneManager.GetActiveScene().name != "Nivel1") return;
        var tex = Resources.Load<Texture2D>("Fondo_Emboscada");
        if (tex == null) return;

        Transform cuarto = null;
        foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (t.name == "Cuarto4_Emboscada") { cuarto = t; break; }
        if (cuarto == null) return;

        SpriteRenderer sr = null;
        foreach (var r in cuarto.GetComponentsInChildren<SpriteRenderer>(true))
            if (r.name.StartsWith("Gemini_Generated_Image")) { sr = r; break; }

        if (sr == null)   // si borraste el fondo viejo, crea uno
        {
            var go = new GameObject("Fondo_Emboscada");
            go.transform.SetParent(cuarto, false);
            sr = go.AddComponent<SpriteRenderer>();
            sr.sortingOrder = -10;
            go.transform.localPosition = new Vector3(0f, 0f, 5f);
        }

        tex.filterMode = FilterMode.Bilinear;
        float ppu = tex.width / Ancho;
        sr.sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), ppu, 0, SpriteMeshType.FullRect);
        sr.color = Color.white;
        var p = sr.transform.localPosition;
        sr.transform.localPosition = new Vector3(0f, CentroY, p.z);
        sr.transform.localScale = Vector3.one;
    }
}
