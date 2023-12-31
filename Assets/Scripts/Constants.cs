using UnityEngine;
using UnityEngine.Rendering;

namespace minescape
{
    public static class Constants
    {
        // chunk data
        public static readonly int ChunkWidth = 16;
        public static readonly int ChunkHeight = 256;
        public static readonly Bounds ChunkBounds = new(new Vector3(8f, 128f, 6f), new Vector3(16f, 256f, 16f));
        public static readonly VertexAttributeDescriptor[] VertexAttributes = new VertexAttributeDescriptor[]
        {
            new VertexAttributeDescriptor(VertexAttribute.Position, VertexAttributeFormat.Float32, 3, 0),
            new VertexAttributeDescriptor(VertexAttribute.Normal, VertexAttributeFormat.Float32, 3, 1),
            new VertexAttributeDescriptor(VertexAttribute.Color, VertexAttributeFormat.UNorm8, 4, 2),
            new VertexAttributeDescriptor(VertexAttribute.TexCoord0, VertexAttributeFormat.Float32, 2, 3),
            new VertexAttributeDescriptor(VertexAttribute.TexCoord1, VertexAttributeFormat.Float32, 2, 3)
        };

        // world data
        public static readonly int WorldSizeInChunks = 1024;
        public static int HalfWorldSizeInChunks => WorldSizeInChunks / 2;
        public static int WorldSizeInBlocks => WorldSizeInChunks * ChunkWidth;

        // view distance
        public static readonly int ViewDistance = 8;

        // misc
        public static readonly int WaterLevel = 63;

        // texture
        public static readonly int TextureAtlasSize = 16;
        public static float NormalizedTextureSize => 1f / TextureAtlasSize;

        // map data
        public static readonly int MapChunkWidth = 512;
        public static int WorldSizeInMapChunks = 1;
        public static int WorldSizeInMapBlocks => WorldSizeInMapChunks * MapChunkWidth;
    }
}