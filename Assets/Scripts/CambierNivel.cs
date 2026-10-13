using UnityEngine;
using UnityEngine.SceneManagement;

public class CambiarNivel : MonoBehaviour
{
    void OnTriggerEnter2D(Collider2D otro)
    {
        // Revisamos que quien cruce la puerta sea el jugador (Jeremy)
        if (otro.name == "Jeremy")
        {
            // Busca en qué número de nivel estamos ahora mismo
            int nivelActual = SceneManager.GetActiveScene().buildIndex;

            // Carga el nivel que sigue en la lista (+1)
            SceneManager.LoadScene(nivelActual + 1);
        }
    }
}