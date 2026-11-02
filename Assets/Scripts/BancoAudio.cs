using UnityEngine;

/// <summary>
/// Banco de clips de Hemorragia. Se crea con Tools > Hemorragia > Audio - Configurar
/// y vive en Assets/Resources/BancoAudio.asset (se carga en runtime con Resources.Load).
/// Para cambiar que pista suena donde, arrastra otro clip aqui en el Inspector.
/// </summary>
[CreateAssetMenu(fileName = "BancoAudio", menuName = "Hemorragia/Banco de Audio")]
public class BancoAudio : ScriptableObject
{
    [Header("Musica")]
    public AudioClip menu;            // Ambiente_1
    public AudioClip nivel1Inicio;    // Ambiente_3 (cuartos 1 y 2, sin monstruos)
    public AudioClip nivel1Resto;     // Ambiente_2 (cuartos 3 y 4)
    public AudioClip nivel2;          // Ambiente_2

    [Header("Efectos")]
    public AudioClip latidos;         // Latidos_Corazon (sangre critica)
    public AudioClip puerta;          // Puerta (puertas y cambio de cuarto)
    public AudioClip maquina;         // Sonido_maquina (fondo industrial)

    [Header("Video")]
    public UnityEngine.Video.VideoClip cinematicaInicio; // Hemorragia1.2.mp4

    [Header("Volumenes (0-1)")]
    [Range(0f, 1f)] public float volLatidos = 0.8f;
    [Range(0f, 1f)] public float volPuerta  = 0.8f;
    [Range(0f, 1f)] public float volMaquina = 0.25f;
}
