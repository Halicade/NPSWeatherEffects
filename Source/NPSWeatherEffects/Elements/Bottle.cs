using RimWorld;
using Verse;

namespace NPSWeather;

public class Bottle : Building_Art
{
    public override void PostPostMake() {
        base.PostPostMake();
        this.TryGetComp<BottleMessage>()?.InitializeArt(ArtGenerationContext.Outsider);
    }
}