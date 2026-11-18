using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.InputSystem;

/// <summary>
/// Final del Nivel 2: Jeremy cae a la caldera. Pantalla roja, el grito se vuelve tos
/// y se carga la escena final (Jeremy despierta en la caseta con la mascarilla).
/// Pon este script en un trigger sobre la compuerta de la caldera.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class CaidaCaldera : MonoBehaviour
{
    public string escenaFinal = "EscenaFinal";

    bool activado;

    void Awake()
    {
        GetComponent<Collider2D>().isTrigger = true;
        ColocarAlFinalDeLaLava();
    }

    // El trigger estaba en la zona B (antes de la lava). Se mueve al final de la zona C, despues de cruzar la lava.
    void ColocarAlFinalDeLaLava()
    {
        var meta = GameObject.Find("Msg_N2C_Final");
        if (meta == null) return;
        transform.position = new Vector3(meta.transform.position.x + 1f, meta.transform.position.y, 0f);
        var caja = GetComponent<BoxCollider2D>();
        if (caja != null)
        {
            Vector3 e = transform.lossyScale;
            caja.size = new Vector2(2f / Mathf.Max(0.01f, Mathf.Abs(e.x)), 10f / Mathf.Max(0.01f, Mathf.Abs(e.y)));
            caja.offset = Vector2.zero;
        }
    }

    void OnTriggerEnter2D(Collider2D otro)
    {
        if (activado || !otro.CompareTag("Player")) return;
        activado = true;
        StartCoroutine(Secuencia(otro.transform));
    }

    // ── Cinematica de la caida ───────────────────────────────────────────────
    // Imagenes y sonidos en Assets/Resources/Caida (Caida_1..4.jpg, Caida_Rejilla/Grito/Viento/Fuego/Golpe/Tos.mp3).
    // Espacio / Enter la saltan.
    bool saltar;

    IEnumerator Secuencia(Transform jugador)
    {
        // Congelar al jugador y apagar a los enemigos para que nada interrumpa la caida
        var mov = jugador.GetComponent<MovimientoJugador>();
        if (mov != null) mov.enabled = false;
        var sangre = jugador.GetComponent<SistemaSangre>();
        if (sangre != null) sangre.enabled = false;
        var rb = jugador.GetComponent<Rigidbody2D>();
        if (rb != null) { rb.linearVelocity = Vector2.zero; rb.bodyType = RigidbodyType2D.Kinematic; }
        foreach (var c in jugador.GetComponents<Collider2D>()) c.enabled = false;
        foreach (var p in FindObjectsByType<PurgadorIA>(FindObjectsSortMode.None)) p.gameObject.SetActive(false);
        foreach (var f in FindObjectsByType<LaFosaIA>(FindObjectsSortMode.None)) f.gameObject.SetActive(false);
        foreach (var m in FindObjectsByType<PequenoMonstruo>(FindObjectsSortMode.None)) m.gameObject.SetActive(false);

        // Lienzo de la cinematica (encima de todo)
        var canvasGO = new GameObject("Overlay_Caida");
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 900;

        var negro = Relleno(canvasGO.transform, "Negro0", Color.black, 0f);
        var tex = new Texture2D[4];
        var capa = new RawImage[4];
        bool hayImagenes = true;
        for (int i = 0; i < 4; i++)
        {
            tex[i] = Resources.Load<Texture2D>("Caida/Caida_" + (i + 1));
            if (tex[i] == null) { hayImagenes = false; break; }
            capa[i] = Capa(canvasGO.transform, tex[i], "Plano" + (i + 1));
        }
        var destello = Relleno(canvasGO.transform, "Destello", Color.white, 0f);
        var cortina = Relleno(canvasGO.transform, "Negro1", Color.black, 0f);
        var pista = AgregarTexto(canvasGO.transform, "[Espacio] saltar");

        var sfx = Fuente(canvasGO, false);
        var viento = Fuente(canvasGO, true);
        var fuego = Fuente(canvasGO, true);
        var grito = Fuente(canvasGO, false);
        var tos = Fuente(canvasGO, false);
        AudioListener.pause = true;                 // calla lava, pasos y musica; las fuentes de aqui lo ignoran
        saltar = false;

        // A negro
        yield return Fundir(negro, 0f, 1f, 0.5f);

        if (hayImagenes)
        {
            var linea = Linea(capa, destello, sfx, viento, fuego, grito, tos);
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
        else yield return new WaitForSeconds(1.5f);

        // Cierre: a negro, se limpian los sonidos y se pasa a la pelea
        if (saltar) cortina.color = Color.black;
        else yield return Fundir(cortina, 0f, 1f, 1.2f);
        foreach (var a in new[] { sfx, viento, fuego, grito, tos }) if (a != null) a.Stop();
        AudioListener.pause = false;
        foreach (var r in capa) if (r != null) r.gameObject.SetActive(false);
        destello.gameObject.SetActive(false);
        pista.gameObject.SetActive(false);
        negro.color = Color.black;

        yield return new WaitForSeconds(0.5f);

        // Jefe final: en vez de ir directo a la escena final, cae a la arena del Demonio de Fuego
        if (ArenaJefe.Disponible())
        {
            ArenaJefe.Comenzar(jugador);
            float t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime / 1.4f;
                negro.color = new Color(0f, 0f, 0f, 1f - t);
                cortina.color = new Color(0f, 0f, 0f, 1f - t);
                yield return null;
            }
            Destroy(canvasGO);
            yield break;
        }

        string destino = Application.CanStreamedLevelBeLoaded(escenaFinal) ? escenaFinal : "MenuPrincipal";
        SceneManager.LoadScene(destino);
    }

    // Linea de tiempo (cada yield es una espera en segundos de juego)
    IEnumerator Linea(RawImage[] p, Image flash, AudioSource sfx, AudioSource viento, AudioSource fuego, AudioSource grito, AudioSource tos)
    {
        // 1. La rejilla cede (0 - 3.8 s)
        Sonar(sfx, "Caida_Rejilla", 0.9f);
        Poner(viento, "Caida_Viento", 0f); StartCoroutine(Volumen(viento, 0f, 0.45f, 3f));
        Poner(fuego, "Caida_Fuego", 0.3f);
        StartCoroutine(Fundir(p[0], 0f, 1f, 0.7f));
        StartCoroutine(Mover(p[0], 4.6f, 1.0f, 1.1f, 0f));
        yield return 3.4f;

        // 2. Se rompe y cae (3.4 - 7.6 s)
        Sonar(sfx, "Caida_Golpe", 0.7f);
        Sonar(grito, "Caida_Grito", 1f);
        StartCoroutine(Volumen(viento, 0.45f, 0.9f, 1f));
        StartCoroutine(Fundir(p[1], 0f, 1f, 0.2f));
        StartCoroutine(Mover(p[1], 4.4f, 1.0f, 1.18f, 0f));
        yield return 4.2f;

        // 3. Sigue cayendo por las tuberias (7.6 - 11 s)
        StartCoroutine(Fundir(p[2], 0f, 1f, 0.4f));
        StartCoroutine(Mover(p[2], 3.6f, 1.15f, 1.0f, 7f));
        yield return 3.4f;

        // 4. Impacto, y despierta tosiendo (11 - 18 s)
        Sonar(sfx, "Caida_Golpe", 1f);
        p[3].color = Color.white;
        StartCoroutine(Fundir(flash, 0.9f, 0f, 0.5f));
        StartCoroutine(Volumen(viento, 0.9f, 0f, 1.2f));
        StartCoroutine(Volumen(fuego, 0.3f, 0.12f, 1.5f));
        StartCoroutine(Mover(p[3], 7.5f, 1.12f, 1.0f, 0f));
        yield return 1.6f;
        Sonar(tos, "Caida_Tos", 0.8f);
        yield return 5.2f;
        StartCoroutine(Volumen(tos, 0.8f, 0f, 1.1f));
        yield return 0.2f;
    }

    // ── Ayudas ───────────────────────────────────────────────────────────────
    static bool Salto()
    {
        var k = Keyboard.current;
        return k != null && (k.spaceKey.wasPressedThisFrame || k.enterKey.wasPressedThisFrame);
    }

    static Image Relleno(Transform padre, string nombre, Color c, float alfa)
    {
        var go = new GameObject(nombre);
        go.transform.SetParent(padre, false);
        var im = go.AddComponent<Image>();
        im.color = new Color(c.r, c.g, c.b, alfa);
        im.raycastTarget = false;
        var rt = im.rectTransform;
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        return im;
    }

    static RawImage Capa(Transform padre, Texture tex, string nombre)
    {
        var go = new GameObject(nombre);
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

    static Text AgregarTexto(Transform padre, string texto)
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
        a.playOnAwake = false;
        a.loop = lazo;
        a.spatialBlend = 0f;
        a.ignoreListenerPause = true;
        return a;
    }

    static void Sonar(AudioSource a, string clip, float vol)
    {
        var c = Resources.Load<AudioClip>("Caida/" + clip);
        if (a == null || c == null) return;
        a.volume = vol;
        a.clip = c;
        a.Play();
    }

    static void Poner(AudioSource a, string clip, float vol) { Sonar(a, clip, vol); }

    static IEnumerator Volumen(AudioSource a, float de, float a2, float dur)
    {
        float t = 0f;
        while (t < 1f && a != null)
        {
            t += Time.deltaTime / dur;
            a.volume = Mathf.Lerp(de, a2, t);
            yield return null;
        }
    }

    static IEnumerator Fundir(Graphic g, float de, float a, float dur)
    {
        float t = 0f;
        while (t < 1f && g != null)
        {
            t += Time.deltaTime / dur;
            var c = g.color; c.a = Mathf.Lerp(de, a, t); g.color = c;
            yield return null;
        }
    }

    // Zoom lento + temblor opcional (en pixeles)
    static IEnumerator Mover(RawImage r, float dur, float z0, float z1, float temblor)
    {
        float t = 0f;
        while (t < 1f && r != null)
        {
            t += Time.deltaTime / dur;
            float z = Mathf.Lerp(z0, z1, Mathf.SmoothStep(0f, 1f, t));
            r.rectTransform.localScale = new Vector3(z, z, 1f);
            r.rectTransform.anchoredPosition = temblor > 0f
                ? new Vector2((Mathf.PerlinNoise(Time.time * 18f, 0f) - 0.5f) * temblor * 2f, (Mathf.PerlinNoise(0f, Time.time * 18f) - 0.5f) * temblor * 2f)
                : Vector2.zero;
            yield return null;
        }
    }
}
