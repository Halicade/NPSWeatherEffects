using System.Collections.Generic;
using Verse;

namespace NPSWeather;

public class PawnKindUtil
{
    public static readonly List<PawnKindDef> BeachAnimals = [];
    public static readonly List<PawnKindDef> CrabCritters = [];

    public static void InitializePawnKindUtil() {
        List<PawnKindDef> pawnList = DefDatabase<PawnKindDef>.AllDefsListForReading;

        foreach (PawnKindDef pawn in pawnList) {
            if (pawn.GetModExtension<WashUpOnBeach>() is { } beachExtension) {
                BeachAnimals.Add(pawn);
                if (beachExtension.isCrabCritter) {
                    CrabCritters.Add(pawn);
                }
            }
        }
    }
}