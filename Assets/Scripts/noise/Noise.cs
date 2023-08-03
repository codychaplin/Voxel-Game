using minescape;
using Unity.Mathematics;

public class Noise
{
    public static float GetPerlin(float2 pos, float offset, float scale)
    {
        pos = new float2(pos.x / Constants.ChunkWidth * scale + offset, pos.y / Constants.ChunkWidth * scale + offset);
        var og = noise.cnoise(pos);
        return (og + 0.707f) / (2f * 0.707f);
    }

    public static float GetSimplex(float2 pos, float offset, float scale)
    {
        pos = new float2(pos.x / Constants.ChunkWidth * scale + offset, pos.y / Constants.ChunkWidth * scale + offset);
        var og = noise.snoise(pos);
        return (og + 0.707f) / (2f * 0.707f);
    }
}