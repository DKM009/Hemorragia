using UnityEngine;

/// <summary>
/// Banco de arte de los enemigos nuevos (Reptante, enemigos pequenos, La Masa).
/// Se crea con Tools > Hemorragia > Enemigos - Configurar y vive en Assets/Resources/BancoEnemigos.asset.
/// Cada hoja es una tira horizontal de cuadros de 256x256 px.
/// </summary>
[CreateAssetMenu(fileName = "BancoEnemigos", menuName = "Hemorragia/Banco de Enemigos")]
public class BancoEnemigos : ScriptableObject
{
    [Header("Reptante sangrante")]
    public Texture2D reptanteIdle;      // 4 cuadros
    public Texture2D reptanteCaminar;   // 8 cuadros
    public Texture2D reptanteAtaque;    // 4 cuadros
    [Header("Hazmat verde (enemigo comun)")]
    public Texture2D hazmatIdle;        // 4 cuadros
    public Texture2D hazmatCaminar;     // 8 cuadros
    public Texture2D hazmatAtaque;      // 4 cuadros

    [Header("Masa roja de ojos (segunda oleada)")]
    public Texture2D rojaIdle;
    public Texture2D rojaCaminar;
    public Texture2D rojaAtaque;

    [Header("Comun")]
    public int   celda = 256;
    public float ppu = 100f;            // 256 px = 2.56 unidades
    public float pixelesBajoPies = 4f;
}
