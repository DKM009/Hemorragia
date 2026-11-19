using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

/// <summary>Tipografias del juego (Assets/Resources/Fuentes): Creepster (titulos) y Special Elite (relato).</summary>
public static class FuentesJuego
{
    static Font titulo, relato;
    public static Font Titulo { get { if (titulo == null) titulo = Resources.Load<Font>("Fuentes/Creepster-Regular"); return titulo; } }
    public static Font Relato { get { if (relato == null) relato = Resources.Load<Font>("Fuentes/SpecialElite-Regular"); return relato; } }
}

/// <summary>
/// Pule la EscenaFinal sin tocar la escena: fondo con la imagen del amanecer (tapa lo que se veia detras),
/// titulo "¡WAKE UP!", tipografia del relato y boton de volver con el mismo estilo del menu.
/// Se instala solo al cargar la escena.
/// </summary>
public class EscenaFinalPulida : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Registrar()
    {
        SceneManager.sceneLoaded -= AlCargar;
        SceneManager.sceneLoaded += AlCargar;
    }

    static void AlCargar(Scene e, LoadSceneMode m)
    {
        if (e.name != "EscenaFinal") return;
        if (FindFirstObjectByType<EscenaFinalPulida>() == null) new GameObject("EscenaFinalPulida").AddComponent<EscenaFinalPulida>();
    }

    void Awake()
    {
        var c = FindFirstObjectByType<ControlEscenaFinal>(FindObjectsInactive.Include);
        var cam = Camera.main;

        // 1. Fuera lo que se veia al fondo (cuarto de cajas y luces sueltas)
        foreach (var r in FindObjectsByType<Renderer>(FindObjectsInactive.Include, FindObjectsSortMode.None)) r.enabled = false;
        foreach (var l in FindObjectsByType<Light2D>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (l.lightType != Light2D.LightType.Global) l.enabled = false;

        // 2. Fondo: la imagen del amanecer en el escritorio, con un zoom muy lento
        var tex = Resources.Load<Texture2D>("Despertar/Despertar_4");
        if (tex != null && cam != null)
        {
            var go = new GameObject("FondoFinal");
            go.transform.position = new Vector3(cam.transform.position.x, cam.transform.position.y, 0f);
            var sr = go.AddComponent<SpriteRenderer>();
            float ppu = tex.height / (2f * cam.orthographicSize);
            sr.sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), ppu);
            sr.sortingOrder = -100;
            sr.color = new Color(0.82f, 0.78f, 0.78f, 1f);
            go.AddComponent<ZoomLento>();
        }
        if (cam != null) cam.backgroundColor = Color.black;

        if (c == null) return;
        Canvas canvas = null;
        if (c.textoFinal != null) canvas = c.textoFinal.canvas.rootCanvas;
        else if (c.textoCreditos != null) canvas = c.textoCreditos.canvas.rootCanvas;
        if (canvas == null) return;

        // 3. Lienzo a 1920x1080 para que las tipografias tengan tamano razonable
        var sc = canvas.GetComponent<CanvasScaler>();
        if (sc == null) sc = canvas.gameObject.AddComponent<CanvasScaler>();
        sc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        sc.referenceResolution = new Vector2(1920f, 1080f);
        sc.matchWidthOrHeight = 0.5f;

        // 4. Velo oscuro detras de los textos
        var velo = UIFab.Panel(canvas.transform, "VeloFinal", new Color(0f, 0f, 0f, 0.5f));
        velo.raycastTarget = false;
        velo.transform.SetAsFirstSibling();

        // 5. Relato (tipografia de maquina de escribir)
        Estilizar(c.textoFinal, FuentesJuego.Relato, 46, new Vector2(0f, 90f), new Vector2(1500f, 320f), new Color(0.93f, 0.88f, 0.8f));
        if (c.textoFinal != null) c.textoFinal.lineSpacing = 1.25f;

        // 6. Titulo (decia HEMORRAGIA)
        Estilizar(c.textoCreditos, FuentesJuego.Titulo, 130, new Vector2(0f, -170f), new Vector2(1500f, 220f), new Color(0.82f, 0.06f, 0.06f));
        foreach (var t in FindObjectsByType<Text>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (t.text != null && t.text.ToUpperInvariant().Contains("HEMORRAGIA")) t.text = "¡WAKE UP!";
        if (c.textoCreditos != null) c.textoCreditos.text = "¡WAKE UP!";

        // 7. Boton volver con el mismo arte que los demas botones
        var viejo = c.botonMenu;
        var nuevo = UIFab.Boton(canvas.transform, "BotonMenuFinal", "VOLVER AL MENÚ", new Vector2(0f, -400f), new Vector2(360f, 62f), null);
        nuevo.gameObject.SetActive(false);
        c.botonMenu = nuevo;
        if (viejo != null) Destroy(viejo.gameObject);
    }

    static void Estilizar(Text t, Font f, int tam, Vector2 pos, Vector2 caja, Color col)
    {
        if (t == null) return;
        if (f != null) { t.font = f; t.fontStyle = FontStyle.Normal; }
        t.fontSize = tam;
        t.color = col;
        t.alignment = TextAnchor.MiddleCenter;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        var rt = t.rectTransform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos; rt.sizeDelta = caja;
        var o = t.GetComponent<Outline>(); if (o == null) o = t.gameObject.AddComponent<Outline>();
        o.effectColor = new Color(0f, 0f, 0f, 0.85f); o.effectDistance = new Vector2(2.5f, -2.5f);
    }

    class ZoomLento : MonoBehaviour
    {
        float t;
        void Update()
        {
            t += Time.deltaTime;
            float z = Mathf.Lerp(1.05f, 1.14f, Mathf.Clamp01(t / 45f));
            transform.localScale = new Vector3(z, z, 1f);
        }
    }
}
