using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Mensajes en pantalla para Hemorragia. No necesita configuracion: se crea solo
/// la primera vez que alguien lo usa y sobrevive entre escenas.
///   UIMensaje.Mostrar("texto", 4f)  -> mensaje central inferior que se desvanece.
///   UIMensaje.Pista("[E] Leer")     -> pista corta; llamala cada frame mientras aplique.
///   UIMensaje.Inventario("...")     -> texto fijo arriba a la derecha.
/// </summary>
public class UIMensaje : MonoBehaviour
{
    static UIMensaje _inst;

    Text txtMensaje, txtPista, txtInventario;
    float tMensaje, tPista;

    public static void Mostrar(string texto, float segundos = 4f)
    {
        Asegurar();
        _inst.txtMensaje.text = texto;
        _inst.tMensaje = segundos;
    }

    public static void Pista(string texto)
    {
        Asegurar();
        _inst.txtPista.text = texto;
        _inst.tPista = 0.15f;
    }

    public static void Inventario(string texto)
    {
        Asegurar();
        _inst.txtInventario.text = texto;
    }

    static void Asegurar()
    {
        if (_inst != null) return;
        var go = new GameObject("UIMensaje");
        DontDestroyOnLoad(go);
        _inst = go.AddComponent<UIMensaje>();
        _inst.Construir();
    }

    void Construir()
    {
        var canvasGO = new GameObject("Canvas", typeof(RectTransform));
        canvasGO.transform.SetParent(transform, false);
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 200;
        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        var rt = canvasGO.transform;
        txtMensaje = CrearTexto(rt, "Mensaje", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
            new Vector2(0f, 190f), new Vector2(1500f, 220f), 36, TextAnchor.LowerCenter, Color.white);
        txtPista = CrearTexto(rt, "Pista", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
            new Vector2(0f, 120f), new Vector2(1200f, 60f), 30, TextAnchor.LowerCenter,
            new Color(1f, 0.85f, 0.4f));
        txtInventario = CrearTexto(rt, "Inventario", new Vector2(1f, 1f), new Vector2(1f, 1f),
            new Vector2(-30f, -30f), new Vector2(700f, 140f), 28, TextAnchor.UpperRight,
            new Color(0.9f, 0.9f, 0.9f));
    }

    Text CrearTexto(Transform padre, string nombre, Vector2 anclaje, Vector2 pivote,
                    Vector2 pos, Vector2 tam, int fuente, TextAnchor alineacion, Color color)
    {
        var go = new GameObject(nombre, typeof(RectTransform));
        go.transform.SetParent(padre, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = anclaje;
        rt.anchorMax = anclaje;
        rt.pivot = pivote;
        rt.anchoredPosition = pos;
        rt.sizeDelta = tam;

        var t = go.AddComponent<Text>();
        t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.fontSize = fuente;
        t.alignment = alineacion;
        t.color = color;
        t.horizontalOverflow = HorizontalWrapMode.Wrap;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        t.raycastTarget = false;
        t.text = "";

        var o = go.AddComponent<Outline>();
        o.effectColor = new Color(0f, 0f, 0f, 0.9f);
        o.effectDistance = new Vector2(2f, -2f);
        return t;
    }

    void Update()
    {
        if (tMensaje > 0f)
        {
            tMensaje -= Time.unscaledDeltaTime;
            SetAlpha(txtMensaje, Mathf.Clamp01(tMensaje / 0.6f));
        }
        else if (txtMensaje.text.Length > 0)
        {
            txtMensaje.text = "";
        }

        if (tPista > 0f)
        {
            tPista -= Time.unscaledDeltaTime;
        }
        else if (txtPista.text.Length > 0)
        {
            txtPista.text = "";
        }
    }

    static void SetAlpha(Text t, float a)
    {
        var c = t.color;
        c.a = a;
        t.color = c;
    }
}
