using System.Collections;
using UnityEngine;

/// <summary>
/// Gestor de audio de Hemorragia (singleton, persiste entre escenas).
/// Maneja musica ambiental con crossfade y efectos de sonido 2D.
///
/// Uso desde cualquier script:
///   AudioManager.ReproducirEfecto(miClip);
///   AudioManager.CambiarMusica(miClipAmbiente, fadeDuracion: 1.5f);
///   AudioManager.PausarMusica();
///
/// Fuentes de audio:
///   musica1 y musica2: crossfade entre pistas de ambiente.
///   efectos: pool de 8 AudioSource para sonidos one-shot.
/// </summary>
public class AudioManager : MonoBehaviour
{
    // ── Singleton ─────────────────────────────────────────────────────────────
    public static AudioManager Instancia { get; private set; }

    // ── Volumen global ────────────────────────────────────────────────────────
    [Header("Volumen")]
    [Range(0f, 1f)] public float volumenMusica  = 0.35f;
    [Range(0f, 1f)] public float volumenEfectos = 0.75f;

    // ── Pistas de musica ambiental ────────────────────────────────────────────
    [Header("Musica ambiental por escena")]
    [Tooltip("Musica del menu principal.")]
    public AudioClip musicaMenu;
    [Tooltip("Musica del Nivel 1 (patio/pasillo/bodega).")]
    public AudioClip musicaNivel1;
    [Tooltip("Musica del Nivel 2 (tuberias y calderas).")]
    public AudioClip musicaNivel2;
    [Tooltip("Musica del Nivel 3 (hornos y escoria).")]
    public AudioClip musicaNivel3;
    [Tooltip("Musica del Nivel 4 (Altar de Desechos — climax).")]
    public AudioClip musicaNivel4;
    [Tooltip("Musica cuando La Fosa persigue.")]
    public AudioClip musicaPersecucion;

    // ── Estado interno ────────────────────────────────────────────────────────
    AudioSource[] fuentesMusica;
    int           indiceActivo;
    AudioSource[] fuentesEfectos;
    Coroutine     rutinaCrossfade;

    const int TAM_POOL_EFECTOS = 8;

    // ─────────────────────────────────────────────────────────────────────────

    void Awake()
    {
        if (Instancia != null && Instancia != this)
        {
            Destroy(gameObject);
            return;
        }
        Instancia = this;
        DontDestroyOnLoad(gameObject);

        // Crear dos fuentes de musica para crossfade
        fuentesMusica    = new AudioSource[2];
        for (int i = 0; i < 2; i++)
        {
            fuentesMusica[i]        = gameObject.AddComponent<AudioSource>();
            fuentesMusica[i].loop   = true;
            fuentesMusica[i].volume = 0f;
        }

        // Pool de efectos
        fuentesEfectos = new AudioSource[TAM_POOL_EFECTOS];
        for (int i = 0; i < TAM_POOL_EFECTOS; i++)
        {
            fuentesEfectos[i]        = gameObject.AddComponent<AudioSource>();
            fuentesEfectos[i].loop   = false;
            fuentesEfectos[i].volume = volumenEfectos;
        }
    }

    // ── API publica ───────────────────────────────────────────────────────────

    /// <summary>Cambia la musica de fondo con crossfade suave.</summary>
    public static void CambiarMusica(AudioClip clip, float duracion = 1.5f)
    {
        if (Instancia == null || clip == null) return;
        Instancia.IniciarCrossfade(clip, duracion);
    }

    /// <summary>Reproduce un efecto de sonido 2D (one-shot).</summary>
    public static void ReproducirEfecto(AudioClip clip, float volumen = -1f)
    {
        if (Instancia == null || clip == null) return;
        var fuente = Instancia.ObtenerFuenteLibre();
        if (fuente == null) return;
        fuente.volume = volumen < 0f ? Instancia.volumenEfectos : volumen;
        fuente.PlayOneShot(clip);
    }

    public static void PausarMusica()
    {
        if (Instancia == null) return;
        foreach (var f in Instancia.fuentesMusica) f.Pause();
    }

    public static void ReanudarMusica()
    {
        if (Instancia == null) return;
        foreach (var f in Instancia.fuentesMusica) f.UnPause();
    }

    /// <summary>Elige la musica correcta segun el nombre de la escena activa.</summary>
    public void AdaptarMusicaAEscena(string nombreEscena)
    {
        AudioClip clip = nombreEscena switch
        {
            "MenuPrincipal" => musicaMenu,
            "Nivel1"        => musicaNivel1,
            "Nivel2"        => musicaNivel2,
            "Nivel3"        => musicaNivel3,
            "Nivel4"        => musicaNivel4,
            _               => musicaNivel1
        };
        CambiarMusica(clip);
    }

    // ── Logica interna ────────────────────────────────────────────────────────

    void IniciarCrossfade(AudioClip nuevoClip, float duracion)
    {
        if (rutinaCrossfade != null) StopCoroutine(rutinaCrossfade);
        rutinaCrossfade = StartCoroutine(RutinaCrossfade(nuevoClip, duracion));
    }

    IEnumerator RutinaCrossfade(AudioClip nuevoClip, float duracion)
    {
        int siguiente = 1 - indiceActivo;

        fuentesMusica[siguiente].clip   = nuevoClip;
        fuentesMusica[siguiente].volume = 0f;
        fuentesMusica[siguiente].Play();

        float t = 0f;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / duracion;
            fuentesMusica[indiceActivo].volume = Mathf.Lerp(volumenMusica, 0f, t);
            fuentesMusica[siguiente].volume    = Mathf.Lerp(0f, volumenMusica, t);
            yield return null;
        }

        fuentesMusica[indiceActivo].Stop();
        indiceActivo = siguiente;
    }

    AudioSource ObtenerFuenteLibre()
    {
        foreach (var f in fuentesEfectos)
            if (!f.isPlaying) return f;
        // Si todas ocupadas, usar la primera (sacrificamos el sonido mas viejo)
        return fuentesEfectos[0];
    }
}
