using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using HarmonyLib;
using UnityEngine;
using Verse;
using RimWorld;

namespace AreaInclusionExclusion.Patches
{
    internal class DebugLoadIDsSavingErrorsCheckerPatches
    {
        public static bool RegisterReferencedPrefix(ILoadReferenceable obj)
        {
            if (Scribe.mode == LoadSaveMode.Saving && obj is AreaExt)
            {
                return false;
            }

            return true;
        }
    }
}
