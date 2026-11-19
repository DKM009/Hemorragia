using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.InputSystem;

/// <summary>
/// Cinematica despues de vencer al Demonio de Fuego: el guardia despierta en su silla (todo fue una pesadilla)
/// y de ahi pasa a la EscenaFinal. Imagenes y sonidos en Assets/Resources/Despertar.
/// Espacio / Enter la saltan. Se lanza con Despertar.Reproducir().
/// </summary>
public class Despertar : MonoBehaviour
{
    static bool activa;
    bool saltar;

    public static void Reproducir()
    {
        if (activa) return;
        activa = true;
        var go = new GameObject("Despertar");
        var d = go.AddComponent<Despertar>(); d.StartCoroutine(d.Secuencia());
    }

    IEnumerator Secuencia()
    {
        var canvasGO = new GameObject("Overlay_Despertar");
        canvasGO.transform.SetParent(transform, false);
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 950;

        var negro = Relleno(canvasGO.transform, Color.black, 0f);
        var p = new RawImage[4];
        bool hay = true;
        for (int i = 0; i < 4; i++)
        {
            var t = Resources.Load<Texture2D>("Despertar/Despertar_" + (i + 1));
            if (t == null) { hay = false; break; }
            p[i] = Capa(canvasGO.transform, t);
        }
        var flash = Relleno(canvasGO.transform, Color.white, 0f);
        var cortina = Relleno(canvasGO.transform, Color.black, 0f);
        var pista = Pista(canvasGO.transform, "[Espacio] saltar");

        var compu = Fuente(canvasGO, true);
        var venti = Fuente(canvasGO, true);
        var resp = Fuente(canvasGO, false);
        var manana = Fuente(canvasGO, true);
        var pajaros = Fuente(canvasGO, true);
        var grito = Fuente(canvasGO, false);
        AudioListener.pause = true;          // calla la arena; las fuentes de aqui lo ignoran
        saltar = false;

        yield return Fundir(negro, 0f, 1f, 0.8f);

        if (hay)
        {
            var linea = Linea(p, flash, compu, venti, resp, manana, pajaros, grito);
            while (!saltar && linea.MoveNext())
            {
                float espera = (float)linea.Current, tt = 0f;
                while (tt < espera)
                {
                    if (Salto()) { saltar = true; break; }
                    tt += Time.deltaTime;
                    yield return null;
                }
            }
        }
        else yield return new WaitForSeconds(1f);

        if (saltar) cortina.color = Color.black;
        else yield return Fundir(cortina, 0f, 1f, 1.5f);
        foreach (var a in new[] { compu, venti, resp, manana, pajaros, grito }) if (a != null) a.Stop();
        pista.gameObject.SetActive(false);
        AudioListener.pause = false;
        activa = false;

        string destino = Application.CanStreamedLevelBeLoaded("EscenaFinal") ? "EscenaFinal" : "MenuPrincipal";
        if (GestorJuego.Instancia != null) GestorJuego.Instancia.CargarEscena(destino);
        else SceneManager.LoadScene(destino);
    }

    IEnumerator Linea(RawImage[] p, Image flash, AudioSource compu, AudioSource venti, AudioSource resp, AudioSource manana, AudioSource pajaros, AudioSource grito)
    {
        // 1. Dormido en el escritorio, de noche (0 - 5.5 s)
        Sonar(compu, "Despertar_Computadora", 0f); StartCoroutine(Volumen(compu, 0f, 0.55f, 2f));
        Sonar(venti, "Despertar_Ventilacion", 0f); StartCoroutine(Volumen(venti, 0f, 0.35f, 2f));
        StartCoroutine(Fundir(p[0], 0f, 1f, 1.2f));
        StartCoroutine(Mover(p[0], 6.5f, 1.0f, 1.1f, 0f));
        yield return 5.5f;

        // 2. Abre los ojos (5.5 - 9.5 s)
        Sonar(resp, "Despertar_Respiracion", 0.8f);
        StartCoroutine(Fundir(p[1], 0f, 1f, 0.5f));
        StartCoroutine(Mover(p[1], 4.5f, 1.0f, 1.14f, 0f));
        yield return 4f;

        // 3. Se incorpora de golpe (9.5 - 14 s)
        p[2].color = Color.white;
        Sonar(grito, "Despertar_Grito", 0.85f);
        StartCoroutine(Fundir(flash, 0.55f, 0f, 0.35f));
        StartCoroutine(Mover(p[2], 4.5f, 1.12f, 1.0f, 5f));
        StartCoroutine(Volumen(resp, 0.8f, 0.45f, 4f));
        yield return 4.5f;

        // 4. Amanece: todo fue un sueno (14 - 22 s)
        StartCoroutine(Fundir(p[3], 0f, 1f, 1.6f));
        StartCoroutine(Mover(p[3], 8f, 1.0f, 1.08f, 0f));
        StartCoroutine(Volumen(compu, 0.55f, 0f, 2f));
        StartCoroutine(Volumen(venti, 0.35f, 0f, 2f));
        StartCoroutine(Volumen(resp, 0.45f, 0f, 1.5f));
        Sonar(manana, "Despertar_Manana", 0f); StartCoroutine(Volumen(manana, 0f, 0.6f, 2f));
        Sonar(pajaros, "Despertar_Pajaros", 0f); StartCoroutine(Volumen(pajaros, 0f, 0.3f, 3f));
        yield return 8f;
        StartCoroutine(Volumen(manana, 0.6f, 0f, 1.5f));
        StartCoroutine(Volumen(pajaros, 0.3f, 0f, 1.5f));
        yield return 0.2f;
    }

