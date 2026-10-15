using UnityEngine;

namespace Deadzone.Power
{
    /// <summary>
    /// Rubric: Solar Blast. A shock ring on a circular mesh whose UVs run outward (v = radius);
    /// the PWR/UVScroll shader scrolls v over time so the rings travel out from the generator.
    /// </summary>
    public class PWR_BlastEffect : MonoBehaviour
    {
        public float lifetime = 1.4f;

        private static Mesh discMesh;
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        private MeshRenderer meshRenderer;
        private MaterialPropertyBlock mpb;
        private float age;
        private float radius;
        private Color baseColor = new Color(1f, 0.75f, 0.3f, 1f);

        public static void Spawn(Vector3 position, float radius, Material material)
        {
            if (material == null) return;
            var go = new GameObject("PWR_Blast");
            go.transform.position = position;
            go.AddComponent<MeshFilter>().sharedMesh = GetDisc();
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = material;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            var fx = go.AddComponent<PWR_BlastEffect>();
            fx.meshRenderer = mr;
            fx.radius = radius;
            fx.mpb = new MaterialPropertyBlock();
            if (material.HasProperty(BaseColorId)) fx.baseColor = material.GetColor(BaseColorId);
            go.transform.localScale = Vector3.one * 0.1f;
        }

        private void Update()
        {
            age += Time.deltaTime;
            float t = Mathf.Clamp01(age / lifetime);
            transform.localScale = Vector3.one * Mathf.Lerp(0.2f, radius, Mathf.Sqrt(t));
            var c = baseColor;
            c.a *= 1f - t;
            mpb.SetColor(BaseColorId, c);
            meshRenderer.SetPropertyBlock(mpb);
            if (age >= lifetime) Destroy(gameObject);
        }

        /// <summary>Flat unit disc on XZ with u = angle (0..4 tiles), v = radius (0..1).</summary>
        public static Mesh GetDisc()
        {
            if (discMesh != null) return discMesh;
            const int segments = 48, rings = 6;
            var verts = new Vector3[(segments + 1) * (rings + 1)];
            var uvs = new Vector2[verts.Length];
            var tris = new int[segments * rings * 6];
            int vi = 0;
            for (int r = 0; r <= rings; r++)
            for (int s = 0; s <= segments; s++)
            {
                float a = s / (float)segments * Mathf.PI * 2f;
                float rad = r / (float)rings;
                verts[vi] = new Vector3(Mathf.Cos(a) * rad, 0f, Mathf.Sin(a) * rad);
                uvs[vi] = new Vector2(s / (float)segments * 4f, rad);
                vi++;
            }
            int ti = 0;
            for (int r = 0; r < rings; r++)
            for (int s = 0; s < segments; s++)
            {
                int a = r * (segments + 1) + s, b = a + segments + 1;
                tris[ti++] = a; tris[ti++] = b; tris[ti++] = a + 1;
                tris[ti++] = a + 1; tris[ti++] = b; tris[ti++] = b + 1;
            }
            discMesh = new Mesh { name = "PWR_BlastDisc", vertices = verts, uv = uvs, triangles = tris };
            discMesh.RecalculateNormals();
            discMesh.RecalculateBounds();
            return discMesh;
        }
    }
}
