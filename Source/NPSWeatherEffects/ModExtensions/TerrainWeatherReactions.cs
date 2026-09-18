using Verse;

namespace NPSWeather;

public class TerrainWeatherReactions : DefModExtension
{
    public TerrainDef freezeTerrain;
    public int freezeAt;
    public bool optionalFreeze;
    
    public bool holdFrost;
    public float temperatureAdjust;
    
    
    public TerrainDef floodTerrain;
    public TerrainDef wetTerrain;
    public int wetAt;

    public TerrainDef tideTerrain;
    public TerrainDef riverTerrain;
}