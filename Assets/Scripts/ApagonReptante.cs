using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

/// <summary>
/// "Apagon" tipo Five Nights: SOLO en el Patio y el Pasillo del Nivel 1 (los dos primeros cuartos, sin enemigos).
/// La luz parpadea y se va. En la oscuridad el Reptante avanza hacia Jeremy; si Jeremy se mueve mientras esta oscuro,
/// lo atrapa y muere. Al volver la luz el Reptante queda congelado donde esta. Pasado el Pasillo desaparece para siempre.
/// Se instala solo (sin tocar la escena).
/// </summary>
public class ApagonReptante : MonoBehaviour
{
    // ── Ajustes ──────────────────────────────────────────────────────────────
    const float XFinPasillo      = 70.5f;   // pasada la puerta del Pasillo ya no sale
    const float XInicioPasillo   = 25.5f;
    const float PrimerApagon     = 4.0f;    // seg. desde que empieza el nivel
    const float EntreApagones    = 7.0f;    // + hasta 3 seg al azar
    const float DuracionParpadeo = 1.1f;
    const float DuracionOscuro   = 3.4f;    // + hasta 1.2 seg al azar
    const float Gracia           = 0.45f;   // al irse la luz puedes terminar de frenar
    const float DistanciaInicial = 13f;
    static readonly float[] Distancias = { 5f, 3.6f, 2.4f };   // distancia al encenderse la luz, 1.er/2.o/3.er apagon
    int apagones;

    enum Fase { Luz, Parpadeo, Oscuro, Ataque, Fin }
    Fase fase = Fase.Luz;
    float t, duracion, tAnim, tParp;
    bool ruidoLuz = true;
    int zona;                      // 0 = Patio, 1 = Pasillo
    bool zona2Iniciada;

    Transform jugador, rep;
    Rigidbody2D jrb;
    SistemaSangre sangre;
    SpriteRenderer sr;
    Sprite[] idle, mover, ataque;
    Material matSinLuz, matLuz;
    Vector2 objetivoPos, origenMov;

    readonly Dictionary<Light2D, float> basesLuz = new Dictionary<Light2D, float>();
    float factor = 1f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Registrar()
    {
        SceneManager.sceneLoaded += (e, m) =>
        {
            if (e.name != "Nivel1") return;
            if (FindFirstObjectByType<ApagonReptante>() != null) return;
            new GameObject("ApagonReptante").AddComponent<ApagonReptante>();
        };
    }

    void OnEnable()  { SistemaSangre.AlResetear += AlReiniciar; }
    void OnDisable() { SistemaSangre.AlResetear -= AlReiniciar; }

    void Start()
    {
        var banco = Resources.Load<BancoEnemigos>("BancoEnemigos");
        if (banco == null || banco.reptanteIdle == null) { enabled = false; return; }
        idle   = Cortar(banco, banco.reptanteIdle);
        mover  = Cortar(banco, banco.reptanteCaminar);
        ataque = Cortar(banco, banco.reptanteAtaque);

        var go = new GameObject("Reptante_Apagon");
        rep = go.transform;
        sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = idle[0]; sr.sortingOrder = 8;
        matLuz = sr.sharedMaterial;
        var sh = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
        if (sh != null) matSinLuz = new Material(sh);
        rep.gameObject.SetActive(false);
        Reiniciar(true);
    }

    static Sprite[] Cortar(BancoEnemigos b, Texture2D tex)
    {
        if (tex == null) return null;
        tex.filterMode = FilterMode.Point;
        int n = Mathf.Max(1, tex.width / b.celda);
        var arr = new Sprite[n];
        float pivY = b.pixelesBajoPies / b.celda;
        for (int i = 0; i < n; i++)
            arr[i] = Sprite.Create(tex, new Rect(i * b.celda, 0, b.celda, b.celda), new Vector2(0.5f, pivY), b.ppu, 0, SpriteMeshType.FullRect);
        return arr;
    }

    void AlReiniciar() { Reiniciar(false); }

    void Reiniciar(bool inicio)
    {
        RestaurarLuces();
        fase = Fase.Luz; t = 0f; duracion = inicio ? PrimerApagon : 3f; ruidoLuz = true;
        if (rep != null) { rep.gameObject.SetActive(false); }
        zona2Iniciada = false; zona = 0; apagones = 0;
        colocar = true;
    }
    bool colocar;

