using UnityEngine;
using Unity.Collections;
using Unity.Mathematics;
using minescape.structures;

namespace minescape.world.chunk
{
    public class Chunk
    {
        public Material textureMap;
        public Material transparentTextureMap;
        public Transform worldTransform;
        public Vector3Int position;

        public ChunkCoord coord; // coordinates of chunk
        public NativeArray<byte> BlockMap; // blocks in chunk
        public NativeArray<byte> LightMap; // Light levels in chunk
        public NativeArray<byte> BiomeMap; // biomes in chunk
        public NativeArray<byte> HeightMap; // terrain height in chunk
        public NativeList<Structure> Structures; // structures in chunks

        // used to decide whether to regenerate chunk mesh
        public NativeReference<bool> isDirty; // for use in jobs
        public bool isRendered = false; // for use on main thread

        // syncronization utils
        public bool generated = false;
        public bool isProcessing = false;
        public bool activate = true;

        // game object and rendering properties
        GameObject chunkObject;
        public MeshFilter meshFilter;
        MeshRenderer meshRenderer;
        MeshCollider meshCollider;

        // mesh data
        public NativeList<float3> vertices;
        public NativeList<float3> normals;
        public NativeList<int> triangles;
        public NativeList<int> transparentTriangles;
        public NativeList<float2> uvs;
        public NativeList<float2> lightUvs;

        public bool IsActive
        {
            get { return chunkObject.activeSelf; }
            set { chunkObject.SetActive(value); }
        }

        public Chunk(Material _textureMap, Material _transparentTextureMap, Transform _worldTransform, ChunkCoord _coord)
        {
            textureMap = _textureMap;
            transparentTextureMap = _transparentTextureMap;
            worldTransform = _worldTransform;
            coord = _coord;
            position = new(coord.x * Constants.ChunkWidth, 0, coord.z * Constants.ChunkWidth);

            BlockMap = new(65536, Allocator.Persistent); // 65536 = 16x16x256 (x,z,y)
            LightMap = new(65536, Allocator.Persistent);
            BiomeMap = new(256, Allocator.Persistent); // 256 = 16x16 (x,z)
            HeightMap = new(256, Allocator.Persistent);
            Structures = new(Allocator.Persistent);

            isDirty = new(false, Allocator.Persistent);
        }

        public static int ConvertToIndex(int x, int z)
        {
            return x + z * Constants.ChunkWidth;
        }

        public static int ConvertToIndex(int x, int y, int z)
        {
            return x + z * Constants.ChunkWidth + y * Constants.ChunkHeight;
        }

        public static int ConvertToIndex(Vector3Int pos)
        {
            return pos.x + pos.z * Constants.ChunkWidth + pos.y * Constants.ChunkHeight;
        }

        public static int ConvertToIndex(int3 pos)
        {
            return pos.x + pos.z * Constants.ChunkWidth + pos.y * Constants.ChunkHeight;
        }

        /// <summary>
        /// Checks if block is within the bounds of its chunk.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
        /// <returns>Whether block is within chunk.</returns>
        public static bool IsBlockInChunk(int x, int y, int z)
        {
            if (x < 0 || x >= Constants.ChunkWidth ||
                y < 0 || y >= Constants.ChunkHeight ||
                z < 0 || z >= Constants.ChunkWidth)
                return false;
            else
                return true;
        }

        /// <summary>
        /// Sets block ID in chunk.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
        /// <param name="block"></param>
        public void SetBlock(int x, int y, int z, byte block)
        {
            int index = ConvertToIndex(x, y, z);
            BlockMap[index] = block;
        }

        /// <summary>
        /// Gets block ID in chunk.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
        /// <returns>Block ID</returns>
        public byte GetBlock(int x, int y, int z)
        {
            int index = ConvertToIndex(x, y, z);
            return BlockMap[index];
        }

        /// <summary>
        /// Renders chunk using block data.
        /// </summary>
        public void RenderChunk()
        {
            if (isDirty.Value)
                isRendered = false;

            if (isRendered)
                return;

            if (!generated)
            {
                chunkObject = new();
                chunkObject.layer = 6;
                meshFilter = chunkObject.AddComponent<MeshFilter>();
                meshRenderer = chunkObject.AddComponent<MeshRenderer>();
                meshCollider = chunkObject.AddComponent<MeshCollider>();
                meshRenderer.materials = new Material[] { textureMap, transparentTextureMap };
                chunkObject.transform.SetParent(worldTransform);
                chunkObject.transform.position = position;
                chunkObject.name = $"{coord.x},{coord.z}";
                generated = true;
            }

            CreateMesh();
            isProcessing = false;
            isRendered = true;
            isDirty.Value = false;

            // triggered if out of view distance
            if (!activate)
            {
                IsActive = false;
                activate = true;
            }
        }

        public void InitializeMeshCollections()
        {
            if (!vertices.IsCreated)
                vertices = new(Allocator.Persistent);
            if (!normals.IsCreated)
                normals = new(Allocator.Persistent);
            if (!uvs.IsCreated)
                uvs = new(Allocator.Persistent);
            if (!lightUvs.IsCreated)
                lightUvs = new(Allocator.Persistent);
            if (!triangles.IsCreated)
                triangles = new(Allocator.Persistent);
            if (!transparentTriangles.IsCreated)
                transparentTriangles = new(Allocator.Persistent);
        }

        /// <summary>
        /// Uses voxel data to create mesh.
        /// </summary>
        void CreateMesh()
        {
            var vertArray = vertices.ToArray(Allocator.Temp);
            var normArray = normals.ToArray(Allocator.Temp);
            var uvsArray = uvs.ToArray(Allocator.Temp);
            var lightUvsArray = lightUvs.ToArray(Allocator.Temp);

            Mesh filterMesh = new() { subMeshCount = 2 };
            filterMesh.SetVertices(vertArray);
            filterMesh.SetTriangles(triangles.ToArray(), 0);
            filterMesh.SetTriangles(transparentTriangles.ToArray(), 1);
            filterMesh.SetUVs(0, uvsArray);
            filterMesh.SetUVs(1, lightUvsArray);
            filterMesh.SetNormals(normArray);

            meshFilter.mesh.Clear();
            meshFilter.mesh = filterMesh;

            Mesh colliderMesh = new();
            colliderMesh.SetVertices(vertArray);
            colliderMesh.SetTriangles(triangles.ToArray(), 0);
            meshCollider.sharedMesh = colliderMesh;
        }

        /// <summary>
        /// Disposes of native collections.
        /// </summary>
        public void Dispose()
        {
            if (BlockMap.IsCreated) BlockMap.Dispose();
            if (LightMap.IsCreated) LightMap.Dispose();
            if (BiomeMap.IsCreated) BiomeMap.Dispose();
            if (HeightMap.IsCreated) HeightMap.Dispose();
            if (Structures.IsCreated) Structures.Dispose();

            if (isDirty.IsCreated) isDirty.Dispose();

            if (vertices.IsCreated) vertices.Dispose();
            if (normals.IsCreated) normals.Dispose();
            if (triangles.IsCreated) triangles.Dispose();
            if (transparentTriangles.IsCreated) transparentTriangles.Dispose();
            if (uvs.IsCreated) uvs.Dispose();
            if (lightUvs.IsCreated) lightUvs.Dispose();
        }

        public override string ToString()
        {
            return $"Chunk: {coord.x},{coord.z}";
        }
    }
}