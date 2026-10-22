using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Genera el greybox del Nivel 3 — HORNOS Y ESCORIA.
/// El mas largo del juego. Estructura:
///   N3-A: Sala de Entrada / Corredor de Ventilacion (20 x 7 u) — x=0
///   N3-B: Zona de Hornos (40 x 9 u) — x=26, multiples plataformas sobre hornos
///   N3-C: Rio de Escoria (30 x 7 u) — x=87, piso de escoria mortal, saltar plataformas
///   N3-D: Sala del Guardian (20 x 10 u) — x=130, boss area con escondite final
/// Uso: Tools > Hemorragia > Crear Nivel 3 (Hornos y Escoria)
/// </summary>
public static class CrearGreyboxNivel3
{
    const string RutaSprite = "Assets/Sprites/Greybox/Cuadrado.png";

    [MenuItem("Tools/Hemorragia/Crear Nivel 3 (Hornos y Escoria)")]
    public static void CrearNivel3()
    {
        if (GameObject.Find("Nivel3_Raiz") != null)
        {
            EditorUtility.DisplayDialog("Nivel 3", "Ya existe Nivel3_Raiz en la escena.", "OK");
            return;
        }

        Sprite sp = ObtenerSprite();
        if (sp == null) { Debug.LogError("No se pudo obtener sprite greybox."); return; }

        var raiz = new GameObject("Nivel3_Raiz");
        Undo.RegisterCreatedObjectUndo(raiz, "Crear Nivel 3");

        var n3a = Hijo(raiz.transform, "N3A_Entrada");        n3a.localPosition = Vector3.zero;
        var n3b = Hijo(raiz.transform, "N3B_Hornos");         n3b.localPosition = new Vector3(26f,  0, 0);
        var n3c = Hijo(raiz.transform, "N3C_RioEscoria");     n3c.localPosition = new Vector3(87f,  0, 0);
        var n3d = Hijo(raiz.transform, "N3D_SalaGuardian");   n3d.localPosition = new Vector3(130f, 0, 0);

        GenN3A(n3a, sp);
        GenN3B(n3b, sp);
        GenN3C(n3c, sp);
        GenN3D(n3d, sp);

        // Luz global del nivel: mas oscuro que N2, presencia opresiva
        var luzGO = new GameObject("Luz_Global_N3");
        luzGO.transform.SetParent(raiz.transform, false);
        var lg = luzGO.AddComponent<Light2D>();
        lg.lightType = Light2D.LightType.Global;
        lg.color     = new Color(0.04f, 0.03f, 0.06f);
        lg.intensity = 0.05f;

        Camera cam = Camera.main;
        if (cam != null)
        {
            Undo.RecordObject(cam, "Configurar camara N3");
            cam.orthographic     = true;
            cam.orthographicSize = 4.5f;
            cam.transform.position = new Vector3(0, 0, -10);
        }

        raiz.transform.position = Vector3.zero;
        Selection.activeGameObject = raiz;
        EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        Debug.Log("Nivel 3 (Hornos y Escoria) creado.\n" +
                  "N3A x=0 | N3B x=26 | N3C x=87 | N3D x=130\n" +
                  "Conecta puertas con Interactuable o TriggerCuarto.\n" +
                  "N3C: el piso de escoria debe matar al jugador (Trigger_EscoriaMuerte).\n" +
                  "N3D: agrega el Purgador jefe y el Escondite final.");
    }

