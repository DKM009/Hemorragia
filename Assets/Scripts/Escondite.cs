using UnityEngine;

/// <summary>
/// Escondite interactivo de Hemorragia.
/// Cuando el jugador entra al trigger:
///   - Se oculta visualmente (reduce alpha del sprite).
///   - Notifica a La Fosa que el jugador esta escondido.
///   - El ruido decae mas rapido.
/// Presionar E / F sale del escondite.
///
/// Setup: Agrega este script y un BoxCollider2D (isTrigger=true) al prop de escondite
/// (armario, tuberia ancha, caja grande, etc.).
/// </summary>
public class Escondite : MonoBehaviour
{
    [Header("Visual")]
    [Tooltip("Alpha del jugador cuando esta escondido (0 = invisible).")]
    [Range(0f, 1f)] public float alphaEscondido = 0.25f;
    [Tooltip("Velocidad de fade al entrar/salir del escondite.")]
    public float velocidadFade = 8f;

    [Header("Indicador")]
    public GameObject indicadorUI;

    bool  jugadorDentro;
    bool  jugadorEscondido;
    SpriteRenderer spriteJugador;
    LaFosaIA       laFosa;

    void Start()
    {
        laFosa = FindFirstObjectByType<LaFosaIA>();
        if (indicadorUI != null) indicadorUI.SetActive(false);
    }

    void Update()
    {
        if (!jugadorDentro) return;

        if (PulsarInteraccion())
        {
            if (!jugadorEscondido) Esconder();
            else                   Salir();
        }
    }

    void LateUpdate()
    {
        if (spriteJugador == null) return;

        float objetivo = jugadorEscondido ? alphaEscondido : 1f;
        Color c = spriteJugador.color;
        c.a = Mathf.Lerp(c.a, objetivo, Time.deltaTime * velocidadFade);
        spriteJugador.color = c;
    }

    void Esconder()
    {
        jugadorEscondido = true;
        laFosa?.JugadorSeEscondio();
        if (indicadorUI != null) indicadorUI.SetActive(false);
    }

    void Salir()
    {
        jugadorEscondido = false;
        laFosa?.JugadorSalioEscondite();
        if (indicadorUI != null) indicadorUI.SetActive(true);
    }

    void OnTriggerEnter2D(Collider2D otro)
    {
        if (!otro.CompareTag("Player")) return;
        jugadorDentro = true;
        spriteJugador = otro.GetComponentInChildren<SpriteRenderer>();
        if (indicadorUI != null) indicadorUI.SetActive(true);
    }

    void OnTriggerExit2D(Collider2D otro)
    {
        if (!otro.CompareTag("Player")) return;
        jugadorDentro    = false;
        jugadorEscondido = false;
        laFosa?.JugadorSalioEscondite();
        if (indicadorUI != null) indicadorUI.SetActive(false);
        // Restaurar alpha
        if (spriteJugador != null)
        {
            Color c = spriteJugador.color; c.a = 1f; spriteJugador.color = c;
        }
    }

    bool PulsarInteraccion()
    {
#if ENABLE_INPUT_SYSTEM
        var k = UnityEngine.InputSystem.Keyboard.current;
        return k != null && (k.eKey.wasPressedThisFrame || k.fKey.wasPressedThisFrame);
#else
        return Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.F);
#endif
    }
}
