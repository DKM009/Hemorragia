using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Utilidades compartidas de Hemorragia: referencia cacheada al jugador,
/// lectura de teclas de interaccion y un sprite blanco generado en runtime.
/// </summary>
public static class JugadorRef
{
    static Transform _t;
    static Sprite _blanco;

    public static Transform Transform
    {
        get
        {
            if (_t == null)
            {
                var go = GameObject.FindGameObjectWithTag("Player");
                if (go != null) _t = go.transform;
            }
            return _t;
        }
    }

    public static bool EstaCerca(Vector3 punto, float radio)
    {
        var t = Transform;
        if (t == null) return false;
        return Vector2.Distance(t.position, punto) <= radio;
    }

    public static bool PulsoInteractuar()
    {
#if ENABLE_INPUT_SYSTEM
        var k = Keyboard.current;
        return k != null && (k.eKey.wasPressedThisFrame || k.fKey.wasPressedThisFrame);
#else
        return Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.F);
#endif
    }

    public static bool PulsoLanzar()
    {
#if ENABLE_INPUT_SYSTEM
        var k = Keyboard.current;
        return k != null && k.qKey.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.Q);
#endif
    }

    /// <summary>Sprite de 1x1 unidad, blanco, para objetos generados por codigo.</summary>
    public static Sprite SpriteBlanco()
    {
        if (_blanco == null)
        {
            var tex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            tex.SetPixel(0, 0, Color.white);
            tex.Apply();
            _blanco = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
        }
        return _blanco;
    }
}
