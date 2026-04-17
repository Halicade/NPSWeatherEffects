using RimWorld;

namespace NPSWeather;

[DefOf]
public class MapMeshDefOf
{

        public static MapMeshFlagDef NPS_Frost;
        
        public static MapMeshFlagDef NPS_Rain;
        
        static MapMeshDefOf() {
                DefOfHelper.EnsureInitializedInCtor(typeof(MapMeshDefOf));
        }
}