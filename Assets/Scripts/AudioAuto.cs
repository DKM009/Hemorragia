using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

/// <summary>
/// Conecta el audio SIN tocar escenas: se crea solo al iniciar el juego.
/// - Asegura que exista el AudioManager (aunque arranques desde Nivel1/Nivel2).
/// - Musica: Menu, Nivel1 (cuartos 1-2 = Ambiente_3, 3-4 = Ambiente_2), Nivel2.
/// - Latidos cuando la sangre esta en estado Critico.
/// - Sonido de puerta al abrir una PuertaConLlave y al cambiar de cuarto.
/// - Sonido de maquina de fondo (Nivel2 y cuartos 3-4 del Nivel1).
/// </summary>
public class AudioAuto : MonoBehaviour
{
    static AudioAuto instancia;
    BancoAudio banco;
    AudioSource latidos, maquina;
    AudioClip musicaActual;
    string escenaActual = "";
    int cuartoPrevio = -1;
    float tBusca;
    GestorNivel1 n1; SistemaSangre sangre;
    readonly Dictionary<PuertaConLlave, bool> puertas = new Dictionary<PuertaConLlave, bool>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Iniciar()
    {
        if (instancia != null) return;
        var go = new GameObject("AudioAuto");
        DontDestroyOnLoad(go);
        instancia = go.AddComponent<AudioAuto>();
    }

    void Awake()
    {
        banco = Resources.Load<BancoAudio>("BancoAudio");
        latidos = Crear(true);
        maquina = Crear(true);
        SceneManager.sceneLoaded += (s, m) => { escenaActual = ""; cuartoPrevio = -1; puertas.Clear(); n1 = null; sangre = null; };
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

        if (Time.unscaledTime > tBusca)
        {
            tBusca = Time.unscaledTime + 0.5f;
            if (n1 == null && esc == "Nivel1") n1 = FindFirstObjectByType<GestorNivel1>();
            if (sangre == null) sangre = FindFirstObjectByType<SistemaSangre>();
            foreach (var p in FindObjectsByType<PuertaConLlave>(FindObjectsSortMode.None))
                if (!puertas.ContainsKey(p)) puertas[p] = p.Abierta;
        }

        // Cuarto actual (solo Nivel1)
        int cuarto = (esc == "Nivel1" && n1 != null) ? n1.cuartoActual : -1;
        if (esc == "Nivel1" && cuarto != cuartoPrevio)
        {
            if (cuartoPrevio >= 0) AudioManager.ReproducirEfecto(banco.puerta, banco.volPuerta);
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
        if (clip != null && clip != musicaActual && AudioManager.Instancia != null)
        {
            musicaActual = clip;
            AudioManager.CambiarMusica(clip, 2f);
        }
        escenaActual = esc;

        // Puertas
        var claves = new List<PuertaConLlave>(puertas.Keys);
        foreach (var p in claves)
        {
            if (p == null) { puertas.Remove(p); continue; }
            if (p.Abierta && !puertas[p]) AudioManager.ReproducirEfecto(banco.puerta, banco.volPuerta);
            puertas[p] = p.Abierta;
        }

        // Latidos
        bool critico = sangre != null && sangre.Estado == SistemaSangre.EstadoSangre.Critico
                       && !(GestorJuego.Instancia != null && GestorJuego.Instancia.EstaEnGameOver);
        Lazo(latidos, banco.latidos, critico && !pausa, banco.volLatidos);

        // Maquina
        bool hayMaquina = esc == "Nivel2" || (esc == "Nivel1" && cuarto >= 2);
        Lazo(maquina, banco.maquina, hayMaquina && !pausa, banco.volMaquina);
    }

    void Lazo(AudioSource a, AudioClip clip, bool activo, float vol)
    {
        if (clip == null) return;
        if (a.clip != clip) { a.clip = clip; }
        float obj = activo ? vol : 0f;
        a.volume = Mathf.MoveTowards(a.volume, obj, Time.unscaledDeltaTime * 1.5f);
        if (activo && !a.isPlaying) a.Play();
        else if (!activo && a.volume <= 0.001f && a.isPlaying) a.Stop();
    }
}