    // ── Utilidades ───────────────────────────────────────────────────────────
    bool BuscarJugador()
    {
        if (jugador != null) return true;
        var p = GameObject.FindGameObjectWithTag("Player");
        if (p == null) return false;
        jugador = p.transform; jrb = p.GetComponent<Rigidbody2D>(); sangre = p.GetComponent<SistemaSangre>();
        return true;
    }

    bool Moviendose(bool conGracia)
    {
        var k = Keyboard.current;
        if (k != null)
        {
            if (k.aKey.isPressed || k.dKey.isPressed || k.leftArrowKey.isPressed || k.rightArrowKey.isPressed ||
                k.spaceKey.isPressed || k.wKey.isPressed || k.upArrowKey.isPressed) return true;
        }
        if (!conGracia && jrb != null && Mathf.Abs(jrb.linearVelocity.x) > 0.35f) return true;
        return false;
    }

    void ColocarDetras()
    {
        float px = jugador.position.x;
        float x = px - DistanciaInicial;
        float y = jugador.position.y;
        var hits = Physics2D.RaycastAll(new Vector2(px - 2f, jugador.position.y + 3f), Vector2.down, 12f);
        foreach (var h in hits)
        {
            if (h.collider == null || h.collider.isTrigger || h.collider.transform.IsChildOf(jugador)) continue;
            y = h.point.y; break;
        }
        // si el suelo detras es distinto, se apoya en el mismo nivel del jugador
        var col = jugador.GetComponent<Collider2D>();
        y = col != null ? col.bounds.min.y : y;
        rep.position = new Vector3(x, y, 0f);
        sr.flipX = false;
        rep.gameObject.SetActive(true);
        Congelar();
    }

    void Congelar()
    {
        sr.sharedMaterial = matLuz;
        sr.color = Color.white;
        sr.sprite = idle[0];
    }

    // ── Luces ────────────────────────────────────────────────────────────────
    void RefrescarLuces()
    {
        foreach (var l in Object.FindObjectsByType<Light2D>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (l == null || basesLuz.ContainsKey(l)) continue;
            var lp = l.GetComponent<LuzParpadeante>();
            basesLuz[l] = lp != null ? lp.intensidadBase : l.intensity;
        }
    }

    void RestaurarLuces()
    {
        factor = 1f;
        foreach (var kv in basesLuz) if (kv.Key != null) kv.Key.intensity = kv.Value;
        enLuces = false;
    }
    bool enLuces;

    void LateUpdate()
    {
        if (factor >= 0.999f) { if (enLuces) RestaurarLuces(); return; }
        enLuces = true;
        foreach (var kv in basesLuz)
        {
            var l = kv.Key; if (l == null) continue;
            float f = factor;
            if (jugador != null && l.transform.IsChildOf(jugador)) f = Mathf.Max(f, 0.3f);
            else if (l.lightType == Light2D.LightType.Global) f = Mathf.Max(f, 0.05f);
            l.intensity = kv.Value * f;
        }
    }

