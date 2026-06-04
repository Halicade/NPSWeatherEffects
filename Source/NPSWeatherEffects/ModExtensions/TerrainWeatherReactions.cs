using Verse;

namespace NPSWeather;

public class TerrainWeatherReactions : DefModExtension
{
    public TerrainDef floodTerrain;
    public TerrainDef freezeTerrain;
    public int freezeAt;
    public bool holdFrost;
    public float temperatureAdjust;
    public int wetAt;
    public TerrainDef wetTerrain;

    public TerrainDef tideTerrain;
    public TerrainDef riverTerrain;
}