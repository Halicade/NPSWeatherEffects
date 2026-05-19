using RimWorld;
using Verse;

namespace NPSWeather;

internal class IncidentWorker_Terraform_Jin
{
    //Mod being referenced https://steamcommunity.com/sharedfiles/filedetails/?id=3403972335
    //Postfix on YaomaJin.IncidentWorker_Terraform_Jin to regen cells list after map terraforming
    public static void Postfix(bool __result, IncidentParms parms) {
        if (__result) {
            Map mapBeingTerraformed = (Map)parms?.target;
            Watcher watcherComponent = mapBeingTerraformed?.GetComponent<Watcher>();
            watcherComponent?.regenCellLists = true;
        }
    }
}