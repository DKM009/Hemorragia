using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Final del Nivel 2: Jeremy cae a la caldera. Pantalla roja, el grito se vuelve tos
/// y se carga la escena final (Jeremy despierta en la caseta con la mascarilla).
/// Pon este script en un trigger sobre la compuerta de la caldera.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class CaidaCaldera : MonoBehaviour
{
    public string escenaFinal = "EscenaFinal";

    bool activado;

    void Awake()
    {
        GetComponent<Collider2D>().isTrigger = true;
    }

    void OnTriggerEnter2D(Collider2D otro)
    {
        if (activado || !otro.CompareTag("Player")) return;
        activado = true;
        StartCoroutine(Secuencia(otro.transform));
    }

    IEnumerator Secuencia(Transform jugador)
    {
        // Congelar al jugador y apagar a los enemigos para que nada interrumpa la caida
        var mov = jugador.GetComponent<MovimientoJugador>();
        if (mov != null) mov.enabled = false;
        var sangre = jugador.GetComponent<SistemaSangre>();
        if (sangre != null) sangre.enabled = false;
        var rb = jugador.GetComponent<Rigidbody2D>();
        if (rb != null) { rb.linearVelocity = Vector2.zero; rb.bodyType = RigidbodyType2D.Kinematic; }
        foreach (var c in jugador.GetComponents<Collider2D>()) c.enabled = false;
        foreach (var p in FindObjectsByType<PurgadorIA>(FindObjectsSortMode.None)) p.gameObject.SetActive(false);
        foreach (var f in FindObjectsByType<LaFosaIA>(FindObjectsSortMode.None)) f.gameObject.SetActive(false);

        var cam = Camera.main != null ? Camera.main.GetComponent<CamaraSeguimiento>() : null;
        if (cam != null) cam.AjustarIntensidad(5f);

        // Overlay rojo
        var canvasGO = new GameObject("Overlay_Caida");
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 900;

        var imgGO = new GameObject("Fondo");
        imgGO.transform.SetParent(canvasGO.transform, false);
        var img = imgGO.AddComponent<Image>();
        img.color = new Color(0.55f, 0f, 0f, 0f);
        img.raycastTarget = false;
        var rt = img.rectTransform;
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;

        var txtGO = new GameObject("Texto");
        txtGO.transform.SetParent(canvasGO.transform, false);
        var txt = txtGO.AddComponent<Text>();
        txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        txt.fontSize = 40;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.color = new Color(1f, 0.9f, 0.9f, 0f);
        txt.raycastTarget = false;
        var trt = txt.rectTransform;
        trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
        trt.offsetMin = Vector2.zero; trt.offsetMax = Vector2.zero;
        txt.text = "AAAAAAAAAAAH!";

        // Caida: el jugador baja y la pantalla se vuelve roja
        Vector3 ini = jugador.position;
        float dur = 2.2f, t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / dur;
            float e = t * t;
            jugador.position = ini + Vector3.down * (7f * e);
            img.color = new Color(0.55f, 0f, 0f, Mathf.Clamp01(t * 1.3f) * 0.95f);
            txt.color = new Color(1f, 0.9f, 0.9f, Mathf.Clamp01(t * 3f));
            yield return null;
        }

        yield return new WaitForSeconds(0.8f);

        // El grito se vuelve tos
        txt.text = "AAAAH...  *cof*  *cof*";
        yield return new WaitForSeconds(0.9f);
        txt.text = "*cof*  ...  *cof*";

        // A negro
        t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / 1.6f;
            img.color = Color.Lerp(new Color(0.55f, 0f, 0f, 0.95f), Color.black, t);
            yield return null;
        }
        yield return new WaitForSeconds(0.6f);

        string destino = Application.CanStreamedLevelBeLoaded(escenaFinal) ? escenaFinal : "MenuPrincipal";
        SceneManager.LoadScene(destino);
    }
}
