using System.Linq;
using RimWorld;
using Verse;
using Verse.Steam;

namespace NPSWeather;

public class CompProperties_BottleMessage : CompProperties_Art
{
    public CompProperties_BottleMessage() {
        compClass = typeof(BottleMessage);
    }
}

public class BottleMessage : CompArt
{
    public override TaggedString GenerateImageDescription() {
        if (bottleMessage.NullOrEmpty())
        {
            Log.Error("Did BottleMessage.GenerateImageDescription without initializing art: " + parent);
            InitializeArt(ArtGenerationContext.Outsider);
        }
        return bottleMessage;
    }

    public override bool Active => !bottleMessage.NullOrEmpty();

    private string bottleMessage;

    protected override string GenerateTitle(ArtGenerationContext context) {
        if (bottleMessage.NullOrEmpty())
        {
            Log.Error("Did BottleMessage.GenerateTitle without initializing art: " + parent);
            InitializeArt(ArtGenerationContext.Outsider);
        }
        return GenText.CapitalizeAsTitle(bottleMessage);
    }

    protected override void InitializeArtInternal(Thing relatedThing, ArtGenerationContext source) {
        if (!titleInt.NullOrEmpty()) {
            return;
        }

        bottleMessage = null;

        if (CanShowArt) {
            // Borrowed from GameplayTipWindow
            bottleMessage = DefDatabase<TipSetDef>.AllDefsListForReading.SelectMany(set =>
                    SteamDeck.IsSteamDeck && set == TipSetDefOf.GameplayTips ? set.tips.Skip(11) : set.tips)
                .RandomElement();

            titleInt = "NPS_MessageInABottle".Translate();
        }
        else {
            titleInt = null;
            bottleMessage = null;
        }
    }

    public override void PostExposeData() {
        base.PostExposeData();
        // Other values get saved in base
        Scribe_Values.Look(ref bottleMessage, "bottleMessage");
    }
}