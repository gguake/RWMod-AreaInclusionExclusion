using System.Linq;
using System.Text;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;

using HarmonyLib;
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
#if DEBUG
            Log.Message(string.Format("new area registerd: {0}", areaExt.GetUniqueLoadID()));
#endif
            allAreaExts.Add(new WeakReference<AreaExt>(areaExt));
        }

        public static void CheckAndRemoveDeadRef()
        {
#if DEBUG
            int n = allAreaExts.Count(x => !x.IsAlive);
            if (n > 0)
            {
                Log.Message(string.Format("{0} area are removed", n));
            }
#endif

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

#if DEBUG
            Log.Message($"Area {area.ID} Removed");
#endif

            foreach (var areaExt in allAreaExts)
            {
                areaExt.Target.OnAreaRemoved(area);
            }
        }

        public static void OnMapRemoved(Map map)
        {
#if DEBUG
            Log.Message($"Map {map.uniqueID} is removed");
#endif
            foreach (var pawn in Find.WorldPawns.AllPawnsAliveOrDead)
            {
                if (pawn.playerSettings == null)
                {
                    continue;
                }

                var areaExt = pawn.playerSettings.AreaRestriction as AreaExt;
                if (areaExt != null)
                {
                    if (map.uniqueID == areaExt.MapID)
                    {
                        pawn.playerSettings.AreaRestriction = null;
                    }
                }
            }
        }

        private static int areaManagerRefCheckTimer = 0;
        private const int areaManagerRefCheckTimerDelay = 300;
        public static void OnAreaManagerUpdate()
        {
            if ((areaManagerRefCheckTimer++ % areaManagerRefCheckTimerDelay) == 0)
            {
                areaManagerRefCheckTimer = 0;
                CheckAndRemoveDeadRef();
            }

            foreach (var areaExt in allAreaExts)
            {
                areaExt.Target.OnAreaUpdate();
            }
        }
    }
}
