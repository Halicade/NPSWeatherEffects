using RimWorld;

namespace NPSWeather;

[DefOf]
public class MapMeshDefOf
{

        public static MapMeshFlagDef NPS_Frost;
        
        static MapMeshDefOf() {
                DefOfHelper.EnsureInitializedInCtor(typeof(MapMeshDefOf));
        }
}