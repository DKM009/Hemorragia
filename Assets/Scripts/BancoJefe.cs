using UnityEngine;

/// <summary>
/// Arte del jefe final (La Masa) y su arena. Se crea con Tools > Hemorragia > Jefe - Configurar
/// y vive en Assets/Resources/BancoJefe.asset.
/// </summary>
[CreateAssetMenu(fileName = "BancoJefe", menuName = "Hemorragia/Banco del Jefe")]
public class BancoJefe : ScriptableObject
{
    public Texture2D arena;      // Arena_Fondo.jpg  (sala de calderas, sin monstruo)
    public Texture2D cerrado;    // Jefe_Cerrado.png (boca cerrada)
    public Texture2D abierto;    // Jefe_Abierto.png (boca abierta con fuego)
    public Texture2D chatarra;   // Chatarra.png     (proyectil al rojo vivo)
    public Texture2D manos;      // ManosPiso.png    (pila de manos que sale del piso)
    public Texture2D brazos;     // Masa_Brazos.png  (la masa enojada con brazos levantados)
    public Texture2D enojada;    // Masa_Enojada.png (craneo rugiendo)
    public Texture2D muerte;     // Masa_Muerte.png  (craneo derrumbado)
    public Texture2D chatarra2;  // Chatarra2.png
    public Texture2D chatarra3;  // Chatarra3.png
    public Texture2D salpicadura;// Salpicadura.png  (impacto)
}
