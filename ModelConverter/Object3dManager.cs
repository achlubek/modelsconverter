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
    public class VertexInfo
    {
        public Vector3 Position, Normal;
        public Vector2 UV;

        public List<float> ToFloatList()
        {
            return new List<float> { Position.X, Position.Y, Position.Z, UV.X, UV.Y, Normal.X, Normal.Y, Normal.Z };
        }

        public static List<float> ToFloatList(VertexInfo[] vbo)
        {
            List<float> bytes = new List<float>(vbo.Length * 8);
            foreach (var v in vbo)
                bytes.AddRange(v.ToFloatList());
            return bytes;
        }
        public static List<float> ToFloatList(List<VertexInfo> vbo)
        {
            List<float> bytes = new List<float>(vbo.Count * 8);
            foreach (var v in vbo)
                bytes.AddRange(v.ToFloatList());
            return bytes;
        }

        public static List<VertexInfo> FromFloatArray(float[] vbo)
        {
            List<VertexInfo> vs = new List<VertexInfo>(vbo.Length / 12);
            var cnt = vbo.Length;
            for (int i = 0; i < cnt; i += 12)
            {
                var v = new VertexInfo();
                v.Position.X = vbo[i];
                v.Position.Y = vbo[i + 1];
                v.Position.Z = vbo[i + 2];
                v.UV.X = vbo[i + 3];
                v.UV.Y = vbo[i + 4];
                v.Normal.X = vbo[i + 5];
                v.Normal.Y = vbo[i + 6];
                v.Normal.Z = vbo[i + 7];
                vs.Add(v);
            }
            return vs;
        }
        public static List<VertexInfo> FromFloatArray2(float[] vbo)
        {
            List<VertexInfo> vs = new List<VertexInfo>(vbo.Length / 8);
            var cnt = vbo.Length;
            for (int i = 0; i < cnt; i += 8)
            {
                var v = new VertexInfo();
                v.Position.X = vbo[i];
                v.Position.Y = vbo[i + 1];
                v.Position.Z = vbo[i + 2];
                v.UV.X = vbo[i + 3];
                v.UV.Y = vbo[i + 4];
                v.Normal.X = vbo[i + 5];
                v.Normal.Y = vbo[i + 6];
                v.Normal.Z = vbo[i + 7];
                vs.Add(v);
            }
            return vs;
        }
    }

    public class Object3dManager
    {
        public List<VertexInfo> Vertices;

        public AxisAlignedBoundingBox AABB;

        public string Name = "unnamed";
        
        public static Object3dManager Empty
        {
            get
            {
                return new Object3dManager(new VertexInfo[0]);
            }
        }

        public struct AxisAlignedBoundingBox
        {
            public Vector3 Minimum, Maximum;
        }

        public class MaterialInfo
        {
            public string AlphaMask;

            public Vector3 DiffuseColor, SpecularColor, AmbientColor;

            public string TextureName, BumpMapName, NormapMapName, SpecularMapName, RoughnessMapName;

            public float Transparency, SpecularStrength;

            public MaterialInfo()
            {
                DiffuseColor = Vector3.One;
                SpecularColor = Vector3.One;
                AmbientColor = Vector3.One;
                Transparency = 1.0f;
                SpecularStrength = 1.0f;
                TextureName = "";
                BumpMapName = "";
                NormapMapName = "";
                SpecularMapName = "";
                RoughnessMapName = "";
                AlphaMask = "";
            }
        }

        private class ObjFileData
        {
            public string Name, MaterialName;
            public List<VertexInfo> VBO;
        }

        public Object3dManager(VertexInfo[] vertices)
        {
            Vertices = vertices.ToList();
        }
        public Object3dManager(List<VertexInfo> vertices)
        {
            Vertices = vertices;
        }

        public Object3dInfo AsObject3dInfo()
        {
            return new Object3dInfo(this.Vertices)
            {
                Manager = this
            };
        }

        public enum NormalRecalculationType
        {
            Flat,
            Smooth
        }
        public void RecalulateNormals(NormalRecalculationType mode, float smoothThreshold = 0)
        {
            if (mode == NormalRecalculationType.Flat)
                RecalculateNormalsFlat();
            if (mode == NormalRecalculationType.Smooth)
                RecalculateNormalsSmooth(smoothThreshold);

        }

        private void RecalculateNormalsFlat()
        {
            int vcount = Vertices.Count;
            for (int i = 0; i < vcount; i += 3)
            {
                var v1 = Vertices[i];
                var v2 = Vertices[i + 1];
                var v3 = Vertices[i + 2];
                var normal = Vector3.Cross(v2.Position - v1.Position, v3.Position - v1.Position).Normalized();
                v1.Normal = normal;
                v2.Normal = normal;
                v3.Normal = normal;
            }
        }

        public void TryToFixVertexWinding()
        {
            int vcount = Vertices.Count;
            for (int i = 0; i < vcount; i += 3)
            {
                var v1 = Vertices[i];
                var v2 = Vertices[i + 1];
                var v3 = Vertices[i + 2];
                var normal = Vector3.Cross(v2.Position - v1.Position, v3.Position - v1.Position).Normalized();
                Vector3 avg = (v1.Normal + v2.Normal + v3.Normal) / 3.0f;
                if (Vector3.Dot(avg, normal) < 0)
                {
                    Vertices[i] = v3;
                    Vertices[i + 2] = v1;
                }
            }
        }

        public void ReverseFaces()
        {
            int vcount = Vertices.Count;
            for (int i = 0; i < vcount; i += 3)
            {
                var v1 = Vertices[i];
                var v3 = Vertices[i + 2];
                Vertices[i + 2] = v1;
                Vertices[i] = v3;
            }
        }

        private void RecalculateNormalsSmooth(float threshold)
        {
            var posmap = new Dictionary<Vector3, List<VertexInfo>>();
            var normmap = new Dictionary<VertexInfo, Vector3>();
            var dividemap = new Dictionary<VertexInfo, float>();
            int vcount = Vertices.Count;
            for (int i = 0; i < vcount; i++)
            {
                if (!posmap.ContainsKey(Vertices[i].Position))
                    posmap.Add(Vertices[i].Position, new List<VertexInfo>());
                posmap[Vertices[i].Position].Add(Vertices[i]);

                normmap[Vertices[i]] = (Vertices[i].Normal);
                dividemap[Vertices[i]] = 0;

                Vertices[i].Normal = Vector3.Zero;
            }
            for (int i = 0; i < vcount; i += 3)
            {
                var v1 = Vertices[i];
                var v2 = Vertices[i + 1];
                var v3 = Vertices[i + 2];
                var normal = Vector3.Cross(v2.Position - v1.Position, v3.Position - v1.Position).Normalized();
                posmap[v1.Position].ForEach((a) =>
                {
                    var n = normmap[a];
                    if (Vector3.Dot(n, normal) * 0.5f + 0.5f > 1.0 - threshold)
                    {
                        dividemap[v1] += 1.0f;
                        a.Normal += normal;
                    }
                });
                posmap[v2.Position].ForEach((a) =>
                {
                    var n = normmap[a];
                    if (Vector3.Dot(n, normal) * 0.5f + 0.5f > 1.0 - threshold)
                    {
                        dividemap[v2] += 1.0f;
                        a.Normal += normal;
                    }
                });
                posmap[v3.Position].ForEach((a) =>
                {
                    var n = normmap[a];
                    if (Vector3.Dot(n, normal) * 0.5f + 0.5f > 1.0 - threshold)
                    {
                        dividemap[v3] += 1.0f;
                        a.Normal += normal;
                    }
                });
            }
            for (int i = 0; i < vcount; i++)
            {
                if (Vertices[i].Normal.Length < 0.001f)
                    Vertices[i].Normal = normmap[Vertices[i]];
                else
                    Vertices[i].Normal /= dividemap[Vertices[i]];
                Vertices[i].Normal.Normalize();
            }
        }

        public static Object3dManager[] LoadFromObj(string infile)
        {
            string[] lines = File.ReadAllLines(infile);
            var data = ParseOBJString(lines);
            return data.Select<ObjFileData, Object3dManager>(a => new Object3dManager(a.VBO) { Name = a.Name }).ToArray();
        }

        public static Object3dManager LoadFromObjSingle(string infile)
        {
            string[] lines = File.ReadAllLines(infile);
            var data = ParseOBJStringSingle(lines);
            return new Object3dManager(data.VBO);
        }

        public static Object3dManager LoadFromRaw(string vboFile)
        {
            var vboBytes = File.ReadAllBytes(vboFile);

            var vboFloats = new float[vboBytes.Length / 4];
            System.Buffer.BlockCopy(vboBytes, 0, vboFloats, 0, vboBytes.Length);

            return new Object3dManager(VertexInfo.FromFloatArray(vboFloats));
        }

        public void SaveRaw(string outfile)
        {
            MemoryStream vboStream = new MemoryStream();

            foreach (var v in Vertices)
                foreach (var v2 in v.ToFloatList())
                    vboStream.Write(BitConverter.GetBytes(v2), 0, 4);

            vboStream.Flush();
            File.WriteAllBytes(outfile, vboStream.ToArray());
        }

        public void SaveRawWithTangents(string outfile)
        {
            MemoryStream vboStream = new MemoryStream();

            var o3i = AsObject3dInfo();
            o3i.UpdateTangents();

            foreach (var v2 in o3i.VBO)
                vboStream.Write(BitConverter.GetBytes(v2), 0, 4);

            vboStream.Flush();
            File.WriteAllBytes(outfile, vboStream.ToArray());
        }

        public void SaveRawWithTangents2(string outfile)
        {
            MemoryStream vboStream = new MemoryStream();

            var o3i = AsObject3dInfo();
            o3i.DrawMode = PrimitiveType.LineStrip;
            o3i.UpdateTangents();

            foreach (var v2 in o3i.VBO)
                vboStream.Write(BitConverter.GetBytes(v2), 0, 4);

            vboStream.Flush();
            File.WriteAllBytes(outfile, vboStream.ToArray());
        }

        public static Dictionary<string, MaterialInfo> LoadMaterialsFromMtl(string filename)
        {
            Dictionary<string, MaterialInfo> materials = new Dictionary<string, MaterialInfo>();
            MaterialInfo currentMaterial = new MaterialInfo();
            string currentName = "";
            string[] lines = File.ReadAllLines(filename);
            Match match;
            foreach (string line in lines)
            {
                if (line.StartsWith("newmtl"))
                {
                    match = Regex.Match(line, @"newmtl (.+)");
                    if (currentName != "")
                    {
                        materials.Add(currentName, currentMaterial);
                        currentMaterial = new MaterialInfo();
                    }
                    currentName = match.Groups[1].Value;
                }
                if (line.StartsWith("Ns"))
                {
                    match = Regex.Match(line, @"Ns ([0-9.-]+)");
                    float val = float.Parse(match.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture);
                    currentMaterial.SpecularStrength = val > 1.0f ? 1.0f : val;
                }
                if (line.StartsWith("d"))
                {
                    match = Regex.Match(line, @"d ([0-9.-]+)");
                    float val = float.Parse(match.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture);
                    // currentMaterial.Transparency = val;
                }
                if (line.StartsWith("Ka"))
                {
                    match = Regex.Match(line, @"Ka ([0-9.-]+) ([0-9.-]+) ([0-9.-]+)");
                    int r = (int)(float.Parse(match.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture) * 255);
                    int g = (int)(float.Parse(match.Groups[2].Value, System.Globalization.CultureInfo.InvariantCulture) * 255);
                    int b = (int)(float.Parse(match.Groups[3].Value, System.Globalization.CultureInfo.InvariantCulture) * 255);
                    if (r > 255)
                        r = 255;
                    if (g > 255)
                        g = 255;
                    if (b > 255)
                        b = 255;
                    // currentMaterial.AmbientColor = Color.FromArgb(r, g, b);
                }
                if (line.StartsWith("Kd"))
                {
                    match = Regex.Match(line, @"Kd ([0-9.-]+) ([0-9.-]+) ([0-9.-]+)");
                    float r = (float.Parse(match.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture));
                    float g = (float.Parse(match.Groups[2].Value, System.Globalization.CultureInfo.InvariantCulture));
                    float b = (float.Parse(match.Groups[3].Value, System.Globalization.CultureInfo.InvariantCulture));
                    currentMaterial.DiffuseColor = new Vector3(r, g, b);
                }
                if (line.StartsWith("Ks"))
                {
                    match = Regex.Match(line, @"Ks ([0-9.-]+) ([0-9.-]+) ([0-9.-]+)");
                    float r = (float.Parse(match.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture));
                    float g = (float.Parse(match.Groups[2].Value, System.Globalization.CultureInfo.InvariantCulture));
                    float b = (float.Parse(match.Groups[3].Value, System.Globalization.CultureInfo.InvariantCulture));
                    currentMaterial.SpecularColor = new Vector3(r, g, b);
                }
                if (line.StartsWith("map_Kd"))
                {
                    match = Regex.Match(line, @"map_Kd (.+)");
                    currentMaterial.TextureName = Path.GetFileName(match.Groups[1].Value);
                }
                if (line.StartsWith("map_Ks"))
                {
                    match = Regex.Match(line, @"map_Ks (.+)");
                    currentMaterial.SpecularMapName = Path.GetFileName(match.Groups[1].Value);
                }
                if (line.StartsWith("map_d"))
                {
                    match = Regex.Match(line, @"map_d (.+)");
                    currentMaterial.AlphaMask = Path.GetFileName(match.Groups[1].Value);
                }
                if (line.StartsWith("map_n"))
                {
                    match = Regex.Match(line, @"map_n (.+)");
                    currentMaterial.NormapMapName = Path.GetFileName(match.Groups[1].Value);
                }
                if (line.StartsWith("map_Bump"))
                {
                    match = Regex.Match(line, @"map_Bump (.+)");
                    currentMaterial.BumpMapName = Path.GetFileName(match.Groups[1].Value);
                }
                if (line.StartsWith("map_Roughness"))
                {
                    match = Regex.Match(line, @"map_Roughness (.+)");
                    currentMaterial.RoughnessMapName = Path.GetFileName(match.Groups[1].Value);
                }
            }
            if (currentName != "")
                materials.Add(currentName, currentMaterial);
            return materials;
        }
        
        public void Append(Object3dManager info)
        {
            Vertices.AddRange(info.Vertices);
        }

        public Object3dManager Copy()
        {
            return new Object3dManager(Vertices);
        }

        public Object3dManager CopyDeep()
        {
            return new Object3dManager(Vertices.ToArray());
        }

        public void FlipFaces()
        {
            for (int i = 0; i < Vertices.Count; i += 3)
            {
                var tmp = Vertices[i];
                Vertices[i] = Vertices[i + 2];
                Vertices[i + 2] = tmp;
            }
        }
       

        public Vector3 GetAverageTranslationFromZero()
        {
            float averagex = 0, averagey = 0, averagez = 0;
            for (int i = 0; i < Vertices.Count; i++)
            {
                var vertex = Vertices[i].Position;
                averagex += vertex.X;
                averagey += vertex.Y;
                averagez += vertex.Z;
            }
            averagex /= (float)Vertices.Count;
            averagey /= (float)Vertices.Count;
            averagez /= (float)Vertices.Count;
            return new Vector3(averagex, averagey, averagez);
        }

        public Vector3 ExtractTranslation()
        {
            float averagex = 0, averagey = 0, averagez = 0;
            for (int i = 0; i < Vertices.Count; i++)
            {
                var vertex = Vertices[i].Position;
                averagex += vertex.X;
                averagey += vertex.Y;
                averagez += vertex.Z;
            }
            averagex /= (float)Vertices.Count;
            averagey /= (float)Vertices.Count;
            averagez /= (float)Vertices.Count;
            var center = new Vector3(averagex, averagey, averagez);

            for (int i = 0; i < Vertices.Count; i++)
            {
                var vertex = Vertices[i].Position;
                vertex.X -= center.X;
                vertex.Y -= center.Y;
                vertex.Z -= center.Z;
                Vertices[i].Position = vertex;
            }
            return center;
        }
        public Vector3 ExtractTranslation2DOnly()
        {
            float averagex = 0, averagey = 0;
            for (int i = 0; i < Vertices.Count; i++)
            {
                var vertex = Vertices[i].Position;
                averagex += vertex.X;
                averagey += vertex.Z;
            }
            averagex /= (float)Vertices.Count;
            averagey /= (float)Vertices.Count;
            var center = new Vector3(averagex, 0.0f, averagey);

            for (int i = 0; i < Vertices.Count; i++)
            {
                var vertex = Vertices[i].Position;
                vertex.X -= center.X;
                vertex.Z -= center.Z;
                Vertices[i].Position = vertex;
            }
            return center;
        }

        public Vector3 GetAxisAlignedBox()
        {
            float maxx = -999999, maxy = -999999, maxz = -999999;
            float minx = 999999, miny = 999999, minz = 999999;
            for (int i = 0; i < Vertices.Count; i++)
            {
                var vertex = Vertices[i].Position;

                maxx = maxx < vertex.X ? vertex.X : maxx;
                maxy = maxy < vertex.Y ? vertex.Y : maxy;
                maxz = maxz < vertex.Z ? vertex.Z : maxz;

                minx = minx > vertex.X ? vertex.X : minx;
                miny = miny > vertex.Y ? vertex.Y : miny;
                minz = minz > vertex.Z ? vertex.Z : minz;
            }
            return new Vector3(maxx - minx, maxy - miny, maxz - minz);
        }
        public Vector3[] GetAxisAlignedBoxEx()
        {
            float maxx = -999999, maxy = -999999, maxz = -999999;
            float minx = 999999, miny = 999999, minz = 999999;
            for (int i = 0; i < Vertices.Count; i++)
            {
                var vertex = Vertices[i].Position;

                maxx = maxx < vertex.X ? vertex.X : maxx;
                maxy = maxy < vertex.Y ? vertex.Y : maxy;
                maxz = maxz < vertex.Z ? vertex.Z : maxz;

                minx = minx > vertex.X ? vertex.X : minx;
                miny = miny > vertex.Y ? vertex.Y : miny;
                minz = minz > vertex.Z ? vertex.Z : minz;
            }
            return new Vector3[2] { new Vector3(minx, miny, minz), new Vector3(maxx, maxy, maxz) };
        }
         
        public float GetDivisorFromPoint(Vector3 point)
        {
            List<Vector3> vectors = new List<Vector3>();
            float maxval = 0.0001f;
            for (int i = 0; i < Vertices.Count; i++)
            {
                var vertex = Vertices[i].Position;
                if ((vertex - point).Length > maxval)
                    maxval = vertex.Length;
            }
            return maxval;
        }

        public float GetNormalizeDivisor()
        {
            float maxval = 0.0001f;
            for (int i = 0; i < Vertices.Count; i++)
            {
                var vertex = Vertices[i].Position;
                if (vertex.Length > maxval)
                    maxval = vertex.Length;
            }
            return maxval;
        }



        public List<Vector3> GetRawVertexList()
        {
            var ot = new List<Vector3>(Vertices.Count);
            for (int i = 0; i < Vertices.Count; i++)
            {
                var vertex = Vertices[i].Position;
                ot.Add(vertex);
            }
            return ot;
        }

        public void MakeDoubleFaced()
        {
            var copy = this.CopyDeep();
            copy.FlipFaces();
            Append(copy);
        }

        public void Normalize()
        {
            float maxval = GetNormalizeDivisor();
            for (int i = 0; i < Vertices.Count; i++)
            {
                Vertices[i].Position /= maxval;
            }
        }

        public void AxisMultiple(Vector3 axes)
        {
            for (int i = 0; i < Vertices.Count; i++)
            {
                Vertices[i].Position *= axes;
            }
        }

        public void AxisDivide(Vector3 axes)
        {
            for (int i = 0; i < Vertices.Count; i++)
            {
                Vertices[i].Position.X /= axes.X;
                Vertices[i].Position.Y /= axes.Y;
                Vertices[i].Position.Z /= axes.Z;
            }
        }

        public void OriginToCenter()
        {
            float averagex = 0, averagey = 0, averagez = 0;
            for (int i = 0; i < Vertices.Count; i++)
            {
                var vertex = Vertices[i].Position;
                averagex += vertex.X;
                averagey += vertex.Y;
                averagez += vertex.Z;
            }
            averagex /= (float)Vertices.Count;
            averagey /= (float)Vertices.Count;
            averagez /= (float)Vertices.Count;
            for (int i = 0; i < Vertices.Count; i++)
            {
                Vertices[i].Position -= new Vector3(averagex, averagey, averagez);
            }
        }

        public void ScaleUV(float x, float y)
        {
            for (int i = 0; i < Vertices.Count; i++)
            {
                Vertices[i].UV *= new Vector2(x, y);
            }
        }

        public void ScaleUV(float scale)
        {
            for (int i = 0; i < Vertices.Count; i++)
            {
                Vertices[i].UV *= new Vector2(scale);
            }
        }

        public void ReverseYUV(float scale)
        {
            for (int i = 0; i < Vertices.Count; i++)
            {
                Vertices[i].UV.Y = 1.0f - Vertices[i].UV.Y;
            }
        }

        public void Transform(Matrix4 ModelMatrix, Matrix4 RotationMatrix)
        {
            for (int i = 0; i < Vertices.Count; i++)
            {
                //     Vertices[i].Position = Vector3.Transform(Vertices[i].Position, ModelMatrix);
                //     Vertices[i].Normal = Vector3.Transform(Vertices[i].Normal, RotationMatrix);
            }
        }

        public void UpdateBoundingBox()
        {
            var vertices = GetRawVertexList();
            var a = vertices[0];
            var b = vertices[0];
            foreach (var v in vertices)
            {
                a = Min(a, v);
                b = Max(b, v);
            }
            AABB = new AxisAlignedBoundingBox()
            {
                Minimum = a,
                Maximum = b
            };
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

        private static List<ObjFileData> ParseOBJString(string[] lines)
        {
            List<ObjFileData> objects = new List<ObjFileData>();
            List<Vector3> temp_vertices = new List<Vector3>(), temp_normals = new List<Vector3>();
            List<Vector2> temp_uvs = new List<Vector2>();
            List<VertexInfo> out_vertex_buffer = new List<VertexInfo>();
            

            ObjFileData current = new ObjFileData();
            string currentMaterial = "";

            //  Match match = Match.Empty;
            foreach (string line in lines)
            {
                if (line.StartsWith("o"))
                {
                    current.VBO = out_vertex_buffer;
                    if (current.VBO.Count >= 1)
                    {
                        current.MaterialName = currentMaterial;
                        objects.Add(current);
                        GC.Collect();
                        Console.WriteLine("GC MEMORY USED {0} MB", GC.GetTotalMemory(true) / 1024.0 / 1024);
                        Console.OpenStandardOutput().Flush();
                    }
                    current = new ObjFileData();
                    current.Name = line.Substring(2);
                    Console.WriteLine("Internal Loading " + current.Name);
                    //temp_vertices = new List<Vector3>();
                    //temp_normals = new List<Vector3>();
                    //temp_uvs = new List<Vector2>();
                    out_vertex_buffer = new List<VertexInfo>();
                }
                if (line.StartsWith("usemtl"))
                {
                    currentMaterial = line.Substring(7);
                }
                if (line.StartsWith("vt"))
                {
                    var groups = line.Substring(3).Replace("nan", "0").Split(' ');
                    temp_uvs.Add(new Vector2(float.Parse(groups[0], System.Globalization.CultureInfo.InvariantCulture),
                        float.Parse(groups[1], System.Globalization.CultureInfo.InvariantCulture)));
                }
                else if (line.StartsWith("vn"))
                {
                    var groups = line.Substring(3).Split(' ');
                    temp_normals.Add(new Vector3(
                        float.Parse(groups[0], System.Globalization.CultureInfo.InvariantCulture),
                        float.Parse(groups[1], System.Globalization.CultureInfo.InvariantCulture),
                        float.Parse(groups[2], System.Globalization.CultureInfo.InvariantCulture)));
                }
                else if (line.StartsWith("v"))
                {
                    var groups = line.Substring(2).Split(' ');
                    temp_vertices.Add(new Vector3(
                        float.Parse(groups[0], System.Globalization.CultureInfo.InvariantCulture),
                        float.Parse(groups[1], System.Globalization.CultureInfo.InvariantCulture),
                        float.Parse(groups[2], System.Globalization.CultureInfo.InvariantCulture)));
                }
                else if (line.StartsWith("f"))
                {
                    // match = Regex.Match(line, @"f ([0-9]+)/([0-9]+)/([0-9]+) ([0-9]+)/([0-9]+)/([0-9]+) ([0-9]+)/([0-9]+)/([0-9]+)");
                    var groups = line.Substring(2).Replace("//", "/").Replace(' ', '/').Split('/');
                    if (groups.Length == 9)
                    {
                        out_vertex_buffer.Add(
                            new VertexInfo()
                            {
                                Position = temp_vertices[int.Parse(groups[0]) - 1],
                                UV = temp_uvs[int.Parse(groups[1]) - 1],
                                Normal = temp_normals[int.Parse(groups[2]) - 1]
                            }
                        );
                        out_vertex_buffer.Add(
                            new VertexInfo()
                            {
                                Position = temp_vertices[int.Parse(groups[3]) - 1],
                                UV = temp_uvs[int.Parse(groups[4]) - 1],
                                Normal = temp_normals[int.Parse(groups[5]) - 1]
                            }
                        );
                        out_vertex_buffer.Add(
                            new VertexInfo()
                            {
                                Position = temp_vertices[int.Parse(groups[6]) - 1],
                                UV = temp_uvs[int.Parse(groups[7]) - 1],
                                Normal = temp_normals[int.Parse(groups[8]) - 1]
                            }
                        );
                    }
                    else
                    {
                        out_vertex_buffer.Add(
                            new VertexInfo()
                            {
                                Position = temp_vertices[int.Parse(groups[0]) - 1],
                                UV = Vector2.One,
                                Normal = temp_normals[int.Parse(groups[1]) - 1]
                            }
                        );
                        out_vertex_buffer.Add(
                            new VertexInfo()
                            {
                                Position = temp_vertices[int.Parse(groups[2]) - 1],
                                UV = Vector2.One,
                                Normal = temp_normals[int.Parse(groups[3]) - 1]
                            }
                        );
                        out_vertex_buffer.Add(
                            new VertexInfo()
                            {
                                Position = temp_vertices[int.Parse(groups[4]) - 1],
                                UV = Vector2.One,
                                Normal = temp_normals[int.Parse(groups[5]) - 1]
                            }
                        );
                    }

                }
            }
            current.VBO = out_vertex_buffer;
            current.MaterialName = currentMaterial;
            objects.Add(current);
            current = new ObjFileData();
            return objects;
        }

        private static ObjFileData ParseOBJStringSingle(string[] lines)
        {
            List<ObjFileData> objects = new List<ObjFileData>();
            List<Vector3> temp_vertices = new List<Vector3>(), temp_normals = new List<Vector3>();
            List<Vector2> temp_uvs = new List<Vector2>();
            List<VertexInfo> out_vertex_buffer = new List<VertexInfo>();
            
            ObjFileData current = new ObjFileData();

            Match match = Match.Empty;
            foreach (string line in lines)
            {
                if (line.StartsWith("vt"))
                {
                    match = Regex.Match(line, @"vt ([0-9.-]+) ([0-9.-]+)");
                    temp_uvs.Add(new Vector2(float.Parse(match.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture), float.Parse(match.Groups[2].Value, System.Globalization.CultureInfo.InvariantCulture)));
                }
                else if (line.StartsWith("vn"))
                {
                    match = Regex.Match(line, @"vn ([0-9.-]+) ([0-9.-]+) ([0-9.-]+)");
                    temp_normals.Add(new Vector3(float.Parse(match.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture), float.Parse(match.Groups[2].Value, System.Globalization.CultureInfo.InvariantCulture), float.Parse(match.Groups[3].Value, System.Globalization.CultureInfo.InvariantCulture)));
                }
                else if (line.StartsWith("v"))
                {
                    match = Regex.Match(line, @"v ([0-9.-]+) ([0-9.-]+) ([0-9.-]+)");
                    temp_vertices.Add(new Vector3(float.Parse(match.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture), float.Parse(match.Groups[2].Value, System.Globalization.CultureInfo.InvariantCulture), float.Parse(match.Groups[3].Value, System.Globalization.CultureInfo.InvariantCulture)));
                }
                else if (line.StartsWith("f"))
                {
                    match = Regex.Match(line, @"f ([0-9]+)/([0-9]+)/([0-9]+) ([0-9]+)/([0-9]+)/([0-9]+) ([0-9]+)/([0-9]+)/([0-9]+)");
                    if (match.Success)
                    {
                        for (int i = 1; ;)
                        {
                            Vector3 vertex = temp_vertices[int.Parse(match.Groups[i++].Value) - 1];
                            Vector2 uv = temp_uvs[int.Parse(match.Groups[i++].Value) - 1];
                            Vector3 normal = temp_normals[int.Parse(match.Groups[i++].Value) - 1];

                            out_vertex_buffer.Add(new VertexInfo() { Position = vertex, Normal = normal, UV = uv });
                            if (i >= 9)
                                break;
                        }
                    }
                    else
                    {
                        match = Regex.Match(line, @"f ([0-9]+)//([0-9]+) ([0-9]+)//([0-9]+) ([0-9]+)//([0-9]+)");
                        if (match.Success)
                        {
                            for (int i = 1; ;)
                            {
                                Vector3 vertex = temp_vertices[int.Parse(match.Groups[i++].Value) - 1];
                                Vector3 normal = temp_normals[int.Parse(match.Groups[i++].Value) - 1];

                                out_vertex_buffer.Add(new VertexInfo() { Position = vertex, Normal = normal, UV = normal.Xz });
                                if (i >= 6)
                                    break;
                            }
                        }
                    }
                }
            }
            current.VBO = out_vertex_buffer;
            objects.Add(current);
            current = new ObjFileData();
            current.Name = match.Groups[1].Value;
            return objects.First();
        }

        private Vector3 CalculateTangent(Vector3 normal, Vector3 v1, Vector3 v2, Vector2 st1, Vector2 st2)
        {
            float coef = 1.0f / (st1.X * st2.Y - st2.X * st1.Y);
            var tangent = Vector3.Zero;

            tangent.X = coef * ((v1.X * st2.Y) + (v2.X * -st1.X));
            tangent.Y = coef * ((v1.Y * st2.Y) + (v2.Y * -st1.X));
            tangent.Z = coef * ((v1.Z * st2.Y) + (v2.Z * -st1.X));

            //float3 binormal = normal.crossProduct(tangent);
            return tangent;
        }
    }
}