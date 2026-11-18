using UnityEditor;

/// <summary>Pone el nombre del juego (ventana y ejecutable) como "Wake Up". Se corre solo una vez.</summary>
[InitializeOnLoad]
public static class NombreJuego
{
    static NombreJuego()
    {
        EditorApplication.delayCall += () =>
        {
            if (PlayerSettings.productName != "Wake Up") PlayerSettings.productName = "Wake Up";
        };
    }
}
