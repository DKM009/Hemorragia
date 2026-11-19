using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

// Ventana para ver TODAS las texturas/sprites del proyecto (Assets/). Menu: Tools > Hemorragia > Ver texturas
public class VisorTexturas : EditorWindow
{
    class Item { public string ruta, nombre, carpeta; public Texture2D tex; }
    List<Item> items = new List<Item>();
    string[] carpetas = new string[0];
    int carpetaSel = 0;
    string filtro = "";
    float tam = 128f;
    Vector2 scroll;

    [MenuItem("Tools/Hemorragia/Ver texturas")]
    public static void Abrir()
    {
        var w = GetWindow<VisorTexturas>("Texturas");
        w.minSize = new Vector2(420, 300);
        w.Recargar();
    }

    void OnEnable() { Recargar(); }

    void Recargar()
    {
        items.Clear();
        foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets" }))
        {
            string p = AssetDatabase.GUIDToAssetPath(guid);
            var t = AssetDatabase.LoadAssetAtPath<Texture2D>(p);
            if (t == null) continue;
            items.Add(new Item { ruta = p, nombre = Path.GetFileNameWithoutExtension(p), carpeta = Path.GetDirectoryName(p).Replace('\\', '/'), tex = t });
        }
        items = items.OrderBy(i => i.carpeta).ThenBy(i => i.nombre).ToList();
        var lista = new List<string> { "(Todas)" };
        lista.AddRange(items.Select(i => i.carpeta).Distinct());
        carpetas = lista.ToArray();
        if (carpetaSel >= carpetas.Length) carpetaSel = 0;
    }

    void OnGUI()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        if (GUILayout.Button("Recargar", EditorStyles.toolbarButton, GUILayout.Width(70))) Recargar();
        carpetaSel = EditorGUILayout.Popup(carpetaSel, carpetas, EditorStyles.toolbarPopup, GUILayout.Width(220));
        GUILayout.Label("Buscar", GUILayout.Width(45));
        filtro = GUILayout.TextField(filtro, EditorStyles.toolbarTextField, GUILayout.MinWidth(80));
        GUILayout.Label("Tamaño", GUILayout.Width(50));
        tam = GUILayout.HorizontalSlider(tam, 48f, 360f, GUILayout.Width(110));
        EditorGUILayout.EndHorizontal();

        string car = carpetaSel <= 0 || carpetaSel >= carpetas.Length ? null : carpetas[carpetaSel];
        var vis = items.Where(i => (car == null || i.carpeta == car) &&
            (string.IsNullOrEmpty(filtro) || i.nombre.ToLower().Contains(filtro.ToLower()))).ToList();

        GUILayout.Label(vis.Count + " texturas (clic = seleccionar en Project, doble clic = abrir)", EditorStyles.miniLabel);

        float celdaW = tam + 12f, celdaH = tam + 34f;
        int cols = Mathf.Max(1, Mathf.FloorToInt((position.width - 20f) / celdaW));
        scroll = EditorGUILayout.BeginScrollView(scroll);
        int n = 0;
        while (n < vis.Count)
        {
            EditorGUILayout.BeginHorizontal();
            for (int c = 0; c < cols && n < vis.Count; c++, n++)
            {
                var it = vis[n];
                Rect r = GUILayoutUtility.GetRect(celdaW, celdaH, GUILayout.Width(celdaW), GUILayout.Height(celdaH));
                Rect img = new Rect(r.x + 4, r.y + 2, tam, tam);
                EditorGUI.DrawRect(img, new Color(0.18f, 0.18f, 0.18f));
                float asp = (float)it.tex.width / Mathf.Max(1, it.tex.height);
                Rect dib = img;
                if (asp > 1f) { dib.height = tam / asp; dib.y += (tam - dib.height) / 2f; }
                else { dib.width = tam * asp; dib.x += (tam - dib.width) / 2f; }
                GUI.DrawTexture(dib, it.tex, ScaleMode.ScaleToFit, true);
                var st = new GUIStyle(EditorStyles.miniLabel) { alignment = TextAnchor.UpperCenter, clipping = TextClipping.Clip };
                GUI.Label(new Rect(r.x, r.y + tam + 4, celdaW, 14), it.nombre, st);
                GUI.Label(new Rect(r.x, r.y + tam + 17, celdaW, 14), it.tex.width + "x" + it.tex.height, st);
                if (Event.current.type == EventType.MouseDown && r.Contains(Event.current.mousePosition))
                {
                    Selection.activeObject = it.tex; EditorGUIUtility.PingObject(it.tex);
                    if (Event.current.clickCount == 2) AssetDatabase.OpenAsset(it.tex);
                    Event.current.Use();
                }
            }
            EditorGUILayout.EndHorizontal();
        }
        EditorGUILayout.EndScrollView();
    }
}
