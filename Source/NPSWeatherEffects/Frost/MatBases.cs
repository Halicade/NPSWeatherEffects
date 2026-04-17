using UnityEngine;
using Verse;

namespace NPSWeather;

[StaticConstructorOnStartup]
public static class MatBases
{
    private static readonly Texture frostTexture = ContentFinder<Texture2D>.Get("TKKN_NPS/Temperature/Wetness");
    
//    public static readonly Material Frost = MatLoader.LoadMat("TKKN_NPS/Temperature/Frost");

    private static Material cachedFrost;

    public static Material Frost
    {
        get
        {
            if (cachedFrost == null) {

                cachedFrost = new Material(Verse.MatBases.Darkness) { mainTexture = frostTexture };
            }

            return cachedFrost;
        }
    }
    
    private static readonly Texture wetnessTexture=ContentFinder<Texture2D>.Get("TKKN_NPS/Temperature/Wetness");
    
//    public static readonly Material Frost = MatLoader.LoadMat("TKKN_NPS/Temperature/Frost");

    private static Material cachedWetness;

    public static Material Wetness
    {
        get
        {
            if (cachedWetness == null) {

                cachedWetness = new Material(Verse.MatBases.Darkness) { mainTexture = wetnessTexture };
            }

            return cachedWetness;
        }
    }
}