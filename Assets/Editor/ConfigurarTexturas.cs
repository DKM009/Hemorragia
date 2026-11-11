using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class ConfigurarTexturas
{
    [MenuItem("Tools/Hemorragia/Texturas - Configurar")]
    public static void Configurar()
    {
        if (!Directory.Exists("Assets/Resources")) AssetDatabase.CreateFolder("Assets", "Resources");
        const string ruta = "Assets/Resources/BancoTexturas.asset";
        var b = AssetDatabase.LoadAssetAtPath<BancoTexturas>(ruta);
        if (b == null) { b = ScriptableObject.CreateInstance<BancoTexturas>(); AssetDatabase.CreateAsset(b, ruta); }

        var lista = new List<Texture2D>();
        foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/Texturas" }))
        {
            string p = AssetDatabase.GUIDToAssetPath(guid);
            string n = Path.GetFileNameWithoutExtension(p);
            if (n == "Textura_Industria" || n == "factorytileset") continue;   // hojas gigantes, no se usan
            var imp = AssetImporter.GetAtPath(p) as TextureImporter;
            if (imp == null) continue;
            bool cambio = false;
            var filtro = n.StartsWith("n2_") ? FilterMode.Bilinear : FilterMode.Point;
            if (imp.filterMode != filtro) { imp.filterMode = filtro; cambio = true; }
            if (n.StartsWith("n2_") && imp.maxTextureSize < 2048) { imp.maxTextureSize = 2048; cambio = true; }
            if (imp.mipmapEnabled) { imp.mipmapEnabled = false; cambio = true; }
            if (!imp.alphaIsTransparency) { imp.alphaIsTransparency = true; cambio = true; }
            if (imp.textureCompression != TextureImporterCompression.Uncompressed) { imp.textureCompression = TextureImporterCompression.Uncompressed; cambio = true; }
            if (cambio) imp.SaveAndReimport();
            var t = AssetDatabase.LoadAssetAtPath<Texture2D>(p);
            if (t != null) lista.Add(t);
        }
        b.texturas = lista.ToArray();
        EditorUtility.SetDirty(b);
        AssetDatabase.SaveAssets();
        Debug.Log("[Texturas] BancoTexturas listo con " + lista.Count + " texturas.");
        EditorUtility.DisplayDialog("Texturas", "Banco de texturas listo (" + lista.Count + ").", "OK");
    }
}
