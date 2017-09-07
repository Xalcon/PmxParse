using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using PmxParse.Types;

namespace PmxParse
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public unsafe struct PmxHeader
    {
        private fixed byte signature[4]; // "PMX\x20"
        public float Version;
        public byte Length;
        public PmxTextEncoding TextEncoding;
        public byte AppendixUV;
        public byte VertexIndexSize;
        public byte TextureIndexSize;
        public byte MaterialIndexSize;
        public byte BoneIndexSize;
        public byte MorphIndexSize;
        public byte RigidBodyIndexSize;
    }

    public class ModelInfo
    {
        public string CharacterName { get; private set; }
        public string UniversalCharacterName { get; private set; }
        public string Comment { get; private set; }
        public string UniversalComment { get; private set; }

        private ModelInfo() { }

        public static unsafe ModelInfo ReadData(byte* pData, PmxHeader header, out byte* offset)
        {
            return new ModelInfo
            {
                CharacterName = PmxUtils.ReadString(header.TextEncoding, pData, out pData),
                UniversalCharacterName = PmxUtils.ReadString(header.TextEncoding, pData, out pData),
                Comment = PmxUtils.ReadString(header.TextEncoding, pData, out pData),
                UniversalComment = PmxUtils.ReadString(header.TextEncoding, pData, out offset)
            };
        }
    }

    public class VertexData
    {
        public PmxVertex[] Vertices { get; private set; }

        public static unsafe VertexData ReadData(byte* pData, PmxHeader pmxHeader, out byte* offset)
        {
            var vertexData = new VertexData();

            var vertexCount = *((int*)PmxUtils.ReadAndIncrement(ref pData, sizeof(int)));
            vertexData.Vertices = new PmxVertex[vertexCount];

            for (int i = 0; i < vertexCount; i++)
            {
                var vertex = new PmxVertex();
                vertex.Position = *((Vec3*) PmxUtils.ReadAndIncrement(ref pData, sizeof(Vec3)));
                vertex.Normal = *((Vec3*) PmxUtils.ReadAndIncrement(ref pData, sizeof(Vec3)));
                vertex.TextureUV = *((Vec2*)PmxUtils.ReadAndIncrement(ref pData, sizeof(Vec2)));
                vertex.AppendixUV = new Vec4[pmxHeader.AppendixUV];

                for (int u = 0; u < pmxHeader.AppendixUV; u++)
                    vertex.AppendixUV[u] = *((Vec4*)PmxUtils.ReadAndIncrement(ref pData, sizeof(Vec4)));

                vertex.WeightType = *((PmxBoneWeightType*) PmxUtils.ReadAndIncrement(ref pData, sizeof(PmxBoneWeightType)));

                switch (vertex.WeightType)
                {
                    case PmxBoneWeightType.BDEF1:
                        pData += pmxHeader.BoneIndexSize;
                        break;
                    case PmxBoneWeightType.BDEF2:
                        pData += pmxHeader.BoneIndexSize * 2 + sizeof(float);
                        break;
                    case PmxBoneWeightType.BDEF4:
                        pData += pmxHeader.BoneIndexSize * 4 + sizeof(float) * 4;
                        break;
                    case PmxBoneWeightType.SDEF:
                        pData += pmxHeader.BoneIndexSize * 2 + sizeof(float) + sizeof(Vec3) * 3;
                        break;
                    case PmxBoneWeightType.QDEF:
                        pData += pmxHeader.BoneIndexSize * 4 + sizeof(float) * 4;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                vertex.EdgeScale = *((float*) PmxUtils.ReadAndIncrement(ref pData, sizeof(float)));

                vertexData.Vertices[i] = vertex;
            }

            offset = pData;
            return vertexData;
        }
    }

    public class PmxVertex
    {
        public Vec3 Position;
        public Vec3 Normal;
        public Vec2 TextureUV;
        public Vec4[] AppendixUV;
        public PmxBoneWeightType WeightType;
        public object WeightedDeform => throw new NotImplementedException();
        public float EdgeScale;
    }

    public enum PmxBoneWeightType : byte
    {
        BDEF1 = 0,
        BDEF2 = 1,
        BDEF4 = 2,
        SDEF = 3,
        QDEF = 4
    }

    public class FaceData
    {
        public uint[] Indices;

        public static unsafe FaceData ReadData(byte* pData, PmxHeader header, out byte* offset)
        {
            var indexCount = *((int*) PmxUtils.ReadAndIncrement(ref pData, sizeof(int)));
            var faceData = new FaceData();
            faceData.Indices = new uint[indexCount];

            switch (header.VertexIndexSize)
            {
                case sizeof(byte):
                    for (int i = 0; i < indexCount; i++)
                        faceData.Indices[i] = *pData++;
                    offset = (byte*)pData;
                    break;
                case sizeof(ushort):
                    var ushortPtr = (ushort*)pData;
                    for (int i = 0; i < indexCount; i++)
                        faceData.Indices[i] = *ushortPtr++;
                    offset = (byte*)ushortPtr;
                    break;
                case sizeof(uint):
                    var uintPtr = (uint*)pData;
                    for (int i = 0; i < indexCount; i++)
                        faceData.Indices[i] = *uintPtr++;
                    offset = (byte*)uintPtr;
                    break;
                default:
                    throw new NotSupportedException($"Unsupported index size of {header.VertexIndexSize}");
            }
            return faceData;
        }
    }

    public class PmxTextureData
    {
        public string[] Textures;

        public static unsafe PmxTextureData ReadData(byte* pData, PmxHeader header, out byte* offset)
        {
            var textureData = new PmxTextureData();
            var texCount = *((int*) PmxUtils.ReadAndIncrement(ref pData, sizeof(int)));
            textureData.Textures = new string[texCount];
            offset = pData;

            for (int i = 0; i < texCount; i++)
                textureData.Textures[i] = PmxUtils.ReadString(header.TextEncoding, offset, out offset);

            return textureData;
        }
    }

    [Flags]
    public enum DrawingModeFlags : byte
    {
        DisableFaceCulling = 0x01,
        GroundShadow = 0x02,
        ReceiveShadowMap = 0x04,
        ReceiveShadow = 0x08,
        HasEdge = 0x10,
        // PMX 2.1
        VertexColor = 0x20,
        PointDrawing = 0x40,
        LineDrawing = 0x80,
    }

    public enum ToonType : byte
    {
        Texture = 0,
        Internal = 1
    }

    public enum EnvironmentBlendMode : byte
    {
        None,
        Multiply,
        Additive,
        AdditiveVec4
    }

    public class PmxMaterial
    {
        public string Name;
        public string UniversalName;
        public Vec4 DiffuseColor;
        public Vec3 SpecularColor;
        public float SpecularStrength;
        public Vec3 AmbientColor;
        public DrawingModeFlags DrawingModeFlags;
        public Vec3 EdgeColor;
        public float EdgeSize;
        public float UnknownFloat;
        public int TextureIndex;
        public int EnvironmentIndex;
        public EnvironmentBlendMode EnvironmentBlendMode;
        public ToonType ToonType;
        public int ToonIndex;
        public string MetaData;
        public int SurfaceCount;

        public static unsafe PmxMaterial ReadData(byte* pData, PmxHeader header, out byte* offset)
        {
            offset = pData;
            ToonType type;
            var mat = new PmxMaterial();
            mat.Name = PmxUtils.ReadString(header.TextEncoding, offset, out offset);
            mat.UniversalName = PmxUtils.ReadString(header.TextEncoding, offset, out offset);
            mat.DiffuseColor = *((Vec4*) PmxUtils.ReadAndIncrement(ref offset, sizeof(Vec4)));
            mat.SpecularColor = *((Vec3*) PmxUtils.ReadAndIncrement(ref offset, sizeof(Vec3)));
            mat.SpecularStrength = *((float*) PmxUtils.ReadAndIncrement(ref offset, sizeof(float)));
            mat.AmbientColor = *((Vec3*) PmxUtils.ReadAndIncrement(ref offset, sizeof(Vec3)));
            mat.DrawingModeFlags =
                *((DrawingModeFlags*) PmxUtils.ReadAndIncrement(ref offset, sizeof(DrawingModeFlags)));
            mat.EdgeColor = *((Vec3*) PmxUtils.ReadAndIncrement(ref offset, sizeof(Vec3)));
            mat.EdgeSize = *((float*) PmxUtils.ReadAndIncrement(ref offset, sizeof(float)));
            var garbage = *((float*)PmxUtils.ReadAndIncrement(ref offset, sizeof(float)));
            mat.TextureIndex = PmxUtils.ReadVarInt(ref offset, header.TextureIndexSize);
            mat.EnvironmentIndex = PmxUtils.ReadVarInt(ref offset, header.TextureIndexSize);
            mat.EnvironmentBlendMode =
                *((EnvironmentBlendMode*) PmxUtils.ReadAndIncrement(ref offset, sizeof(EnvironmentBlendMode)));
            mat.ToonType = (type = *((ToonType*) PmxUtils.ReadAndIncrement(ref offset, sizeof(ToonType))));
            mat.ToonIndex = type == ToonType.Internal
                ? *PmxUtils.ReadAndIncrement(ref offset, sizeof(byte))
                : *((int*) PmxUtils.ReadAndIncrement(ref offset, header.TextureIndexSize));
            mat.MetaData = PmxUtils.ReadString(header.TextEncoding, offset, out offset);
            mat.SurfaceCount = *((int*) PmxUtils.ReadAndIncrement(ref offset, sizeof(int)));
            return mat;
        }
    }

    public class PmxMaterialData
    {
        public PmxMaterial[] Materials;

        public static unsafe PmxMaterialData ReadData(byte* pData, PmxHeader header, out byte* offset)
        {
            var materialData = new PmxMaterialData();
            var count = *((int*) PmxUtils.ReadAndIncrement(ref pData, sizeof(int)));
            materialData.Materials = new PmxMaterial[count];
            offset = pData;

            for (int i = 0; i < count; i++)
                materialData.Materials[i] = PmxMaterial.ReadData(offset, header, out offset);

            offset = pData;
            return materialData;
        }
    }

    public class PmxModel
    {
        public PmxHeader Header { get; }
        public ModelInfo ModelInfo { get; }
        public VertexData VertexData { get; }
        public FaceData FaceData { get; }
        public PmxTextureData TextureData { get; }
        public PmxMaterialData MaterialData { get; }

        public unsafe PmxModel(Stream model)
        {
            var buffer = new byte[model.Length];
            model.Read(buffer, 0, (int) model.Length);

            fixed (byte* p = buffer)
            {
                var mp = p;
                this.Header = *((PmxHeader*) mp);
                mp += sizeof(PmxHeader);
                
                this.ModelInfo = ModelInfo.ReadData(mp, this.Header, out mp);
                this.VertexData = VertexData.ReadData(mp, this.Header, out mp);
                this.FaceData = FaceData.ReadData(mp, this.Header, out mp);
                this.TextureData = PmxTextureData.ReadData(mp, this.Header, out mp);
                this.MaterialData = PmxMaterialData.ReadData(mp, this.Header, out mp);
            }
        }
    }

    public enum PmxTextEncoding : byte
    {
        UTF16 = 0,
        UTF8 = 1
    }
}