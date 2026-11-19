using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

/// <summary>
/// Cuarto de la Emboscada (Nivel 1) mas largo y mas dificil, sin tocar la escena:
///  - la puerta de salida queda al final del cuarto (el cuarto crece hacia la derecha),
///  - hay mas Purgadores y objetos,
///  - los Purgadores estan escondidos: cuando Jeremy se acerca (unos 2 pasos) salen de la nada y se lanzan a atacar.
/// Para ajustar: cambia los numeros de arriba.
/// </summary>
public class EmboscadaReforzada : MonoBehaviour
{
    const float Alargar = 14f;          // cuantos metros mas largo queda el cuarto
    const float DistanciaSalto = 4.5f;  // a esta distancia sale y se lanza

    Transform cuarto;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Registrar()
    {
        SceneManager.sceneLoaded += (e, m) =>
        {
            if (e.name != "Nivel1" || FindFirstObjectByType<EmboscadaReforzada>() != null) return;
            new GameObject("EmboscadaReforzada").AddComponent<EmboscadaReforzada>();
        };
    }

    Transform Hallar(string nombre)
    {
        foreach (var t in cuarto.GetComponentsInChildren<Transform>(true)) if (t.name == nombre) return t;
        return null;
    }

    void Mover(string nombre, float dx)
    {
        var t = Hallar(nombre); if (t != null) t.position += new Vector3(dx, 0f, 0f);
    }

    void Estirar(string nombre)
    {
        var t = Hallar(nombre); if (t == null) return;
        var sr = t.GetComponent<SpriteRenderer>();
        if (sr != null && sr.drawMode != SpriteDrawMode.Simple)
        {
            sr.size = new Vector2(sr.size.x + Alargar, sr.size.y);
        }
        else
        {
            var ls = t.localScale;
            float w = (sr != null && sr.sprite != null) ? Mathf.Abs(ls.x) * sr.sprite.bounds.size.x : Mathf.Abs(ls.x);
            float k = (w + Alargar) / Mathf.Max(0.01f, w);
            t.localScale = new Vector3(ls.x * k, ls.y, ls.z);
        }
        t.position += new Vector3(Alargar * 0.5f, 0f, 0f);
    }

    void Awake()
    {
        var go = GameObject.Find("Cuarto4_Emboscada");
        if (go == null) { Destroy(gameObject); return; }
        cuarto = go.transform;

        // 1) la salida y la pared al final del cuarto nuevo
        foreach (var n in new[] { "Pared_Der", "Arte_Pared_Der", "Puerta_Salida_N2", "Arte_Puerta_Salida_N2", "Trigger_SalidaN2", "Luz_Salida" })
            Mover(n, Alargar);

        // 2) piso y techo mas largos
        foreach (var n in new[] { "Suelo", "Arte_Suelo", "Techo", "Arte_Techo" }) Estirar(n);

        // 3) limites de la camara
        var lim = Hallar("Limites_Cuarto4");
        var bc = lim != null ? lim.GetComponent<BoxCollider2D>() : null;
        if (bc != null) { bc.size = new Vector2(bc.size.x + Alargar, bc.size.y); bc.offset += new Vector2(Alargar * 0.5f, 0f); }
    }

    IEnumerator Start()
    {
        if (cuarto == null) yield break;
        yield return null; yield return null;

        // 4) luces para la parte nueva
        foreach (var n in new[] { "Luz_Ambiental", "Luz_Roja_Alarma" })
        {
            var t = Hallar(n); if (t == null) continue;
            var c = Instantiate(t.gameObject, t.parent); c.name = n + "_2";
            c.transform.position = t.position + new Vector3(Alargar, 0f, 0f);
        }

        // 5) fondo: copia espejada a la derecha
        SpriteRenderer fondo = null;
        foreach (var r in cuarto.GetComponentsInChildren<SpriteRenderer>(true))
            if (r.name.StartsWith("Gemini_Generated_Image") || r.name == "Fondo_Emboscada") { fondo = r; break; }
        if (fondo != null)
        {
            var c = Instantiate(fondo.gameObject, fondo.transform.parent); c.name = "Fondo_Emboscada_2";
            var sc = c.GetComponent<SpriteRenderer>(); sc.flipX = !fondo.flipX;
            c.transform.position = fondo.transform.position + new Vector3(fondo.bounds.size.x - 0.05f, 0f, 0f);
            var d = Instantiate(fondo.gameObject, fondo.transform.parent); d.name = "Fondo_Emboscada_3";
            d.transform.position = c.transform.position + new Vector3(fondo.bounds.size.x - 0.05f, 0f, 0f);
        }

        // 6) mas objetos en la parte nueva
        Clonar("Arte_Escombro_A", 121.5f); Clonar("Arte_Escombro_B", 127f); Clonar("Arte_Escombro_C", 130f);
        Clonar("Lata_7", 122.5f); Clonar("Lata_7", 128.5f); Clonar("Venda_4", 125f);

        // 7) mas Purgadores
        var fuente = Hallar("Purgador_B");
        if (fuente != null)
        {
            float y = fuente.position.y;
            foreach (var x in new[] { 121f, 128f })
            {
                var c = Instantiate(fuente.gameObject, fuente.parent);
                c.name = "Purgador_Extra_" + (int)x;
                c.transform.position = new Vector3(x, y, fuente.position.z);
                var ia = c.GetComponent<PurgadorIA>();
                if (ia != null)
                {
                    var a = new GameObject("Patrulla_" + (int)x + "_A").transform; a.SetParent(fuente.parent, true); a.position = new Vector3(x - 2f, y, 0f);
                    var b = new GameObject("Patrulla_" + (int)x + "_B").transform; b.SetParent(fuente.parent, true); b.position = new Vector3(x + 2f, y, 0f);
                    ia.puntosPatrulla = new[] { a, b };
                }
            }
        }
        yield return null;

        // 8) todos los Purgadores del cuarto se esconden y saltan al acercarse
        foreach (var ia in cuarto.GetComponentsInChildren<PurgadorIA>(true))
            if (ia.GetComponent<AcechoSalto>() == null) { var ac = ia.gameObject.AddComponent<AcechoSalto>(); ac.distancia = DistanciaSalto; }
    }

