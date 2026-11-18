using UnityEditor;
using UnityEngine;

/// <summary>Llena Resources/BancoUI.asset con el arte de Bloodlines Dark UI. Corre solo al compilar.</summary>
[InitializeOnLoad]
public static class ConfigurarUI
{
    const string T = "Assets/Alebardium/Bloodlines UI/Textures/";
    const string Ruta = "Assets/Resources/BancoUI.asset";

    static ConfigurarUI() { EditorApplication.delayCall += Auto; }

    static void Auto()
    {
        var b = AssetDatabase.LoadAssetAtPath<BancoUI>(Ruta);
        if (b != null && b.btnNormal != null && b.barraLlena != null) return;
        if (AssetDatabase.LoadAssetAtPath<Sprite>(T + "Button/Button1/Status_Red_Default.png") == null) return;   // paquete aun no importado
        Configurar(false);
    }

    [MenuItem("Tools/Hemorragia/UI - Configurar")]
    public static void Configurar() { Configurar(true); }

    static void Configurar(bool avisar)
    {
        if (!AssetDatabase.IsValidFolder("Assets/Resources")) AssetDatabase.CreateFolder("Assets", "Resources");
        var b = AssetDatabase.LoadAssetAtPath<BancoUI>(Ruta);
        if (b == null) { b = ScriptableObject.CreateInstance<BancoUI>(); AssetDatabase.CreateAsset(b, Ruta); }
        b.btnNormal     = S("Button/Button1/Status_Red_Default.png");
        b.btnHover      = S("Button/Button1/Status_Red_Hover.png");
        b.btnPresionado = S("Button/Button1/Status_Pressed.png");
        b.btnApagado    = S("Button/Button1/Status_Disable.png");
        b.marco         = S("Frame/Frame_main_menu_red.png");
        b.barraVacia    = S("Progress_Bar/Rectangle/Progress_Bar_Rectangle_empty_v1.png");
        b.barraLlena    = S("Progress_Bar/Rectangle/Progress_Bar_Rectangle_full_v1.png");
        b.fuente        = AssetDatabase.LoadAssetAtPath<Font>("Assets/Alebardium/Bloodlines UI/Fonts/ManufacturingConsent/ManufacturingConsent-Regular.ttf");
        EditorUtility.SetDirty(b);
        AssetDatabase.SaveAssets();
        Debug.Log("[UI] BancoUI listo (botones, barra de sangre, fuente).");
        if (avisar) EditorUtility.DisplayDialog("UI", "Arte de interfaz listo.", "OK");
    }

    static Sprite S(string rel)
    {
        var s = AssetDatabase.LoadAssetAtPath<Sprite>(T + rel);
        if (s == null) Debug.LogWarning("[UI] no encontre " + rel);
        return s;
    }
}
