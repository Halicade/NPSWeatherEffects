using System.Xml;
using Verse;

namespace NPSWeather;

public class WetnessThought : PatchOperationReplace
{
    protected override bool ApplyWorker(XmlDocument xml)
    {
        if (EffectSettings.allowPawnsToGetWet) {
            return base.ApplyWorker(xml);
        }

        return true;
    }
    
}