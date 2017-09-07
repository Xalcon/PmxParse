using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PmxParse
{
    class Program
    {
        static void Main(string[] args)
        {
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            var path = args.Length > 0 ? args[0] : Console.ReadLine();
            using (var stream = File.OpenRead(path))
            {
                var model = new PmxModel(stream);

                Console.WriteLine($"Parsed PMX v{model.Header.Version} successfully");

                var matDict = new Dictionary<PmxMaterial, string>();

                using (var mtl = new StreamWriter(Path.GetFileNameWithoutExtension(path) + ".mtl"))
                {
                    foreach (var mat in model.MaterialData.Materials)
                    {
                        // todo: hookup google translate to translate non-universal names to english
                        var name = string.IsNullOrEmpty(mat.UniversalName) || mat.UniversalName == "en"
                            ? mat.Name
                            : mat.UniversalName;
                        name = name.Replace(" ", "_");

                        if (matDict.ContainsValue(name))
                        {
                            name = name + matDict.Values.Count(v => v.StartsWith(name));
                        }
                        matDict.Add(mat, name);

                        mtl.WriteLine($"newmtl {name}");
                        /*if(mat.EnvironmentIndex >= 0)
                            mtl.WriteLine($"map_Ka {model.TextureData.Textures[mat.EnvironmentIndex]}");*/
                        if (mat.TextureIndex >= 0)
                            mtl.WriteLine($"map_Kd {model.TextureData.Textures[mat.TextureIndex]}");
                        mtl.WriteLine($"Ka {mat.AmbientColor.X} {mat.AmbientColor.Y} {mat.AmbientColor.Z}");
                        mtl.WriteLine($"Kd {mat.DiffuseColor.X} {mat.DiffuseColor.Y} {mat.DiffuseColor.Z}");
                        mtl.WriteLine($"Ks {mat.SpecularColor.X} {mat.SpecularColor.Y} {mat.SpecularColor.Z}");
                        mtl.WriteLine($"Ns {mat.SpecularStrength}");
                        mtl.WriteLine($"#Toon {mat.ToonIndex}; EdgeFlag {mat.DrawingModeFlags}");
                        mtl.WriteLine($"d {mat.DiffuseColor.W}");
                        mtl.WriteLine();
                    }
                }

                using (var obj = new StreamWriter(Path.GetFileNameWithoutExtension(path) + ".obj"))
                {
                    obj.WriteLine($"mtllib {Path.GetFileNameWithoutExtension(path) + ".mtl"}");
                    obj.WriteLine($"o object");

                    foreach (var vertex in model.VertexData.Vertices)
                        obj.WriteLine($"v {vertex.Position.X:0.0000000} {vertex.Position.Y:0.0000000} {vertex.Position.Z:0.0000000}");

                    foreach (var vertex in model.VertexData.Vertices)
                        obj.WriteLine($"vt {vertex.TextureUV.X} {-vertex.TextureUV.Y}");

                    foreach (var vertex in model.VertexData.Vertices)
                        obj.WriteLine($"vn {vertex.Normal.X} {vertex.Normal.Y} {vertex.Normal.Z}");

                    var indices = model.FaceData.Indices;
                    var nextFaceMat = 0;
                    var matIndex = 0;
                    for (var i = 0; i < model.FaceData.Indices.Length; i += 3)
                    {
                        if (i >= nextFaceMat)
                        {
                            var mat = model.MaterialData.Materials[matIndex++];
                            nextFaceMat += mat.SurfaceCount;

                            
                            obj.WriteLine($"usemtl {matDict[mat]}");
                            //obj.WriteLine($"g {name}");
                        }

                        uint a = indices[i] + 1;
                        uint b = indices[i + 1] + 1;
                        uint c = indices[i + 2] + 1;
                        obj.WriteLine($"f {a}/{a}/{a} {b}/{b}/{b} {c}/{c}/{c}");
                    }
                }
            }
        }
    }
}
