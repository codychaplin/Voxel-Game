using Unity.Mathematics;

public class Noise
{
    public static float GetTerrainNoise(float2 pos, int seed, float offset, float scale, int octaves, float persistance, float lacunarity, int fuzziness, float normalizeFactor)
    {
        // init position and multipliers
        pos = new float2((pos.x + seed) / 100 * scale + offset, (pos.y + seed) / 100 * scale + offset);
        float totalNoise = 0f;
        float amplitude = 1f;
        float frequency = 1f;

        // calculate noise with octaves
        for (int oct = 0; oct < octaves; oct++)
        {
            var noiseValue = noise.cnoise(pos * frequency);
            totalNoise += noiseValue * amplitude;
            amplitude *= persistance;
            frequency *= lacunarity;
        }

        var fuzzyNoise = noise.cnoise(pos * fuzziness);
        totalNoise = (totalNoise + fuzzyNoise / 10) / 1.1f;

        // normalize between -1 to 1
        float normalizedValue = totalNoise / octaves;
        normalizedValue *= normalizeFactor; // more octaves = closer to 0. Amplify to get back to original range
        return normalizedValue;
    }

    public static float GetBiomeNoise(float2 pos, int seed, float offset, float scale, bool isFuzzy)
    {
        // get noise value
        pos = new float2((pos.x + seed) / 100 * scale + offset, (pos.y + seed) / 100 * scale + offset);
        var noiseValue = noise.cnoise(pos);
        if (isFuzzy)
        {
            var fuzzyNoise = noise.cnoise(pos * 15);
            noiseValue = (noiseValue + fuzzyNoise / 10) / 1.1f;
        }

        // normalize between 0-1
        float normalized = (noiseValue + 1f) / 2f;

        // clamp to intervals
        if (normalized >= 0 && normalized < 0.2f)
            normalized = 0.1f;
        else if (normalized >= 0.2f && normalized < 0.4f)
            normalized = 0.3f;
        else if (normalized >= 0.4f && normalized < 0.6f)
            normalized = 0.5f;
        else if (normalized >= 0.6f && normalized < 0.8f)
            normalized = 0.7f;
        else if (normalized >= 0.8f && normalized <= 1f)
            normalized = 0.9f;

        return normalized;
    }

    public static float GetCaveNoise(float3 pos, int seed, float offset, float scale, int octaves)
    {
        pos = new float3((pos.x + seed) / 100 * scale + offset, (pos.y + seed) / 100 * scale + offset, (pos.z + seed) / 100 * scale + offset);
        float totalNoise = 0f;
        float amplitude = 1f;
        float frequency = 1f;

        // calculate noise with octaves
        for (int oct = 0; oct < octaves; oct++)
        {
            var noiseValue = noise.cnoise(pos * frequency);
            totalNoise += noiseValue * amplitude;
            amplitude *= 0.5f;
            frequency *= 2;
        }

        float normalizedValue = totalNoise / octaves;
        return math.abs(normalizedValue);
    }

    public static float GetOreVeinNoise(float3 pos, int seed, float offset, float scale)
    {
        pos = new float3((pos.x + seed) / 100 * scale + offset, (pos.y + seed) / 100 * scale + offset, (pos.z + seed) / 100 * scale + offset);
        int octaves = 2;
        float totalNoise = 0f;
        float amplitude = 1f;
        float frequency = 1f;

        // calculate noise with octaves
        for (int oct = 0; oct < octaves; oct++)
        {
            var noiseValue = noise.cnoise(pos * frequency);
            totalNoise += noiseValue * amplitude;
            amplitude *= 0.5f;
            frequency *= 2;
        }

        return totalNoise / octaves;
    }

    public static float FoliageNoise(float2 pos, float scale)
    {
        pos = new float2(pos.x / 32 * scale, pos.y / 32 * scale);
        return noise.snoise(pos);
    }
}