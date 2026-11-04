using UnityEditor;
using UnityEngine;
using System.IO;

public static class ConfigurarAudio
{
    [MenuItem("Tools/Hemorragia/Audio - Configurar")]
    public static void Configurar()
    {
        if (!Directory.Exists("Assets/Resources")) AssetDatabase.CreateFolder("Assets", "Resources");
        const string ruta = "Assets/Resources/BancoAudio.asset";
        var b = AssetDatabase.LoadAssetAtPath<BancoAudio>(ruta);
        if (b == null) { b = ScriptableObject.CreateInstance<BancoAudio>(); AssetDatabase.CreateAsset(b, ruta); }

        b.menu         = Cargar("Ambiente_1");
        b.nivel1Inicio = Cargar("Ambiente_3");
        b.nivel1Resto  = Cargar("Ambiente_2");
        b.nivel2       = Cargar("Ambiente_2");
        b.latidos      = Cargar("Latidos_Corazon");
        b.puerta       = Cargar("Puerta");
        b.maquina      = Cargar("Sonido_maquina");

        b.cinematicaInicio = AssetDatabase.LoadAssetAtPath<UnityEngine.Video.VideoClip>("Assets/Cinematica/Hemorragia1.2.mp4");
        if (b.cinematicaInicio == null) Debug.LogWarning("[Audio] No encontre Assets/Cinematica/Hemorragia1.2.mp4 como VideoClip");

        EditorUtility.SetDirty(b);
        AssetDatabase.SaveAssets();
        Debug.Log("[Audio] BancoAudio listo en " + ruta);
        EditorUtility.DisplayDialog("Audio", "Banco de audio y cinematica configurados.\nYa puedes dar Play.", "OK");
    }

    static AudioClip Cargar(string n)
    {
        var c = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audios/" + n + ".mp3");
        if (c == null) Debug.LogWarning("[Audio] No encontre Assets/Audios/" + n + ".mp3");
        return c;
    }
}
