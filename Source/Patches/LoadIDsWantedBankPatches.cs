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

namespace AreaInclusionExclusion.Patches
{
    internal class LoadIDsWantedBankPatches
    {
        private static Regex mapIDDecoder = new Regex(@"\@\!\[Map_(\d*)\]", RegexOptions.Compiled);

        static FieldInfo fieldLoadedObjectDirectory = AccessTools.Field(typeof(CrossRefHandler), "loadedObjectDirectory");
        static FieldInfo fieldAllObjectsByLoadID = AccessTools.Field(typeof(LoadedObjectDirectory), "allObjectsByLoadID");

        public static void RegisterLoadIDReadFromXmlPostfix(LoadIDsWantedBank __instance, string targetLoadID, Type targetType, string pathRelToParent, IExposable parent)
        {
            if (targetType == typeof(Area) || targetType.IsInstanceOfType(typeof(Area)))
            {
                if (targetLoadID.StartsWith("@!"))
                {
                    Dictionary<string, ILoadReferenceable> allObjectByLoadID = fieldAllObjectsByLoadID.GetValue(fieldLoadedObjectDirectory.GetValue(Scribe.loader.crossRefs))
                        as Dictionary<string, ILoadReferenceable>;

                    AreaExt areaExt = AreaExtLoadHelper.OnLoadingVars(targetLoadID);
                    if (!allObjectByLoadID.ContainsKey(targetLoadID))
                    {
                        allObjectByLoadID.Add(targetLoadID, areaExt);
                    }
                }
            }
        }
    }
}
