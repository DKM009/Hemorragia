using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Ver en la escena (sin Play) lo que el juego genera al darle Play (decorado, escalones, etc.).
// 1) Dale Play y espera a que cargue el nivel.
// 2) Tools > Hemorragia > Play: capturar lo generado   (estando en Play)
// 3) Sal de Play. Tools > Hemorragia > Play: pegar lo capturado en la escena
// Se crea el grupo "Arte_Captura" con objetos movibles. Solo se copian sprites decorativos (sin scripts).
public static class CapturaPlay
{
    const string KeyInicial = "CapturaPlay.inicial";
    static string Archivo { get { return Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Library/CapturaPlay.json"); } }

    [System.Serializable] class Reg
    {
        public string nombre, spritePath, spriteNombre;
        public Vector3 pos, rot, esc; public Color color; public bool flipX, flipY;
        public int layer, orden, modo, tile; public Vector2 size;
    }
    [System.Serializable] class Lista { public string escena; public List<Reg> regs = new List<Reg>(); }

    [InitializeOnLoadMethod]
    static void Iniciar()
    {
        EditorApplication.playModeStateChanged += s =>
        {
            if (s != PlayModeStateChange.ExitingEditMode) return;
            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < SceneManager.sceneCount; i++)
                foreach (var r in SceneManager.GetSceneAt(i).GetRootGameObjects()) Recorrer(r.transform, "", sb, null, null);
            SessionState.SetString(KeyInicial, sb.ToString());
        };
    }

    static string Ruta(Transform t, string padre) { return padre + "/" + t.name; }

    static void Recorrer(Transform t, string padre, System.Text.StringBuilder sb, Dictionary<string, int> visto, List<Transform> nuevos)
    {
        string p = Ruta(t, padre);
        if (sb != null) sb.Append(p).Append('\n');
        if (visto != null)
        {
            int n; visto.TryGetValue(p, out n); n++; visto[p] = n;
            int orig; if (!original.TryGetValue(p, out orig)) orig = 0;
            if (n > orig) { nuevos.Add(t); }
        }
        for (int i = 0; i < t.childCount; i++) Recorrer(t.GetChild(i), p, sb, visto, nuevos);
    }

    static Dictionary<string, int> original = new Dictionary<string, int>();

    [MenuItem("Tools/Hemorragia/Play - capturar lo generado (estando en Play)")]
    public static void Capturar()
    {
        if (!Application.isPlaying) { EditorUtility.DisplayDialog("Captura", "Primero dale Play, espera a que cargue el nivel, y vuelve a elegir esta opcion.", "OK"); return; }
        original.Clear();
        foreach (var l in SessionState.GetString(KeyInicial, "").Split('\n'))
        { if (l.Length == 0) continue; int n; original.TryGetValue(l, out n); original[l] = n + 1; }

        var escena = SceneManager.GetActiveScene();
        var visto = new Dictionary<string, int>();
        var nuevos = new List<Transform>();
        foreach (var r in escena.GetRootGameObjects()) Recorrer(r.transform, "", null, visto, nuevos);

        var datos = new Lista { escena = escena.name };
        int omitidos = 0;
        foreach (var t in nuevos)
        {
            var sr = t.GetComponent<SpriteRenderer>();
            if (sr == null || sr.sprite == null) continue;
            string sp = AssetDatabase.GetAssetPath(sr.sprite);
            if (string.IsNullOrEmpty(sp)) { omitidos++; continue; }            // sprite creado por codigo (no es un archivo)
            if (TieneLogica(t)) { omitidos++; continue; }
            var tr = t;
            datos.regs.Add(new Reg
            {
                nombre = t.name, spritePath = sp, spriteNombre = sr.sprite.name,
                pos = tr.position, rot = tr.eulerAngles, esc = tr.lossyScale, color = sr.color, flipX = sr.flipX, flipY = sr.flipY,
                layer = sr.sortingLayerID, orden = sr.sortingOrder, modo = (int)sr.drawMode, tile = (int)sr.tileMode, size = sr.size
            });
        }
        File.WriteAllText(Archivo, JsonUtility.ToJson(datos));
        Debug.Log("[Captura] " + datos.regs.Count + " sprites capturados (" + omitidos + " omitidos por tener scripts o sprite generado por codigo).");
        EditorUtility.DisplayDialog("Captura", datos.regs.Count + " sprites capturados.\n(" + omitidos + " omitidos: con scripts o creados por codigo.)\n\nAhora sal de Play y elige: Tools > Hemorragia > Play - pegar lo capturado en la escena.", "OK");
    }

    static bool TieneLogica(Transform t)
    {
        foreach (var m in t.GetComponents<MonoBehaviour>())
            if (m != null && !(m is ArteHorneado) && !(m is N2Oculto)) return true;
        for (var p = t; p != null; p = p.parent)
        {
            if (p.CompareTag("Player")) return true;
            var rb = p.GetComponent<Rigidbody2D>();
            if (rb != null && rb.bodyType == RigidbodyType2D.Dynamic) return true;
        }
        string n = t.name.ToLower();
        return n.Contains("gota") || n.Contains("lata") || n.Contains("sangre") || n.Contains("bola") || n.Contains("proyectil") || n.Contains("particula");
    }

    [MenuItem("Tools/Hemorragia/Play - pegar lo capturado en la escena")]
    public static void Pegar()
    {
        if (Application.isPlaying) { EditorUtility.DisplayDialog("Captura", "Sal de Play primero.", "OK"); return; }
        if (!File.Exists(Archivo)) { EditorUtility.DisplayDialog("Captura", "No hay nada capturado. Dale Play y captura primero.", "OK"); return; }
        var datos = JsonUtility.FromJson<Lista>(File.ReadAllText(Archivo));
        var esc = SceneManager.GetActiveScene();
        if (esc.name != datos.escena) { EditorUtility.DisplayDialog("Captura", "Lo capturado es de \"" + datos.escena + "\". Abre esa escena y vuelve a intentar.", "OK"); return; }

        var viejo = GameObject.Find("Arte_Captura");
        while (viejo != null) { Object.DestroyImmediate(viejo); viejo = GameObject.Find("Arte_Captura"); }
        var raiz = new GameObject("Arte_Captura");
        Undo.RegisterCreatedObjectUndo(raiz, "Captura");
        int n = 0;
        foreach (var r in datos.regs)
        {
            Sprite sp = null;
            foreach (var o in AssetDatabase.LoadAllAssetsAtPath(r.spritePath))
                if (o is Sprite && o.name == r.spriteNombre) { sp = (Sprite)o; break; }
            if (sp == null) continue;
            var go = new GameObject("Arte_" + r.nombre);
            go.transform.SetParent(raiz.transform, false);
            go.transform.position = r.pos;
            go.transform.eulerAngles = r.rot;
            go.transform.localScale = r.esc;
            go.AddComponent<ArteHorneado>();
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sp; sr.color = r.color; sr.flipX = r.flipX; sr.flipY = r.flipY;
            sr.sortingLayerID = r.layer; sr.sortingOrder = r.orden;
            if (r.modo != 0) { sr.drawMode = (SpriteDrawMode)r.modo; sr.tileMode = (SpriteTileMode)r.tile; sr.size = r.size; }
            n++;
        }
        EditorSceneManager.MarkSceneDirty(esc);
        EditorSceneManager.SaveScene(esc);
        Selection.activeGameObject = raiz;
        Debug.Log("[Captura] " + n + " objetos pegados en \"" + esc.name + "\" (grupo Arte_Captura).");
        EditorUtility.DisplayDialog("Captura", n + " objetos pegados en el grupo Arte_Captura.\nEscribe Arte_ en la Hierarchy para moverlos.", "OK");
    }
}
