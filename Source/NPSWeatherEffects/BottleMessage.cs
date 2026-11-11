using System.Linq;
using System.Text;
using LudeonTK;
using RimWorld;
using Verse;
using Verse.Grammar;
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
    [DebugOutput("Text generation", true)]
    public static void MessageInABottleText() {
        StringBuilder stringBuilder = new StringBuilder();
        for (int i = 0; i < 200; i++) {

            stringBuilder.AppendLine(EffectSettings.messageInBottleSelector switch
            {
                0 when Rand.Bool => generateTravelerMessage(),
                0 => generateTipMessage(),
                1 => generateTravelerMessage(),
                _ => generateTipMessage()
            });
        }
        Log.Message(stringBuilder.ToString());

    }


    public override TaggedString GenerateImageDescription() {
        if (bottleMessage.NullOrEmpty()) {
            Log.Error("Did BottleMessage.GenerateImageDescription without initializing art: " + parent);
            InitializeArt(ArtGenerationContext.Outsider);
        }
        return bottleMessage;
    }

    public override bool Active => !bottleMessage.NullOrEmpty();

    private string bottleMessage;

    protected override string GenerateTitle(ArtGenerationContext context) {
        if (bottleMessage.NullOrEmpty()) {
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

            bottleMessage = EffectSettings.messageInBottleSelector switch
            {
                0 when Rand.Bool => generateTravelerMessage(),
                0 => generateTipMessage(),
                1 => generateTravelerMessage(),
                _ => generateTipMessage()
            };

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

    /*
     * Let me tell you the amount of bologna I went through to get this thing figured out and working.
     * It started out with the random tip messages popping up. That was easy. just copied the art comp stuff, changed some stuff, everybody happy.
     * Then someone sends me an idea "You should make messages feel like they come from actual people too" (paraphrased) Cool doesn't sound too hard.
     * Me thinking "Oh I can just look at the art sculptures, see how they do their stuff"
     * Turns out they use tales instead of rulepacks and that's a whole mess of worms that I did not need.
     * Rulepacks and tales don't normally have random pawn inserts to get names and stuff for them.
     * Apparently, with all the (limited)usage of rulepacks and stuff I got my answer in books! (You should go to your local library person reading this comment)
     * So yeah, this was a huge butt to figure out, but I finally got it working.
     * And thanks to the power of books, rather than having the cellData function that generates a random thing
     *  and has to check them all for if they're a message in a bottle, I found out that I could just use a custom thingClass. Yay me!
     *
     */
    private static string generateTravelerMessage() {
        GrammarRequest request = new GrammarRequest();
        request.Includes.Add(RulePackDefOf.NPS_MessageInABottle);
        foreach (Rule rule in TaleData_Pawn.GenerateRandom(humanLike: true).GetRules("ANYPAWN", request.Constants)) {
            request.Rules.Add(rule);
        }

        return GrammarResolver.Resolve("r_art_description", request);
    }

    private static string generateTipMessage() {
        // Borrowed from GameplayTipWindow
        return DefDatabase<TipSetDef>.AllDefsListForReading.SelectMany(set =>
                SteamDeck.IsSteamDeck && set == TipSetDefOf.GameplayTips ? set.tips.Skip(11) : set.tips)
            .RandomElement();
    }

}