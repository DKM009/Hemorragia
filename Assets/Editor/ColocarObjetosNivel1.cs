using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

/// <summary>
/// Arma la jugabilidad del Nivel 1 de Hemorragia con un solo clic:
///   - Arregla el recorrido (cajas solidas, escalones a los tubos, barrera de La Fosa).
///   - Convierte los marcadores en Purgadores y La Fosa reales.
///   - Coloca vendas, latas, fusible, notas, mensajes de tutorial y la puerta con fusible.
///   - Crea GestorJuego y GestorRuido si faltan y conecta todo en GestorNivel1.
/// Se puede ejecutar varias veces: reemplaza lo que creo la vez anterior.
/// Menu: Tools > Hemorragia > Nivel 1 - Colocar objetos y jugabilidad
/// </summary>
public static class ColocarObjetosNivel1
{
    const string RutaSprite = "Assets/Sprites/Greybox/Cuadrado.png";
    const string NombreContenedor = "Gameplay_Nivel1";

    [MenuItem("Tools/Hemorragia/Nivel 1 - Colocar objetos y jugabilidad")]
    public static void Colocar()
    {
        if (SceneManager.GetActiveScene().name != "Nivel1")
        {
            EditorUtility.DisplayDialog("Nivel 1",
                "Abre la escena Nivel1 (Assets/Scenes/Nivel1.unity) y vuelve a ejecutar.", "OK");
            return;
        }

        var gestor = Object.FindFirstObjectByType<GestorNivel1>(FindObjectsInactive.Include);
        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(RutaSprite);
        var c1 = Buscar("Cuarto1_Patio");
        var c2 = Buscar("Cuarto2_Pasillo");
        var c3 = Buscar("Cuarto3_Bodega");
        var c4 = Buscar("Cuarto4_Emboscada");

        if (gestor == null || sprite == null || c1 == null || c2 == null || c3 == null || c4 == null)
        {
            EditorUtility.DisplayDialog("Nivel 1",
                "Falta algo en la escena:\n" +
                $"GestorNivel1: {(gestor ? "ok" : "NO")}\n" +
                $"Sprite greybox: {(sprite ? "ok" : "NO (genera un cuarto greybox primero)")}\n" +
                $"Cuartos 1-4: {(c1 && c2 && c3 && c4 ? "ok" : "NO")}", "OK");
            return;
        }

        var rep = new List<string>();
        Undo.IncrementCurrentGroup();
        int grupo = Undo.GetCurrentGroup();

        Color metal   = new Color(0.38f, 0.38f, 0.42f);
        Color madera  = new Color(0.45f, 0.32f, 0.18f);
        Color venda   = new Color(0.95f, 0.88f, 0.88f);
        Color lata    = new Color(0.75f, 0.78f, 0.85f);
        Color fusible = new Color(1f, 0.85f, 0.2f);

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

        // ── 2. Jugador ───────────────────────────────────────────────────────
        var sangre = Object.FindFirstObjectByType<SistemaSangre>(FindObjectsInactive.Include);
        if (sangre != null)
        {
            var go = sangre.gameObject;
            if (go.GetComponent<InventarioJugador>() == null) Undo.AddComponent<InventarioJugador>(go);
            if (go.GetComponent<LanzadorLatas>() == null) Undo.AddComponent<LanzadorLatas>(go);

            var hud = Object.FindFirstObjectByType<HUDSangre>(FindObjectsInactive.Include);
            if (hud != null && sangre.hud == null)
            {
                Undo.RecordObject(sangre, "Asignar HUD");
                sangre.hud = hud;
                EditorUtility.SetDirty(sangre);
                rep.Add("HUD de sangre conectado al jugador");
            }
            if (hud != null && hud.rellenoBarra == null)
                rep.Add("AVISO: HUDSangre no tiene 'Relleno Barra' asignado (la barra no se vera)");
            if (hud == null)
                rep.Add("AVISO: no hay HUDSangre en la escena");
        }
        else
        {
            rep.Add("AVISO: no se encontro SistemaSangre en el Player");
        }

        // ── 3. Cuarto 1: Patio ───────────────────────────────────────────────
        var k1 = Contenedor(c1);
        Zona(k1, "Msg_Controles", new Vector2(-11f, -3f), 5f, 8f,
            "A / D mover   ·   Shift correr   ·   Espacio saltar   ·   E interactuar   ·   Q lanzar lata");
        Zona(k1, "Msg_Sangre", new Vector2(-3f, -3.5f), 3f, 8f,
            "Jeremy se desangra. Correr lo desangra mas rapido y deja charcos que los Purgadores siguen.");
        Zona(k1, "Msg_Salida", new Vector2(10f, -3.5f), 3f, 5f,
            "La puerta de la nave esta al fondo.");
        Pickup(k1, sprite, "Lata_1", Recolectable.Tipo.Lata, new Vector2(-8f, -3.75f), lata);
        Pickup(k1, sprite, "Venda_1", Recolectable.Tipo.Venda, new Vector2(9f, -3.75f), venda);
        Nota(c1, "Nota_0", 0, "Turno 14",
            "La herida no cierra. Si corres, sangras mas rapido. Busca el calor de las tuberias: cauterizan.", null);

        // ── 4. Cuarto 2: Pasillo ─────────────────────────────────────────────
        var k2 = Contenedor(c2);
        // Las cajas de metal eran decorativas: ahora son solidas para poder subir el muro
        SolidificarCaja(c2, "Caja_Metal_A");
        Solido(k2, sprite, "Escalon_Muro", new Vector2(-7.9f, -1.45f), new Vector2(0.9f, 0.9f), metal * 1.1f);
        // Escalones para llegar a la tuberia caliente (queda a 2.6 de altura)
        Solido(k2, sprite, "Escalon_Tubo_A", new Vector2(-5.4f, -2.55f), new Vector2(1.4f, 0.9f), metal * 0.9f);
        Solido(k2, sprite, "Escalon_Tubo_B", new Vector2(-4.0f, -2.1f), new Vector2(1.2f, 1.8f), metal);
        Zona(k2, "Msg_Muro", new Vector2(-12f, -2.5f), 4f, 7f,
            "Un muro bloquea el pasillo. Sube por las cajas.");
        Zona(k2, "Msg_Tubo", new Vector2(-3f, -2.5f), 3f, 8f,
            "El tubo caliente cauteriza: sube y quedate encima unos segundos.");
        Pickup(k2, sprite, "Lata_2", Recolectable.Tipo.Lata, new Vector2(-10.5f, -2.75f), lata);
        Pickup(k2, sprite, "Venda_2", Recolectable.Tipo.Venda, new Vector2(9f, -2.75f), venda);
        Pickup(k2, sprite, "Lata_3", Recolectable.Tipo.Lata, new Vector2(6f, -2.75f), lata);
        Nota(c2, "Nota_1", 1, "Pared del pasillo",
            "Los Purgadores huelen la sangre. Cada gota en el suelo es un mapa hacia ti.", new Vector2(-11f, -2.85f));
        Nota(c2, "Nota_2", 2, "Sobre el tubo",
            "Aqui el tubo aun quema. Quedate encima y la herida se cierra... pero no te quedes demasiado.", null);

        // ── 5. Cuarto 3: Bodega ──────────────────────────────────────────────
        var k3 = Contenedor(c3);
        Zona(k3, "Msg_Bodega", new Vector2(-8f, -3f), 3.5f, 9f,
            "Dos Purgadores patrullan. Lanza una lata (Q) lejos para distraerlos y escondete en la caja grande (E).");
        Zona(k3, "Msg_Fusible", new Vector2(6f, -3f), 3f, 6f,
            "La puerta de salida no tiene energia. Necesitas el fusible.");
        Pickup(k3, sprite, "Lata_4", Recolectable.Tipo.Lata, new Vector2(-7f, -3.75f), lata);
        Pickup(k3, sprite, "Lata_5", Recolectable.Tipo.Lata, new Vector2(-6.2f, -3.75f), lata);
        Pickup(k3, sprite, "Lata_6", Recolectable.Tipo.Lata, new Vector2(-5.4f, -3.75f), lata);
        Pickup(k3, sprite, "Venda_3", Recolectable.Tipo.Venda, new Vector2(-2.2f, -3.75f), venda);
        Pickup(k3, sprite, "Fusible", Recolectable.Tipo.Fusible, new Vector2(6.4f, -3.7f), fusible);
        Nota(c3, "Nota_3", 3, "Letrero de la bodega",
            "No hagas ruido. Algo duerme bajo la bodega. Una lata lanzada lejos puede salvarte la vida.",
            new Vector2(-3f, -3.85f));

        // Puerta de salida: pide fusible. El trigger se mueve detras de la puerta
        var puertaC4 = BuscarEn(c3, "Puerta_Salida_C4");
        if (puertaC4 != null)
        {
            var pl = puertaC4.GetComponent<PuertaConLlave>();
            if (pl == null) pl = Undo.AddComponent<PuertaConLlave>(puertaC4.gameObject);
            pl.nombreItem = "Fusible";
            EditorUtility.SetDirty(pl);
        }
        else rep.Add("AVISO: no se encontro Puerta_Salida_C4");
        var trigC4 = BuscarEn(c3, "Trigger_SalidaC4");
        if (trigC4 != null)
        {
            Undo.RecordObject(trigC4, "Mover Trigger_SalidaC4");
            trigC4.localPosition = new Vector3(9.9f, trigC4.localPosition.y, trigC4.localPosition.z);
        }

        // Purgadores de la Bodega
        var pA3 = Purgador(c3, k3, "Purgador_Marcador_A", new Vector2(-2.5f, -3.5f), -4f, 0.5f, rep);
        var pB3 = Purgador(c3, k3, "Purgador_Marcador_B", null, 3.5f, 7.5f, rep);

        // ── 6. Cuarto 4: Emboscada ───────────────────────────────────────────
        var k4 = Contenedor(c4);
        Solido(k4, sprite, "Escalon_Tubo_A", new Vector2(-0.3f, -3.05f), new Vector2(1.2f, 0.9f), metal * 0.9f);
        Solido(k4, sprite, "Escalon_Tubo_B", new Vector2(1.1f, -2.6f), new Vector2(1.2f, 1.8f), metal);
        Zona(k4, "Msg_Aviso", new Vector2(-3.8f, -3f), 2.5f, 5f,
            "Algo grande se mueve al otro lado de la pared...");
        Pickup(k4, sprite, "Lata_7", Recolectable.Tipo.Lata, new Vector2(-4.4f, -3.25f), lata);
        Pickup(k4, sprite, "Venda_4", Recolectable.Tipo.Venda, new Vector2(-2.6f, -3.25f), venda);
        Pickup(k4, sprite, "Venda_5", Recolectable.Tipo.Venda, new Vector2(6.2f, -3.25f), venda);
        Nota(c4, "Nota_4", 4, "Ultima nota",
            "La Fosa no te ve: te oye. Escondete y deja de correr.", new Vector2(5.2f, -3.35f));

        // El jugador reaparece del lado correcto de la barrera
        var spawn4 = BuscarEn(c4, "Spawn_Cuarto4");
        if (spawn4 != null)
        {
            Undo.RecordObject(spawn4, "Mover Spawn_Cuarto4");
            spawn4.localPosition = new Vector3(-3.8f, -3f, 0f);
        }
        else rep.Add("AVISO: no se encontro Spawn_Cuarto4");

        var pA4 = Purgador(c4, k4, "Purgador_A", null, -3.5f, -1.5f, rep);
        var pB4 = Purgador(c4, k4, "Purgador_B", null, 3f, 5.5f, rep);

        // La Fosa
        LaFosaIA fosa = null;
        var tFosa = BuscarEn(c4, "LaFosa_Marcador");
        if (tFosa != null)
        {
            Undo.RecordObject(tFosa, "Mover La Fosa");
            tFosa.localPosition = new Vector3(-6f, -2f, tFosa.localPosition.z);
            var rb = tFosa.GetComponent<Rigidbody2D>();
            if (rb == null) rb = Undo.AddComponent<Rigidbody2D>(tFosa.gameObject);
            rb.freezeRotation = true;
            if (tFosa.GetComponent<Collider2D>() == null) Undo.AddComponent<BoxCollider2D>(tFosa.gameObject);
            fosa = tFosa.GetComponent<LaFosaIA>();
            if (fosa == null) fosa = Undo.AddComponent<LaFosaIA>(tFosa.gameObject);
            EditorUtility.SetDirty(fosa);
        }
        else rep.Add("AVISO: no se encontro LaFosa_Marcador");

        // Triggers del nivel
        ConfigTrigger(c3, "Trigger_Emboscada", TriggerCuarto.TipoTrigger.ActivarEmboscada, rep);
        ConfigTrigger(c4, "Trigger_CliMax", TriggerCuarto.TipoTrigger.ActivarClimax, rep);

        // Salida del nivel -> siguiente escena en Build Settings
        var salida = BuscarEn(c4, "Trigger_SalidaN2");
        if (salida != null)
        {
            if (salida.GetComponent<CambiarNivel>() == null) Undo.AddComponent<CambiarNivel>(salida.gameObject);
        }
        else rep.Add("AVISO: no se encontro Trigger_SalidaN2");

        // ── 7. Conectar GestorNivel1 ─────────────────────────────────────────
        Undo.RecordObject(gestor, "Conectar GestorNivel1");
        gestor.purgadoresBodega    = Limpiar(pA3, pB3);
        gestor.purgadoresEmboscada = Limpiar(pA4, pB4);
        gestor.laFosa              = fosa;
        var barrera = BuscarEn(c4, "Barrera_Fosa");
        gestor.barreraFosa = barrera != null ? barrera.gameObject : null;
        EditorUtility.SetDirty(gestor);

        Undo.CollapseUndoOperations(grupo);
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());