    // ── Ayudas ───────────────────────────────────────────────────────────────
    static bool Salto()
    {
        var k = Keyboard.current;
        return k != null && (k.spaceKey.wasPressedThisFrame || k.enterKey.wasPressedThisFrame);
    }

    static Image Relleno(Transform padre, Color c, float alfa)
    {
        var go = new GameObject("Relleno");
        go.transform.SetParent(padre, false);
        var im = go.AddComponent<Image>();
        im.color = new Color(c.r, c.g, c.b, alfa);
        im.raycastTarget = false;
        var rt = im.rectTransform;
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        return im;
    }

    static RawImage Capa(Transform padre, Texture tex)
    {
        var go = new GameObject("Plano");
        go.transform.SetParent(padre, false);
        var ri = go.AddComponent<RawImage>();
        ri.texture = tex;
        ri.raycastTarget = false;
        ri.color = new Color(1f, 1f, 1f, 0f);
        var rt = ri.rectTransform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = Vector2.zero;
        var f = go.AddComponent<AspectRatioFitter>();
        f.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
        f.aspectRatio = (float)tex.width / tex.height;
        return ri;
    }

    static Text Pista(Transform padre, string texto)
    {
        var go = new GameObject("Pista");
        go.transform.SetParent(padre, false);
        var t = go.AddComponent<Text>();
        t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.fontSize = 20;
        t.alignment = TextAnchor.LowerRight;
        t.color = new Color(1f, 1f, 1f, 0.45f);
        t.raycastTarget = false;
        t.text = texto;
        var rt = t.rectTransform;
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(20f, 16f); rt.offsetMax = new Vector2(-24f, -16f);
        return t;
    }

    static AudioSource Fuente(GameObject go, bool lazo)
    {
        var a = go.AddComponent<AudioSource>();
        a.playOnAwake = false; a.loop = lazo; a.spatialBlend = 0f; a.ignoreListenerPause = true;
        return a;
    }

    static void Sonar(AudioSource a, string clip, float vol)
    {
        var c = Resources.Load<AudioClip>("Despertar/" + clip);
        if (a == null || c == null) return;
        a.volume = vol; a.clip = c; a.Play();
    }

    static IEnumerator Volumen(AudioSource a, float de, float a2, float dur)
    {
        float t = 0f;
        while (t < 1f && a != null)
        {
            t += Time.unscaledDeltaTime / dur;
            a.volume = Mathf.Lerp(de, a2, t);
            yield return null;
        }
    }

    static IEnumerator Fundir(Graphic g, float de, float a, float dur)
    {
        float t = 0f;
        while (t < 1f && g != null)
        {
            t += Time.unscaledDeltaTime / dur;
            var c = g.color; c.a = Mathf.Lerp(de, a, t); g.color = c;
            yield return null;
        }
    }

    static IEnumerator Mover(RawImage r, float dur, float z0, float z1, float temblor)
    {
        float t = 0f;
        while (t < 1f && r != null)
        {
            t += Time.unscaledDeltaTime / dur;
            float z = Mathf.Lerp(z0, z1, Mathf.SmoothStep(0f, 1f, t));
            r.rectTransform.localScale = new Vector3(z, z, 1f);
            float k = temblor * (1f - t);
            r.rectTransform.anchoredPosition = temblor > 0f
                ? new Vector2((Mathf.PerlinNoise(Time.unscaledTime * 18f, 0f) - 0.5f) * k * 2f, (Mathf.PerlinNoise(0f, Time.unscaledTime * 18f) - 0.5f) * k * 2f)
                : Vector2.zero;
            yield return null;
        }
    }
}
