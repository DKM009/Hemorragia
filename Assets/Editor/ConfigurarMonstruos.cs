using UnityEditor;
using UnityEngine;
using System.IO;

public static class ConfigurarMonstruos
{
    const string M = "Assets/Prefab/Monsters Creatures Fantasy/Sprites/";

    [MenuItem("Tools/Hemorragia/Monstruos - Configurar")]
    public static void Configurar()
    {
        if (!Directory.Exists("Assets/Resources")) AssetDatabase.CreateFolder("Assets", "Resources");
        const string ruta = "Assets/Resources/BancoMonstruos.asset";
        var b = AssetDatabase.LoadAssetAtPath<BancoMonstruos>(ruta);
        if (b == null) { b = ScriptableObject.CreateInstance<BancoMonstruos>(); AssetDatabase.CreateAsset(b, ruta); }

        b.esqueleto = new BancoMonstruos.DatosMonstruo
        {
            nombre = "Esqueleto", idle = T(M + "Skeleton/Idle.png"), mover = T(M + "Skeleton/Walk.png"),
            ataque = T(M + "Skeleton/Attack1.png"), pivotX = 0.55f, pixelesBajoPies = 49f,
            collider = new Vector2(0.5f, 1f)
        };
        b.goblin = new BancoMonstruos.DatosMonstruo
        {
            nombre = "Goblin", idle = T(M + "Goblin/Idle.png"), mover = T(M + "Goblin/Run.png"),
            ataque = T(M + "Goblin/Attack1.png"), pivotX = 0.5f, pixelesBajoPies = 49f,
            collider = new Vector2(0.45f, 0.75f), fpsMover = 11f
        };
        b.hongo = new BancoMonstruos.DatosMonstruo
        {
            nombre = "Hongo", idle = T(M + "Mushroom/Idle.png"), mover = T(M + "Mushroom/Run.png"),
            ataque = T(M + "Mushroom/Attack1.png"), pivotX = 0.5f, pixelesBajoPies = 49f,
            collider = new Vector2(0.5f, 0.78f), fpsMover = 10f
        };
        b.jefeFuego = T("Assets/Prefab/enemigodelava/PNG/Undead_warrior/Character6_face1.png");

        EditorUtility.SetDirty(b);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[Monstruos] BancoMonstruos listo.");
        EditorUtility.DisplayDialog("Monstruos", "Listo. Dale Play: los Purgadores ahora usan los monstruos y el de lava es el esqueleto de fuego.", "OK");
    }

    static Texture2D T(string p)
    {
        var imp = AssetImporter.GetAtPath(p) as TextureImporter;
        if (imp != null)
        {
            bool c = false;
            if (imp.filterMode != FilterMode.Point) { imp.filterMode = FilterMode.Point; c = true; }
            if (imp.textureCompression != TextureImporterCompression.Uncompressed) { imp.textureCompression = TextureImporterCompression.Uncompressed; c = true; }
            if (c) imp.SaveAndReimport();
        }
        var t = AssetDatabase.LoadAssetAtPath<Texture2D>(p);
        if (t == null) Debug.LogWarning("[Monstruos] No encontre " + p);
        return t;
    }
}
