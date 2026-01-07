using UnityEngine;

namespace ArrowBlast.Game
{
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class RoundedCube : MonoBehaviour
    {
        public int xSize = 10, ySize = 10, zSize = 10;
        public float roundness = 0.15f;

        private Mesh mesh;
        private Vector3[] vertices;
        private Vector3[] normals;

        private void Awake()
        {
            Generate();
        }

        public void Generate()
        {
            GetComponent<MeshFilter>().mesh = mesh = new Mesh();
            mesh.name = "Rounded Cube";
            CreateVertices();
            CreateTriangles();
        }

        private void CreateVertices()
        {
            int cornerVertices = 8;
            int edgeVertices = (xSize + ySize + zSize - 3) * 4;
            int faceVertices = (
                (xSize - 1) * (ySize - 1) +
                (xSize - 1) * (zSize - 1) +
                (ySize - 1) * (zSize - 1)) * 2;
            vertices = new Vector3[cornerVertices + edgeVertices + faceVertices];
            normals = new Vector3[vertices.Length];

            int v = 0;
            for (int y = 0; y <= ySize; y++)
            {
                for (int x = 0; x <= xSize; x++)
                {
                    SetVertex(v++, x, y, 0);
                }
                for (int z = 1; z <= zSize; z++)
                {
                    SetVertex(v++, xSize, y, z);
                }
                for (int x = xSize - 1; x >= 0; x--)
                {
                    SetVertex(v++, x, y, zSize);
                }
                for (int z = zSize - 1; z > 0; z--)
                {
                    SetVertex(v++, 0, y, z);
                }
            }
            for (int z = 1; z < zSize; z++)
            {
                for (int x = 1; x < xSize; x++)
                {
                    SetVertex(v++, x, ySize, z);
                }
            }
            for (int z = 1; z < zSize; z++)
            {
                for (int x = 1; x < xSize; x++)
                {
                    SetVertex(v++, x, 0, z);
                }
            }

            mesh.vertices = vertices;
            mesh.normals = normals;
        }

        private void SetVertex(int v, int x, int y, int z)
        {
            Vector3 inner = new Vector3(x, y, z);

            if (x < roundness * xSize) inner.x = roundness * xSize;
            else if (x > xSize - roundness * xSize) inner.x = xSize - roundness * xSize;

            if (y < roundness * ySize) inner.y = roundness * ySize;
            else if (y > ySize - roundness * ySize) inner.y = ySize - roundness * ySize;

            if (z < roundness * zSize) inner.z = roundness * zSize;
            else if (z > zSize - roundness * zSize) inner.z = zSize - roundness * zSize;

            normals[v] = (new Vector3(x, y, z) - inner).normalized;
            vertices[v] = inner + normals[v] * (roundness * Mathf.Min(xSize, ySize, zSize));

            // Normalize to 1x1x1 cube centered at 0
            vertices[v].x = (vertices[v].x / xSize) - 0.5f;
            vertices[v].y = (vertices[v].y / ySize) - 0.5f;
            vertices[v].z = (vertices[v].z / zSize) - 0.5f;
        }

        private void CreateTriangles()
        {
            int quads = (xSize * ySize + xSize * zSize + ySize * zSize) * 2;
            int[] triangles = new int[quads * 6];
            int ring = (xSize + zSize) * 2;
            int t = 0, v = 0;

            for (int y = 0; y < ySize; y++)
            {
                for (int q = 0; q < ring - 1; q++)
                {
                    t = SetQuad(triangles, t, v, v + 1, v + ring, v + ring + 1);
                    v++;
                }
                t = SetQuad(triangles, t, v, v - ring + 1, v + ring, v + 1);
                v++;
            }

            t = CreateTopFace(triangles, t, ring);
            t = CreateBottomFace(triangles, t, ring);
            mesh.triangles = triangles;
        }

        private int CreateTopFace(int[] triangles, int t, int ring)
        {
            int v = ring * ySize;
            for (int x = 0; x < xSize - 1; x++)
            {
                t = SetQuad(triangles, t, v, v + 1, v + ring - 1, v + ring);
                v++;
            }
            t = SetQuad(triangles, t, v, v + 1, v + ring - 1, v + 2);

            int vMin = ring * (ySize + 1) - 1;
            int vMid = vMin + 1;
            int vMax = v + 2;

            for (int z = 1; z < zSize - 1; z++)
            {
                t = SetQuad(triangles, t, vMin, vMid, vMin - 1, vMid + xSize - 1);
                for (int x = 1; x < xSize - 1; x++)
                {
                    t = SetQuad(triangles, t, vMid, vMid + 1, vMid + xSize - 1, vMid + xSize);
                    vMid++;
                }
                t = SetQuad(triangles, t, vMid, vMax, vMid + xSize - 1, vMax + 1);
                vMid++;
                vMin--;
                vMax++;
            }

            int vTop = vMin - 2;
            t = SetQuad(triangles, t, vMin, vMid, vTop + 1, vTop);
            for (int x = 1; x < xSize - 1; x++)
            {
                t = SetQuad(triangles, t, vMid, vMid + 1, vTop, vTop - 1);
                vMid++;
                vTop--;
            }
            t = SetQuad(triangles, t, vMid, vMax, vTop, vTop - 1);

            return t;
        }

        private int CreateBottomFace(int[] triangles, int t, int ring)
        {
            int v = 1;
            int vMid = vertices.Length - (xSize - 1) * (zSize - 1);
            t = SetQuad(triangles, t, ring - 1, vMid, 0, 1);
            for (int x = 1; x < xSize - 1; x++)
            {
                t = SetQuad(triangles, t, vMid, vMid + 1, x, x + 1);
                vMid++;
            }
            t = SetQuad(triangles, t, vMid, xSize, xSize - 1, xSize);

            int vMin = ring - 2;
            vMid = vertices.Length - (xSize - 1) * (zSize - 1);
            int vMax = xSize + 1;

            for (int z = 1; z < zSize - 1; z++)
            {
                t = SetQuad(triangles, t, vMin, vMid + xSize - 1, vMin + 1, vMid);
                for (int x = 1; x < xSize - 1; x++)
                {
                    t = SetQuad(triangles, t, vMid + xSize - 1, vMid + xSize, vMid, vMid + 1);
                    vMid++;
                }
                t = SetQuad(triangles, t, vMid + xSize - 1, vMax, vMid, vMax - 1);
                vMid++;
                vMin--;
                vMax++;
            }

            int vBottom = vMin - 1;
            t = SetQuad(triangles, t, vMin, vBottom, vMin + 1, vBottom + 1);
            for (int x = 1; x < xSize - 1; x++)
            {
                t = SetQuad(triangles, t, vBottom, vBottom - 1, vBottom + 1, vBottom);
                vBottom--;
            }
            t = SetQuad(triangles, t, vBottom, vBottom - 1, vBottom + 1, vBottom);

            return t;
        }

        private static int SetQuad(int[] triangles, int i, int v00, int v10, int v01, int v11)
        {
            triangles[i] = v00;
            triangles[i + 1] = triangles[i + 4] = v01;
            triangles[i + 2] = triangles[i + 3] = v10;
            triangles[i + 5] = v11;
            return i + 6;
        }
    }
}