    // ── Logica principal ─────────────────────────────────────────────────────
    void Update()
    {
        if (fase == Fase.Fin) return;
        if (!BuscarJugador()) return;
        if (GestorJuego.Instancia != null && GestorJuego.Instancia.EstaEnGameOver) { RestaurarLuces(); return; }
        if (colocar) { ColocarDetras(); colocar = false; RefrescarLuces(); }

        float px = jugador.position.x;
        // pasado el Pasillo: se acaba para siempre
        if (px > XFinPasillo) { Terminar(); return; }
        if (px >= XInicioPasillo && !zona2Iniciada)
        {
            zona2Iniciada = true; zona = 1; apagones = 0;
            fase = Fase.Luz; t = 0f; duracion = 5f; ruidoLuz = true;
            RestaurarLuces();
            ColocarDetras();
        }
        if (px < XInicioPasillo - 0.5f && zona2Iniciada) { /* volvio al patio: se queda donde esta */ }

        t += Time.deltaTime;
        tAnim += Time.deltaTime;

        switch (fase)
        {
            case Fase.Luz:
                if (t >= duracion)
                {
                    RefrescarLuces();
                    fase = Fase.Parpadeo; t = 0f; duracion = DuracionParpadeo; tParp = 0f;
                    AudioAuto.Rugido(rep.position, false);
                }
                break;

            case Fase.Parpadeo:
                tParp -= Time.deltaTime;
                if (tParp <= 0f) { factor = Random.value < 0.5f ? 1f : 0.1f; tParp = Random.Range(0.05f, 0.16f); }
                if (t >= duracion) IniciarOscuridad();
                break;

            case Fase.Oscuro:
                factor = 0f;
                MoverReptante();
                if (t >= Gracia && Moviendose(false)) { Atacar(); break; }
                if (t >= duracion) Encender();
                break;

            case Fase.Ataque:
                factor = 0f;
                int i = Mathf.Clamp(Mathf.FloorToInt(t / 0.55f * ataque.Length), 0, ataque.Length - 1);
                sr.sprite = ataque[i];
                if (t >= 0.55f)
                {
                    RestaurarLuces();
                    fase = Fase.Luz; t = 0f; duracion = 3f; colocar = true; rep.gameObject.SetActive(false);
                    if (sangre != null) sangre.RecibirDano(9999f);
                    else if (GestorJuego.Instancia != null) GestorJuego.Instancia.GameOver();
                }
                break;
        }
    }

    void IniciarOscuridad()
    {
        fase = Fase.Oscuro; t = 0f;
        duracion = DuracionOscuro + Random.Range(0f, 1.2f);
        factor = 0f;
        if (matSinLuz != null) { sr.sharedMaterial = matSinLuz; sr.color = new Color(0.55f, 0.1f, 0.1f, 1f); }
        // al volver la luz queda visible atras de Jeremy, cada vez mas cerca
        origenMov = rep.position;
        float dx = jugador.position.x - rep.position.x;
        float lado = dx >= 0f ? 1f : -1f;                       // lado en el que esta el, respecto a Jeremy
        float distMeta = apagones < Distancias.Length ? Distancias[apagones] : 0.8f;
        apagones++;
        float distAhora = Mathf.Abs(dx);
        float nuevaDist = Mathf.Min(distAhora, distMeta);       // nunca se aleja
        if (distAhora > distMeta) nuevaDist = distMeta;
        float nuevoX = jugador.position.x - lado * nuevaDist;
        objetivoPos = new Vector2(nuevoX, rep.position.y);
        tAnim = 0f;
    }

    void MoverReptante()
    {
        float dx = jugador.position.x - rep.position.x;
        float k = Mathf.Clamp01(t / Mathf.Max(0.5f, Mathf.Min(duracion - 0.4f, 2.2f)));
        k = k * k * (3f - 2f * k);
        var p = Vector2.Lerp(origenMov, objetivoPos, k);
        rep.position = new Vector3(p.x, rep.position.y, 0f);
        sr.flipX = dx < 0f;
        sr.sprite = mover[Mathf.FloorToInt(tAnim * 9f) % mover.Length];
        // si llega hasta Jeremy en la oscuridad, lo atrapa aunque este quieto
        if (Mathf.Abs(dx) < 1.0f) Atacar();
    }

    void Atacar()
    {
        if (fase == Fase.Ataque) return;
        fase = Fase.Ataque; t = 0f;
        float dx = jugador.position.x - rep.position.x;
        rep.position = new Vector3(jugador.position.x - Mathf.Sign(dx == 0f ? 1f : dx) * 1.2f, rep.position.y, 0f);
        sr.flipX = dx < 0f;
        AudioAuto.Rugido(rep.position, true);
    }

    void Encender()
    {
        RestaurarLuces();
        fase = Fase.Luz; t = 0f; duracion = EntreApagones + Random.Range(0f, 3f);
        Congelar();
        sr.flipX = jugador.position.x < rep.position.x;
    }

    void Terminar()
    {
        fase = Fase.Fin;
        RestaurarLuces();
        if (rep != null) rep.gameObject.SetActive(false);
        Destroy(gameObject, 0.1f);
    }
}
