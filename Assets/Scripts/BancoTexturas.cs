using UnityEngine;

/// <summary>
/// Texturas de Pedro (props, puertas, maquinas, carne...) y los tiles de piso/pared.
/// Se crea con Tools > Hemorragia > Texturas - Configurar y vive en Assets/Resources/BancoTexturas.asset.
/// </summary>
[CreateAssetMenu(fileName = "BancoTexturas", menuName = "Hemorragia/Banco de Texturas")]
public class BancoTexturas : ScriptableObject
{
    public Texture2D[] texturas;

    public Texture2D Buscar(string nombre)
    {
        if (texturas == null) return null;
        foreach (var t in texturas) if (t != null && t.name == nombre) return t;
        return null;
    }
}
