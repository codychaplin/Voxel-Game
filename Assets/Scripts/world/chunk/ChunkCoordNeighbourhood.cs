using System.Collections.Generic;

namespace minescape.world.chunk
{
    public struct ChunkCoordNeighbourhood
    {
        public ChunkCoord Center;
        public ChunkCoord North;
        public ChunkCoord NorthEast;
        public ChunkCoord East;
        public ChunkCoord SouthEast;
        public ChunkCoord South;
        public ChunkCoord SouthWest;
        public ChunkCoord West;
        public ChunkCoord NorthWest;

        public ChunkCoordNeighbourhood(ChunkCoord center)
        {
            Center = center;
            North = new ChunkCoord();
            NorthEast = new ChunkCoord();
            East = new ChunkCoord();
            SouthEast = new ChunkCoord();
            South = new ChunkCoord();
            SouthWest = new ChunkCoord();
            West = new ChunkCoord();
            NorthWest = new ChunkCoord();
        }

        /// <summary>
        /// Sets x/z coordinates for the center ChunkCoord
        /// </summary>
        /// <param name="x"></param>
        /// <param name="z"></param>
        public void SetCenter(int x, int z)
        {
            Center.x = x;
            Center.z = z;
        }

        /// <summary>
        /// Sets all x/z coordinates of surrounding ChunkCoords
        /// </summary>
        public void SetAllNeighbours()
        {
            North.x = Center.x;
            North.z = Center.z + 1;
            NorthEast.x = Center.x + 1;
            NorthEast.z = Center.z + 1;
            East.x = Center.x + 1;
            East.z = Center.z;
            SouthEast.x = Center.x + 1;
            SouthEast.z = Center.z - 1;
            South.x = Center.x;
            South.z = Center.z - 1;
            SouthWest.x = Center.x - 1;
            SouthWest.z = Center.z - 1;
            West.x = Center.x - 1;
            West.z = Center.z;
            NorthWest.x = Center.x - 1;
            NorthWest.z = Center.z + 1;
        }

        /// <summary>
        /// Sets all x/z coordinates of adjacent ChunkCoords
        /// </summary>
        public void SetAdjacentNeighbours()
        {
            North.x = Center.x;
            North.z = Center.z + 1;
            East.x = Center.x + 1;
            East.z = Center.z;
            South.x = Center.x;
            South.z = Center.z - 1;
            West.x = Center.x - 1;
            West.z = Center.z;
        }

        public void AddNeighboursToQueue(ref Queue<ChunkCoord> queue)
        {
            queue.Enqueue(North);
            queue.Enqueue(NorthEast);
            queue.Enqueue(East);
            queue.Enqueue(SouthEast);
            queue.Enqueue(South);
            queue.Enqueue(SouthWest);
            queue.Enqueue(West);
            queue.Enqueue(NorthWest);
        }
    }
}