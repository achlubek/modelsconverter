using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

using OpenTK;
using OpenTK.Graphics.OpenGL4;

namespace ModelConverter
{
    public class Object3dInfo
    {
        public PrimitiveType DrawMode = PrimitiveType.Triangles;

        public float[] VBO;

        public bool WireFrameRendering = false;
        
        public Object3dManager Manager = Object3dManager.Empty;

        private int IndicesCount = 0;

        public Vector3 BoundingBoxMin = Vector3.Zero, BoundingBoxMax = Vector3.Zero;


        public Object3dInfo(VertexInfo[] vbo)
        {
            VBO = VertexInfo.ToFloatList(vbo).ToArray();
            IndicesCount = vbo.Length;
            if (vbo.Length > 0)
            {
                var a = vbo[0].Position;
                var b = vbo[0].Position;
                foreach (var v in vbo)
                {
                    a = Min(a, v.Position);
                    b = Max(b, v.Position);
                }
                BoundingBoxMin = a;
                BoundingBoxMax = b;
            }
        }

        public Object3dInfo(List<VertexInfo> vbo)
        {
            VBO = VertexInfo.ToFloatList(vbo).ToArray();
            IndicesCount = vbo.Count;
            if (vbo.Count > 0)
            {
                var a = vbo[0].Position;
                var b = vbo[0].Position;
                foreach (var v in vbo)
                {
                    a = Min(a, v.Position);
                    b = Max(b, v.Position);
                }
                BoundingBoxMin = a;
                BoundingBoxMax = b;
            }
        }

        private static float Max(float a, float b)
        {
            return a > b ? a : b;
        }

        private static Vector3 Max(Vector3 a, Vector3 b)
        {
            return new Vector3(
                Max(a.X, b.X),
                Max(a.Y, b.Y),
                Max(a.Z, b.Z)
            );
        }

        private static float Min(float a, float b)
        {
            return a < b ? a : b;
        }

        private static Vector3 Min(Vector3 a, Vector3 b)
        {
            return new Vector3(
                Min(a.X, b.X),
                Min(a.Y, b.Y),
                Min(a.Z, b.Z)
            );
        }

        public static Object3dInfo Empty
        {
            get
            {
                return new Object3dInfo(new VertexInfo[0]);
            }
        }
        
