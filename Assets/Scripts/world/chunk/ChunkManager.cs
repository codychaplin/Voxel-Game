using System.Linq;
using System.Collections;
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
        public Queue<ChunkCoord> ChunksToCreate = new();

        public List<MapChunk> MapChunks = new();

        JobHandle dependency;
        bool canRender = false;

        private void Start()
        {
            if (renderChunks)
                GenerateChunksOnStart();
            if (renderMap)
                GenerateMap();
        }

        /*void Update()
        {
            if (canRender)
                GenerateMeshDataAndRenderChunks();
        }

        void LateUpdate()
        {
            if (ChunksToCreate.Count > 0)
                CreateChunks();
        }*/

        public Chunk GetChunkNow(ChunkCoord chunkCoord)
        {
            if (Chunks.TryGetValue(chunkCoord, out var chunk))
                return chunk;
            else
                return CreateChunkNow(chunkCoord);
        }

        /// <summary>
        /// Tries to get chunk if it exists, otherwise returns null.
        /// </summary>
        /// <param name="chunkCoord"></param>
        /// <returns>Chunk or null</returns>
        public Chunk TryGetChunk(ChunkCoord chunkCoord)
        {
            return Chunks.GetValueOrDefault(chunkCoord);
        }

        /// <summary>
        /// Creates a chunk and schedules a job to set the blocks.
        /// </summary>
        /// <param name="coord"></param>
        /// <returns>JobHandle</returns>
        public JobHandle CreateChunk(ChunkCoord coord)
        {
            Chunk chunk = new(world, coord);
            var handle = SetBlocksInChunk(chunk);
            Chunks.Add(coord, chunk);
            return handle;
        }

        /// <summary>
        /// Creates a chunk and schedules/completes a job to the set the blocks.
        /// </summary>
        /// <param name="coord"></param>
        /// <returns>Chunk</returns>
        public Chunk CreateChunkNow(ChunkCoord coord)
        {
            Chunk chunk = new(world, coord);
            var handle = SetBlocksInChunk(chunk);
            handle.Complete();
            Chunks.Add(coord, chunk);
            return chunk;
        }

        JobHandle SetBlocksInChunk(Chunk chunk)
        {
            SetBlockDataJob job = new()
            {
                position = new int3(chunk.position.x, 0, chunk.position.z),
                map = chunk.BlockMap
            };
            return job.Schedule();
        }

        JobHandle GenerateMeshData(Chunk chunk, JobHandle dependency)
        {
            // generate mesh data
            var northChunk = Chunks[new ChunkCoord(chunk.coord.x, chunk.coord.z + 1)];
            var southChunk = Chunks[new ChunkCoord(chunk.coord.x, chunk.coord.z - 1)];
            var eastChunk = Chunks[new ChunkCoord(chunk.coord.x + 1, chunk.coord.z)];
            var westChunk = Chunks[new ChunkCoord(chunk.coord.x - 1, chunk.coord.z)];

            GenerateMeshDataJob generateMeshDataJob = new()
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

            // render chunk
            var generateMeshDataHandle = generateMeshDataJob.Schedule(dependency);
            return generateMeshDataHandle;
        }

        void CreateChunks()
        {
            canRender = false;
            NativeList<JobHandle> handles = new(ChunksToCreate.Count + 2, Allocator.TempJob);
            foreach (var coord in ChunksToCreate)
            {
                // if chunk doesn't exist, schedule it to be created
                if (!Chunks.ContainsKey(coord))
                    handles.Add(CreateChunk(coord));
                Chunks[coord].isProcessing = true;

                // get adjacent chunks
                var north = new ChunkCoord(coord.x, coord.z + 1);
                var south = new ChunkCoord(coord.x, coord.z - 1);
                var east = new ChunkCoord(coord.x + 1, coord.z);
                var west = new ChunkCoord(coord.x - 1, coord.z);

                // if they don't already exist, schedule them to be created
                if (!Chunks.ContainsKey(north))
                    handles.Add(CreateChunk(north));
                if (!Chunks.ContainsKey(south))
                    handles.Add(CreateChunk(south));
                if (!Chunks.ContainsKey(east))
                    handles.Add(CreateChunk(east));
                if (!Chunks.ContainsKey(west))
                    handles.Add(CreateChunk(west));

            }

            dependency = JobHandle.CombineDependencies(handles);
            handles.Dispose();

            canRender = true;
        }

        void GenerateMeshDataAndRenderChunks()
        {
            if (!dependency.IsCompleted)
                return;
            else
                dependency.Complete();

            while (ChunksToCreate.Count > 0)
            {
                // get chunk
                var coord = ChunksToCreate.Peek();
                var chunk = Chunks[coord];

                // if rendered, dequeue and skip
                if (chunk.isRenderd)
                {
                    ChunksToCreate.Dequeue();
                    continue;
                }

                // generate mesh and render chunk
                var generateMeshDataHandle = GenerateMeshData(chunk, dependency);
                StartCoroutine(RenderChunk(generateMeshDataHandle, chunk));
                ChunksToCreate.Dequeue();
            }

            canRender = false;
        }

        IEnumerator RenderChunk(JobHandle dependency, Chunk chunk)
        {
            while (!dependency.IsCompleted)
            {
                yield return null;
            }

            dependency.Complete();
            chunk.RenderChunk();

            if (chunk.coord.x - world.playerChunkCoord.x >= Constants.ViewDistance || chunk.coord.z - world.playerChunkCoord.z >= Constants.ViewDistance)
                chunk.IsActive = false;
        }

        public void GenerateChunksOnStart()
        {
            var SetBlocksInChunkHandle = SetBlocksInChunksOnStart();
            var GenerateMeshDataHandle = GenerateMeshDataForChunksOnStart(SetBlocksInChunkHandle);
            StartCoroutine(RenderChunksOnStart(GenerateMeshDataHandle));
        }

        JobHandle SetBlocksInChunksOnStart()
        {
            int index = 0;
            int count = (Constants.ViewDistance * 2) * (Constants.ViewDistance * 2);
            NativeArray<JobHandle> SetBlocksInChunkHandles = new(count, Allocator.TempJob);

            for (int x = Constants.HalfWorldSizeInChunks - Constants.ViewDistance; x < Constants.HalfWorldSizeInChunks + Constants.ViewDistance; x++)
                for (int z = Constants.HalfWorldSizeInChunks - Constants.ViewDistance; z < Constants.HalfWorldSizeInChunks + Constants.ViewDistance; z++)
                {
                    ChunkCoord coord = new(x, z);
                    Chunk chunk = new(world, coord);
                    SetBlockDataJob job = new()
                    {
                        position = new int3(chunk.position.x, 0, chunk.position.z),
                        map = chunk.BlockMap
                    };
                    var handle = job.Schedule();
                    SetBlocksInChunkHandles[index++] = handle;

                    Chunks.Add(coord, chunk);
                    activeChunks.Add(coord);
                }

            var SetBlocksInChunkHandle = JobHandle.CombineDependencies(SetBlocksInChunkHandles);
            SetBlocksInChunkHandles.Dispose();
            return SetBlocksInChunkHandle;
        }

        JobHandle GenerateMeshDataForChunksOnStart(JobHandle dependency)
        {
            int index = 0;
            NativeArray<JobHandle> GenerateMeshDataHandles = new(activeChunks.Count, Allocator.TempJob);
            foreach (var coord in activeChunks)
            {
                var chunk = GetChunkNow(coord);
                var northChunk = GetChunkNow(new ChunkCoord(chunk.coord.x, chunk.coord.z + 1));
                var southChunk = GetChunkNow(new ChunkCoord(chunk.coord.x, chunk.coord.z - 1));
                var eastChunk = GetChunkNow(new ChunkCoord(chunk.coord.x + 1, chunk.coord.z));
                var westChunk = GetChunkNow(new ChunkCoord(chunk.coord.x - 1, chunk.coord.z));

                GenerateMeshDataJob job = new()
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
                var handle = job.Schedule(dependency);
                GenerateMeshDataHandles[index++] = handle;
            }

            var GenerateMeshDataHandle = JobHandle.CombineDependencies(GenerateMeshDataHandles);
            GenerateMeshDataHandles.Dispose();
            return GenerateMeshDataHandle;
        }

        IEnumerator RenderChunksOnStart(JobHandle dependency)
        {
            while (!dependency.IsCompleted)
            {
                yield return null;
            }

            dependency.Complete();
            foreach (var coord in activeChunks)
            {
                var chunk = GetChunkNow(coord);
                chunk.RenderChunk();
            }
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
            int index = 0;
            int length = Constants.WorldSizeInMapChunks * Constants.WorldSizeInMapChunks;
            NativeArray<JobHandle> handles = new(length, Allocator.TempJob);
            for (int x = 0; x < Constants.WorldSizeInMapChunks; x++)
                for (int z = 0; z < Constants.WorldSizeInMapChunks; z++)
                {
                    MapChunk mapChunk = new(new ChunkCoord(x, z));
                    SetMapBlockDataJob job = new()
                    {
                        position = new int2(mapChunk.position.x, mapChunk.position.y),
                        map = mapChunk.BlockMap
                    };
                    handles[index++] = job.Schedule();
                    MapChunks.Add(mapChunk);
                }
            var dependency = JobHandle.CombineDependencies(handles);
            handles.Dispose();
            StartCoroutine(ConvertMapToPng(dependency));
        }

        IEnumerator ConvertMapToPng(JobHandle dependency)
        {
            while (!dependency.IsCompleted)
            {
                yield return null;
            }

            dependency.Complete();
            ConvertMapToPng();
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

            Texture2D texture = new(Constants.WorldSizeInMapBlocks, Constants.WorldSizeInMapBlocks);

            for (int x = 0; x < Constants.WorldSizeInMapChunks; x++)
            {
                for (int y = 0; y < Constants.WorldSizeInMapChunks; y++)
                {
                    MapChunk mapChunk = GetMapChunk(new ChunkCoord(x, y));
                    int offsetX = x * Constants.MapChunkWidth;
                    int offsetY = y * Constants.MapChunkWidth;
                    for (int chunkX = 0; chunkX < Constants.MapChunkWidth; chunkX++)
                        for (int chunkY = 0; chunkY < Constants.MapChunkWidth; chunkY++)
                        {
                            int index = MapChunk.ConvertToIndex(chunkX, chunkY);
                            var block = mapChunk.BlockMap[index];
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