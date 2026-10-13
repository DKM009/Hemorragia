using UnityEngine;

public class MovimientoJeremy : MonoBehaviour
{
    // Esta variable controla qu� tan r�pido corre. Puedes cambiar el 5 desde Unity.
    public float velocidad = 5f;

    private Rigidbody2D rb;
    private float movimientoX;

    void Start()
    {
        // Esto busca el Rigidbody2D que le pusiste al cubo para poder controlarlo
        rb = GetComponent<Rigidbody2D>();
    }

    void Update()
    {
        // Detecta si presionas las flechas izquierda/derecha o las teclas A/D
        movimientoX = Input.GetAxisRaw("Horizontal");
    }

    void FixedUpdate()
    {
        // Aplica la fuerza para moverlo, pero mantiene la ca�da de gravedad normal (eje Y)
        rb.linearVelocity = new Vector2(movimientoX * velocidad, rb.linearVelocity.y);
    }
}