    // ── N3-A: ENTRADA / CORREDOR DE VENTILACION (20 x 7) ──────────────────────
    static void GenN3A(Transform padre, Sprite sp)
    {
        Color c = new Color(0.20f, 0.17f, 0.17f);
        Color m = new Color(0.28f, 0.26f, 0.32f);
        Color ox = new Color(0.40f, 0.24f, 0.10f);

        var e = Hijo(padre, "Estructura");
        var p = Hijo(padre, "Props");
        var l = Hijo(padre, "Luces");
        var t = Hijo(padre, "Triggers");

        Caja("Suelo",     e, sp, new Vector2(0,    -4f),   new Vector2(20, 1),   c,  true);
        Caja("Techo",     e, sp, new Vector2(0,     3.5f), new Vector2(20, 1),   c,  true);
        Caja("Pared_Izq", e, sp, new Vector2(-10.5f, 0),  new Vector2(1, 9),    m,  true);
        Caja("Pared_Der", e, sp, new Vector2( 10.5f, 0),  new Vector2(1, 9),    m,  true);

        // Tubo de ventilacion horizontal que el jugador debe pasar por debajo
        Caja("Tubo_Vent_A", e, sp, new Vector2(-4f, 0f),  new Vector2(4, 0.4f), ox, true, 2);
        Caja("Tubo_Vent_B", e, sp, new Vector2( 4f, -1f), new Vector2(4, 0.4f), ox, true, 2);

        // Plataformas desplazadas para crear ritmo
        Caja("Plat_A", e, sp, new Vector2(-6f, -2f),  new Vector2(3, 0.25f), m, true, 1);
        Caja("Plat_B", e, sp, new Vector2( 3f, -0.5f),new Vector2(3, 0.25f), m, true, 1);

        // Zona de cauterizacion (tubo caliente del suelo)
        Color cal = new Color(0.9f, 0.4f, 0.1f);
        Caja("Tubo_Caliente_Suelo", e, sp, new Vector2(7f, -3.3f), new Vector2(4, 0.4f), cal, true, 2);
        var tCal = new GameObject("Trigger_Cauteriz_N3A");
        tCal.transform.SetParent(t, false);
        tCal.transform.localPosition = new Vector3(7f, -2.9f, 0);
        var bcCal = tCal.AddComponent<BoxCollider2D>(); bcCal.size = new Vector2(4.5f, 0.8f); bcCal.isTrigger = true;
        tCal.tag = "ZonaCauterizacion";

        // Puerta entrada (desde N2)
        var pi = Caja("Puerta_EntradaN2", e, sp, new Vector2(-9.5f, -1.5f), new Vector2(1, 3.5f), new Color(0.5f, 0.5f, 0.1f), true, 3);
        pi.tag = "Puerta";

        // Spawn point
        var spawn = new GameObject("Spawn_N3A");
        spawn.transform.SetParent(padre, false);
        spawn.transform.localPosition = new Vector3(-8f, -3f, 0);

        // Nota 7
        Caja("Nota_7", p, sp, new Vector2(-3f, -3.5f), new Vector2(0.4f, 0.25f), Color.white, false, 4);
        // Nota 8 sobre plataforma B
        Caja("Nota_8", p, sp, new Vector2(4f, -0.15f), new Vector2(0.4f, 0.25f), Color.white, false, 4);

        // Trigger salida
        var ts = new GameObject("Trigger_Salida_N3B");
        ts.transform.SetParent(t, false);
        ts.transform.localPosition = new Vector3(9.5f, -1.5f, 0);
        var bcTs = ts.AddComponent<BoxCollider2D>(); bcTs.size = new Vector2(1.5f, 4f); bcTs.isTrigger = true;
        ts.tag = "SalidaNivel";

        // Limites
        var lim = new GameObject("Limites_N3A"); lim.transform.SetParent(padre, false);
        var bcL = lim.AddComponent<BoxCollider2D>(); bcL.size = new Vector2(20, 7); bcL.isTrigger = true;

        // Luces
        Luz("Luz_Ent_N3A",  l, Light2D.LightType.Point, new Vector2(-8f,  0f), new Color(0.8f, 0.7f, 0.5f), 0.6f, 5f);
        Luz("Luz_Cal_N3A",  l, Light2D.LightType.Point, new Vector2( 7f, -3f), new Color(1f, 0.4f, 0.1f),   0.7f, 3f);
        Luz("Luz_Vent_N3A", l, Light2D.LightType.Point, new Vector2( 0f,  1f), new Color(0.4f, 0.5f, 0.8f), 0.4f, 4f);
    }

