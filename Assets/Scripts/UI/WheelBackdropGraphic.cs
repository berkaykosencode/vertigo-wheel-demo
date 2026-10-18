using UnityEngine;
using UnityEngine.UI;

namespace VertigoWheelDemo.UI
{
    // A resolution-independent background; no extra full-screen texture is needed.
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class WheelBackdropGraphic : Graphic
    {
        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            Rect r = rectTransform.rect;
            Quad(mesh, r.min, new Vector2(r.xMax, r.yMin), r.max, new Vector2(r.xMin, r.yMax),
                new Color32(7, 13, 23, 255), new Color32(18, 31, 46, 255));

            Color line = new Color(0.34f, 0.54f, 0.66f, 0.045f);
            for (float x = r.xMin - r.height; x < r.xMax; x += 100)
            {
                Quad(mesh, new Vector2(x, r.yMin), new Vector2(x + 1, r.yMin),
                    new Vector2(x + r.height + 1, r.yMax), new Vector2(x + r.height, r.yMax), line, line);
            }
            Color ring = new Color(0.88f, 0.59f, 0.27f, 0.07f);
            for (int radius = 360; radius <= 480; radius += 60)
                Ring(mesh, radius, ring);
        }

        private static void Ring(VertexHelper mesh, float radius, Color color)
        {
            for (int i = 0; i < 128; i++)
            {
                float a = i * Mathf.PI * 2 / 128;
                float b = (i + 1) * Mathf.PI * 2 / 128;
                Vector2 p = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                Vector2 q = new Vector2(Mathf.Cos(b), Mathf.Sin(b));
                Quad(mesh, p * radius, q * radius, q * (radius + 1), p * (radius + 1), color, color);
            }
        }

        private static void Quad(VertexHelper mesh, Vector2 a, Vector2 b, Vector2 c, Vector2 d, Color lower, Color upper)
        {
            int start = mesh.currentVertCount;
            mesh.AddVert(a, lower, Vector2.zero);
            mesh.AddVert(b, lower, Vector2.zero);
            mesh.AddVert(c, upper, Vector2.zero);
            mesh.AddVert(d, upper, Vector2.zero);
            mesh.AddTriangle(start, start + 1, start + 2);
            mesh.AddTriangle(start, start + 2, start + 3);
        }
    }
}
