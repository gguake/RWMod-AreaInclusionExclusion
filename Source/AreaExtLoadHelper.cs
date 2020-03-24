using System.Text;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;

using HarmonyLib;
using UnityEngine;
using Verse;
using RimWorld;

namespace AreaInclusionExclusion
{
    public class AreaExtLoadHelper
    {
        private static Dictionary<int, List<WeakReference<AreaExt>>> cache = new Dictionary<int, List<WeakReference<AreaExt>>>();
        
        public static AreaExt OnLoadingVars(string id)
        {
            AreaExtID areaExtID = new AreaExtID(id);
            if (!cache.ContainsKey(areaExtID.MapID))
            {
                cache[areaExtID.MapID] = new List<WeakReference<AreaExt>>();
            }

            AreaExt areaExt;
            WeakReference<AreaExt> refAreaExt = cache[areaExtID.MapID].Find(x => x.IsAlive && x.Target.GetUniqueLoadID() == id);
            if (refAreaExt != null)
            {
                areaExt = refAreaExt.Target;
            }
            else
            {
                areaExt = new AreaExt(areaExtID);
                cache[areaExtID.MapID].Add(new WeakReference<AreaExt>(areaExt));
            }

            return areaExt;
        }

        public static void OnResolveCrossRef(int mapID)
        {
            if (cache.ContainsKey(mapID))
            {
                foreach (var v in cache[mapID])
                {
                    if (v.IsAlive)
                    {
                        v.Target.Init();
                    }
                }
            }

            cache.Remove(mapID);
        }
    }
}
