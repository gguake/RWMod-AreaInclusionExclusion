using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Reflection;
using System.Reflection.Emit;

using HarmonyLib;
using UnityEngine;
using Verse;
using RimWorld;

namespace AreaInclusionExclusion
{
    [StaticConstructorOnStartup]
    public class HarmonyPatches
    {
        static HarmonyPatches()
        {
            Harmony harmony = new Harmony(id: "rimworld.gguake.areainclusionexclusion.main");

            #region AreaAllowedGUI
            harmony.Patch(AccessTools.Method(type: typeof(AreaAllowedGUI), name: "DoAllowedAreaSelectors"),
                transpiler: new HarmonyMethod(typeof(Patches.AreaAllowedGUIPatches), nameof(Patches.AreaAllowedGUIPatches.DoAllowedAreaSelectorsTranspiler)));

            harmony.Patch(AccessTools.Method(type: typeof(AreaAllowedGUI), name: "DoAreaSelector"),
                prefix: new HarmonyMethod(typeof(Patches.AreaAllowedGUIPatches), nameof(Patches.AreaAllowedGUIPatches.DoAreaSelectorPrefix)));
            #endregion

            #region AreaManager
            harmony.Patch(AccessTools.Method(type: typeof(AreaManager), name: "NotifyEveryoneAreaRemoved"),
                prefix: new HarmonyMethod(typeof(Patches.AreaManagerPatches), nameof(Patches.AreaManagerPatches.NotifyEveryoneAreaRemovedPrefix)));

            harmony.Patch(AccessTools.Method(type: typeof(AreaManager), name: "ExposeData"),
                postfix: new HarmonyMethod(typeof(Patches.AreaManagerPatches), nameof(Patches.AreaManagerPatches.ExposeDataPostfix)));

            harmony.Patch(AccessTools.Method(type: typeof(AreaManager), name: "AreaManagerUpdate"),
                postfix: new HarmonyMethod(typeof(Patches.AreaManagerPatches), nameof(Patches.AreaManagerPatches.AreaManagerUpdatePostfix)));
            #endregion

            #region Area
            harmony.Patch(AccessTools.Method(type: typeof(Area), name: "MarkForDraw"),
                prefix: new HarmonyMethod(typeof(Patches.AreaPatches), nameof(Patches.AreaPatches.MarkForDrawPrefix)));

            harmony.Patch(AccessTools.Method(type: typeof(Area), name: "MarkDirty"),
                postfix: new HarmonyMethod(typeof(Patches.AreaPatches), nameof(Patches.AreaPatches.MarkDirtyPostfix)));

            harmony.Patch(AccessTools.Method(type: typeof(Area), name: "Invert"),
                postfix: new HarmonyMethod(typeof(Patches.AreaPatches), nameof(Patches.AreaPatches.InvertPostfix)));
            
            harmony.Patch(typeof(Area).GetProperties().Single(x => x.GetIndexParameters().Length > 0 && x.GetIndexParameters()[0].ParameterType == typeof(int)).GetGetMethod(),
                transpiler: new HarmonyMethod(typeof(Patches.AreaPatches), nameof(Patches.AreaPatches.ItemPropertyGetterTranspiler)));

            harmony.Patch(typeof(Area).GetProperties().Single(x => x.GetIndexParameters().Length > 0 && x.GetIndexParameters()[0].ParameterType == typeof(IntVec3)).GetGetMethod(),
                transpiler: new HarmonyMethod(typeof(Patches.AreaPatches), nameof(Patches.AreaPatches.ItemPropertyGetterTranspiler)));
            #endregion

            #region DebugLoadIDsSavingErrorsChecker
            harmony.Patch(AccessTools.Method(type: typeof(DebugLoadIDsSavingErrorsChecker), name: "RegisterReferenced"),
                prefix: new HarmonyMethod(typeof(Patches.DebugLoadIDsSavingErrorsCheckerPatches), nameof(Patches.DebugLoadIDsSavingErrorsCheckerPatches.RegisterReferencedPrefix)));
            #endregion
            
            #region LoadIDsWantedBank
            harmony.Patch(AccessTools.Method(type: typeof(LoadIDsWantedBank), name: "RegisterLoadIDReadFromXml", parameters: new Type[] { typeof(string), typeof(Type), typeof(string), typeof(IExposable) }),
                postfix: new HarmonyMethod(typeof(Patches.LoadIDsWantedBankPatches), nameof(Patches.LoadIDsWantedBankPatches.RegisterLoadIDReadFromXmlPostfix)));
            #endregion

            #region Pawn_PlayerSettings
            harmony.Patch(AccessTools.PropertySetter(typeof(Pawn_PlayerSettings), "AreaRestriction"),
                prefix: new HarmonyMethod(typeof(Patches.PlayerSettingsPatches), nameof(Patches.PlayerSettingsPatches.AreaRestrictionSetterPrefix)));

            harmony.Patch(AccessTools.Method(type: typeof(Pawn_PlayerSettings), name: "Notify_AreaRemoved"),
                postfix: new HarmonyMethod(typeof(Patches.PlayerSettingsPatches), nameof(Patches.PlayerSettingsPatches.NotifyAreaRemovedPostfix)));
            #endregion

            #region Game
            harmony.Patch(AccessTools.Method(type: typeof(MapComponentUtility), name: "MapRemoved"),
                postfix: new HarmonyMethod(typeof(Patches.GamePatches), nameof(Patches.GamePatches.MapRemovedPostfix)));
            #endregion

            Log.Message("[Area Inclusion&Exclusion] harmony patched");

        }
    }
}
