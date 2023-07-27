using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using Unity.Jobs;
using Unity.Collections;
using Unity.Mathematics;
using minescape.init;
using minescape.jobs;

namespace minescape.world.chunk
{
    public class ChunkManager : MonoBehaviour
    {
        public World world;
        public bool renderMap;
        public bool renderChunks;

        public Dictionary<ChunkCoord, Chunk> Chunks = new();

        public List<ChunkCoord> activeChunks = new();
        public Queue<ChunkCoord> chunksToCreate = new();

        public List<MapChunk> MapChunks = new();

        JobHandle renderHandles;

        private void Start()
        {
            GenerateChunks();
        }

        void Update()
        {
            if (renderHandles.IsCompleted)
            {
                foreach (var chunk in Chunks.Values)
                {
                    chunk.RenderChunk();
                }
            }
            else
            {
                renderHandles.Complete();
            }
        }

        public Chunk GetChunk(ChunkCoord chunkCoord)
        {
            if (Chunks.TryGetValue(chunkCoord, out var chunk))
                return chunk;
            else
                return CreateChunkNow(chunkCoord);
        }

        public Chunk CreateChunk(ChunkCoord coord)
        {
            Chunk chunk = new(world, coord);
            SetBlocksInChunk(chunk);
            Chunks.Add(coord, chunk);
            return chunk;
        }

        public Chunk CreateChunkNow(ChunkCoord coord)
        {
            Chunk chunk = new(world, coord);
            var handle = SetBlocksInChunk(chunk);
            handle.Complete();
            Chunks.Add(coord, chunk);
            return chunk;
        }

        /*void CreateChunks()
        {
            while (chunksToCreate.Count > 0)
            {
                Chunk chunk = GetChunk(chunksToCreate.Peek());
                chunk.RenderChunk();
                chunksToCreate.Dequeue();
                if (chunk.coord.x - world.playerChunkCoord.x >= Constants.ViewDistance ||
                    chunk.coord.z - world.playerChunkCoord.z >= Constants.ViewDistance)
                {
                    chunk.IsActive = false;
                }

            }
        }*/

        public void GenerateChunks()
        {
            // set up jobs
            int index = 0;
            int chunkCount = (Constants.ViewDistance * 2) * (Constants.ViewDistance * 2);
            NativeArray<JobHandle> SetBlocksInChunkHandles = new(chunkCount, Allocator.TempJob);

            for (int x = Constants.HalfWorldSizeInChunks - Constants.ViewDistance; x < Constants.HalfWorldSizeInChunks + Constants.ViewDistance; x++)
                for (int z = Constants.HalfWorldSizeInChunks - Constants.ViewDistance; z < Constants.HalfWorldSizeInChunks + Constants.ViewDistance; z++)
                {
                    ChunkCoord coord = new(x, z);
                    Chunk chunk = new(world, coord);
                    var handle = SetBlocksInChunk(chunk);
                    SetBlocksInChunkHandles[index++] = handle;

                    Chunks.Add(coord, chunk);
                    activeChunks.Add(chunk.coord);
                }

            // complete jobs
            JobHandle.CompleteAll(SetBlocksInChunkHandles);
            SetBlocksInChunkHandles.Dispose();

            index = 0;
            NativeArray<JobHandle> renderChunkHandles = new(chunkCount, Allocator.TempJob);
            var chunks = Chunks.Values.ToArray();
            foreach (var chunk in chunks)
            {
                var handle = RenderChunk(chunk);
                renderChunkHandles[index++] = handle;
            }

            renderHandles = JobHandle.CombineDependencies(renderChunkHandles);
            renderChunkHandles.Dispose();
        }

