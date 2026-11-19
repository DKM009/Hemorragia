using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

// Pone el logo "WAKE UP" como icono de la aplicacion y como pantalla de inicio (en lugar del logo de Unity).
// Menu: Tools > Hemorragia > Logo - Configurar (icono y splash)
public static class ConfigurarLogo
{
    const string RutaIcono  = "Assets/Logo/Icono_WakeUp.png";
    const string RutaSplash = "Assets/Logo/Splash_WakeUp.png";

    [MenuItem("Tools/Hemorragia/Logo - Configurar (icono y splash)")]
    public static void Configurar()
    {
        AssetDatabase.ImportAsset(RutaIcono, ImportAssetOptions.ForceUpdate);
        AssetDatabase.ImportAsset(RutaSplash, ImportAssetOptions.ForceUpdate);

        var ii = AssetImporter.GetAtPath(RutaIcono) as TextureImporter;
        if (ii != null)
        {
            ii.textureType = TextureImporterType.Default;
            ii.alphaIsTransparency = true;
            ii.mipmapEnabled = false;
            ii.npotScale = TextureImporterNPOTScale.None;
            ii.textureCompression = TextureImporterCompression.Uncompressed;
            ii.maxTextureSize = 1024;
            ii.SaveAndReimport();
        }
        var si = AssetImporter.GetAtPath(RutaSplash) as TextureImporter;
        if (si != null)
        {
            si.textureType = TextureImporterType.Sprite;
            si.spriteImportMode = SpriteImportMode.Single;
            si.alphaIsTransparency = true;
            si.mipmapEnabled = false;
            si.textureCompression = TextureImporterCompression.Uncompressed;
            si.SaveAndReimport();
        }

        var icono = AssetDatabase.LoadAssetAtPath<Texture2D>(RutaIcono);
        var splash = AssetDatabase.LoadAssetAtPath<Sprite>(RutaSplash);
        string aviso = "";

        if (icono != null)
        {
            // Icono por defecto (se usa para el .exe y la ventana)
            PlayerSettings.SetIcons(NamedBuildTarget.Unknown, new[] { icono }, IconKind.Application);
        }
        else aviso += "No encontre el icono. ";

        try
        {
            if (splash != null)
            {
                PlayerSettings.SplashScreen.show = true;
                PlayerSettings.SplashScreen.showUnityLogo = false;
                PlayerSettings.SplashScreen.drawMode = PlayerSettings.SplashScreen.DrawMode.AllSequential;
                PlayerSettings.SplashScreen.backgroundColor = Color.black;
                PlayerSettings.SplashScreen.logos = new[] { PlayerSettings.SplashScreenLogo.Create(2.5f, splash) };
            }
            else aviso += "No encontre el logo del splash. ";
        }
        catch (System.Exception e) { aviso += "El splash no se pudo cambiar (" + e.Message + "). "; }

        AssetDatabase.SaveAssets();
        Debug.Log("[Logo] Icono y splash configurados. " + aviso);
        EditorUtility.DisplayDialog("Logo", string.IsNullOrEmpty(aviso)
            ? "Listo: el icono y la pantalla de inicio ahora son WAKE UP.\nVuelve a hacer Build para verlo en el .exe."
            : "Quedo a medias: " + aviso, "OK");
    }
}
