using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class World : MonoBehaviour
{
    public int seed;
    public bool renderWorld;
    public BiomeAttribute biome;
    public RawImage image;

    public Transform player;
    [NonSerialized]
    public Vector3 spawnpoint;
    public Material material;
    public BlockType[] BlockTypes;

    Chunk[,] chunks = new Chunk[Block.WorldSizeInChunks, Block.WorldSizeInChunks];

    List<ChunkCoord> activeChunks = new();
    ChunkCoord playerChunkCoord;
    ChunkCoord playerLastChunkCoord;

    void Start()
    {
        UnityEngine.Random.InitState(seed);
        GenerateWorld();
        ConvertMapToPng();

        spawnpoint = new Vector3(Block.WorldSizeInBlocks / 2, Block.ChunkHeight + 2, Block.WorldSizeInBlocks / 2);
        player.position = spawnpoint;
        playerLastChunkCoord = GetChunkCoord(player.position);
    }

    /*void Update()
    {
        playerChunkCoord = GetChunkCoord(player.position);
        if (!playerChunkCoord.Equals(playerLastChunkCoord))
            CheckViewDistance();
    }*/

    void GenerateWorld()
    {
        for (int x = 0; x < Block.WorldSizeInChunks; x++)
            for (int z = 0; z < Block.WorldSizeInChunks; z++)
            {
                CreateChunk(x, z);
            }
    }

    void CreateChunk(int x, int z)
    {
        chunks[x, z] = new(this, new ChunkCoord(x, z), renderWorld);
        activeChunks.Add(new ChunkCoord(x, z));
    }

    public byte GetBlock(Vector3 pos)
    {
        int y = Mathf.FloorToInt(pos.y);

        if (!IsBlockInWorld(pos))
            return 0; // air

        if (y == 0)
            return 1; // bedrock

        int terrainHeight = Mathf.FloorToInt(biome.maxTerrainHeight * Noise.Get2DPerlin(new Vector2(pos.x, pos.z), 0, biome.scale)) + biome.minTerrainHeight;
        if (y == terrainHeight)
            return 4; // grass
        else if (y > terrainHeight && y <= 64)
            return 5; // water
        else if (y < terrainHeight && y >= terrainHeight - 4)
            return 3; // dirt
        else if (y < terrainHeight)
            return 2; // stone
        else
            return 0; // air
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
            { 5, new Color32(80,172,220,255) } // water
        };

        string path = "Assets/Textures/test.png";
        Texture2D texture = new(Block.WorldSizeInBlocks, Block.WorldSizeInBlocks);

        for (int x = 0; x < Block.WorldSizeInChunks; x++)
        {
            for (int y = 0; y < Block.WorldSizeInChunks; y++)
            {
                int offsetX = x * Block.ChunkWidth;
                int offsetY = y * Block.ChunkWidth;
                for (int chunkX = 0 + offsetX; chunkX < Block.ChunkWidth + offsetX; chunkX++)
                {
                    for (int chunkY = 0 + offsetY; chunkY < Block.ChunkWidth + offsetY; chunkY++)
                    {
                        var block = chunks[x, y].Map2D[chunkX - offsetX, chunkY - offsetY];
                        Color32 tintedColour;
                        if (block.type != 5)
                            tintedColour = Color32.Lerp(colours[block.type], Color.white, (float)block.height / Block.ChunkHeight);
                        else
                            tintedColour = colours[block.type];
                        texture.SetPixel(chunkX, chunkY, tintedColour);
                    }
                }
            }
        }

        texture.Apply();
        image.texture = texture;
        byte[] bytes = texture.EncodeToPNG();
        System.IO.File.WriteAllBytes(path, bytes);
    }


    void CheckViewDistance()
    {
        ChunkCoord coord = GetChunkCoord(player.position);

        // cache then clear activeChunks
        List<ChunkCoord> previouslyActiveChunks = new(activeChunks);
        //activeChunks.Clear();

        for (int x = coord.x - Block.ViewDistance; x <= coord.x + Block.ViewDistance; x++)
            for (int z = coord.z - Block.ViewDistance; z <= coord.z + Block.ViewDistance; z++)
            {
                // if chunk is out of bounds, skip
                if (!IsChunkInWorld(x, z))
                    continue;

                if (chunks[x, z] == null) // if doesn't exist, create one
                {
                    CreateChunk(x, z);
                }
                else if (!chunks[x, z].IsActive) // if not active, activate and add to activeChunks
                {
                    chunks[x, z].IsActive = true;
                    activeChunks.Add(new ChunkCoord(x, z));
                }

                // remove active chunks from previouslyActiveChunks
                for (int i = 0; i < previouslyActiveChunks.Count; i++)
                {
                    if (previouslyActiveChunks[i].Equals(new ChunkCoord(x, z)))
                        previouslyActiveChunks.RemoveAt(i);
                }
            }

        // disable leftover chunks in previouslyActiveChunks
        foreach (var chunk in previouslyActiveChunks)
            chunks[chunk.x, chunk.z].IsActive = false;
    }

    bool IsBlockInWorld(Vector3 pos)
    {
        return pos.x >= 0 && pos.x < Block.WorldSizeInBlocks &&
               pos.y >= 0 && pos.y < Block.ChunkHeight &&
               pos.z >= 0 && pos.z < Block.WorldSizeInBlocks;
    }

    bool IsChunkInWorld(int x, int z)
    {
        return x >= 0 && x < Block.WorldSizeInChunks &&
               z >= 0 && z < Block.WorldSizeInChunks;
    }

    ChunkCoord GetChunkCoord(Vector3 pos)
    {
        int x = Mathf.FloorToInt(pos.x / Block.ChunkWidth);
        int z = Mathf.FloorToInt(pos.z / Block.ChunkWidth);
        return new ChunkCoord(x, z);
    }
}