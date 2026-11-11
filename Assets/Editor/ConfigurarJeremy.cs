using UnityEditor;
using UnityEngine;
using System.IO;

public static class ConfigurarJeremy
{
    [MenuItem("Tools/Hemorragia/Jeremy - Configurar")]
    public static void Configurar()
    {
        if (!Directory.Exists("Assets/Resources")) AssetDatabase.CreateFolder("Assets", "Resources");
        const string ruta = "Assets/Resources/BancoJeremy.asset";
        var b = AssetDatabase.LoadAssetAtPath<BancoJeremy>(ruta);
        if (b == null) { b = ScriptableObject.CreateInstance<BancoJeremy>(); AssetDatabase.CreateAsset(b, ruta); }

        b.idle    = Hoja("Idle");
        b.caminar = Hoja("Walk");
        b.correr  = Hoja("Run");
        b.salto   = Hoja("Jump");
        b.herido  = Hoja("Hurt");
        b.muerte  = Hoja("Dead");

        EditorUtility.SetDirty(b);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[Jeremy] BancoJeremy listo. Dale Play: Jeremy reemplaza al cubo en Nivel1 y Nivel2.");
        EditorUtility.DisplayDialog("Jeremy", "Listo. Dale Play: Jeremy reemplaza al cubo en los niveles.\n\nPara cambiar su tamano: Assets/Resources/BancoJeremy > Pixeles Por Unidad.", "OK");
    }

    static Texture2D Hoja(string nombre)
    {
        string p = "Assets/Jeremy_Images/" + nombre + ".png";
        var imp = AssetImporter.GetAtPath(p) as TextureImporter;
        if (imp != null)
        {
            bool cambio = false;
            if (imp.filterMode != FilterMode.Point) { imp.filterMode = FilterMode.Point; cambio = true; }
            if (imp.textureCompression != TextureImporterCompression.Uncompressed) { imp.textureCompression = TextureImporterCompression.Uncompressed; cambio = true; }
            if (cambio) imp.SaveAndReimport();
        }
        var t = AssetDatabase.LoadAssetAtPath<Texture2D>(p);
        if (t == null) Debug.LogWarning("[Jeremy] No encontre " + p);
        return t;
    }
}