        string reporte =
            "Nivel 1 listo.\n\n" +
            $"Purgadores Bodega: {gestor.purgadoresBodega.Length}   Purgadores Cuarto 4: {gestor.purgadoresEmboscada.Length}\n" +
            $"La Fosa: {(fosa ? "ok" : "NO")}   Barrera: {(barrera ? "ok" : "NO")}\n" +
            (rep.Count > 0 ? "\n" + string.Join("\n", rep) + "\n" : "") +
            "\nGuarda la escena con Ctrl+S y prueba de Menu a Nivel 4.";
        Debug.Log("[ColocarObjetosNivel1] " + reporte);
        EditorUtility.DisplayDialog("Nivel 1", reporte, "OK");
    }

    // ── Busqueda (incluye objetos inactivos) ─────────────────────────────────
    static GameObject Buscar(string nombre)
    {
        foreach (var t in Resources.FindObjectsOfTypeAll<Transform>())
            if (t.name == nombre && t.gameObject.scene.IsValid() && !EditorUtility.IsPersistent(t.gameObject))
                return t.gameObject;
        return null;
    }

    static Transform BuscarEn(GameObject raiz, string nombre)
    {
        foreach (var t in raiz.GetComponentsInChildren<Transform>(true))
            if (t.name == nombre) return t;
        return null;
    }

    static Transform Contenedor(GameObject cuarto)
    {
        var viejo = BuscarEn(cuarto, NombreContenedor);
        if (viejo != null) Undo.DestroyObjectImmediate(viejo.gameObject);
        var go = new GameObject(NombreContenedor);
        Undo.RegisterCreatedObjectUndo(go, "Crear " + NombreContenedor);
        go.transform.SetParent(cuarto.transform, false);
        return go.transform;
    }

    static PurgadorIA[] Limpiar(params PurgadorIA[] lista)
    {
        var res = new List<PurgadorIA>();
        foreach (var p in lista) if (p != null) res.Add(p);
        return res.ToArray();
    }

    // ── Constructores ────────────────────────────────────────────────────────
    static void Zona(Transform padre, string nombre, Vector2 pos, float radio, float duracion, string texto)
    {
        var go = new GameObject(nombre);
        go.transform.SetParent(padre, false);
        go.transform.localPosition = new Vector3(pos.x, pos.y, 0f);
        var z = go.AddComponent<ZonaMensaje>();
        z.texto = texto;
        z.radio = radio;
        z.duracion = duracion;
    }

    static void Solido(Transform padre, Sprite sprite, string nombre, Vector2 pos, Vector2 tam, Color color)
    {
        var go = new GameObject(nombre);
        go.transform.SetParent(padre, false);
        go.transform.localPosition = new Vector3(pos.x, pos.y, 0f);
        go.transform.localScale = new Vector3(tam.x, tam.y, 1f);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.color = color;
        sr.sortingOrder = 1;
        go.AddComponent<BoxCollider2D>();
    }

    static void SolidificarCaja(GameObject cuarto, string nombre)
    {
        var t = BuscarEn(cuarto, nombre);
        if (t == null) return;
        if (t.GetComponent<Collider2D>() == null) Undo.AddComponent<BoxCollider2D>(t.gameObject);
    }

    static void Pickup(Transform padre, Sprite sprite, string nombre, Recolectable.Tipo tipo, Vector2 pos, Color color)
    {
        var raiz = new GameObject(nombre);
        raiz.transform.SetParent(padre, false);
        raiz.transform.localPosition = new Vector3(pos.x, pos.y, 0f);
        var r = raiz.AddComponent<Recolectable>();
        r.tipo = tipo;

        var visual = new GameObject("Sprite");
        visual.transform.SetParent(raiz.transform, false);
        Vector2 tam = tipo == Recolectable.Tipo.Fusible ? new Vector2(0.25f, 0.45f)
                    : tipo == Recolectable.Tipo.Lata    ? new Vector2(0.25f, 0.35f)
                                                          : new Vector2(0.4f, 0.3f);
        visual.transform.localScale = new Vector3(tam.x, tam.y, 1f);
        var sr = visual.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.color = color;
        sr.sortingOrder = 5;

        AgregarLuz(raiz.transform, color, 0.9f, 2.5f);
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

    static void Nota(GameObject cuarto, string nombre, int numero, string titulo, string cuerpo, Vector2? nuevaPos)
    {
        var t = BuscarEn(cuarto, nombre);
        if (t == null) { Debug.LogWarning("[ColocarObjetosNivel1] No se encontro " + nombre); return; }

        if (nuevaPos.HasValue)
        {
            Undo.RecordObject(t, "Mover " + nombre);
            t.localPosition = new Vector3(nuevaPos.Value.x, nuevaPos.Value.y, t.localPosition.z);
        }

        var nota = t.GetComponent<NotaLore>();
        if (nota == null) nota = Undo.AddComponent<NotaLore>(t.gameObject);
        nota.titulo = titulo;
        nota.cuerpo = cuerpo;
        nota.numeroNota = numero;
        EditorUtility.SetDirty(nota);

        // Zona de lectura de unos 3 x 3 unidades (el collider hereda la escala del objeto)
        var bc = t.GetComponent<BoxCollider2D>();
        if (bc == null) bc = Undo.AddComponent<BoxCollider2D>(t.gameObject);
        bc.isTrigger = true;
        var s = t.localScale;
        bc.size = new Vector2(3f / Mathf.Max(0.01f, s.x), 3f / Mathf.Max(0.01f, s.y));

        if (BuscarEn(t.gameObject, "Luz_Nota") == null)
        {
            var go = new GameObject("Luz_Nota");
            go.transform.SetParent(t, false);
            // La luz hereda la escala pequena de la nota: se compensa para que se vea
            go.transform.localScale = new Vector3(1f / Mathf.Max(0.01f, s.x), 1f / Mathf.Max(0.01f, s.y), 1f);
            var l = go.AddComponent<Light2D>();
            l.lightType = Light2D.LightType.Point;
            l.color = new Color(0.95f, 0.95f, 0.8f);
            l.intensity = 0.7f;
            l.pointLightInnerRadius = 0f;
            l.pointLightOuterRadius = 2.2f;
        }
    }

    static PurgadorIA Purgador(GameObject cuarto, Transform contenedor, string nombre, Vector2? nuevaPos,
                               float xA, float xB, List<string> rep)
    {
        var t = BuscarEn(cuarto, nombre);
        if (t == null) { rep.Add("AVISO: no se encontro " + nombre); return null; }

        if (nuevaPos.HasValue)
        {
            Undo.RecordObject(t, "Mover " + nombre);
            t.localPosition = new Vector3(nuevaPos.Value.x, nuevaPos.Value.y, t.localPosition.z);
        }

        var rb = t.GetComponent<Rigidbody2D>();
        if (rb == null) rb = Undo.AddComponent<Rigidbody2D>(t.gameObject);
        rb.freezeRotation = true;
        if (t.GetComponent<Collider2D>() == null) Undo.AddComponent<BoxCollider2D>(t.gameObject);

        var ia = t.GetComponent<PurgadorIA>();
        if (ia == null) ia = Undo.AddComponent<PurgadorIA>(t.gameObject);

        float y = t.localPosition.y;
        var a = new GameObject("Patrulla_" + nombre + "_A");
        a.transform.SetParent(contenedor, false);
        a.transform.localPosition = new Vector3(xA, y, 0f);
        var b = new GameObject("Patrulla_" + nombre + "_B");
        b.transform.SetParent(contenedor, false);
        b.transform.localPosition = new Vector3(xB, y, 0f);

        ia.puntosPatrulla = new[] { a.transform, b.transform };
        ia.patrullaLoop = true;
        EditorUtility.SetDirty(ia);
        return ia;
    }

    static void ConfigTrigger(GameObject cuarto, string nombre, TriggerCuarto.TipoTrigger tipo, List<string> rep)
    {
        var t = BuscarEn(cuarto, nombre);
        if (t == null) { rep.Add("AVISO: no se encontro " + nombre); return; }
        var tc = t.GetComponent<TriggerCuarto>();
        if (tc == null) tc = Undo.AddComponent<TriggerCuarto>(t.gameObject);
        tc.tipo = tipo;
        tc.soloUnaVez = true;
        EditorUtility.SetDirty(tc);
    }
}
