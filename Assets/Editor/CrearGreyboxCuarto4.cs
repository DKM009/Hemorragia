using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Genera el greybox del Cuarto 4 — EMBOSCADA / SALIDA del Nivel 1.
/// Medidas: 16 x 7 u. El cuarto mas tenso del nivel: corredor angosto
/// con dos Purgadores activos y La Fosa irrumpiendo al final.
/// Uso: Tools > Hemorragia > Crear Cuarto 4 (Emboscada)
/// Se crea en x = 110 (Cuarto3 = 80, ancho 20 => extremo der en 90; gap de 10 u antes del 4).
/// </summary>
public static class CrearGreyboxCuarto4
{
    const string RutaSprite = "Assets/Sprites/Greybox/Cuadrado.png";

    [MenuItem("Tools/Hemorragia/Crear Cuarto 4 (Emboscada)")]
    public static void CrearCuarto4()
    {
        if (GameObject.Find("Cuarto4_Emboscada") != null)
        {
            EditorUtility.DisplayDialog("Cuarto 4", "Ya existe Cuarto4_Emboscada.", "OK");
            return;
        }

        Sprite cuadrado = ObtenerSprite();
        if (cuadrado == null) { Debug.LogError("No se pudo obtener sprite greybox."); return; }

        var raiz = new GameObject("Cuarto4_Emboscada");
        Undo.RegisterCreatedObjectUndo(raiz, "Crear Cuarto 4");

        Transform estructura = Hijo(raiz.transform, "Estructura");
        Transform props      = Hijo(raiz.transform, "Props");
        Transform enemigos   = Hijo(raiz.transform, "Enemigos");
        Transform triggers   = Hijo(raiz.transform, "Triggers");
        Transform luces      = Hijo(raiz.transform, "Luces");

        Color concreto  = new Color(0.20f, 0.18f, 0.18f);
        Color metal     = new Color(0.28f, 0.28f, 0.32f);
        Color herrumbre = new Color(0.35f, 0.20f, 0.08f);
        Color rojo      = new Color(0.55f, 0.08f, 0.08f);

        // ESTRUCTURA
        Caja("Suelo",     estructura, cuadrado, new Vector2(0,    -4f),   new Vector2(16, 1),  concreto, true);
        Caja("Techo",     estructura, cuadrado, new Vector2(0,     3.5f), new Vector2(16, 1),  concreto, true);
        Caja("Pared_Izq", estructura, cuadrado, new Vector2(-8.5f, 0),   new Vector2(1, 9),   metal,    true);
        Caja("Pared_Der", estructura, cuadrado, new Vector2( 8.5f, 0),   new Vector2(1, 9),   metal,    true);

        // Barrera que La Fosa rompe (visualmente diferente)
        Caja("Barrera_Fosa", estructura, cuadrado, new Vector2(-6f, -1f), new Vector2(0.4f, 5), rojo, true, 3);

        // Tuberia caliente de emergencia (ultima oportunidad de cauterizar antes del jefe)
        Color caliente = new Color(0.9f, 0.4f, 0.1f);
        Caja("Tuberia_Emergencia", estructura, cuadrado, new Vector2(4f, -1.2f), new Vector2(4, 0.25f), caliente, true, 2);

        // Debris/escombros que ralentizan (sin collider, solo visual)
        Caja("Escombro_A", props, cuadrado, new Vector2(-3f, -3.5f), new Vector2(1.2f, 0.5f), concreto * 1.3f, false, 1);
        Caja("Escombro_B", props, cuadrado, new Vector2( 1f, -3.5f), new Vector2(0.8f, 0.6f), concreto * 1.2f, false, 1);
        Caja("Escombro_C", props, cuadrado, new Vector2( 6f, -3.5f), new Vector2(1.5f, 0.4f), concreto,        false, 1);

        // Tuberias decorativas techo
        Caja("Tub_Techo_A", props, cuadrado, new Vector2(-2f, 3f), new Vector2(8, 0.2f), herrumbre, false, -2);

        // Escondite (unico en este cuarto, muy estrecho)
        var goEsc = Caja("Escondite_Conducto", props, cuadrado, new Vector2(-1f, -2f), new Vector2(2f, 1.5f), metal * 0.8f, false, -1);
        var bcEsc = goEsc.AddComponent<BoxCollider2D>(); bcEsc.isTrigger = true;
        goEsc.AddComponent<Escondite>();

        // Nota 4 — pegada en la pared derecha
        Caja("Nota_4", props, cuadrado, new Vector2(7.5f, -2.5f), new Vector2(0.4f, 0.25f), Color.white, false, 4);

        // Puertas
        Color puertaEntrada = new Color(0.12f, 0.40f, 0.18f);
        Color puertaSalida  = new Color(0.7f,  0.6f,  0.1f); // dorada => salida del nivel
        var goPI = Caja("Puerta_Entrada_C3", estructura, cuadrado, new Vector2(-7.5f, -1.5f), new Vector2(1, 3.5f), puertaEntrada, true, 3);
        goPI.tag = "Puerta";
        var goPD = Caja("Puerta_Salida_N2",  estructura, cuadrado, new Vector2( 7.5f, -1.5f), new Vector2(1, 3.5f), puertaSalida,  true, 3);
        goPD.tag = "Puerta";

        // ENEMIGOS
        // Purgador A — patrulla por el corredor izquierdo
        Caja("Purgador_A", enemigos, cuadrado, new Vector2(-4f, -3.1f), new Vector2(0.5f, 1f), new Color(0.8f,0.15f,0.5f), false, 5);
        // Purgador B — patrulla corredor derecho
        Caja("Purgador_B", enemigos, cuadrado, new Vector2( 2f, -3.1f), new Vector2(0.5f, 1f), new Color(0.8f,0.15f,0.5f), false, 5);
        // La Fosa — aparece desde la izquierda al trigger de clímax
        Caja("LaFosa_Marcador", enemigos, cuadrado, new Vector2(-8f, -2f), new Vector2(1.5f, 3f), new Color(0.5f,0.1f,0.1f), false, 5);

        // TRIGGERS
        {   // Trigger que activa La Fosa (Clímax del Nivel 1)
            var t = new GameObject("Trigger_CliMax");
            t.transform.SetParent(triggers, false);
            t.transform.localPosition = new Vector3(0, -2f, 0);
            var bc = t.AddComponent<BoxCollider2D>(); bc.size = new Vector2(4, 6); bc.isTrigger = true;
            t.tag = "ZonaClimax";
        }
        {   // Cauterización de emergencia
            var t = new GameObject("Trigger_CauterizEmergencia");
            t.transform.SetParent(triggers, false);
            t.transform.localPosition = new Vector3(4f, -0.8f, 0);
            var bc = t.AddComponent<BoxCollider2D>(); bc.size = new Vector2(4.5f, 1f); bc.isTrigger = true;
            t.tag = "ZonaCauterizacion";
        }
        {   // Salida al Nivel 2
            var t = new GameObject("Trigger_SalidaN2");
            t.transform.SetParent(triggers, false);
            t.transform.localPosition = new Vector3(7f, -1.5f, 0);
            var bc = t.AddComponent<BoxCollider2D>(); bc.size = new Vector2(1.5f, 4f); bc.isTrigger = true;
            t.tag = "SalidaNivel";
        }

        // LUCES
        Luz("Luz_Ambiental", luces, Light2D.LightType.Global, Vector2.zero, new Color(0.03f,0.03f,0.06f), 0.06f);
        Luz("Luz_Roja_Alarma", luces, Light2D.LightType.Point, new Vector2(0, 2f),   new Color(1f,0.1f,0.1f), 0.5f, 8f);
        Luz("Luz_Tuberia_Cal", luces, Light2D.LightType.Point, new Vector2(4f,-1.2f), new Color(1f,0.4f,0.1f), 0.8f, 3.5f);
        Luz("Luz_Salida",      luces, Light2D.LightType.Point, new Vector2(7f,-1.5f), new Color(0.9f,0.8f,0.4f),0.6f, 4f);

        // Parpadeo en la luz roja de alarma
        var tipoParpadeo = TypeCache.GetTypesDerivedFrom<MonoBehaviour>().FirstOrDefault(t => t.Name == "LuzParpadeante");
        var luzRoja = GameObject.Find("Luz_Roja_Alarma");
        if (tipoParpadeo != null && luzRoja != null)
        {
            var lp = (LuzParpadeante)luzRoja.AddComponent(tipoParpadeo);
            lp.probCorte = 0.25f; // mas frecuente en la sala de climax
        }

        // LIMITES
        var limGO = new GameObject("Limites_Cuarto4");
        limGO.transform.SetParent(raiz.transform, false);
        var limBC = limGO.AddComponent<BoxCollider2D>(); limBC.size = new Vector2(16, 7); limBC.isTrigger = true;

        raiz.transform.position = new Vector3(110f, 0f, 0f);

        Selection.activeGameObject = raiz;
        EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        Debug.Log("Cuarto 4 (Emboscada) creado en x=110.\n" +
                  "• Agrega PurgadorIA a Purgador_A y Purgador_B.\n" +
                  "• Agrega LaFosaIA a LaFosa_Marcador.\n" +
                  "• Trigger_Climax debe activar LaFosa (GestorNivel1).\n" +
                  "• Trigger_SalidaN2 conecta al Nivel 2 via Interactuable (CambioEscena).");
    }

