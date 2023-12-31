﻿using Unity.Jobs;
using Unity.Burst;
using Unity.Collections;
using minescape.init;

namespace minescape.lighting
{
    [BurstCompile]
    public struct CalculateSunlightJob : IJob
    {
        [ReadOnly] public bool run;

        [ReadOnly] public NativeArray<byte> blockMap;
        [WriteOnly] public NativeArray<byte> lightMap;

        public void Execute()
        {
            if (!run)
                return;

            for (int x = 0; x < Constants.ChunkWidth; x++)
                for (int z = 0; z < Constants.ChunkWidth; z++)
                    for (int y = Constants.ChunkHeight - 1; y >= 0; y--)
                    {
                        int index = Utils.ConvertToIndex(x, y, z);
                        if (blockMap[index] == BlockIDs.AIR || blockMap[index] == BlockIDs.WATER || blockMap[index] == BlockIDs.GRASS_PLANT)
                            lightMap[index] = 15;
                        else
                            break;
                    }
        }
    }
}