using RimWorld;
using Verse;

namespace NPSWeather;

public class IncidentWorker_TKKN_Weather : IncidentWorker_MakeGameCondition
{
    private readonly bool relevantSetting = EffectSettings.doWeather;
    public string label;
    public string text;
    public ThingDef thingDef;

    protected bool settingsCheck()
    {
        return relevantSetting;
    }

    protected override bool TryExecuteWorker(IncidentParms parms) {
        return settingsCheck() && base.TryExecuteWorker(parms);
    }
}