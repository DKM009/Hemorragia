using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Atajo de PRUEBA (quitalo antes de entregar): F9 hace aparecer al Reptante detras del jugador.
/// Sirve para probar el rastro de sangre sin armar todavia la sala de las dos puertas.
/// </summary>
public class PruebaEnemigos : MonoBehaviour
{
    ReptanteIA actual;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Iniciar()
    {
        var go = new GameObject("PruebaEnemigos");
        DontDestroyOnLoad(go);
        go.AddComponent<PruebaEnemigos>();
    }

    void Update()
    {
        bool f9;
#if ENABLE_INPUT_SYSTEM
        var k = Keyboard.current; f9 = k != null && k.f9Key.wasPressedThisFrame;
#else
        f9 = Input.GetKeyDown(KeyCode.F9);
#endif
        if (!f9) return;
        var j = JugadorRef.Transform;
        if (j == null) return;
        if (actual != null) Destroy(actual.gameObject);
        actual = ReptanteIA.Crear(j.position + new Vector3(-9f, 1.0f, 0f));
        Debug.Log("[PRUEBA] Reptante invocado en " + actual.transform.position);
        UIMensaje.Mostrar("[PRUEBA] Reptante invocado. Sigue tu rastro de sangre.", 3f);
    }
}
