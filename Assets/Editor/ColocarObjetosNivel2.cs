using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

/// <summary>
/// Deja el Nivel 2 jugable de principio a fin con las MISMAS reglas que el Nivel 1:
///   - Trae de Nivel1 el jugador, el HUD de sangre y el EventSystem; configura la camara.
///   - Arregla muros y alturas (bloques para subir, tubos a altura de salto, escondite alcanzable).
///   - Purgadores, La Fosa dormida (ruido), escondites, latas, vendas, fusible y puerta con fusible.
///   - Tubos calientes con tag ZonaCauterizacion, notas y mensajes de tutorial.
///   - Final: compuerta de la caldera -> caida -> escena final (se crea sola si falta).
/// Se puede ejecutar varias veces. Menu: Tools > Hemorragia > Nivel 2 - Colocar objetos y jugabilidad
/// </summary>
public static class ColocarObjetosNivel2
{
    const string RutaSprite  = "Assets/Sprites/Greybox/Cuadrado.png";
    const string RutaNivel1  = "Assets/Scenes/Nivel1.unity";
    const string RutaNivel2  = "Assets/Scenes/Nivel2.unity";
    const string Contenedor  = "Gameplay_Nivel2";

    static readonly Color metal   = new Color(0.38f, 0.38f, 0.42f);
    static readonly Color venda   = new Color(0.95f, 0.88f, 0.88f);
    static readonly Color lata    = new Color(0.75f, 0.78f, 0.85f);
    static readonly Color fusible = new Color(1f, 0.85f, 0.2f);

