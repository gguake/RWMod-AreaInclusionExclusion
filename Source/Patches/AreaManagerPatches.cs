using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

using Harmony;
using UnityEngine;
using Verse;
using RimWorld;

namespace AreaInclusionExclusion.Patches
{
    internal class AreaManagerPatches
    {
        public static bool NotifyEveryoneAreaRemovedPrefix(Area area)
        {
            AreaExtEventManager.OnAreaRemoved(area);
            return true;
        }

        public static void ExposeDataPostfix(AreaManager __instance)
        {
            if (Scribe.mode == LoadSaveMode.ResolvingCrossRefs)
            {
                AreaExtLoadHelper.OnResolveCrossRef(__instance.map.uniqueID);
            }
        }

        public static void AreaManagerUpdatePostfix(AreaManager __instance)
        {
            AreaExtEventManager.OnAreaManagerUpdate();
        }
    }
}