    // ── N3-B: ZONA DE HORNOS (40 x 9) ─────────────────────────────────────────
    static void GenN3B(Transform padre, Sprite sp)
    {
        Color c    = new Color(0.18f, 0.15f, 0.15f);
        Color m    = new Color(0.25f, 0.25f, 0.30f);
        Color cal  = new Color(0.92f, 0.42f, 0.08f);
        Color rojo = new Color(0.70f, 0.05f, 0.05f);
        Color horno = new Color(0.55f, 0.22f, 0.05f);

        var e = Hijo(padre, "Estructura");
        var p = Hijo(padre, "Props");
        var l = Hijo(padre, "Luces");
        var t = Hijo(padre, "Triggers");
        var en = Hijo(padre, "Enemigos");

        Caja("Suelo",     e, sp, new Vector2(0,    -5f),   new Vector2(40, 1),   c,  true);
        Caja("Techo",     e, sp, new Vector2(0,     4.5f), new Vector2(40, 1),   c,  true);
        Caja("Pared_Izq", e, sp, new Vector2(-20.5f, 0),  new Vector2(1, 11),   m,  true);
        Caja("Pared_Der", e, sp, new Vector2( 20.5f, 0),  new Vector2(1, 11),   m,  true);

        // 5 Hornos grandes (cuerpos verticales)
        float[] xHornos = { -16f, -8f, 0f, 8f, 16f };
        foreach (float x in xHornos)
        {
            Caja($"Horno_Cuerpo_{(int)x}", e, sp, new Vector2(x, -2.5f), new Vector2(4, 5), horno, true, 2);
            Caja($"Horno_Tapa_{(int)x}",  e, sp, new Vector2(x,  0f),   new Vector2(4.4f, 0.35f), cal, true, 3);
            // Brillo del horno
            Luz($"Luz_Horno_{(int)x}", l, Light2D.LightType.Point,
                new Vector2(x, 0.5f), new Color(1f, 0.35f, 0.05f), 1.0f, 5.5f);

            // Cauterizacion encima de cada horno
            var tr = new GameObject($"Cauteriz_Horno_{(int)x}");
            tr.transform.SetParent(t, false);
            tr.transform.localPosition = new Vector3(x, 0.3f, 0);
            var bc = tr.AddComponent<BoxCollider2D>(); bc.size = new Vector2(4.5f, 0.8f); bc.isTrigger = true;
            tr.tag = "ZonaCauterizacion";
        }

        // Plataformas entre hornos (zigzag para crear ruta no trivial)
        Caja("Plat_Inter_AB", e, sp, new Vector2(-12f, -2f), new Vector2(2, 0.25f), m, true, 1);
        Caja("Plat_Inter_BC", e, sp, new Vector2(-4f,  -1f), new Vector2(2, 0.25f), m, true, 1);
        Caja("Plat_Inter_CD", e, sp, new Vector2( 4f,  -2f), new Vector2(2, 0.25f), m, true, 1);
        Caja("Plat_Inter_DE", e, sp, new Vector2(12f,  -1f), new Vector2(2, 0.25f), m, true, 1);

        // Suelo magmatico entre hornos (mata al jugador si cae)
        Caja("Suelo_Magma", p, sp, new Vector2(0, -4.8f), new Vector2(38, 0.15f), rojo, false, 3);

        // Trigger de muerte en el magma
        var tMagma = new GameObject("Trigger_MuerteMagma_N3B");
        tMagma.transform.SetParent(t, false);
        tMagma.transform.localPosition = new Vector3(0, -4.5f, 0);
        var bcMagma = tMagma.AddComponent<BoxCollider2D>(); bcMagma.size = new Vector2(38, 1f); bcMagma.isTrigger = true;
        tMagma.tag = "ZonaDeathFall";

        // 2 Purgadores en los hornos centrales
        Caja("Purgador_N3B_A", en, sp, new Vector2(-4f, -0.3f), new Vector2(0.5f, 1f), new Color(0.8f, 0.15f, 0.5f), false, 5);
        Caja("Purgador_N3B_B", en, sp, new Vector2( 4f, -0.3f), new Vector2(0.5f, 1f), new Color(0.8f, 0.15f, 0.5f), false, 5);

        // Nota 9
        Caja("Nota_9", p, sp, new Vector2(-16f, 0.45f), new Vector2(0.4f, 0.25f), Color.white, false, 4);

        // Puertas
        var pi = Caja("Puerta_Ent_N3B", e, sp, new Vector2(-19.5f, -2.5f), new Vector2(1, 4), new Color(0.12f, 0.4f, 0.18f), true, 3);
        pi.tag = "Puerta";
        var pd = Caja("Puerta_Sal_N3C", e, sp, new Vector2(19.5f, -2.5f), new Vector2(1, 4), new Color(0.12f, 0.4f, 0.18f), true, 3);
        pd.tag = "Puerta";

        // Trigger salida
        var ts = new GameObject("Trigger_Salida_N3C");
        ts.transform.SetParent(t, false);
        ts.transform.localPosition = new Vector3(19f, -2.5f, 0);
        var bcts = ts.AddComponent<BoxCollider2D>(); bcts.size = new Vector2(1.5f, 4f); bcts.isTrigger = true;
        ts.tag = "SalidaNivel";

        // Escondite entre horno B y C
        var goEsc = Caja("Escondite_EntreHornos", p, sp, new Vector2(-4f, -3.5f), new Vector2(2.5f, 1.5f), m * 0.7f, false, -1);
        var bcEsc = goEsc.AddComponent<BoxCollider2D>(); bcEsc.isTrigger = true;
        goEsc.AddComponent<Escondite>();

        // Limites
        var lim = new GameObject("Limites_N3B"); lim.transform.SetParent(padre, false);
        var bcL = lim.AddComponent<BoxCollider2D>(); bcL.size = new Vector2(40, 9); bcL.isTrigger = true;

        // Luz de alarma roja al fondo de la sala
        Luz("Luz_Alarma_N3B", l, Light2D.LightType.Point, new Vector2(0, 3.5f), new Color(1f, 0.1f, 0.1f), 0.4f, 10f);

        var tipoParpadeo = TypeCache.GetTypesDerivedFrom<MonoBehaviour>().FirstOrDefault(x => x.Name == "LuzParpadeante");
        var luzAlarma = GameObject.Find("Luz_Alarma_N3B");
        if (tipoParpadeo != null && luzAlarma != null) luzAlarma.AddComponent(tipoParpadeo);
    }

