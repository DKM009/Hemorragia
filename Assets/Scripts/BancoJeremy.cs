using UnityEngine;

/// <summary>
/// Datos del personaje Jeremy (hojas de sprites de Assets/Jeremy_Images).
/// Se crea con Tools > Hemorragia > Jeremy - Configurar y vive en Resources/BancoJeremy.asset.
/// TAMANO: sube "pixelesPorUnidad" para hacerlo mas chico, bajalo para hacerlo mas grande
/// (100 = mide 0.7 unidades de alto; 70 = 1 unidad). El collider se ajusta solo.
/// </summary>
[CreateAssetMenu(fileName = "BancoJeremy", menuName = "Hemorragia/Banco de Jeremy")]
public class BancoJeremy : ScriptableObject
{
    [Header("Hojas (cada cuadro de 128x128 px)")]
    public Texture2D idle, caminar, correr, salto, herido, muerte;

    [Header("Tamano")]
    [Tooltip("100 = 0.7 unidades de alto. Menos = mas grande.")]
    public float pixelesPorUnidad = 100f;
    [Tooltip("Alto del personaje en pixeles dentro del cuadro (sin contar espacio vacio).")]
    public float altoPersonajePx = 70f;
    [Tooltip("Ancho del collider en unidades (a ppu 100).")]
    public float anchoCollider = 0.3f;
    [Tooltip("Distancia del centro del Player a sus pies, en unidades (0.3 = igual que el cubo anterior).")]
    public float distanciaPies = 0.3f;

    [Header("Velocidad de animacion (cuadros por segundo)")]
    public float fpsIdle = 6f;
    public float fpsCaminar = 10f;
    public float fpsCorrer = 13f;
    public float fpsHerido = 12f;
    public float fpsMuerte = 8f;
}