    [MenuItem("Tools/Hemorragia/Nivel 2 - Colocar objetos y jugabilidad")]
    public static void Colocar()
    {
        if (SceneManager.GetActiveScene().name != "Nivel2")
        {
            EditorUtility.DisplayDialog("Nivel 2",
                "Abre la escena Nivel2 (Assets/Scenes/Nivel2.unity) y vuelve a ejecutar.", "OK");
            return;
        }

        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(RutaSprite);
        var limA = Buscar("Limites_N2A");
        var limB = Buscar("Limites_N2B");
        var limC = Buscar("Limites_N2C");
        var spawnEntrada = Buscar("Spawn_Entrada");
        if (sprite == null || limA == null || limB == null || limC == null || spawnEntrada == null)
        {
            EditorUtility.DisplayDialog("Nivel 2",
                "Falta algo en la escena:\n" +
                $"Sprite greybox: {(sprite ? "ok" : "NO")}\n" +
                $"Limites_N2A/B/C: {(limA && limB && limC ? "ok" : "NO")}\n" +
                $"Spawn_Entrada: {(spawnEntrada ? "ok" : "NO")}\n\n" +
                "Genera el Nivel 2 con Tools > Hemorragia (greybox) primero.", "OK");
            return;
        }

        var rep = new List<string>();
        Undo.IncrementCurrentGroup();
        int grupo = Undo.GetCurrentGroup();

        // Contenedor nuevo (se borra el de la vez anterior)
        var viejo = Buscar(Contenedor);
        if (viejo != null) Undo.DestroyObjectImmediate(viejo);
        var cont = new GameObject(Contenedor);
        Undo.RegisterCreatedObjectUndo(cont, Contenedor);
        var k = cont.transform;

        // ── 1. Gestores globales ─────────────────────────────────────────────
        if (Object.FindFirstObjectByType<GestorJuego>(FindObjectsInactive.Include) == null)
        {
            var g = new GameObject("GestorJuego");
            Undo.RegisterCreatedObjectUndo(g, "GestorJuego");
            g.AddComponent<GestorJuego>();
            rep.Add("Creado GestorJuego");
        }
        if (Object.FindFirstObjectByType<GestorRuido>(FindObjectsInactive.Include) == null)
        {
            var g = new GameObject("GestorRuido");
            Undo.RegisterCreatedObjectUndo(g, "GestorRuido");
            g.AddComponent<GestorRuido>();
            rep.Add("Creado GestorRuido");
        }

        // ── 2. Spawns ────────────────────────────────────────────────────────
        Undo.RecordObject(spawnEntrada.transform, "Mover Spawn_Entrada");
        spawnEntrada.transform.position = new Vector3(-3.2f, -3.2f, 0f);
        var spawnB = Vacio(k, "Spawn_N2B", new Vector2(8.2f, -2.2f));
        var spawnC = Vacio(k, "Spawn_N2C", new Vector2(66.6f, -5.5f));

        // ── 3. Jugador, HUD, EventSystem y camara ────────────────────────────
        var jugador = PrepararJugador(spawnEntrada.transform.position, limA.GetComponent<Collider2D>(), rep);

        // ── 4. Zona A: sala de llegada ───────────────────────────────────────
        Zona(k, "Msg_N2_Inicio", new Vector2(-3f, -3f), 3.5f, 9f,
            "Nivel 2. Algo enorme duerme junto a la entrada: camina, no corras ni saltes, el ruido lo despierta.");
        Zona(k, "Msg_N2_Tubo", new Vector2(-2f, -3f), 2.2f, 6f,
            "Este tubo quema lo justo para cerrar la herida. Quedate encima unos segundos.");
        Pickup(k, sprite, "Lata_N2_0", Recolectable.Tipo.Lata, new Vector2(0.4f, -3.75f), lata);
        Pickup(k, sprite, "Lata_N2_1", Recolectable.Tipo.Lata, new Vector2(1.2f, -3.75f), lata);

        // Los tubos de suelo y sus zonas de cauterizacion
        foreach (var n in new[] { "Trigger_Cauteriz_N2A_0", "Trigger_Cauteriz_N2A_1" })
            Cauterizar(Buscar(n), rep);
        // Los tubos del suelo de la sala son decorativos y no estorban a nadie
        foreach (var n in new[] { "Tubo_Suelo_A", "Tubo_Suelo_B" })
        {
            var tubo = Buscar(n);
            if (tubo == null) continue;
            foreach (var c in tubo.GetComponents<Collider2D>()) { Undo.RecordObject(c, "Tubo decorativo"); c.enabled = false; }
        }

        // La Fosa duerme junto a la entrada: el ruido la despierta
        var fosa = CrearFosa(k, sprite, new Vector2(-5.1f, -2.4f));

        var salidaA = Disparador(k, "Salida_N2A", new Vector2(4.4f, -2.9f), new Vector2(1.4f, 2.2f), 1, null);

        // ── 5. Zona B: corredor de tuberias ──────────────────────────────────
        // Tubos calientes a altura de salto (arriba del suelo del pasillo, -2.5)
        for (int i = 0; i < 4; i++)
        {
            Mover(Buscar("Tubo_Cal_" + i), null, -1.2f);
            Mover(Buscar("Cauteriz_" + i), null, -0.78f);
            Mover(Buscar("Luz_Tubo_" + i), null, -0.9f);
            Cauterizar(Buscar("Cauteriz_" + i), rep);
        }
        // El ultimo tubo caliente se quita: ahi va el escondite y el Purgador de la puerta
        Apagar(Buscar("Tubo_Cal_3")); Apagar(Buscar("Cauteriz_3")); Apagar(Buscar("Luz_Tubo_3"));

        // Muros bajados al suelo y bloques para subirlos
        foreach (var par in new[] { ("Muro_B_A", 14.5f), ("Muro_B_B", 25.5f), ("Muro_B_C", 33.5f) })
        {
            Mover(Buscar(par.Item1), par.Item2, -1.0f);
            float wx = par.Item2;
            Solido(k, sprite, "Escalon_" + par.Item1 + "_1", new Vector2(wx - 1.95f, -1.95f), new Vector2(0.9f, 1.1f), metal * 0.9f);
            Solido(k, sprite, "Escalon_" + par.Item1 + "_2", new Vector2(wx - 0.95f, -1.45f), new Vector2(0.9f, 2.1f), metal);
        }

        // Escondite 1: bajo el tubo frio de la primera celda
        var frioA = Buscar("Tubo_Frio_A");
        Mover(frioA, 16.5f, -1.2f);
        ConfigurarEscondite(Buscar("Escondite_RanuraFria"), new Vector2(16.5f, -1.8f), new Vector2(2.6f, 1.4f));
        // Escondite 2: en la ultima celda, cerca de la puerta
        var frioB = Buscar("Tubo_Frio_B");
        Mover(frioB, 35.2f, -1.2f);
        if (frioB != null)
        {
            var esc2 = Vacio(k, "Escondite_FinalPasillo", new Vector2(35.2f, -1.8f));
            esc2.localScale = new Vector3(2.6f, 1.4f, 1f);
            var bc = esc2.gameObject.AddComponent<BoxCollider2D>();
            bc.isTrigger = true;
            esc2.gameObject.tag = "Escondite";
            esc2.gameObject.AddComponent<Escondite>();
            AgregarLuz(esc2, new Color(0.4f, 0.5f, 0.8f), 0.5f, 2.2f);
        }

        Zona(k, "Msg_N2B_Inicio", new Vector2(8.6f, -2f), 3f, 7f,
            "Pasillo de tuberias. Los muros se suben por los bloques. Los Purgadores patrullan entre muros.");
        Zona(k, "Msg_N2B_Muro", new Vector2(11.8f, -2f), 2.2f, 5f, "Un muro. Sube por los bloques.");
        Zona(k, "Msg_N2B_Escondite", new Vector2(15.8f, -2f), 2.5f, 6f,
            "Bajo el tubo frio puedes esconderte (E) hasta que el Purgador se aleje.");
        Zona(k, "Msg_N2B_Fusible", new Vector2(27.3f, -2f), 2.5f, 7f,
            "El fusible esta debajo del tubo. Lanza una lata (Q) para alejar al Purgador.");
        Zona(k, "Msg_N2B_Puerta", new Vector2(38f, -2f), 2.2f, 5f, "La puerta no tiene energia. Necesita el fusible.");

        Pickup(k, sprite, "Lata_N2_2", Recolectable.Tipo.Lata, new Vector2(10.4f, -2.25f), lata);
        Pickup(k, sprite, "Venda_N2_1", Recolectable.Tipo.Venda, new Vector2(12.2f, -0.85f), venda);
        Pickup(k, sprite, "Lata_N2_3", Recolectable.Tipo.Lata, new Vector2(15.2f, -2.25f), lata);
        Pickup(k, sprite, "Venda_N2_2", Recolectable.Tipo.Venda, new Vector2(19.5f, -0.85f), venda);
        Pickup(k, sprite, "Lata_N2_4", Recolectable.Tipo.Lata, new Vector2(22.6f, -2.25f), lata);
        Pickup(k, sprite, "Fusible_N2", Recolectable.Tipo.Fusible, new Vector2(29.6f, -2.2f), fusible);
        Pickup(k, sprite, "Venda_N2_3", Recolectable.Tipo.Venda, new Vector2(34.4f, -2.25f), venda);

        Nota(Buscar("Nota_5"), 5, "Parte de mantenimiento",
            "Las tuberias se calientan sin motivo. Vi algo moverse al fondo del pasillo, entre los muros. No fui a revisar.",
            new Vector2(9.5f, -2.35f), rep);

        // Purgadores del pasillo
        var pA = Purgador(k, sprite, "Purgador_N2_A", new Vector2(21f, -2.0f), 18.5f, 24.2f);
        var pB = Purgador(k, sprite, "Purgador_N2_B", new Vector2(28f, -2.0f), 26.8f, 30.0f);

        var pC = Purgador(k, sprite, "Purgador_N2_C", new Vector2(37.8f, -2.0f), 36.8f, 38.8f);

        // Puerta de salida: alta como el pasillo y con fusible
        var puerta = Buscar("Puerta_Salida_N2C");
        PuertaConLlave pl = null;
        if (puerta != null)
        {
            Undo.RecordObject(puerta.transform, "Puerta salida N2C");
            puerta.transform.position = new Vector3(40.5f, -0.25f, 0f);
            puerta.transform.localScale = new Vector3(1f, 4.5f, 1f);
            pl = puerta.GetComponent<PuertaConLlave>();
            if (pl == null) pl = Undo.AddComponent<PuertaConLlave>(puerta);
            pl.nombreItem = "Fusible";
            EditorUtility.SetDirty(pl);
        }
        else rep.Add("AVISO: no se encontro Puerta_Salida_N2C");
        var salidaB = Disparador(k, "Salida_N2B", new Vector2(39.2f, -0.25f), new Vector2(1.4f, 4.4f), 2, pl);

        // ── 6. Zona C: calderas ──────────────────────────────────────────────
        // La puerta de entrada de las calderas ya no hace falta
        Apagar(Buscar("Puerta_Entrada_N2B"));

        // Caldera izquierda mas baja para poder subir; el resto como estaba
        var cuerpoI = Buscar("Caldera_Izq_Cuerpo");
        if (cuerpoI != null)
        {
            Undo.RecordObject(cuerpoI.transform, "Caldera izq");
            cuerpoI.transform.position = new Vector3(70f, -4.8f, 0f);
            cuerpoI.transform.localScale = new Vector3(4f, 2.4f, 1f);
        }
        Mover(Buscar("Caldera_Izq_Tapa"), null, -3.6f);
        Mover(Buscar("Cauteriz_Izq"), null, -3.05f);
        Mover(Buscar("Luz_Caldera_Izq"), null, -3.4f);
        Cauterizar(Buscar("Cauteriz_Izq"), rep);
        Cauterizar(Buscar("Cauteriz_Der"), rep);

        // Plataformas (de abajo hacia arriba) y bloques
        Solido(k, sprite, "Escalon_C_1", new Vector2(67.45f, -5.4f), new Vector2(0.9f, 1.2f), metal * 0.9f);
        Mover(Buscar("Plat_Escalada_A"), 73f, -2.5f);
        Solido(k, sprite, "Plat_C_M1", new Vector2(75f, -1.325f), new Vector2(1.6f, 0.25f), metal);
        Mover(Buscar("Plat_Escalada_B"), 77f, -0.125f);
        Solido(k, sprite, "Escalon_C_B2", new Vector2(77.6f, 0.55f), new Vector2(0.8f, 1.1f), metal * 0.9f);
        Solido(k, sprite, "Relleno_Der", new Vector2(83f, -1.925f), new Vector2(2f, 8.15f), metal * 0.7f);
        Apagar(Buscar("Plat_Escalada_C"));

        Zona(k, "Msg_N2C", new Vector2(67f, -5f), 3f, 8f,
            "Las calderas. Sube por los bloques hasta la compuerta de la caldera derecha.");
        Zona(k, "Msg_N2C_Final", new Vector2(79f, 2.5f), 3f, 6f,
            "La compuerta esta abierta. Salta.");
        Pickup(k, sprite, "Venda_N2_4", Recolectable.Tipo.Venda, new Vector2(76.4f, 0.15f), venda);
        Nota(Buscar("Nota_6"), 6, "Ultima hoja",
            "La caldera se traga todo lo que cae. Yo ya no tengo fuerzas para volver a subir.",
            new Vector2(79.3f, 2.4f), rep);

        // Un Purgador patrulla la tapa de la caldera izquierda
        var pD = Purgador(k, sprite, "Purgador_N2_D", new Vector2(70f, -2.95f), 69.2f, 71.0f);

        AgregarLava(k, sprite);

        // Caida final
        var caida = Buscar("Trigger_CaidaFinal");
        if (caida != null)
        {
            Undo.RecordObject(caida.transform, "Trigger caida");
            caida.transform.position = new Vector3(80.5f, 2.5f, 0f);
            caida.transform.localScale = new Vector3(1.2f, 1.2f, 1f);
            if (caida.GetComponent<CaidaCaldera>() == null) Undo.AddComponent<CaidaCaldera>(caida);
        }
        else rep.Add("AVISO: no se encontro Trigger_CaidaFinal");

        // ── 7. Gestor del nivel ──────────────────────────────────────────────
        var gGO = new GameObject("GestorNivel2");
        gGO.transform.SetParent(k, false);
        Undo.RegisterCreatedObjectUndo(gGO, "GestorNivel2");
        var gestor = gGO.AddComponent<GestorNivel2>();
        gestor.spawnA = spawnEntrada.transform;
        gestor.spawnB = spawnB;
        gestor.spawnC = spawnC;
        gestor.limitesA = limA.GetComponent<Collider2D>();
        gestor.limitesB = limB.GetComponent<Collider2D>();
        gestor.limitesC = limC.GetComponent<Collider2D>();
        gestor.purgadoresZonaB = new[] { pA, pB, pC };
        gestor.purgadoresZonaC = new[] { pD };
        gestor.laFosa = fosa;
        EditorUtility.SetDirty(gestor);

        Undo.CollapseUndoOperations(grupo);
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene());