    // ── N3-C: RIO DE ESCORIA (30 x 7) ─────────────────────────────────────────
    static void GenN3C(Transform padre, Sprite sp)
    {
        Color c     = new Color(0.15f, 0.13f, 0.13f);
        Color m     = new Color(0.22f, 0.22f, 0.28f);
        Color escoria = new Color(0.65f, 0.20f, 0.02f);
        Color cal   = new Color(0.9f, 0.42f, 0.08f);

        var e  = Hijo(padre, "Estructura");
        var p  = Hijo(padre, "Props");
        var l  = Hijo(padre, "Luces");
        var t  = Hijo(padre, "Triggers");
        var en = Hijo(padre, "Enemigos");

        Caja("Techo",     e, sp, new Vector2(0,  3.5f), new Vector2(30, 1),   c, true);
        Caja("Pared_Izq", e, sp, new Vector2(-15.5f, 0), new Vector2(1, 9),   m, true);
        Caja("Pared_Der", e, sp, new Vector2( 15.5f, 0), new Vector2(1, 9),   m, true);

        // El suelo es el rio de escoria: NO es solido, el jugador cae si lo toca
        Caja("Rio_Escoria_Visual", p, sp, new Vector2(0, -3.5f), new Vector2(30, 1.5f), escoria, false, -1);
        Luz("Luz_Escoria", l, Light2D.LightType.Point, new Vector2(0, -3f), new Color(1f, 0.22f, 0.0f), 1.2f, 16f);

        // Trigger de muerte (escoria)
        var tEsc = new GameObject("Trigger_MuerteEscoria");
        tEsc.transform.SetParent(t, false);
        tEsc.transform.localPosition = new Vector3(0, -3f, 0);
        var bcEsc = tEsc.AddComponent<BoxCollider2D>(); bcEsc.size = new Vector2(30, 2f); bcEsc.isTrigger = true;
        tEsc.tag = "ZonaDeathFall";

        // Plataformas saltables sobre el rio (irregulares para tension)
        float[] px = { -13f, -9f, -5.5f, -1f, 3f, 6.5f, 10f, 13.5f };
        float[] py = {  -1f, -2f,  -0.5f, -1.5f, 0f, -1f, -2f, -0.5f };
        float[] pw = {  2.5f, 1.5f, 2f, 2f, 1.5f, 2f, 2f, 2f };
        for (int i = 0; i < px.Length; i++)
            Caja($"Plat_Escoria_{i}", e, sp, new Vector2(px[i], py[i]), new Vector2(pw[i], 0.3f), m, true, 2);

        // Cauterizaciones en plataformas alternas
        int[] idxCal = { 1, 3, 5, 7 };
        foreach (int i in idxCal)
        {
            var tr = new GameObject($"Cauteriz_EscoriaPlt_{i}");
            tr.transform.SetParent(t, false);
            tr.transform.localPosition = new Vector3(px[i], py[i] + 0.45f, 0);
            var bc = tr.AddComponent<BoxCollider2D>(); bc.size = new Vector2(pw[i] + 0.3f, 0.8f); bc.isTrigger = true;
            tr.tag = "ZonaCauterizacion";
            Caja($"TuboTop_EscoriaPlt_{i}", e, sp, new Vector2(px[i], py[i] + 0.15f), new Vector2(pw[i], 0.15f), cal, false, 3);
        }

        // Purgador sobre las plataformas centrales
        Caja("Purgador_N3C", en, sp, new Vector2(3f, 0.3f), new Vector2(0.5f, 1f), new Color(0.8f, 0.15f, 0.5f), false, 5);

        // Notas 10 y 11
        Caja("Nota_10", p, sp, new Vector2(-11f, py[1] + 0.3f), new Vector2(0.4f, 0.25f), Color.white, false, 4);
        Caja("Nota_11", p, sp, new Vector2(11f,  py[6] + 0.3f), new Vector2(0.4f, 0.25f), Color.white, false, 4);

        // Escondite — nicho en la pared izquierda
        var goEsc2 = Caja("Escondite_NichoParad", p, sp, new Vector2(-14.5f, -1f), new Vector2(1.5f, 2f), m * 0.6f, false, -1);
        var bcNicho = goEsc2.AddComponent<BoxCollider2D>(); bcNicho.isTrigger = true;
        goEsc2.AddComponent<Escondite>();

        // Puertas
        var pi = Caja("Puerta_Ent_N3C", e, sp, new Vector2(-14.5f, -1f), new Vector2(1, 3.5f), new Color(0.12f, 0.4f, 0.18f), true, 3);
        pi.tag = "Puerta";
        var pd = Caja("Puerta_Sal_N3D", e, sp, new Vector2(14.5f, -1f), new Vector2(1, 3.5f), new Color(0.12f, 0.4f, 0.18f), true, 3);
        pd.tag = "Puerta";

        // Trigger salida
        var ts = new GameObject("Trigger_Salida_N3D");
        ts.transform.SetParent(t, false);
        ts.transform.localPosition = new Vector3(14f, -1f, 0);
        var bcts = ts.AddComponent<BoxCollider2D>(); bcts.size = new Vector2(1.5f, 4f); bcts.isTrigger = true;
        ts.tag = "SalidaNivel";

        // Checkpoint a mitad del rio
        var ckGO = new GameObject("Checkpoint_RioEscoria");
        ckGO.transform.SetParent(padre, false);
        ckGO.transform.localPosition = new Vector3(3f, -0.5f, 0);
        var ckBC = ckGO.AddComponent<BoxCollider2D>(); ckBC.isTrigger = true; ckBC.size = new Vector2(2f, 2f);
        ckGO.AddComponent<Checkpoint>();

        // Limites
        var lim = new GameObject("Limites_N3C"); lim.transform.SetParent(padre, false);
        var bcL = lim.AddComponent<BoxCollider2D>(); bcL.size = new Vector2(30, 7); bcL.isTrigger = true;

        // Luces
        Luz("Luz_Puerta_Ent_N3C", l, Light2D.LightType.Point, new Vector2(-13f, 0f), new Color(0.7f, 0.65f, 0.5f), 0.5f, 4f);
        Luz("Luz_Puerta_Sal_N3D", l, Light2D.LightType.Point, new Vector2( 13f, 0f), new Color(0.7f, 0.65f, 0.5f), 0.5f, 4f);
    }

