using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Cinematica de inicio: Menu > Jugar > Cinematica > Nivel1.
/// Sin escena propia: se dibuja encima del menu. Se salta con Espacio, Enter o Esc.
/// El video se toma de Resources/BancoAudio (campo cinematicaInicio).
/// </summary>
public class CinematicaInicio : MonoBehaviour
{
    string destino;
    VideoPlayer vp;
    RenderTexture rt;
    GameObject canvasGO;
    bool terminada;
    float tInicio;
    static bool reproduciendo;

    /// <summary>Devuelve false si no hay video configurado (entonces se carga el nivel directo).</summary>
    public static bool Reproducir(string escenaDestino)
    {
        if (reproduciendo) return true;
        var banco = Resources.Load<BancoAudio>("BancoAudio");
        if (banco == null || banco.cinematicaInicio == null) return false;
        reproduciendo = true;
        var go = new GameObject("CinematicaInicio");
        var c = go.AddComponent<CinematicaInicio>();
        c.Iniciar(escenaDestino, banco.cinematicaInicio);
        return true;
    }

    void Iniciar(string escenaDestino, VideoClip clip)
    {
        destino = escenaDestino;
        tInicio = Time.unscaledTime;
        AudioManager.PausarMusica();

        // Canvas negro encima de todo
        canvasGO = new GameObject("CanvasCinematica");
        canvasGO.transform.SetParent(transform);
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 2000;
        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        var fondo = new GameObject("Fondo", typeof(RectTransform), typeof(Image));
        fondo.transform.SetParent(canvasGO.transform, false);
        fondo.GetComponent<Image>().color = Color.black;
        Estirar(fondo.GetComponent<RectTransform>());

        int w = Mathf.Max(16, (int)clip.width), h = Mathf.Max(16, (int)clip.height);
        rt = new RenderTexture(w, h, 0);
        var vGO = new GameObject("Video", typeof(RectTransform), typeof(RawImage), typeof(AspectRatioFitter));
        vGO.transform.SetParent(canvasGO.transform, false);
        var raw = vGO.GetComponent<RawImage>();
        raw.texture = rt;
        var fit = vGO.GetComponent<AspectRatioFitter>();
        fit.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
        fit.aspectRatio = (float)w / h;
        Estirar(vGO.GetComponent<RectTransform>());

        var tGO = new GameObject("Saltar", typeof(RectTransform), typeof(Text));
        tGO.transform.SetParent(canvasGO.transform, false);
        var t = tGO.GetComponent<Text>();
        t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.text = "ESPACIO / ESC para saltar";
        t.fontSize = 22;
        t.alignment = TextAnchor.LowerRight;
        t.color = new Color(1f, 1f, 1f, 0.45f);
        var rtt = tGO.GetComponent<RectTransform>();
        rtt.anchorMin = Vector2.zero; rtt.anchorMax = Vector2.one;
        rtt.offsetMin = new Vector2(30, 24); rtt.offsetMax = new Vector2(-30, -24);

        var audio = gameObject.AddComponent<AudioSource>();
        audio.playOnAwake = false;
        vp = gameObject.AddComponent<VideoPlayer>();
        vp.playOnAwake = false;
        vp.clip = clip;
        vp.isLooping = false;
        vp.renderMode = VideoRenderMode.RenderTexture;
        vp.targetTexture = rt;
        vp.audioOutputMode = VideoAudioOutputMode.AudioSource;
        vp.controlledAudioTrackCount = 1;
        vp.SetTargetAudioSource(0, audio);
        vp.prepareCompleted += v => v.Play();
        vp.loopPointReached += v => Terminar();
        vp.errorReceived += (v, m) => { Debug.LogWarning("[Cinematica] " + m); Terminar(); };
        vp.Prepare();
    }

    static void Estirar(RectTransform r)
    {
        r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one;
        r.offsetMin = Vector2.zero; r.offsetMax = Vector2.zero;
    }

    void Update()
    {
        if (terminada) return;
        // Si no arranca en 12 s, seguimos al nivel para no trabar al jugador
        if (vp != null && !vp.isPlaying && Time.unscaledTime - tInicio > 12f && vp.time <= 0.01) { Terminar(); return; }
        if (Time.unscaledTime - tInicio < 0.4f) return; // evita saltar con el clic de Jugar

        bool saltar = false;
#if ENABLE_INPUT_SYSTEM
        var k = Keyboard.current;
        if (k != null && (k.spaceKey.wasPressedThisFrame || k.escapeKey.wasPressedThisFrame || k.enterKey.wasPressedThisFrame))
            saltar = true;
#else
        if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Return))
            saltar = true;
#endif
        if (saltar) Terminar();
    }

    void Terminar()
    {
        if (terminada) return;
        terminada = true;
        if (vp != null) vp.Stop();
        // Dejar solo el fondo negro tapando el menu hasta que cargue el nivel
        foreach (var r in GetComponentsInChildren<RawImage>(true)) r.enabled = false;
        foreach (var tx in GetComponentsInChildren<Text>(true)) tx.enabled = false;
        AudioManager.ReanudarMusica();
        reproduciendo = false;
        if (GestorJuego.Instancia != null) GestorJuego.Instancia.CargarEscena(destino);
        else UnityEngine.SceneManagement.SceneManager.LoadScene(destino);
        // Sobrevive el cambio de escena y se quita cuando el nivel ya cargo
        transform.SetParent(null);
        DontDestroyOnLoad(gameObject);
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += AlCargarEscena;
        Destroy(gameObject, 15f); // seguro por si algo falla
    }

    void AlCargarEscena(UnityEngine.SceneManagement.Scene esc, UnityEngine.SceneManagement.LoadSceneMode modo)
    {
        if (esc.name != destino) return;
        UnityEngine.SceneManagement.SceneManager.sceneLoaded -= AlCargarEscena;
        Destroy(gameObject, 0.6f);
    }

    void OnDestroy()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded -= AlCargarEscena;
        reproduciendo = false;
        if (rt != null) { rt.Release(); Destroy(rt); }
    }
}
