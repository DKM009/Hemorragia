using UnityEngine;

/// <summary>
/// Dibujo de los charcos de sangre (se genera por codigo, sin imagenes): mancha irregular con borde
/// oscuro, brillo humedo y gotitas salpicadas. Hay 4 variantes para que no se repitan.
/// </summary>
public static class SangreArte
{
    const int W = 64, H = 24;
    static Sprite[] variantes;

    public static Sprite Charco(int i)
    {
        if (variantes == null) variantes = new Sprite[4];
        i = Mathf.Abs(i) % variantes.Length;
        if (variantes[i] == null) variantes[i] = Crear(101 + i * 37);
        return variantes[i];
    }

    static Sprite Crear(int semilla)
    {
        var rng = new System.Random(semilla);
        var px = new Color32[W * H];
        float cx = W * 0.5f + Rango(rng, -3f, 3f), cy = H * 0.36f;
        float rx = W * Rango(rng, 0.26f, 0.34f), ry = H * Rango(rng, 0.20f, 0.27f);
        float f1 = Rango(rng, 2f, 4f), f2 = Rango(rng, 5f, 8f), p1 = Rango(rng, 0f, 6.28f), p2 = Rango(rng, 0f, 6.28f);

        for (int y = 0; y < H; y++)
            for (int x = 0; x < W; x++)
            {
                float dx = (x - cx) / rx, dy = (y - cy) / ry;
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                float ang = Mathf.Atan2(dy, dx);
                float lim = 1f + 0.20f * Mathf.Sin(f1 * ang + p1) + 0.12f * Mathf.Sin(f2 * ang + p2);
                if (d > lim) continue;
                float borde = Mathf.InverseLerp(lim, lim * 0.65f, d);          // 0 en el borde, 1 adentro
                Color c = Color.Lerp(new Color(0.20f, 0.01f, 0.02f), new Color(0.46f, 0.03f, 0.04f), borde);
                px[y * W + x] = c;
            }

        // brillo humedo: un par de manchitas claras
        for (int k = 0; k < 2; k++)
        {
            int bx = (int)(cx + Rango(rng, -rx * 0.4f, rx * 0.4f)), by = (int)(cy + Rango(rng, -ry * 0.1f, ry * 0.4f));
            Punto(px, bx, by, 2, new Color(0.66f, 0.09f, 0.09f), true);
        }

        // gotitas salpicadas alrededor
        int gotas = rng.Next(5, 9);
        for (int k = 0; k < gotas; k++)
        {
            float lado = rng.NextDouble() < 0.5 ? -1f : 1f;
            int gx = (int)(cx + lado * Rango(rng, rx * 1.05f, rx * 1.6f));
            int gy = (int)(cy + Rango(rng, -ry * 0.5f, ry * 1.1f));
            Punto(px, gx, gy, rng.Next(1, 3), new Color(0.38f, 0.02f, 0.03f), false);
        }

        var tex = new Texture2D(W, H, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        tex.wrapMode = TextureWrapMode.Clamp;
        for (int i = 0; i < px.Length; i++) if (px[i].a == 0) px[i] = new Color32(0, 0, 0, 0);
        tex.SetPixels32(px);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, W, H), new Vector2(0.5f, 0.12f), 32f);
    }

    static float Rango(System.Random r, float a, float b) { return a + (float)r.NextDouble() * (b - a); }

    static void Punto(Color32[] px, int x0, int y0, int r, Color c, bool soloSiHayMancha)
    {
        for (int y = y0 - r; y <= y0 + r; y++)
            for (int x = x0 - r; x <= x0 + r; x++)
            {
                if (x < 0 || y < 0 || x >= W || y >= H) continue;
                if ((x - x0) * (x - x0) + (y - y0) * (y - y0) > r * r) continue;
                if (soloSiHayMancha && px[y * W + x].a == 0) continue;
                px[y * W + x] = c;
            }
    }
}
