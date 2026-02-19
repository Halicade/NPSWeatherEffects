using System.Xml;
using Verse;

namespace NPSWeather;

public class GrassGraphics : PatchOperation
{

    public PatchOperation patchOp;
    
    
    protected override bool ApplyWorker(XmlDocument xml)
    {
        if (EffectSettings.changeGrassGraphics) {
            return patchOp.Apply(xml);
        }

        return true;
    }
}