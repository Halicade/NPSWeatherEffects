using RimWorld;

namespace NPSWeather;

public class GiveMePlants : TileMutatorWorker
{
    // This is needed so that AdditionalPlants actually gives additional plants
    public GiveMePlants(TileMutatorDef def) : base(def) { }
}