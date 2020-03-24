using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

using HarmonyLib;
using UnityEngine;
using Verse;
using RimWorld;

namespace AreaInclusionExclusion.Patches
{
    internal class PlayerSettingsPatches
    {
        internal static class FieldInfos
        {
            public static FieldInfo areaAllowedInt = AccessTools.Field(typeof(Pawn_PlayerSettings), "areaAllowedInt");
        }

        public static void NotifyAreaRemovedPostfix(Pawn_PlayerSettings __instance, Area area)
        {
            AreaExt pawnArea = FieldInfos.areaAllowedInt.GetValue(__instance) as AreaExt;
            if (pawnArea != null && pawnArea.Empty)
            {
                FieldInfos.areaAllowedInt.SetValue(__instance, null);
            }
        }
    }
}