    // ── N3-D: SALA DEL GUARDIAN (20 x 10) ─────────────────────────────────────
    static void GenN3D(Transform padre, Sprite sp)
    {
        Color c    = new Color(0.14f, 0.12f, 0.12f);
        Color m    = new Color(0.22f, 0.22f, 0.28f);
        Color cal  = new Color(0.92f, 0.42f, 0.08f);
        Color rojo = new Color(0.60f, 0.05f, 0.05f);

        var e  = Hijo(padre, "Estructura");
        var p  = Hijo(padre, "Props");
        var l  = Hijo(padre, "Luces");
        var t  = Hijo(padre, "Triggers");
        var en = Hijo(padre, "Enemigos");

        Caja("Suelo",     e, sp, new Vector2(0,  -5.5f), new Vector2(20, 1),   c, true);
        Caja("Techo",     e, sp, new Vector2(0,   5f),   new Vector2(20, 1),   c, true);
        Caja("Pared_Izq", e, sp, new Vector2(-10.5f, 0), new Vector2(1, 12),   m, true);
        Caja("Pared_Der", e, sp, new Vector2( 10.5f, 0), new Vector2(1, 12),   m, true);

        // Plataformas verticales (escalar para llegar al jefe en altura)
        Caja("Plat_G_A", e, sp, new Vector2(-6f,  -2f),  new Vector2(4, 0.25f), m, true, 1);
        Caja("Plat_G_B", e, sp, new Vector2( 4f,   0.5f),new Vector2(4, 0.25f), m, true, 1);
        Caja("Plat_G_C", e, sp, new Vector2(-3f,   3f),  new Vector2(4, 0.25f), m, true, 1);

        // Tuberia caliente de emergencia (penultima cauterizacion del juego)
        Caja("Tuberia_Emergency_G", e, sp, new Vector2(6f, -1.5f), new Vector2(4, 0.35f), cal, true, 2);
        var tCal = new GameObject("Cauteriz_Guardian");
        tCal.transform.SetParent(t, false);
        tCal.transform.localPosition = new Vector3(6f, -1.1f, 0);
        var bcCal = tCal.AddComponent<BoxCollider2D>(); bcCal.size = new Vector2(4.5f, 0.8f); bcCal.isTrigger = true;
        tCal.tag = "ZonaCauterizacion";
        Luz("Luz_Tubo_Guard", l, Light2D.LightType.Point, new Vector2(6f, -1.5f), new Color(1f, 0.4f, 0.1f), 0.8f, 3.5f);

        // Jefe: Purgador Grande (marcador - mismo script PurgadorIA con stats altos)
        var jefe = Caja("Purgador_Jefe_N3", en, sp, new Vector2(0f, -4.5f), new Vector2(1f, 2f), new Color(0.6f, 0.05f, 0.35f), false, 5);

        // Escondite grande (ultimo del nivel, recompensa)
        var goEsc = Caja("Escondite_CamaraOculta", p, sp, new Vector2(-8.5f, -3.5f), new Vector2(2.5f, 2.5f), m * 0.55f, false, -1);
        var bcEsc = goEsc.AddComponent<BoxCollider2D>(); bcEsc.isTrigger = true;
        goEsc.AddComponent<Escondite>();

        // Nota 12 (ultima nota del nivel, revela el proposito del Altar)
        Caja("Nota_12", p, sp, new Vector2(8f, -4.8f), new Vector2(0.4f, 0.25f), Color.white, false, 4);

        // Checkpoint antes del jefe
        var ckGO = new GameObject("Checkpoint_AntesJefe");
        ckGO.transform.SetParent(padre, false);
        ckGO.transform.localPosition = new Vector3(-7f, -4.5f, 0);
        var ckBC = ckGO.AddComponent<BoxCollider2D>(); ckBC.isTrigger = true; ckBC.size = new Vector2(2f, 2f);
        ckGO.AddComponent<Checkpoint>();

        // Puerta de entrada desde N3C
        var pi = Caja("Puerta_Ent_N3D", e, sp, new Vector2(-9.5f, -2.5f), new Vector2(1, 4), new Color(0.12f, 0.4f, 0.18f), true, 3);
        pi.tag = "Puerta";

        // Puerta de salida al Nivel 4 (Altar de Desechos) — dorada
        var pd = Caja("Puerta_Sal_N4", e, sp, new Vector2(9.5f, -2.5f), new Vector2(1, 4), new Color(0.7f, 0.6f, 0.1f), true, 3);
        pd.tag = "Puerta";

        // Trigger salida al N4 (CambiarNivel)
        var ts = new GameObject("Trigger_SalidaN4");
        ts.transform.SetParent(t, false);
        ts.transform.localPosition = new Vector3(9f, -2.5f, 0);
        var bcts = ts.AddComponent<BoxCollider2D>(); bcts.size = new Vector2(1.5f, 4f); bcts.isTrigger = true;
        bcts.tag = "SalidaNivel";
        ts.tag = "SalidaNivel";
        ts.AddComponent<CambiarNivel>(); // configura nombreEscena = "Nivel4" en inspector

        // Luces dramaticas
        Luz("Luz_Roja_Sala_G", l, Light2D.LightType.Point, new Vector2(0, 3f),   new Color(1f, 0.08f, 0.08f), 0.6f, 12f);
        Luz("Luz_Ent_N3D",     l, Light2D.LightType.Point, new Vector2(-8f, 0f), new Color(0.7f, 0.65f, 0.5f), 0.4f, 4f);
        Luz("Luz_Sal_N3D",     l, Light2D.LightType.Point, new Vector2( 8f, 0f), new Color(0.9f, 0.8f, 0.4f), 0.5f, 4f);

        var tipoParpadeo = TypeCache.GetTypesDerivedFrom<MonoBehaviour>().FirstOrDefault(x => x.Name == "LuzParpadeante");
        var luzRoja = GameObject.Find("Luz_Roja_Sala_G");
        if (tipoParpadeo != null && luzRoja != null)
        {
            var lp = (LuzParpadeante)luzRoja.AddComponent(tipoParpadeo);
            lp.probCorte = 0.3f;
        }

        // Limites
        var lim = new GameObject("Limites_N3D"); lim.transform.SetParent(padre, false);
        var bcL = lim.AddComponent<BoxCollider2D>(); bcL.size = new Vector2(20, 10); bcL.isTrigger = true;
    }

    // ── Utilidades ─────────────────────────────────────────────────────────────
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
