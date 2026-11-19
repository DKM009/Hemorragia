using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

// Muestra los sprites que YA estan colocados en la escena abierta (tubos, escaleras, decoracion...).
// Menu: Tools > Hemorragia > Ver texturas colocadas en la escena
public class VisorColocadas : EditorWindow
{
    class Grupo { public Sprite sprite; public string nombre; public List<SpriteRenderer> srs = new List<SpriteRenderer>(); int idx; public int Sig() { idx = (idx + 1) % srs.Count; return idx; } }
    List<Grupo> grupos = new List<Grupo>();
    string filtro = "";
    float tam = 110f;
    Vector2 scroll;
    int total;

    [MenuItem("Tools/Hemorragia/Ver texturas colocadas en la escena")]
    public static void Abrir()
    {
        var w = GetWindow<VisorColocadas>("Colocadas");
        w.minSize = new Vector2(420, 300);
        w.Recargar();
    }

    void OnEnable() { Recargar(); EditorApplication.hierarchyChanged += Recargar; }
    void OnDisable() { EditorApplication.hierarchyChanged -= Recargar; }

    void Recargar()
    {
        grupos.Clear(); total = 0;
        var todos = Object.FindObjectsByType<SpriteRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        var dic = new Dictionary<Sprite, Grupo>();
        foreach (var sr in todos)
        {
            if (sr == null || sr.sprite == null || sr.gameObject.scene.name == null) continue;
            total++;
            if (!dic.TryGetValue(sr.sprite, out var g)) { g = new Grupo { sprite = sr.sprite, nombre = sr.sprite.name }; dic[sr.sprite] = g; grupos.Add(g); }
            g.srs.Add(sr);
        }
        grupos = grupos.OrderByDescending(g => g.srs.Count).ThenBy(g => g.nombre).ToList();
        Repaint();
    }

    void OnGUI()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        if (GUILayout.Button("Recargar", EditorStyles.toolbarButton, GUILayout.Width(70))) Recargar();
        GUILayout.Label("Buscar", GUILayout.Width(45));
        filtro = GUILayout.TextField(filtro, EditorStyles.toolbarTextField, GUILayout.MinWidth(80));
        GUILayout.Label("Tamaño", GUILayout.Width(50));
        tam = GUILayout.HorizontalSlider(tam, 48f, 300f, GUILayout.Width(110));
        EditorGUILayout.EndHorizontal();

        var vis = grupos.Where(g => string.IsNullOrEmpty(filtro) || g.nombre.ToLower().Contains(filtro.ToLower())
            || g.srs.Any(s => s != null && s.name.ToLower().Contains(filtro.ToLower()))).ToList();
        GUILayout.Label(vis.Count + " texturas distintas, " + total + " objetos en \"" + SceneManager.GetActiveScene().name + "\". Clic = selecciona y enfoca uno (clic otra vez = el siguiente).", EditorStyles.miniLabel);

        float celdaW = tam + 12f, celdaH = tam + 48f;
        int cols = Mathf.Max(1, Mathf.FloorToInt((position.width - 20f) / celdaW));
        scroll = EditorGUILayout.BeginScrollView(scroll);
        int n = 0;
        while (n < vis.Count)
        {
            EditorGUILayout.BeginHorizontal();
            for (int c = 0; c < cols && n < vis.Count; c++, n++)
            {
                var g = vis[n];
                Rect r = GUILayoutUtility.GetRect(celdaW, celdaH, GUILayout.Width(celdaW), GUILayout.Height(celdaH));
                Rect img = new Rect(r.x + 4, r.y + 2, tam, tam);
                EditorGUI.DrawRect(img, new Color(0.18f, 0.18f, 0.18f));
                var tex = g.sprite.texture;
                Rect rc = g.sprite.textureRect;
                var uv = new Rect(rc.x / tex.width, rc.y / tex.height, rc.width / tex.width, rc.height / tex.height);
                float asp = rc.width / Mathf.Max(1f, rc.height);
                Rect dib = img;
                if (asp > 1f) { dib.height = tam / asp; dib.y += (tam - dib.height) / 2f; }
                else { dib.width = tam * asp; dib.x += (tam - dib.width) / 2f; }
                GUI.DrawTextureWithTexCoords(dib, tex, uv, true);
                var st = new GUIStyle(EditorStyles.miniLabel) { alignment = TextAnchor.UpperCenter, clipping = TextClipping.Clip };
                GUI.Label(new Rect(r.x, r.y + tam + 4, celdaW, 14), g.nombre, st);
                GUI.Label(new Rect(r.x, r.y + tam + 17, celdaW, 14), "x" + g.srs.Count + "  (" + (g.srs[0] != null ? g.srs[0].name : "") + ")", st);
                GUI.Label(new Rect(r.x, r.y + tam + 30, celdaW, 14), (int)rc.width + "x" + (int)rc.height, st);
                if (Event.current.type == EventType.MouseDown && r.Contains(Event.current.mousePosition))
                {
                    int i = g.Sig();
                    var sr = g.srs[i];
                    if (sr != null)
                    {
                        Selection.activeGameObject = sr.gameObject;
                        EditorGUIUtility.PingObject(sr.gameObject);
                        if (SceneView.lastActiveSceneView != null) SceneView.lastActiveSceneView.FrameSelected();
                    }
                    Event.current.Use();
                }
            }
            EditorGUILayout.EndHorizontal();
        }
        EditorGUILayout.EndScrollView();
    }
}
