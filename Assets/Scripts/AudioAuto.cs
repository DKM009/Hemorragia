using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

/// <summary>
/// Conecta el audio SIN tocar escenas: se crea solo al iniciar el juego.
/// - Musica: Menu, Nivel1 (cuartos 1-2 = Ambiente_3, 3-4 = Ambiente_2), Nivel2.
/// - Latidos con sangre Critica, puertas, maquina de fondo.
/// - Pasos / Correr segun el movimiento de Jeremy.
/// - Lava: ambiente segun la cercania + sonido al caer.
/// - Grito al morir, rugido cuando un enemigo te detecta, papel al recoger/leer, golpe de lata.
/// </summary>
public class AudioAuto : MonoBehaviour
{
    static AudioAuto instancia;
    public static BancoAudio Banco { get; private set; }

    BancoAudio banco;
    AudioSource latidos, maquina, pasos, correr, lava;
    AudioClip musicaActual;
    string escenaActual = "";
    int cuartoPrevio = -1;
    float tBusca, tRugido, tEnem;
    bool goPrev, enLavaPrev;
    GestorNivel1 n1; SistemaSangre sangre; MovimientoJugador mov;
    readonly Dictionary<PuertaConLlave, bool> puertas = new Dictionary<PuertaConLlave, bool>();
    readonly Dictionary<PurgadorIA, PurgadorIA.Estado> estados = new Dictionary<PurgadorIA, PurgadorIA.Estado>();
    readonly List<Collider2D> lavas = new List<Collider2D>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Iniciar()
    {
        if (instancia != null) return;
        var go = new GameObject("AudioAuto");
        DontDestroyOnLoad(go);
        instancia = go.AddComponent<AudioAuto>();
    }

    // ── Efectos puntuales (se llaman desde otros scripts) ────────────────────
    static void Sfx(AudioClip c, float v)
    {
        if (c != null && AudioManager.Instancia != null) AudioManager.ReproducirEfecto(c, v);
    }
    public static void Lata()  { if (Banco != null) Sfx(Banco.latas, Banco.volLatas); }
    public static void Golpe() { if (Banco != null) Sfx(Banco.latas, Banco.volLatas); }
    public static void Papel(bool nota)
    {
        if (Banco == null) return;
        Sfx(nota ? Banco.papel1 : Banco.papel2, Banco.volPapel);   // cartas/notas: Papel_1; objetos recogidos: Papel_2
    }
    public static void Rugido(Vector2 donde, bool fuerte)
    {
        if (Banco == null) return;
        var j = JugadorRef.Transform;
        float d = j != null ? Vector2.Distance(j.position, donde) : 0f;
        float v = Banco.volRugido * Mathf.Clamp01(1.25f - d / 22f);
        if (v <= 0.02f) return;
        Sfx(Random.value < 0.5f && !fuerte ? Banco.rugido1 : Banco.rugido2, v);
    }

    void Awake()
    {
        banco = Resources.Load<BancoAudio>("BancoAudio");
        Banco = banco;
        latidos = Crear(true);
        maquina = Crear(true);
        pasos   = Crear(true);
        correr  = Crear(true);
        lava    = Crear(true);
        SceneManager.sceneLoaded += (s, m) =>
        {
            escenaActual = ""; cuartoPrevio = -1; puertas.Clear(); estados.Clear(); lavas.Clear();
            n1 = null; sangre = null; mov = null; goPrev = false; enLavaPrev = false;
        };
    }

    AudioSource Crear(bool loop)
    {
        var a = gameObject.AddComponent<AudioSource>();
        a.loop = loop; a.playOnAwake = false; a.volume = 0f;
        return a;
    }

