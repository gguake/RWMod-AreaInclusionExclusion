using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Collections;
using System.Text.RegularExpressions;

using Harmony;
using UnityEngine;
using Verse;
using RimWorld;

namespace AreaInclusionExclusion
{
    public class AreaExtEventManager
    {
        private static List<WeakReference<AreaExt>> allAreaExts = new List<WeakReference<AreaExt>>();
        
        public static void Register(AreaExt areaExt)
        {
            allAreaExts.Add(new WeakReference<AreaExt>(areaExt));
        }

        public static void CheckAndRemoveDeadRef()
        {
            allAreaExts.RemoveAll(x => !x.IsAlive);
        }

        public static void OnAreaEdited(Area area)
        {
            CheckAndRemoveDeadRef();

            foreach (var r in allAreaExts)
            {
                r.Target.OnAreaEdited(area);
            }
        }

        public static void OnAreaRemoved(Area area)
        {
            CheckAndRemoveDeadRef();

            foreach (var r in allAreaExts)
            {
                r.Target.OnAreaRemoved(area);
            }
        }

        public static void OnAreaManagerUpdate()
        {
            CheckAndRemoveDeadRef();

            foreach (var r in allAreaExts)
            {
                r.Target.AreaUpdate();
            }
        }
    }
}
