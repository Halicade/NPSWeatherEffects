using RimWorld;
using Verse;

namespace NPSWeather;

public class IncidentWorker_Drought : IncidentWorker_MakeGameCondition
{

    protected override bool TryExecuteWorker(IncidentParms parms)
    {
        if (!EffectSettings.doWeather) {
            return false;
        }
        
        var map = (Map)parms.target;

        if (map.weatherManager.RainRate > 0 || map.weatherManager.SnowRate > 0)
        {
            return false;
        }

        Find.LetterStack.ReceiveLetter(def.letterLabel.Translate(), def.letterText.Translate(), def.letterDef);


        return base.TryExecuteWorker(parms);
    }
}