    void Update()
    {
        if (banco == null) return;
        if (AudioManager.Instancia == null)
            new GameObject("AudioManager").AddComponent<AudioManager>();

        bool pausa = Time.timeScale == 0f;
        string esc = SceneManager.GetActiveScene().name;
        bool gameOver = GestorJuego.Instancia != null && GestorJuego.Instancia.EstaEnGameOver;

        if (Time.unscaledTime > tBusca)
        {
            tBusca = Time.unscaledTime + 0.5f;
            if (n1 == null && esc == "Nivel1") n1 = FindFirstObjectByType<GestorNivel1>();
            if (sangre == null) sangre = FindFirstObjectByType<SistemaSangre>();
            if (mov == null) mov = FindFirstObjectByType<MovimientoJugador>();
            foreach (var p in FindObjectsByType<PuertaConLlave>(FindObjectsSortMode.None))
                if (!puertas.ContainsKey(p)) puertas[p] = p.Abierta;
            if (lavas.Count == 0)
                foreach (var z in FindObjectsByType<ZonaLava>(FindObjectsSortMode.None))
                {
                    var c = z.GetComponent<Collider2D>();
                    if (c != null) lavas.Add(c);
                }
        }

        // Cuarto actual (solo Nivel1)
        int cuarto = (esc == "Nivel1" && n1 != null) ? n1.cuartoActual : -1;
        if (esc == "Nivel1" && cuarto != cuartoPrevio)
        {
            if (cuartoPrevio >= 0) Sfx(banco.puerta, banco.volPuerta);
            cuartoPrevio = cuarto;
        }

        // Musica
        AudioClip clip = null;
        switch (esc)
        {
            case "MenuPrincipal": case "EscenaFinal": clip = banco.menu; break;
            case "Nivel1": clip = (cuarto >= 2) ? banco.nivel1Resto : banco.nivel1Inicio; break;
            case "Nivel2": clip = banco.nivel2; break;
        }
        bool jefeActivo = esc == "Nivel2" && ArenaJefe.Instancia != null && banco.jefe != null;
        if (jefeActivo) clip = banco.jefe;
        if (clip != null && clip != musicaActual && AudioManager.Instancia != null)
        {
            musicaActual = clip;
            AudioManager.escalaProxima = (clip == banco.nivel1Resto) ? 2.6f : (clip == banco.jefe ? 1.6f : 1f);
            AudioManager.CambiarMusica(clip, 2f);
        }
        escenaActual = esc;

        // Puertas
        var claves = new List<PuertaConLlave>(puertas.Keys);
        foreach (var p in claves)
        {
            if (p == null) { puertas.Remove(p); continue; }
            if (p.Abierta && !puertas[p]) Sfx(banco.puerta, banco.volPuerta);
            puertas[p] = p.Abierta;
        }

        // Latidos
        bool critico = sangre != null && sangre.Estado == SistemaSangre.EstadoSangre.Critico && !gameOver;
        Lazo(latidos, banco.latidos, critico && !pausa, banco.volLatidos);

        // Maquina
        bool hayMaquina = false;
        Lazo(maquina, banco.maquina, hayMaquina && !pausa, banco.volMaquina);

        // Pasos / correr
        bool camina = mov != null && mov.enabled && mov.EstaEnSuelo && Mathf.Abs(mov.Horizontal) > 0.01f
                      && !pausa && !gameOver && !Escondite.JugadorOculto;
        bool corre = camina && mov.EstaCorriendo;
        Lazo(pasos,  banco.pasos,  camina && !corre, banco.volPasos,  9f);
        Lazo(correr, banco.correr, corre,            banco.volCorrer, 9f);

        // Lava: ambiente por cercania + sonido al caer
        float dLava = 999f;
        var jt = JugadorRef.Transform;
        if (jt != null)
            foreach (var c in lavas)
            {
                if (c == null) continue;
                float d = Vector2.Distance(c.ClosestPoint(jt.position), jt.position);
                if (d < dLava) dLava = d;
            }
        float volLava = banco.volLava * Mathf.Clamp01(1f - dLava / 14f);
        Lazo(lava, banco.lava, dLava < 14f && !pausa, volLava, 2f);
        bool enLava = dLava < 0.05f;
        if (enLava && !enLavaPrev) Sfx(banco.lavaQuemando, banco.volLavaQuemando);
        enLavaPrev = enLava;

        // Grito al morir
        if (gameOver && !goPrev) Sfx(banco.grito, banco.volGrito);
        goPrev = gameOver;

        // Rugido cuando un enemigo pasa a Perseguir
        if (Time.unscaledTime > tEnem)
        {
            tEnem = Time.unscaledTime + 0.2f;
            foreach (var p in FindObjectsByType<PurgadorIA>(FindObjectsSortMode.None))
            {
                if (!p.isActiveAndEnabled) continue;
                var actual = p.EstadoActual;
                PurgadorIA.Estado previo;
                bool conocido = estados.TryGetValue(p, out previo);
                if (conocido && previo != PurgadorIA.Estado.Perseguir && actual == PurgadorIA.Estado.Perseguir
                    && Time.unscaledTime > tRugido && !pausa)
                {
                    tRugido = Time.unscaledTime + 3f;
                    Rugido(p.transform.position, false);
                }
                estados[p] = actual;
            }
        }
    }

    void Lazo(AudioSource a, AudioClip clip, bool activo, float vol, float velocidad = 1.5f)
    {
        if (clip == null) return;
        if (a.clip != clip) { a.clip = clip; }
        float obj = activo ? vol : 0f;
        a.volume = Mathf.MoveTowards(a.volume, obj, Time.unscaledDeltaTime * velocidad);
        if (activo && !a.isPlaying) a.Play();
        else if (!activo && a.volume <= 0.001f && a.isPlaying) a.Stop();
    }
}
