using UnityEditor;
using UnityEngine;
using System.IO;

public static class ConfigurarJefe
{
    [MenuItem("Tools/Hemorragia/Jefe - Configurar")]
    public static void Configurar()
    {
        if (!Directory.Exists("Assets/Resources")) AssetDatabase.CreateFolder("Assets", "Resources");
        const string ruta = "Assets/Resources/BancoJefe.asset";
        var b = AssetDatabase.LoadAssetAtPath<BancoJefe>(ruta);
        if (b == null) { b = ScriptableObject.CreateInstance<BancoJefe>(); AssetDatabase.CreateAsset(b, ruta); }

        b.arena    = Preparar("Arena_Fondo.jpg");
        b.cerrado  = Preparar("Masa_Cerrada.png");
        b.abierto  = Preparar("Masa_Abierta.png");
        b.brazos   = Preparar("Masa_Brazos.png");
        b.enojada  = Preparar("Masa_Enojada.png");
        b.muerte   = Preparar("Masa_Muerte.png");
        b.chatarra = Preparar("Chatarra1.png");
        b.chatarra2 = Preparar("Chatarra2.png");
        b.chatarra3 = Preparar("Chatarra3.png");
        b.salpicadura = Preparar("Salpicadura.png");
        b.manos    = Preparar("ManosPiso.png");

        EditorUtility.SetDirty(b);
        AssetDatabase.SaveAssets();
        Debug.Log("[Jefe] BancoJefe listo en " + ruta);
        EditorUtility.DisplayDialog("Jefe", "Banco del jefe configurado.", "OK");
    }

    static Texture2D Preparar(string archivo)
    {
        string p = "Assets/Jefe/" + archivo;
        var imp = AssetImporter.GetAtPath(p) as TextureImporter;
        if (imp == null) { Debug.LogWarning("[Jefe] No encontre " + p); return null; }
        imp.textureType = TextureImporterType.Default;
        imp.filterMode = FilterMode.Bilinear;
        imp.alphaIsTransparency = true;
        imp.mipmapEnabled = false;
        imp.maxTextureSize = 4096;
        imp.textureCompression = TextureImporterCompression.Uncompressed;
        imp.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Texture2D>(p);
    }
}
