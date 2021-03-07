using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using RimWorld;

namespace AreaInclusionExclusion.Patches
{
    public static class GamePatches
    {
        public static void MapRemovedPostfix(Map map)
        {
            AreaExtEventManager.OnMapRemoved(map);
        }
    }
}
