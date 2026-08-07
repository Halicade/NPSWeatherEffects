using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;

namespace NPSWeather;

[StaticConstructorOnStartup]
public class HarmonyWeatherEffects
{
    // https://steamcommunity.com/sharedfiles/filedetails/?id=2079784964
    public static readonly bool RimBrellasActive;

    // Desire Paths https://steamcommunity.com/sharedfiles/filedetails/?id=3555461401
    // Dynamic Trails https://steamcommunity.com/sharedfiles/filedetails/?id=3772398443
    public static readonly bool PathModActive;

    // https://steamcommunity.com/sharedfiles/filedetails/?id=3403972335
    public static readonly bool StorytellerJianghuActive;

    public static readonly bool WaterFreezesActive;

    public static readonly bool SeasonalWeatherModActive;

    public delegate bool HasUmbrellaDelegate(Pawn pawn);

    public static HasUmbrellaDelegate HasUmbrella;


    static HarmonyWeatherEffects() {
        TerrainTagUtil.InitializeTerrainTags();
        ThingUtil.InitializeThingUtil();
        PlantReactionUtil.InitializePlantGraphics();
        BiomeUtil.InitializeDefaults();
        PawnKindUtil.InitializePawnKindUtil();

        PathModActive = ModLister.AnyModActiveNoSuffix(["mlie.desirepaths", "neku.dynamictrails"]);
        RimBrellasActive = ModLister.AnyModActiveNoSuffix(["battlemage64.Rimbrellas"]);
        StorytellerJianghuActive = ModLister.AnyModActiveNoSuffix(["zal.jianghujin"]);
        WaterFreezesActive = ModLister.AnyModActiveNoSuffix(["mlie.waterfreezes"]);
        SeasonalWeatherModActive = ModLister.AnyModActiveNoSuffix(["nightmare.weathercontrol", "mlie.seasonalweather"]);

        if (ModsConfig.OdysseyActive) {
            EffectSettings.doIce = false;
            EffectSettings.doFloods = false;
            EffectSettings.allowPawnsSwim = false;
        }

        if (WaterFreezesActive) {
            EffectSettings.doIce = false;
        }

        if (PathModActive) {
            EffectSettings.doDirtPath = false;
        }

        if (SeasonalWeatherModActive) {
            EffectSettings.seasonalWeather = false;
        }


        var harmony = new Harmony("Hali.NPS_WeatherEffects");
        harmony.PatchAll();

        if (RimBrellasActive) {
            HasUmbrella = AccessTools.MethodDelegate<HasUmbrellaDelegate>(
                AccessTools.Method("Umbrellas.UmbrellaDefMethods:HasUmbrella"));

            if (HasUmbrella == null) {
                Log.Warning(
                    "NPSWeatherEffects: Rimbrella loaded but could not find the correct method to check for umbrellas");
                RimBrellasActive = false;
            }
        }
    }
}