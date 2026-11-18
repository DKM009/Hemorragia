using UnityEngine;

/// <summary>
/// Arte de interfaz (paquete Bloodlines Dark UI). Vive en Resources/BancoUI.asset
/// y se llena solo desde Assets/Alebardium (ver Editor/ConfigurarUI.cs).
/// </summary>
public class BancoUI : ScriptableObject
{
    public Sprite btnNormal, btnHover, btnPresionado, btnApagado;
    public Sprite marco;
    public Sprite barraVacia, barraLlena;
    public Font   fuente;

    static BancoUI cache; static bool buscado;
    public static BancoUI Actual
    {
        get
        {
            if (!buscado) { cache = Resources.Load<BancoUI>("BancoUI"); buscado = true; }
            return cache != null && cache.btnNormal != null ? cache : null;
        }
    }
}
