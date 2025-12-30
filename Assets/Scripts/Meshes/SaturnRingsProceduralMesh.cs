// SaturnRingsProceduralMesh.cs
// Runtime annulus mesh with UVs suitable for "ring strip" textures (width = radial detail).
// Unity 6000.3+ / WebGL OK.

using UnityEngine;
using UnityEngine.Rendering;

[ExecuteAlways]
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public sealed class SaturnRingsProceduralMesh : MonoBehaviour
{
    public float innerRadius = 0.8f;
    public float outerRadius = 1.2f;

    [Min(3)] public int angleSegments = 256;
    [Min(1)] public int radialSegments = 8;

    public bool doubleSided = true;

    [Header("UV Mapping (for strip textures)")]
    [Tooltip("Crop the strip horizontally (useful if your strip has black margins).")]
    [Range(0f, 1f)] public float uStart = 0.0f;
    [Range(0f, 1f)] public float uEnd   = 1.0f;

    [Tooltip("How many times to repeat V around the ring (usually 1).")]
    [Min(0.001f)] public float vRepeat = 1.0f;

    [Tooltip("Offset V around the ring (0..1).")]
    [Range(0f, 1f)] public float vOffset = 0.0f;

    [Tooltip("If true: U=radius, V=angle. If false: U=angle, V=radius.")]
    public bool radiusIsU = true;

    Mesh _mesh;

    void OnEnable()  => Rebuild();
    void OnValidate() => Rebuild();

    void OnDestroy()
    {
        if (_mesh != null)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying) DestroyImmediate(_mesh);
            else Destroy(_mesh);
#else
            Destroy(_mesh);
#endif
            _mesh = null;
        }
    }

    public void Rebuild()
    {
        innerRadius = Mathf.Max(0.0001f, innerRadius);
        outerRadius = Mathf.Max(innerRadius + 0.0001f, outerRadius);

        angleSegments = Mathf.Max(3, angleSegments);
        radialSegments = Mathf.Max(1, radialSegments);

        uEnd = Mathf.Max(uStart + 0.0001f, uEnd);

        var mf = GetComponent<MeshFilter>();

        if (_mesh == null)
        {
            _mesh = new Mesh { name = "Procedural Saturn Rings" };
            _mesh.indexFormat = IndexFormat.UInt32;
        }
        else
        {
            _mesh.Clear();
        }

        // Base (single-sided) vertex grid: (angleSegments+1) * (radialSegments+1)
        int aCount = angleSegments + 1;
        int rCount = radialSegments + 1;
        int baseVertCount = aCount * rCount;

        int baseTriCount = angleSegments * radialSegments * 2; // quads -> 2 tris
        int baseIndexCount = baseTriCount * 3;

        int vertCount = doubleSided ? baseVertCount * 2 : baseVertCount;
        int indexCount = doubleSided ? baseIndexCount * 2 : baseIndexCount;

        var verts = new Vector3[vertCount];
        var norms = new Vector3[vertCount];
        var uvs   = new Vector2[vertCount];
        var tris  = new int[indexCount];

        // Build vertices
        int vi = 0;
        for (int r = 0; r < rCount; r++)
        {
            float tR = (float)r / radialSegments;
            float radius = Mathf.Lerp(innerRadius, outerRadius, tR);

            // For strip textures: radial detail should map across U (or V if radiusIsU=false)
            float uRad = Mathf.Lerp(uStart, uEnd, tR);

            for (int a = 0; a < aCount; a++)
            {
                float tA = (float)a / angleSegments;            // 0..1
                float ang = tA * Mathf.PI * 2.0f;

                float x = Mathf.Cos(ang) * radius;
                float z = Mathf.Sin(ang) * radius;

                verts[vi] = new Vector3(x, 0f, z);
                norms[vi] = Vector3.up;

                float u, v;
                float vAng = Mathf.Repeat(tA * vRepeat + vOffset, 1f);

                if (radiusIsU)
                {
                    u = uRad;
                    v = vAng;
                }
                else
                {
                    u = vAng;
                    v = uRad;
                }

                uvs[vi] = new Vector2(u, v);
                vi++;
            }
        }

        // Duplicate for backside (optional)
        if (doubleSided)
        {
            int baseOffset = baseVertCount;
            for (int i = 0; i < baseVertCount; i++)
            {
                verts[baseOffset + i] = verts[i];
                norms[baseOffset + i] = Vector3.down;
                uvs[baseOffset + i]   = uvs[i];
            }
        }

        // Build triangles (front)
        int ti = 0;
        for (int r = 0; r < radialSegments; r++)
        {
            int row0 = r * aCount;
            int row1 = (r + 1) * aCount;

            for (int a = 0; a < angleSegments; a++)
            {
                int i0 = row0 + a;
                int i1 = row0 + a + 1;
                int i2 = row1 + a;
                int i3 = row1 + a + 1;

                // Winding for +Y normal
                tris[ti++] = i0;
                tris[ti++] = i2;
                tris[ti++] = i1;

                tris[ti++] = i1;
                tris[ti++] = i2;
                tris[ti++] = i3;
            }
        }

        // Backside triangles (reverse winding + offset indices)
        if (doubleSided)
        {
            int off = baseVertCount;
            for (int i = 0; i < baseIndexCount; i += 3)
            {
                int a0 = tris[i + 0] + off;
                int a1 = tris[i + 1] + off;
                int a2 = tris[i + 2] + off;

                tris[ti++] = a0;
                tris[ti++] = a2;
                tris[ti++] = a1;
            }
        }

        _mesh.vertices = verts;
        _mesh.normals  = norms;
        _mesh.uv       = uvs;
        _mesh.triangles = tris;
        _mesh.RecalculateBounds();

        // Optional, but helpful if you later use normal maps:
        // (Safe to call; Unity will ignore if not supported.)
        try { _mesh.RecalculateTangents(); } catch { /* ignore */ }

        mf.sharedMesh = _mesh;
    }
}