    static Transform Hijo(Transform padre, string nombre)
    {
        var go = new GameObject(nombre); go.transform.SetParent(padre, false); return go.transform;
    }

    static GameObject Caja(string nombre, Transform padre, Sprite sprite, Vector2 pos, Vector2 tam,
                            Color color, bool solido, int orden = 0)
    {
        var go = new GameObject(nombre);
        go.transform.SetParent(padre, false);
        go.transform.localPosition = new Vector3(pos.x, pos.y, 0);
        go.transform.localScale    = new Vector3(tam.x, tam.y, 1);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite; sr.color = color; sr.sortingOrder = orden;
        if (solido) go.AddComponent<BoxCollider2D>();
        return go;
    }

    static Light2D Luz(string nombre, Transform padre, Light2D.LightType tipo,
                       Vector2 pos, Color color, float intensidad, float radio = 0f)
    {
        var go = new GameObject(nombre);
        go.transform.SetParent(padre, false);
        go.transform.localPosition = new Vector3(pos.x, pos.y, 0);
        var l = go.AddComponent<Light2D>();
        l.lightType = tipo; l.color = color; l.intensity = intensidad;
        if (tipo == Light2D.LightType.Point) { l.pointLightInnerRadius = 0f; l.pointLightOuterRadius = radio; }
        return l;
    }

    static Sprite ObtenerSprite()
    {
        var ex = AssetDatabase.LoadAssetAtPath<Sprite>(RutaSprite);
        if (ex != null) return ex;
        Directory.CreateDirectory(Path.GetDirectoryName(RutaSprite));
        var tex = new Texture2D(4, 4, TextureFormat.RGBA32, false);
        tex.SetPixels(Enumerable.Repeat(Color.white, 16).ToArray()); tex.Apply();
        File.WriteAllBytes(RutaSprite, tex.EncodeToPNG()); Object.DestroyImmediate(tex);
        AssetDatabase.ImportAsset(RutaSprite);
        var imp = (TextureImporter)AssetImporter.GetAtPath(RutaSprite);
        imp.textureType = TextureImporterType.Sprite; imp.spriteImportMode = SpriteImportMode.Single;
        imp.spritePixelsPerUnit = 4; imp.filterMode = FilterMode.Point;
        imp.textureCompression = TextureImporterCompression.Uncompressed; imp.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(RutaSprite);
    }
}