        JobHandle RenderChunk(Chunk chunk)
        {
            var northChunk = GetChunk(new ChunkCoord(chunk.coord.x, chunk.coord.z + 1));
            var southChunk = GetChunk(new ChunkCoord(chunk.coord.x, chunk.coord.z - 1));
            var eastChunk = GetChunk(new ChunkCoord(chunk.coord.x + 1, chunk.coord.z));
            var westChunk = GetChunk(new ChunkCoord(chunk.coord.x - 1, chunk.coord.z));
            RenderChunkJob jobData = new()
            {
                coord = chunk.coord,
                position = new int3(chunk.position.x, 0, chunk.position.z),
                map = chunk.BlockMap,
                north = northChunk.BlockMap,
                south = southChunk.BlockMap,
                east = eastChunk.BlockMap,
                west = westChunk.BlockMap,
                vertices = chunk.vertices,
                triangles = chunk.triangles,
                uvs = chunk.uvs,
                vertexIndex = 0
            };

            return jobData.Schedule();
        }

        JobHandle SetBlocksInChunk(Chunk chunk)
        {
            SetBlockDataJob jobData = new()
            {
                position = new int3(chunk.position.x, 0, chunk.position.z),
                map = chunk.BlockMap
            };

            return jobData.Schedule();
        }

        void ReplaceSurfaceBlocks(Chunk chunk)
        {

        }

        public MapChunk GetMapChunk(ChunkCoord chunkCoord)
        {
            return MapChunks.FirstOrDefault(c => c.coord.Equals(chunkCoord));
        }

        public void GenerateMap()
        {
            for (int x = 0; x < Constants.WorldSizeInChunks; x++)
                for (int z = 0; z < Constants.WorldSizeInChunks; z++)
                {
                    MapChunk chunk = new(new ChunkCoord(x, z));
                    SetBlocksInMapChunk(ref chunk);
                    MapChunks.Add(chunk);
                }

            ConvertMapToPng();
        }

        void SetBlocksInMapChunk(ref MapChunk chunk)
        {
            for (int x = 0; x < Constants.ChunkWidth; x++)
            {
                for (int z = 0; z < Constants.ChunkWidth; z++)
                {
                    var terrainHeight = Mathf.FloorToInt(128 * Noise.Get2DPerlin(new float2(chunk.position.x + x, chunk.position.y + z), 0, 0.5f)) + 16;
                    if (terrainHeight > Constants.WaterLevel)
                        chunk.SetBlock(x, z, Blocks.STONE.ID);
                    else
                        chunk.SetBlock(x, z, Blocks.WATER.ID);
                }
            }
        }

        void ConvertMapToPng()
        {
            Dictionary<byte, Color32> colours = new()
            {
                { 0, new Color32(255,255,255,255) }, // air
                { 1, new Color32(41,41,41,255) }, // bedrock
                { 2, new Color32(115,115,115,255) }, // stone
                { 3, new Color32(108,83,47,255) }, // dirt
                { 4, new Color32(66,104,47,255) }, // grass
                { 5, new Color32(227,213,142,255) }, // sand
                { 6, new Color32(80,172,220,255) } // water
            };

            Texture2D texture = new(Constants.WorldSizeInBlocks, Constants.WorldSizeInBlocks);

            for (int x = 0; x < Constants.WorldSizeInChunks; x++)
            {
                for (int y = 0; y < Constants.WorldSizeInChunks; y++)
                {
                    MapChunk mapChunk = GetMapChunk(new ChunkCoord(x, y));
                    int offsetX = x * Constants.ChunkWidth;
                    int offsetY = y * Constants.ChunkWidth;
                    for (int chunkX = 0; chunkX < Constants.ChunkWidth; chunkX++)
                        for (int chunkY = 0; chunkY < Constants.ChunkWidth; chunkY++)
                        {
                            var block = mapChunk.BlockMap[chunkX, chunkY];
                            texture.SetPixel(chunkX + offsetX, chunkY + offsetY, colours[block]);
                        }
                }
            }

            texture.Apply();
            world.image.texture = texture;
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Point;
        }
    }
}