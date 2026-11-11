using System;
using UnityEngine;

/// <summary>
/// Sprites de los monstruos (Assets/Prefab/Monsters Creatures Fantasy) y del jefe de lava
/// (Assets/Prefab/enemigodelava). Se llena con Tools > Hemorragia > Monstruos - Configurar
/// y vive en Resources/BancoMonstruos.asset. Ajusta tamanos desde el Inspector.
/// </summary>
[CreateAssetMenu(fileName = "BancoMonstruos", menuName = "Hemorragia/Banco de Monstruos")]
public class BancoMonstruos : ScriptableObject
{
    [Serializable]
    public class DatosMonstruo
    {
        public string nombre;
        public Texture2D idle, mover, ataque;
        [Tooltip("Tamano de cada cuadro en pixeles (150 en este pack).")]
        public int celda = 150;
        [Tooltip("Pixeles por unidad: 50 = el esqueleto mide ~1 unidad. Menos = mas grande.")]
        public float ppu = 50f;
        [Tooltip("Centro horizontal del cuerpo dentro del cuadro (0-1).")]
        public float pivotX = 0.5f;
        [Tooltip("Pixeles vacios debajo de los pies dentro del cuadro.")]
        public float pixelesBajoPies = 49f;
        [Tooltip("Collider en unidades (ancho, alto). Los pies no se mueven.")]
        public Vector2 collider = new Vector2(0.5f, 1f);
        public float fpsIdle = 6f, fpsMover = 8f, fpsAtaque = 10f;
    }

    public DatosMonstruo esqueleto, goblin, hongo;

    [Header("Jefe de lava (ilustracion estatica)")]
    public Texture2D jefeFuego;
    public float jefePpu = 145f;                       // ~2 unidades de alto
    public Vector2 jefeCollider = new Vector2(0.7f, 1.9f);
    public Color jefeLuz = new Color(1f, 0.45f, 0.1f);
}
