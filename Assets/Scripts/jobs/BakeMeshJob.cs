﻿using Unity.Jobs;
using Unity.Burst;
using UnityEngine;

namespace minescape.jobs
{
    [BurstCompile]
    public struct BakeMeshJob : IJob
    {
        public int meshID;

        public void Execute()
        {
            Physics.BakeMesh(meshID, false);
        }
    }
}