    void Clonar(string nombre, float x)
    {
        var t = Hallar(nombre); if (t == null) return;
        var c = Instantiate(t.gameObject, t.parent);
        c.name = nombre + "_Ext";
        c.transform.position = new Vector3(x, t.position.y, t.position.z);
        c.SetActive(true);
        if (c.GetComponent<Recolectable>() != null) ObjetosArte.Asentar(c.transform);
    }
}

/// <summary>
/// El Purgador esta escondido (invisible e inofensivo). Cuando Jeremy se acerca, sale de la nada, ruge y se lanza sobre el.
/// Si Jeremy muere, vuelve a esconderse en su lugar.
/// </summary>
[RequireComponent(typeof(PurgadorIA))]
public class AcechoSalto : MonoBehaviour
{
    public float distancia = 4.5f;

    PurgadorIA ia;
    Rigidbody2D rb;
    SpriteRenderer[] srs;
    Collider2D[] cols;
    Vector3 inicio;
    float vPat, vRas, vPer, tBoost;
    bool dormido, pendiente;

    void Awake()
    {
        ia = GetComponent<PurgadorIA>();
        rb = GetComponent<Rigidbody2D>();
        srs = GetComponentsInChildren<SpriteRenderer>(true);
        cols = GetComponents<Collider2D>();
        inicio = transform.position;
        vPat = ia.velocidadPatrulla; vRas = ia.velocidadRastreo; vPer = ia.velocidadPersecucion;
        Dormir();
    }

    void OnEnable()  { SistemaSangre.AlResetear += OnReset; }
    void OnDisable() { SistemaSangre.AlResetear -= OnReset; }
    void OnReset() { pendiente = true; }

    void Dormir()
    {
        dormido = true; tBoost = 0f;
        transform.position = inicio;
        if (rb != null) { rb.linearVelocity = Vector2.zero; rb.simulated = false; }
        foreach (var c in cols) if (c != null) c.enabled = false;
        foreach (var s in srs) if (s != null) s.enabled = false;
        ia.velocidadPatrulla = vPat; ia.velocidadRastreo = vRas; ia.velocidadPersecucion = vPer;
        ia.enabled = false;
    }

    void Despertar(float dx)
    {
        dormido = false;
        if (rb != null) rb.simulated = true;
        foreach (var c in cols) if (c != null) c.enabled = true;
        foreach (var s in srs) if (s != null) s.enabled = true;
        ia.enabled = true;
        ia.anguloVision = 360f; ia.radioVision = Mathf.Max(ia.radioVision, 10f); ia.radioOlfato = Mathf.Max(ia.radioOlfato, 9f);
        // se lanza: arco hacia Jeremy y mucha velocidad un momento
        ia.velocidadRastreo = vRas * 2.4f; ia.velocidadPersecucion = vPer * 2.4f; tBoost = 1.0f;
        if (rb != null) rb.linearVelocity = new Vector2(Mathf.Sign(dx) * 7f, 6.5f);
        AudioAuto.Rugido(transform.position, true);
    }

    void Update()
    {
        if (pendiente) { pendiente = false; Dormir(); return; }

        if (dormido)
        {
            var j = JugadorRef.Transform; if (j == null) return;
            float dx = j.position.x - transform.position.x, dy = j.position.y - transform.position.y;
            if (Mathf.Abs(dx) < distancia && Mathf.Abs(dy) < 3f) Despertar(dx);
            return;
        }
        if (tBoost > 0f)
        {
            tBoost -= Time.deltaTime;
            if (tBoost <= 0f) { ia.velocidadRastreo = vRas; ia.velocidadPersecucion = vPer; }
        }
    }
}
