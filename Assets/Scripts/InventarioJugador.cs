using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Inventario simple de Hemorragia: items por nombre (fusible, llaves) y latas.
/// Muestra lo que lleva el jugador arriba a la derecha.
/// </summary>
public class InventarioJugador : MonoBehaviour
{
    readonly HashSet<string> items = new HashSet<string>();

    public int latas;

    void Start()
    {
        ActualizarUI();
    }

    /// <summary>Agrega un item al inventario (llamar al recoger un objeto).</summary>
    public void AgregarItem(string nombre)
    {
        items.Add(nombre);
        Debug.Log($"[Inventario] Item recogido: {nombre}");
        ActualizarUI();
    }

    /// <summary>Comprueba si el jugador tiene un item especifico.</summary>
    public bool TieneItem(string nombre) => items.Contains(nombre);

    /// <summary>Elimina un item (al usarlo, por ejemplo una llave).</summary>
    public void UsarItem(string nombre)
    {
        items.Remove(nombre);
        ActualizarUI();
    }

    public void AgregarLata(int cantidad = 1)
    {
        latas += cantidad;
        ActualizarUI();
    }

    /// <summary>Gasta una lata. Devuelve false si no queda ninguna.</summary>
    public bool UsarLata()
    {
        if (latas <= 0) return false;
        latas--;
        ActualizarUI();
        return true;
    }

    void ActualizarUI()
    {
        string texto = "";
        if (latas > 0) texto += $"Latas: {latas}   [Q] lanzar";
        if (items.Count > 0)
            texto += (texto.Length > 0 ? "\n" : "") + string.Join("  ·  ", items);
        UIMensaje.Inventario(texto);
    }
}
