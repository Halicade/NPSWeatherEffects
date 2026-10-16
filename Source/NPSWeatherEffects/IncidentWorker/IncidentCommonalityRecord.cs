using System.Xml;
using RimWorld;
using Verse;

namespace NPSWeather;

public class TKKN_IncidentCommonalityRecord
{
    public float commonality;
    public IncidentDef incident;

    public void LoadDataFromXmlCustom(XmlNode xmlRoot) {
        DirectXmlCrossRefLoader.RegisterObjectWantsCrossRef(this, "incident", xmlRoot.Name);
        if (xmlRoot.FirstChild != null)
            commonality = ParseHelper.FromString<float>(xmlRoot.FirstChild.Value);
    }
}