        // ── 8. Escena final ──────────────────────────────────────────────────
        string finalMsg = CrearEscenaFinalAuto.CrearYRegistrar();

        string reporte =
            "Nivel 2 listo y guardado.\n\n" +
            $"Jugador: {(jugador ? "ok" : "NO")}   Purgadores: 4   La Fosa: {(fosa ? "ok" : "NO")}\n" +
            (rep.Count > 0 ? "\n" + string.Join("\n", rep) + "\n" : "") +
            "\n" + finalMsg +
            "\n\nPrueba: Menu principal > Nivel 1 > sal por la puerta (o abre Nivel2 y dale Play).";
        Debug.Log("[ColocarObjetosNivel2] " + reporte);
        EditorUtility.DisplayDialog("Nivel 2", reporte, "OK");
    }

    // ── Lava del suelo entre las calderas ────────────────────────────────────
    [MenuItem("Tools/Hemorragia/Nivel 2 - Agregar lava")]
    public static void MenuLava()
    {
        if (SceneManager.GetActiveScene().name != "Nivel2")
        {
            EditorUtility.DisplayDialog("Lava", "Abre la escena Nivel2 y vuelve a ejecutar.", "OK");
            return;
        }
        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(RutaSprite);
        var cont = Buscar(Contenedor);
        if (cont == null || sprite == null)
        {
            EditorUtility.DisplayDialog("Lava", "Primero ejecuta 'Nivel 2 - Colocar objetos y jugabilidad'.", "OK");
            return;
        }
        AgregarLava(cont.transform, sprite);
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        EditorUtility.DisplayDialog("Lava", "Lava agregada y escena guardada.", "OK");
    }

    static void AgregarLava(Transform k, Sprite sprite)
    {
        var viejo = BuscarEn(k, "Lava_N2C");
        if (viejo != null) Undo.DestroyObjectImmediate(viejo.gameObject);

        // Piso entre las dos calderas: x de 72 a 78, desde el suelo (-6) hasta -5.4
        var raiz = Vacio(k, "Lava_N2C", new Vector2(75f, -5.7f));
        raiz.localScale = new Vector3(6f, 0.6f, 1f);
        var sr = raiz.gameObject.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.color = new Color(1f, 0.38f, 0.04f, 0.92f);
        sr.sortingOrder = 20; // por encima del jugador: se ve como se hunde
        var bc = raiz.gameObject.AddComponent<BoxCollider2D>();
        bc.isTrigger = true;
        raiz.gameObject.AddComponent<ZonaLava>();

        // Brillo
        var luz = new GameObject("Luz_Lava");
        luz.transform.SetParent(raiz, false);
        luz.transform.localScale = new Vector3(1f / 6f, 1f / 0.6f, 1f);
        luz.transform.localPosition = new Vector3(0f, 0.5f, 0f);
        var l = luz.AddComponent<Light2D>();
        l.lightType = Light2D.LightType.Point;
        l.color = new Color(1f, 0.4f, 0.1f);
        l.intensity = 1.4f;
        l.pointLightInnerRadius = 0f;
        l.pointLightOuterRadius = 6f;

        // Parpadeo de la superficie
        var borde = new GameObject("Borde_Brillante");
        borde.transform.SetParent(raiz, false);
        borde.transform.localPosition = new Vector3(0f, 0.45f, 0f);
        borde.transform.localScale = new Vector3(1f, 0.12f, 1f);
        var sb = borde.AddComponent<SpriteRenderer>();
        sb.sprite = sprite;
        sb.color = new Color(1f, 0.8f, 0.2f, 0.95f);
        sb.sortingOrder = 21;
    }

    // ── Jugador / HUD / camara ───────────────────────────────────────────────
    static GameObject PrepararJugador(Vector3 posSpawn, Collider2D limitesInicial, List<string> rep)
    {
        var escenaN2 = SceneManager.GetActiveScene();

        // Ya hay jugador (ejecucion anterior)?
        GameObject jug = null;
        foreach (var s in Object.FindObjectsByType<SistemaSangre>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (s.gameObject.scene == escenaN2) { jug = s.gameObject; break; }

        HUDSangre hud = null;
        foreach (var h in Object.FindObjectsByType<HUDSangre>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (h.gameObject.scene == escenaN2) { hud = h; break; }

        CamaraSeguimiento camOrigen = null;

        if (jug == null || hud == null)
        {
            bool yaAbierta = SceneManager.GetSceneByPath(RutaNivel1).isLoaded;
            Scene n1 = yaAbierta ? SceneManager.GetSceneByPath(RutaNivel1)
                                 : EditorSceneManager.OpenScene(RutaNivel1, OpenSceneMode.Additive);
            SceneManager.SetActiveScene(escenaN2);

            GameObject jugSrc = null, hudSrc = null, esSrc = null;
            foreach (var raiz in n1.GetRootGameObjects())
            {
                var sg = raiz.GetComponentInChildren<SistemaSangre>(true);
                if (sg != null && jugSrc == null) jugSrc = sg.gameObject;
                var hd = raiz.GetComponentInChildren<HUDSangre>(true);
                if (hd != null && hudSrc == null) hudSrc = raiz; // raiz del HUD (Canvas)
                if (raiz.GetComponentInChildren<UnityEngine.EventSystems.EventSystem>(true) != null && esSrc == null)
                    esSrc = raiz;
                var cm = raiz.GetComponentInChildren<CamaraSeguimiento>(true);
                if (cm != null && camOrigen == null) camOrigen = cm;
            }

            if (jug == null && jugSrc != null)
            {
                jug = Object.Instantiate(jugSrc);
                jug.name = "Player";
                Undo.RegisterCreatedObjectUndo(jug, "Player N2");
                rep.Add("Jugador traido de Nivel1");
            }
            if (hud == null && hudSrc != null)
            {
                var h = Object.Instantiate(hudSrc);
                h.name = "HUD_Sangre";
                Undo.RegisterCreatedObjectUndo(h, "HUD N2");
                hud = h.GetComponentInChildren<HUDSangre>(true);
                rep.Add("HUD de sangre traido de Nivel1");
            }
            if (esSrc != null && Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                var e = Object.Instantiate(esSrc);
                e.name = "EventSystem";
                Undo.RegisterCreatedObjectUndo(e, "EventSystem N2");
            }

            // Camara: copiar ajustes del Nivel 1 antes de cerrarlo
            var camN2 = Camera.main;
            if (camN2 != null)
            {
                var cs = camN2.GetComponent<CamaraSeguimiento>();
                if (cs == null) cs = Undo.AddComponent<CamaraSeguimiento>(camN2.gameObject);
                if (camOrigen != null) EditorUtility.CopySerialized(camOrigen, cs);
            }

            if (!yaAbierta) EditorSceneManager.CloseScene(n1, true);
            SceneManager.SetActiveScene(escenaN2);
        }

        if (jug == null)
        {
            rep.Add("AVISO: no se pudo traer el jugador de Nivel1");
            return null;
        }

        jug.transform.position = posSpawn;
        var sangre = jug.GetComponent<SistemaSangre>();
        var camara = Camera.main != null ? Camera.main.GetComponent<CamaraSeguimiento>() : null;
        if (camara != null)
        {
            Undo.RecordObject(camara, "Camara N2");
            camara.objetivo = jug.transform;
            camara.limites = limitesInicial;
            EditorUtility.SetDirty(camara);
        }
        if (sangre != null)
        {
            Undo.RecordObject(sangre, "Sangre N2");
            sangre.camara = camara;
            sangre.hud = hud;
            EditorUtility.SetDirty(sangre);
        }
        if (jug.GetComponent<InventarioJugador>() == null) Undo.AddComponent<InventarioJugador>(jug);
        if (jug.GetComponent<LanzadorLatas>() == null) Undo.AddComponent<LanzadorLatas>(jug);
        if (hud == null) rep.Add("AVISO: no hay HUDSangre en Nivel2");
        return jug;
    }

    // ── Busqueda ─────────────────────────────────────────────────────────────
    static GameObject Buscar(string nombre)
    {
        foreach (var t in Resources.FindObjectsOfTypeAll<Transform>())
            if (t.name == nombre && t.gameObject.scene == SceneManager.GetActiveScene()
                && !EditorUtility.IsPersistent(t.gameObject))
                return t.gameObject;
        return null;
    }

    static Transform BuscarEn(Transform raiz, string nombre)
    {
        if (raiz == null) return null;
        foreach (var t in raiz.GetComponentsInChildren<Transform>(true))
            if (t.name == nombre) return t;
        return null;
    }

    static void Apagar(GameObject go)
    {
        if (go == null) return;
        Undo.RecordObject(go, "Apagar " + go.name);
        go.SetActive(false);
    }

    static void Mover(GameObject go, float? x, float? y)
    {
        if (go == null) return;
        Undo.RecordObject(go.transform, "Mover " + go.name);
        var p = go.transform.position;
        go.transform.position = new Vector3(x ?? p.x, y ?? p.y, p.z);
    }

    // ── Constructores ────────────────────────────────────────────────────────
    static Transform Vacio(Transform padre, string nombre, Vector2 pos)
    {
        var go = new GameObject(nombre);
        go.transform.SetParent(padre, false);
        go.transform.position = new Vector3(pos.x, pos.y, 0f);
        return go.transform;
    }

    static void Zona(Transform padre, string nombre, Vector2 pos, float radio, float duracion, string texto)
    {
        var t = Vacio(padre, nombre, pos);
        var z = t.gameObject.AddComponent<ZonaMensaje>();
        z.texto = texto;
        z.radio = radio;
        z.duracion = duracion;
    }

    static void Solido(Transform padre, Sprite sprite, string nombre, Vector2 pos, Vector2 tam, Color color)
    {
        var t = Vacio(padre, nombre, pos);
        t.localScale = new Vector3(tam.x, tam.y, 1f);
        var sr = t.gameObject.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.color = color;
        sr.sortingOrder = 1;
        t.gameObject.AddComponent<BoxCollider2D>();
    }

    static void Pickup(Transform padre, Sprite sprite, string nombre, Recolectable.Tipo tipo, Vector2 pos, Color color)
    {
        var raiz = Vacio(padre, nombre, pos);
        var r = raiz.gameObject.AddComponent<Recolectable>();
        r.tipo = tipo;

        var visual = new GameObject("Sprite");
        visual.transform.SetParent(raiz, false);
        Vector2 tam = tipo == Recolectable.Tipo.Fusible ? new Vector2(0.25f, 0.45f)
                    : tipo == Recolectable.Tipo.Lata    ? new Vector2(0.25f, 0.35f)
                                                          : new Vector2(0.4f, 0.3f);
        visual.transform.localScale = new Vector3(tam.x, tam.y, 1f);
        var sr = visual.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.color = color;
        sr.sortingOrder = 5;

        AgregarLuz(raiz, color, 0.9f, 2.5f);
    }

    static void AgregarLuz(Transform padre, Color color, float intensidad, float radio)
    {
        var go = new GameObject("Luz");
        go.transform.SetParent(padre, false);
        var l = go.AddComponent<Light2D>();
        l.lightType = Light2D.LightType.Point;
        l.color = color;
        l.intensity = intensidad;
        l.pointLightInnerRadius = 0f;
        l.pointLightOuterRadius = radio;
    }

    static void Cauterizar(GameObject go, List<string> rep)
    {
        if (go == null) return;
        Undo.RecordObject(go, "Tag cauterizar");
        go.tag = "ZonaCauterizacion";
        var c = go.GetComponent<Collider2D>();
        if (c != null) { Undo.RecordObject(c, "Trigger"); c.isTrigger = true; }
        else
        {
            var bc = Undo.AddComponent<BoxCollider2D>(go);
            bc.isTrigger = true;
            bc.size = new Vector2(4f, 0.8f);
        }
    }

    static void ConfigurarEscondite(GameObject go, Vector2 pos, Vector2 escala)
    {
        if (go == null) return;
        Undo.RecordObject(go.transform, "Escondite");
        go.transform.position = new Vector3(pos.x, pos.y, 0f);
        go.transform.localScale = new Vector3(escala.x, escala.y, 1f);
        go.tag = "Escondite";
        var c = go.GetComponent<Collider2D>();
        if (c == null) c = Undo.AddComponent<BoxCollider2D>(go);
        c.isTrigger = true;
        if (go.GetComponent<Escondite>() == null) Undo.AddComponent<Escondite>(go);
    }

    static TransicionZona Disparador(Transform padre, string nombre, Vector2 pos, Vector2 tam, int zona, PuertaConLlave puerta)
    {
        var t = Vacio(padre, nombre, pos);
        var bc = t.gameObject.AddComponent<BoxCollider2D>();
        bc.isTrigger = true;
        bc.size = tam;
        var tz = t.gameObject.AddComponent<TransicionZona>();
        tz.zonaDestino = zona;
        tz.puerta = puerta;
        return tz;
    }

    static void Nota(GameObject go, int numero, string titulo, string cuerpo, Vector2 pos, List<string> rep)
    {
        if (go == null) { rep.Add("AVISO: no se encontro Nota_" + numero); return; }
        Undo.RecordObject(go.transform, "Mover nota");
        go.transform.position = new Vector3(pos.x, pos.y, 0f);

        var nota = go.GetComponent<NotaLore>();
        if (nota == null) nota = Undo.AddComponent<NotaLore>(go);
        nota.titulo = titulo;
        nota.cuerpo = cuerpo;
        nota.numeroNota = numero;
        EditorUtility.SetDirty(nota);

        var s = go.transform.localScale;
        var bc = go.GetComponent<BoxCollider2D>();
        if (bc == null) bc = Undo.AddComponent<BoxCollider2D>(go);
        bc.isTrigger = true;
        bc.size = new Vector2(3f / Mathf.Max(0.01f, s.x), 3f / Mathf.Max(0.01f, s.y));

        foreach (var viejo in go.GetComponentsInChildren<Transform>(true))
            if (viejo.name == "Luz_Nota") { Undo.DestroyObjectImmediate(viejo.gameObject); break; }
        var l = new GameObject("Luz_Nota");
        l.transform.SetParent(go.transform, false);
        l.transform.localScale = new Vector3(1f / Mathf.Max(0.01f, s.x), 1f / Mathf.Max(0.01f, s.y), 1f);
        var luz = l.AddComponent<Light2D>();
        luz.lightType = Light2D.LightType.Point;
        luz.color = new Color(0.95f, 0.95f, 0.8f);
        luz.intensity = 0.7f;
        luz.pointLightInnerRadius = 0f;
        luz.pointLightOuterRadius = 2.2f;
    }

    static PurgadorIA Purgador(Transform padre, Sprite sprite, string nombre, Vector2 pos, float xA, float xB)
    {
        var t = Vacio(padre, nombre, pos);
        t.localScale = new Vector3(0.8f, 1.0f, 1f);
        var sr = t.gameObject.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.color = new Color(0.50f, 0.12f, 0.12f);
        sr.sortingOrder = 3;
        var rb = t.gameObject.AddComponent<Rigidbody2D>();
        rb.freezeRotation = true;
        t.gameObject.AddComponent<BoxCollider2D>();
        var ia = t.gameObject.AddComponent<PurgadorIA>();

        var a = Vacio(padre, "Patrulla_" + nombre + "_A", new Vector2(xA, pos.y));
        var b = Vacio(padre, "Patrulla_" + nombre + "_B", new Vector2(xB, pos.y));
        ia.puntosPatrulla = new[] { a, b };
        ia.patrullaLoop = true;

        var luz = new GameObject("Luz");
        luz.transform.SetParent(t, false);
        luz.transform.localScale = new Vector3(1f / 0.8f, 1f, 1f);
        var l = luz.AddComponent<Light2D>();
        l.lightType = Light2D.LightType.Point;
        l.color = new Color(1f, 0.25f, 0.2f);
        l.intensity = 0.45f;
        l.pointLightInnerRadius = 0f;
        l.pointLightOuterRadius = 1.8f;
        return ia;
    }

    static LaFosaIA CrearFosa(Transform padre, Sprite sprite, Vector2 pos)
    {
        var t = Vacio(padre, "LaFosa_N2", pos);
        t.localScale = new Vector3(1.6f, 3.2f, 1f);
        var sr = t.gameObject.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.color = new Color(0.10f, 0.02f, 0.04f);
        sr.sortingOrder = 3;
        var rb = t.gameObject.AddComponent<Rigidbody2D>();
        rb.freezeRotation = true;
        t.gameObject.AddComponent<BoxCollider2D>();
        var fosa = t.gameObject.AddComponent<LaFosaIA>();

        var luz = new GameObject("Luz");
        luz.transform.SetParent(t, false);
        luz.transform.localScale = new Vector3(1f / 1.6f, 1f / 3.2f, 1f);
        var l = luz.AddComponent<Light2D>();
        l.lightType = Light2D.LightType.Point;
        l.color = new Color(0.8f, 0.1f, 0.1f);
        l.intensity = 0.5f;
        l.pointLightInnerRadius = 0f;
        l.pointLightOuterRadius = 3.5f;
        return fosa;
    }
}

/// <summary>
/// Crea la escena EscenaFinal (caseta, texto de cierre, boton de menu) si no existe,
/// la guarda y la agrega a Build Settings. Devuelve un mensaje con el resultado.
/// Menu: Tools > Hemorragia > Crear Escena Final (automatica)
/// </summary>
public static class CrearEscenaFinalAuto
{
    const string Ruta = "Assets/Scenes/EscenaFinal.unity";

    [MenuItem("Tools/Hemorragia/Crear Escena Final (automatica)")]
    public static void Menu()
    {
        EditorUtility.DisplayDialog("Escena Final", CrearYRegistrar(), "OK");
    }

    public static string CrearYRegistrar()
    {
        string volver = SceneManager.GetActiveScene().path;
        string msg;

        if (!File.Exists(Ruta))
        {
            if (!EditorSceneManager.SaveOpenScenes())
                return "Escena final NO creada (cancelaste el guardado).";

            var escena = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            foreach (var l in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
                Object.DestroyImmediate(l.gameObject);

            CrearEscenaFinal.Crear();

            // Conectar el control de la escena final (el creador original no asigna los campos)
            var ctrl = Object.FindFirstObjectByType<ControlEscenaFinal>();
            if (ctrl != null)
            {
                foreach (var t in Resources.FindObjectsOfTypeAll<UnityEngine.UI.Text>())
                {
                    if (t.gameObject.scene != escena) continue;
                    if (t.name == "Texto_Final") ctrl.textoFinal = t;
                    if (t.name == "Texto_Creditos") ctrl.textoCreditos = t;
                }
                foreach (var b in Resources.FindObjectsOfTypeAll<UnityEngine.UI.Button>())
                    if (b.gameObject.scene == escena && b.name == "BotonMenu") ctrl.botonMenu = b;
                EditorUtility.SetDirty(ctrl);
            }

#if ENABLE_INPUT_SYSTEM
            // Con el Input System nuevo el modulo clasico de UI da error
            var es = Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>();
            if (es != null)
            {
                var viejo = es.GetComponent<UnityEngine.EventSystems.StandaloneInputModule>();
                if (viejo != null) Object.DestroyImmediate(viejo);
                if (es.GetComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>() == null)
                    es.gameObject.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
            }
#endif

            EditorSceneManager.SaveScene(escena, Ruta);
            msg = "Escena final creada (Assets/Scenes/EscenaFinal.unity) y agregada a Build Settings.";
        }
        else
        {
            msg = "La escena final ya existia; se verifico Build Settings.";
        }

        RegistrarEnBuild();

        if (!string.IsNullOrEmpty(volver) && SceneManager.GetActiveScene().path != volver)
            EditorSceneManager.OpenScene(volver);
        return msg;
    }

    static void RegistrarEnBuild()
    {
        var lista = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        foreach (var s in lista)
            if (s.path == Ruta) { s.enabled = true; EditorBuildSettings.scenes = lista.ToArray(); return; }
        lista.Add(new EditorBuildSettingsScene(Ruta, true));
        EditorBuildSettings.scenes = lista.ToArray();
    }
}
