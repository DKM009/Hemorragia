using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Stub de InventarioJugador para Hemorragia.
/// Requerido por Interactuable.cs para el chequeo de items en puertas con llave.
/// Implementar completamente en el sprint de sistemas de juego.
/// </summary>
public class InventarioJugador : MonoBehaviour
{
    readonly HashSet<string> items = new HashSet<string>();

    /// <summary>Agrega un item al inventario (llamar al recoger un objeto).</summary>
    public void AgregarItem(string nombre)
    {
        items.Add(nombre);
        Debug.Log($"[Inventario] Item recogido: {nombre}");
    }

    /// <summary>Comprueba si el jugador tiene un item especifico.</summary>
    public bool TieneItem(string nombre) => items.Contains(nombre);

    /// <summary>Elimina un item (al usarlo, por ejemplo una llave).</summary>
    public void UsarItem(string nombre) => items.Remove(nombre);
}
