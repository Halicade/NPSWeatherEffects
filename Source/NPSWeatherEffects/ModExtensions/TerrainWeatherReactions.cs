using Verse;

namespace NPSWeather;

public class TerrainWeatherReactions : DefModExtension
{
    public TerrainDef floodTerrain;
    public freezeTerrain freezeTerrain;
    public bool holdFrost;
    public bool isSalty;
    public float temperatureAdjust;
    public int wetAt;
    public TerrainDef wetTerrain;
    
    public TerrainDef tideTerrain;
    public TerrainDef riverTerrain;
}

public class freezeTerrain
{
    public TerrainDef terrain;
    public int freezeAt;
}