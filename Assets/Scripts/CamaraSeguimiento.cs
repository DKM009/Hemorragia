using UnityEngine;

/// <summary>
/// Camara 2D que sigue al jugador con suavizado, respeta los limites del cuarto
/// y tiene un balanceo sutil tipo pendulo (desplazamiento + ligera inclinacion).
/// Ponerla en la Main Camera.
///
/// VALORES AFINADOS (v1.2):
///   - tiempoSuavizado 0.18: un poco mas perezoso; la camara "respira" junto al jugador.
///   - amplitudPosicion 0.05: apenas perceptible en calma; se nota al subir intensidad.
///   - amplitudRotacion 0.3: sutil, no mareante.
///   - frecuencia 0.28: oscilacion lenta, como latido lento de tension.
///   - Para momentos de tension (herida, persecucion): intensidad = 2.5 desde otro script.
/// </summary>
[RequireComponent(typeof(Camera))]
public class CamaraSeguimiento : MonoBehaviour
{
    [Header("Seguimiento")]
    [Tooltip("Si queda vacio, busca el objeto con tag Player.")]
    public Transform objetivo;
    public Vector2 desfase = new Vector2(0f, 0.4f);
    [Tooltip("0.18 = camara que 'respira'. Sube si la camara se siente lenta.")]
    public float tiempoSuavizado = 0.18f;

    [Header("Limites del cuarto (opcional)")]
    [Tooltip("Arrastra aqui el BoxCollider2D de Limites_CuartoX.")]
    public Collider2D limites;

    [Header("Balanceo tipo pendulo (muy sutil)")]
    public bool balanceoActivo = true;
    [Tooltip("Desplazamiento maximo en unidades. 0.05 = apenas perceptible en calma.")]
    public float amplitudPosicion = 0.05f;
    [Tooltip("Inclinacion maxima en grados. 0.3 = suave sin marear.")]
    public float amplitudRotacion = 0.3f;
    [Tooltip("Oscilaciones por segundo. 0.28 = latido lento de tension.")]
    public float frecuencia = 0.28f;
    [Tooltip("1 = calma. 2-2.5 = tension (herida/persecucion). Cambia desde SistemaSangre o IA.")]
    public float intensidad = 1f;

    Camera cam;
    Vector3 velocidad;
    Vector3 posBase;
    float intensidadActual;
    float z;

    void Awake()
    {
        cam              = GetComponent<Camera>();
        z                = transform.position.z;
        posBase          = transform.position;
        intensidadActual = intensidad;
    }

    void Start()
    {
        if (objetivo == null)
        {
            var jugador = GameObject.FindGameObjectWithTag("Player");
            if (jugador != null) objetivo = jugador.transform;
        }
        if (objetivo != null)
        {
            posBase = LimitarAMapa(PosDeseada());
            transform.position = posBase;
        }
    }

    void LateUpdate()
    {
        if (objetivo != null)
        {
            Vector3 destino = LimitarAMapa(PosDeseada());
            posBase = Vector3.SmoothDamp(posBase, destino, ref velocidad, tiempoSuavizado);
        }

        Vector3 pos  = posBase;
        float   roll = 0f;

        if (balanceoActivo)
        {
            // Interpolacion suave de intensidad: responde a cambios externos progresivamente
            intensidadActual = Mathf.Lerp(intensidadActual, intensidad, Time.deltaTime * 1.5f);
            float t = Time.time * frecuencia * Mathf.PI * 2f;

            // Dos ondas con frecuencias distintas: se siente organico, no mecanico
            float ox = Mathf.Sin(t) * 0.7f + Mathf.Sin(t * 0.37f + 1.3f) * 0.3f;
            float oy = Mathf.Sin(t * 0.8f + 0.5f) * 0.6f;
            roll = Mathf.Sin(t * 0.9f + 0.2f) * amplitudRotacion * intensidadActual;

            pos.x += ox * amplitudPosicion * intensidadActual;
            pos.y += oy * amplitudPosicion * intensidadActual;
        }

        transform.position = new Vector3(pos.x, pos.y, z);
        transform.rotation = Quaternion.Euler(0f, 0f, roll);
    }

    /// <summary>
    /// Llama esto desde otro script para cambiar la intensidad del balanceo en tiempo real.
    /// Ejemplo: camara.AjustarIntensidad(2.5f); // tension maxima
    ///          camara.AjustarIntensidad(1f);   // calma
    /// </summary>
    public void AjustarIntensidad(float nuevaIntensidad)
    {
        intensidad = Mathf.Max(0f, nuevaIntensidad);
    }

    Vector3 PosDeseada()
    {
        return new Vector3(objetivo.position.x + desfase.x, objetivo.position.y + desfase.y, z);
    }

    Vector3 LimitarAMapa(Vector3 p)
    {
        if (limites == null) return p;

        Bounds b         = limites.bounds;
        float mitadAlto  = cam.orthographicSize;
        float mitadAncho = mitadAlto * cam.aspect;

        float minX = b.min.x + mitadAncho, maxX = b.max.x - mitadAncho;
        float minY = b.min.y + mitadAlto,  maxY = b.max.y - mitadAlto;

        // Si el cuarto es mas chico que la pantalla en un eje, se centra
        p.x = minX > maxX ? b.center.x : Mathf.Clamp(p.x, minX, maxX);
        p.y = minY > maxY ? b.center.y : Mathf.Clamp(p.y, minY, maxY);
        return p;
    }
}
