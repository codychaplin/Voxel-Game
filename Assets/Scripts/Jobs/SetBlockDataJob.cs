﻿using Unity.Jobs;
using Unity.Collections;
using Unity.Mathematics;
using minescape.init;
using minescape.world.chunk;

namespace minescape.jobs
{
    public struct SetBlockDataJob : IJob
    {
        [ReadOnly] public float temperatureScale;
        [ReadOnly] public float temperatureOffset;
        [ReadOnly] public float humidityScale;
        [ReadOnly] public float humidityOffset;

        [ReadOnly] public float landOffset;
        [ReadOnly] public float landScale;
        
        [ReadOnly] public int minTerrainheight;
        [ReadOnly] public int maxTerrainheight;

        [ReadOnly] public int3 position;
        [WriteOnly] public NativeArray<byte> blockMap;
        [WriteOnly] public NativeArray<byte> biomeMap;

        public void Execute()
        {
            for (int x = 0; x < Constants.ChunkWidth; x++)
            {
                for (int z = 0; z < Constants.ChunkWidth; z++)
                {
                    var pos = new float2(position.x + x, position.z + z);

                    // land and sea
                    float land1 = Noise.Get2DPerlin(pos, landOffset, landScale);
                    float land2 = Noise.Get2DPerlin(pos, landOffset, landScale * 8);
                    float land = (land1 + land2 / 5) / 1.2f;

                    // temperature
                    float temperature1 = Noise.Get2DPerlin(pos, temperatureOffset, temperatureScale);
                    float temperature2 = Noise.Get2DPerlin(pos, temperatureOffset, temperatureScale * 6);
                    float temperature = (temperature1 + temperature2 / 4) / 1.25f;

                    // humidity
                    float humidity1 = Noise.Get2DPerlin(pos, humidityOffset, humidityScale);
                    float humidity2 = Noise.Get2DPerlin(pos, temperatureOffset, temperatureScale * 6);
                    float humidity = (humidity1 + humidity2 / 4) / 1.25f;

                    // set biome for x/z coordinates in chunk
                    byte biomeID = GetBiome(land, temperature, humidity);
                    int biomeIndex = x + z * Constants.ChunkWidth;
                    biomeMap[biomeIndex] = biomeID;

                    // get terrain height
                    var terrainHeight = (int)math.floor(maxTerrainheight * land + minTerrainheight);

                    // set blocks
                    for (int y = 0; y < Constants.ChunkHeight; y++)
                    {
                        int index = Chunk.ConvertToIndex(x, y, z);
                        if (y == 0)
                            blockMap[index] = Blocks.BEDROCK.ID;
                        else if (y < terrainHeight)
                            blockMap[index] = Blocks.STONE.ID;
                        else if (y == terrainHeight)
                            blockMap[index] = biomeID;
                        else if (y > terrainHeight && y == Constants.WaterLevel)
                            blockMap[index] = Blocks.WATER.ID;
                        else if (y > terrainHeight && y > Constants.WaterLevel)
                            break;
                    }
                }
            }
        }

        byte GetBiome(float land, float temperature, float humidity)
        {
            if (land < 0.3)
            {
                if (temperature >= 0 && temperature < 0.33)
                    return Biomes.COLD_OCEAN.SurfaceBlock.ID;
                if (temperature >= 0.33 && temperature < 0.66)
                    return Biomes.OCEAN.SurfaceBlock.ID;
                if (temperature >= 0.66 && temperature <= 1)
                    return Biomes.WARM_OCEAN.SurfaceBlock.ID;
            }
            else if (land >= 0.3 && land < 0.32)
                return Biomes.BEACH.SurfaceBlock.ID;

            if (temperature >= 0 && temperature < 0.2 && humidity >= 0 && humidity < 0.6)
                return Biomes.TUNDRA.SurfaceBlock.ID;
            if (temperature >= 0.2 && temperature < 0.6 && humidity >= 0 && humidity < 0.4)
                return Biomes.PLAINS.SurfaceBlock.ID;
            if (temperature >= 0.6 && temperature < 0.8 && humidity >= 0 && humidity < 0.4)
                return Biomes.SAVANNA.SurfaceBlock.ID;
            if (temperature >= 0.8 && temperature <= 1 && humidity >= 0 && humidity < 0.6)
                return Biomes.DESERT.SurfaceBlock.ID;
            if (temperature >= 0 && temperature < 0.2 && humidity >= 0.6 && humidity <= 1)
                return Biomes.BOREAL_FOREST.SurfaceBlock.ID;
            if (temperature >= 0.2 && temperature < 0.4 && humidity >= 0.4 && humidity <= 1)
                return Biomes.TAIGA.SurfaceBlock.ID;
            if (temperature >= 0.4 && temperature < 0.8 && humidity >= 0.4 && humidity < 0.6)
                return Biomes.SHRUBLAND.SurfaceBlock.ID;
            if (temperature >= 0.4 && temperature < 0.8 && humidity >= 0.6 && humidity < 0.8)
                return Biomes.TEMPERATE_FOREST.SurfaceBlock.ID;
            if (temperature >= 0.4 && temperature < 0.6 && humidity >= 0.8 && humidity <= 1)
                return Biomes.SWAMP.SurfaceBlock.ID;
            if (temperature >= 0.8 && temperature <= 1 && humidity >= 0.6 && humidity < 0.8)
                return Biomes.SEASONAL_FOREST.SurfaceBlock.ID;
            if (temperature >= 0.6 && temperature <= 1 && humidity >= 0.8 && humidity <= 1)
                return Biomes.TROPICAL_FOREST.SurfaceBlock.ID;

            return Biomes.PLAINS.SurfaceBlock.ID;
        }
    }
}