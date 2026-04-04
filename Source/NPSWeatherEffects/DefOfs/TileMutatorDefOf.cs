using RimWorld;

namespace NPSWeather;

[DefOf]
public class TileMutatorDefOf
{


        public static TileMutatorDef NPS_ColdSpringMutator;
        
        public static TileMutatorDef NPS_WeakOceanTide;
        
        public static TileMutatorDef NPS_StrongOceanTide;

        public static TileMutatorDef NPS_DiurnalTide;

        public static TileMutatorDef NPS_MixedSemidiurnal;

        [MayRequire("vanillaexpanded.vexploratione")]
        public static TileMutatorDef VEE_RisingWaters;
        
        static TileMutatorDefOf()
        {
                DefOfHelper.EnsureInitializedInCtor(typeof(TileMutatorDefOf));
        }
}