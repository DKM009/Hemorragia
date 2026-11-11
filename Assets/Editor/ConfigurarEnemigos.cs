using UnityEditor;
using UnityEngine;
using System.IO;

public static class ConfigurarEnemigos
{
    [MenuItem("Tools/Hemorragia/Enemigos - Configurar")]
    public static void Configurar()
    {
        if (!Directory.Exists("Assets/Resources")) AssetDatabase.CreateFolder("Assets", "Resources");
        const string ruta = "Assets/Resources/BancoEnemigos.asset";
        var b = AssetDatabase.LoadAssetAtPath<BancoEnemigos>(ruta);
        if (b == null) { b = ScriptableObject.CreateInstance<BancoEnemigos>(); AssetDatabase.CreateAsset(b, ruta); }

        b.reptanteIdle    = Preparar("Reptante_Idle");
        b.reptanteCaminar = Preparar("Reptante_Caminar");
        b.reptanteAtaque  = Preparar("Reptante_Ataque");

        b.hazmatIdle    = Preparar("Hazmat_Idle");
        b.hazmatCaminar = Preparar("Hazmat_Caminar");
        b.hazmatAtaque  = Preparar("Hazmat_Ataque");
        b.rojaIdle      = Preparar("MasaRoja_Idle");
        b.rojaCaminar   = Preparar("MasaRoja_Caminar");
        b.rojaAtaque    = Preparar("MasaRoja_Ataque");

        EditorUtility.SetDirty(b);
        AssetDatabase.SaveAssets();
        Debug.Log("[Enemigos] BancoEnemigos listo en " + ruta);
        EditorUtility.DisplayDialog("Enemigos", "Banco de enemigos configurado.", "OK");
    }

    static Texture2D Preparar(string nombre)
    {
        string p = "Assets/Enemigos/" + nombre + ".png";
        var imp = AssetImporter.GetAtPath(p) as TextureImporter;
        if (imp == null) { Debug.LogWarning("[Enemigos] No encontre " + p); return null; }
        imp.textureType = TextureImporterType.Default;
        imp.filterMode = FilterMode.Point;
        imp.textureCompression = TextureImporterCompression.Uncompressed;
        imp.mipmapEnabled = false;
        imp.alphaIsTransparency = true;
        imp.isReadable = false;
        imp.maxTextureSize = 4096;
        imp.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Texture2D>(p);
    }
}
