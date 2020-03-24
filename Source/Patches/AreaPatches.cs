using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HarmonyLib;
using UnityEngine;
using Verse;
using RimWorld;

using System.Reflection;
using System.Reflection.Emit;

namespace AreaInclusionExclusion.Patches
{
    internal class AreaPatches
    {
        public static bool MarkForDrawPrefix(Area __instance)
        {
            if (__instance is AreaExt)
            {
                ((AreaExt)__instance).MarkForDraw();
                return false;
            }

            return true;
        }

        public static void MarkDirtyPostfix(Area __instance)
        {
            AreaExtEventManager.OnAreaEdited(__instance);
        }

        public static void InvertPostfix(Area __instance)
        {
            AreaExtEventManager.OnAreaEdited(__instance);
        }

        public static IEnumerable<CodeInstruction> ItemPropertyGetterTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator il)
        {
            Label labelOriginal = il.DefineLabel();

            List<CodeInstruction> instList = instructions.ToList();
            yield return new CodeInstruction(OpCodes.Ldarg_0);
            yield return new CodeInstruction(OpCodes.Isinst, typeof(AreaExt));
            yield return new CodeInstruction(OpCodes.Brfalse_S, labelOriginal);
            yield return new CodeInstruction(OpCodes.Ldarg_0);
            yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(AreaExt), nameof(AreaExt.CheckAndUpdate)));
            
            for (int i = 0; i < instList.Count; ++i)
            {
                if (i == 0)
                {
                    yield return new CodeInstruction(OpCodes.Ldarg_0) { labels = new List<Label> { labelOriginal } };
                }
                else
                {
                    yield return instList[i];
                }
            }
        }
    }
}
