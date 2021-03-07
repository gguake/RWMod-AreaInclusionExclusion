using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Collections;
using System.Text.RegularExpressions;

using HarmonyLib;
using UnityEngine;
using Verse;
using RimWorld;

namespace AreaInclusionExclusion
{
    public class AreaExt : Area
    {
        #region FieldInfos
        internal static class FieldInfos
        {
            public static FieldInfo innerGrid = AccessTools.Field(typeof(Area), "innerGrid");
            public static FieldInfo boolGridArr = AccessTools.Field(typeof(BoolGrid), "arr");
            public static FieldInfo boolGridTrueCount = AccessTools.Field(typeof(BoolGrid), "trueCountInt");
        }
        #endregion

        private readonly AreaExtID areaExtID;
        private string cachedLabel = "";
        private Color cachedColor = Color.black;
        private bool initialized = false;

        private AreaExtCellDrawer drawer;

        public override string Label => cachedLabel;
        public override Color Color => cachedColor;
        public override int ListPriority => int.MaxValue;
        public int MapID => areaExtID.MapID;
        public bool Empty => areaExtID.Areas.Count == 0;
        public bool IsOneInclusion => areaExtID.Areas.Count == 1 && areaExtID.Areas[0].Value == AreaExtOperator.Inclusion;
        public bool IsWholeExclusive => areaExtID.Areas.Count >= 1 && areaExtID.Areas[0].Value == AreaExtOperator.Exclusion;

        public bool Contains(Area area) => areaExtID.Areas.Any(x => x.Key == area.ID);
        public bool Contains(Area area, AreaExtOperator op) => areaExtID.Areas.Any(x => x.Key == area?.ID && x.Value == op);

        public List<KeyValuePair<Area, AreaExtOperator>> InnerAreas
        {
            get
            {
                var innerAreas = new List<KeyValuePair<Area, AreaExtOperator>>();
                for (int i = 0; i < areaExtID.Areas.Count; ++i)
                {
                    int areaID = areaExtID.Areas[i].Key;
                    Area area = areaManager.AllAreas.Find(x => x.ID == areaID);
                    innerAreas.Add(new KeyValuePair<Area, AreaExtOperator>(area, areaExtID.Areas[i].Value));
                }

                return innerAreas;
            }
        }

        public override string GetUniqueLoadID()
        {
            return this.areaExtID.ToString();
        }

        public AreaExt(AreaExtID areaExtID)
        {
            this.areaExtID = areaExtID;
            this.Init();

            AreaExtEventManager.Register(this);
        }

        public AreaExt(Map map, AreaExtOperator op, Area area)
        {
            var areaIDList = new List<KeyValuePair<int, AreaExtOperator>>();
            areaIDList.Add(new KeyValuePair<int, AreaExtOperator>(area.ID, op));

            this.areaExtID = new AreaExtID(map.uniqueID, areaIDList);
            this.Init();

            AreaExtEventManager.Register(this);
        }

        public void Init()
        {
            if (Scribe.mode != LoadSaveMode.LoadingVars)
            {
                base.areaManager = Find.Maps.Find(x => x.uniqueID == areaExtID.MapID).areaManager;
                FieldInfos.innerGrid.SetValue(this, new BoolGrid(areaManager.map));
                drawer = new AreaExtCellDrawer(this);
                Update();
            }
        }

        public AreaExt CloneWithOperationArea(AreaExtOperator op, Area area)
        {
            List<KeyValuePair<int, AreaExtOperator>> newAreaList = new List<KeyValuePair<int, AreaExtOperator>>(areaExtID.Areas);
            newAreaList.RemoveAll(x => x.Key == area.ID);

            AreaExtID newID = areaExtID;
            if (op == AreaExtOperator.Inclusion)
            {
                newAreaList.Insert(0, new KeyValuePair<int, AreaExtOperator>(area.ID, AreaExtOperator.Inclusion));
            }
            else if (op == AreaExtOperator.Exclusion)
            {
                newAreaList.Add(new KeyValuePair<int, AreaExtOperator>(area.ID, AreaExtOperator.Exclusion));
            }
            else
            {
                if (newAreaList.Count == 0)
                {
                    return null;
                }
            }

            newID = new AreaExtID(areaManager.map.uniqueID, newAreaList);
            return new AreaExt(newID);
        }

        public AreaExtOperator GetAreaOperator(int areaID)
        {
            return areaExtID.Areas.Find(x => x.Key == areaID).Value;
        }

        public void CheckAndUpdate()
        {
            if (!initialized)
            {
                Update();
            }
        }

        public static BitArray GetAreaBitArray(Area area)
        {
            return new BitArray((bool[])FieldInfos.boolGridArr.GetValue(FieldInfos.innerGrid.GetValue(area)));
        }

        public void Update()
        {
#if DEBUG
            var watch = System.Diagnostics.Stopwatch.StartNew();
#endif

            Map map = Find.Maps.Find(x => x.uniqueID == areaExtID.MapID);
            if (map == null)
            {
                return;
            }

            this.areaManager = map.areaManager;

            var innerAreas = InnerAreas;

            BoolGrid innerGrid = (BoolGrid)FieldInfos.innerGrid.GetValue(this);
            bool[] arr = (bool[])FieldInfos.boolGridArr.GetValue(innerGrid);

            BitArray arrBA = new BitArray(arr);
            if (innerAreas.Count > 0 && innerAreas.Any(x => x.Value == AreaExtOperator.Inclusion))
            {
                arrBA.SetAll(false);
            }
            else
            {
                arrBA.SetAll(true);
            }

            var areaNameBuilder = new StringBuilder();
            for (int i = 0; i < innerAreas.Count; ++i)
            {
                var kv = innerAreas[i];

                Area area = kv.Key;
                AreaExtOperator op = kv.Value;

                BitArray targetArrBA = GetAreaBitArray(area);
                if (op == AreaExtOperator.Inclusion)
                {
                    arrBA = arrBA.Or(targetArrBA);
                    if (i > 0)
                    {
                        areaNameBuilder.Append("+");
                    }
                }
                else if (op == AreaExtOperator.Exclusion)
                {
                    arrBA = arrBA.And(targetArrBA.Not());
                    areaNameBuilder.Append("-");
                }

                areaNameBuilder.Append(area.Label);
            }

            arrBA.CopyTo(arr, 0);
            FieldInfos.boolGridArr.SetValue(innerGrid, arr);
            FieldInfos.boolGridTrueCount.SetValue(innerGrid, arr.Count(x => x));
            
            cachedLabel = areaNameBuilder.ToString();

            if (innerAreas.Count == 1)
            {
                cachedColor = innerAreas[0].Key.Color;
            }
            else
            {
                cachedColor = Color.black;
            }

            initialized = true;
            drawer.dirty = true;

#if DEBUG
            watch.Stop();
            Log.Message(string.Format("Update elapsed : {0}", watch.ElapsedMilliseconds));
#endif
        }

        public void OnAreaEdited(Area area)
        {
            if (areaExtID.Areas.Any(x => x.Key == area.ID))
            {
                initialized = false;
            }
        }

        public void OnAreaRemoved(Area area)
        {
            if (areaExtID.Areas.Any(x => x.Key == area.ID))
            {
                initialized = false;
                areaExtID.OnAreaRemoved(area);
            }
        }

        public void OnAreaUpdate()
        {
            if (!initialized)
            {
                Update();
            }

            drawer.Update();
        }

        public new void MarkForDraw()
        {
            if (MapID == Find.CurrentMap.uniqueID)
            {
                drawer.MarkForDraw();
            }
        }
    }
}