        public void UpdateTangents()
        {
            var floats = new List<float>();
            if (DrawMode != PrimitiveType.Triangles)
            {
                for (int i = 0; i < IndicesCount; i++)
                {
                    int vboIndex1 = (int)i * 8;
                    var pos1 = new Vector3(VBO[vboIndex1], VBO[vboIndex1 + 1], VBO[vboIndex1 + 2]);
                    var uv1 = new Vector2(VBO[vboIndex1 + 3], VBO[vboIndex1 + 4]);
                    var nor1 = new Vector3(VBO[vboIndex1 + 5], VBO[vboIndex1 + 6], VBO[vboIndex1 + 7]);

                    var tan1 = Vector4.Zero;
                    floats.AddRange(new float[]{
                        pos1.X, pos1.Y, pos1.Z, uv1.X, uv1.Y, nor1.X, nor1.Y, nor1.Z, tan1.X, tan1.Y, tan1.Z, tan1.W
                    });
                }
                VBO = floats.ToArray();
                return;
            }
            for (int i = 0; i < IndicesCount; i += 3)
            {
                int vboIndex1 = (int)i * 8;
                int vboIndex2 = (int)(i + 1) * 8;
                int vboIndex3 = (int)(i + 2) * 8;
                //if (vboIndex3 >= VBO.Length) break;
                //  Console.WriteLine(vboIndex3 + "/" + VBO.Length + "/" + ((IndicesCount) * 8));
                var pos1 = new Vector3(VBO[vboIndex1], VBO[vboIndex1 + 1], VBO[vboIndex1 + 2]);
                var pos2 = new Vector3(VBO[vboIndex2], VBO[vboIndex2 + 1], VBO[vboIndex2 + 2]);
                var pos3 = new Vector3(VBO[vboIndex3], VBO[vboIndex3 + 1], VBO[vboIndex3 + 2]);
                var uv1 = new Vector2(VBO[vboIndex1 + 3], VBO[vboIndex1 + 4]);
                var uv2 = new Vector2(VBO[vboIndex2 + 3], VBO[vboIndex2 + 4]);
                var uv3 = new Vector2(VBO[vboIndex3 + 3], VBO[vboIndex3 + 4]);
                var nor1 = new Vector3(VBO[vboIndex1 + 5], VBO[vboIndex1 + 6], VBO[vboIndex1 + 7]);
                var nor2 = new Vector3(VBO[vboIndex2 + 5], VBO[vboIndex2 + 6], VBO[vboIndex2 + 7]);
                var nor3 = new Vector3(VBO[vboIndex3 + 5], VBO[vboIndex3 + 6], VBO[vboIndex3 + 7]);

                var tan1 = Vector4.Zero;
                floats.AddRange(new float[]{
                    pos1.X, pos1.Y, pos1.Z, uv1.X, uv1.Y, nor1.X, nor1.Y, nor1.Z, tan1.X, tan1.Y, tan1.Z, tan1.W
                });
                floats.AddRange(new float[]{
                    pos2.X, pos2.Y, pos2.Z, uv2.X, uv2.Y, nor2.X, nor2.Y, nor2.Z, tan1.X, tan1.Y, tan1.Z, tan1.W
                });
                floats.AddRange(new float[]{
                    pos3.X, pos3.Y, pos3.Z, uv3.X, uv3.Y, nor3.X, nor3.Y, nor3.Z, tan1.X, tan1.Y, tan1.Z, tan1.W
                });
            }
            VBO = floats.ToArray();
            List<Vector3> t1a = new List<Vector3>();
            List<Vector3> t2a = new List<Vector3>();
            for (int i = 0; i < IndicesCount; i += 3)
            {
                int vboIndex1 = (int)i * 12;
                int vboIndex2 = (int)(i + 1) * 12;
                int vboIndex3 = (int)(i + 2) * 12;
                //if (vboIndex3 >= VBO.Length) break;
                var pos1 = new Vector3(VBO[vboIndex1], VBO[vboIndex1 + 1], VBO[vboIndex1 + 2]);
                var pos2 = new Vector3(VBO[vboIndex2], VBO[vboIndex2 + 1], VBO[vboIndex2 + 2]);
                var pos3 = new Vector3(VBO[vboIndex3], VBO[vboIndex3 + 1], VBO[vboIndex3 + 2]);
                var uv1 = new Vector2(VBO[vboIndex1 + 3], VBO[vboIndex1 + 4]);
                var uv2 = new Vector2(VBO[vboIndex2 + 3], VBO[vboIndex2 + 4]);
                var uv3 = new Vector2(VBO[vboIndex3 + 3], VBO[vboIndex3 + 4]);
                var nor1 = new Vector3(VBO[vboIndex1 + 5], VBO[vboIndex1 + 6], VBO[vboIndex1 + 7]);
                var nor2 = new Vector3(VBO[vboIndex2 + 5], VBO[vboIndex2 + 6], VBO[vboIndex2 + 7]);
                var nor3 = new Vector3(VBO[vboIndex3 + 5], VBO[vboIndex3 + 6], VBO[vboIndex3 + 7]);
                float x1 = pos2.X - pos1.X;
                float x2 = pos3.X - pos1.X;
                float y1 = pos2.Y - pos1.Y;
                float y2 = pos3.Y - pos1.Y;
                float z1 = pos2.Z - pos1.Z;
                float z2 = pos3.Z - pos1.Z;

                float s1 = uv2.X - uv1.X;
                float s2 = uv3.X - uv1.X;
                float t1 = uv2.Y - uv1.Y;
                float t2 = uv3.Y - uv1.Y;

                float r = 1.0F / (s1 * t2 - s2 * t1);
                Vector3 sdir = new Vector3((t2 * x1 - t1 * x2) * r, (t2 * y1 - t1 * y2) * r,
                        (t2 * z1 - t1 * z2) * r);
                Vector3 tdir = new Vector3((s1 * x2 - s2 * x1) * r, (s1 * y2 - s2 * y1) * r,
                        (s1 * z2 - s2 * z1) * r);
                t1a.Add(sdir);
                t1a.Add(sdir);
                t1a.Add(sdir);
                t2a.Add(tdir);
                t2a.Add(tdir);
                t2a.Add(tdir);
                VBO[vboIndex1 + 8] += sdir.X;
                VBO[vboIndex1 + 9] += sdir.Y;
                VBO[vboIndex1 + 10] += sdir.Z;
                VBO[vboIndex2 + 8] += sdir.X;
                VBO[vboIndex2 + 9] += sdir.Y;
                VBO[vboIndex2 + 10] += sdir.Z;
                VBO[vboIndex3 + 8] += sdir.X;
                VBO[vboIndex3 + 9] += sdir.Y;
                VBO[vboIndex3 + 10] += sdir.Z;
            }
            for (int i = 0; i < IndicesCount; i++)
            {
                int vboIndex1 = i * 12;
                // if (vboIndex1 >= VBO.Length) break;
                var nor1 = new Vector3(VBO[vboIndex1 + 5], VBO[vboIndex1 + 6], VBO[vboIndex1 + 7]);
                var tan1 = t1a[i];
                var tan = -(tan1 - nor1 * Vector3.Dot(nor1, tan1)).Normalized();
                var h = (Vector3.Dot(Vector3.Cross(nor1, tan1), t2a[i]) < 0.0f) ? -1.0f : 1.0f;
                VBO[vboIndex1 + 8] = tan.X;
                VBO[vboIndex1 + 9] = tan.Y;
                VBO[vboIndex1 + 10] = tan.Z;
                VBO[vboIndex1 + 11] = h;
            }
        }
        
    }
}