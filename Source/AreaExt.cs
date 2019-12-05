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
    public class AreaExt : Area
    {
        private AreaExtID areaExtID;

        private string cachedLabel = "";
        private Color cachedColor = Color.black;

        #region FieldInfos
        internal static class FieldInfos
        {
            public static FieldInfo innerGrid = AccessTools.Field(typeof(Area), "innerGrid");
            public static FieldInfo boolGridArr = AccessTools.Field(typeof(BoolGrid), "arr");
            public static FieldInfo boolGridTrueCount = AccessTools.Field(typeof(BoolGrid), "trueCountInt");
        }
        #endregion

        public override string Label => cachedLabel;
        public override Color Color => cachedColor;

        public override int ListPriority => int.MaxValue;
        public int MapID => areaExtID.MapID;
        public bool Empty => areaExtID.Areas.Count == 0;
        private List<KeyValuePair<Area, AreaExtOperator>> InnerAreas
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

                innerAreas.SortBy(x => areaManager.AllAreas.IndexOf(x.Key));
                return innerAreas;
            }
        }
        private bool initialized = false;

        public override string GetUniqueLoadID()
        {
            return this.areaExtID.ToString();
        }

        public AreaExt(AreaExtID areaExtID)
        {
            AreaExtEventManager.Register(this);

            this.areaExtID = areaExtID;
            this.Init();
        }

        public AreaExt(Map map, AreaExtOperator op, Area area)
        {
            AreaExtEventManager.Register(this);

            var areaIDList = new List<KeyValuePair<int, AreaExtOperator>>();
            areaIDList.Add(new KeyValuePair<int, AreaExtOperator>(area.ID, op));

            this.areaExtID = new AreaExtID(map.uniqueID, areaIDList);
            this.Init();
        }

        public void Init()
        {
            if (Scribe.mode != LoadSaveMode.LoadingVars)
            {
                base.areaManager = Find.Maps.Find(x => x.uniqueID == areaExtID.MapID).areaManager;
                FieldInfos.innerGrid.SetValue(this, new BoolGrid(areaManager.map));
                Update();
            }
        }

        public AreaExt CloneWithOperationArea(AreaExtOperator op, Area area)
        {
            List<KeyValuePair<int, AreaExtOperator>> newAreaList = new List<KeyValuePair<int, AreaExtOperator>>(areaExtID.Areas);

            if (op == AreaExtOperator.Inclusion || op == AreaExtOperator.Exclusion)
            {
                var existingAreaIndex = newAreaList.FindIndex(x => x.Key == area.ID);
                if (existingAreaIndex < 0)
                {
                    newAreaList.Add(new KeyValuePair<int, AreaExtOperator>(area.ID, op));
                }
                else
                {
                    newAreaList[existingAreaIndex] = new KeyValuePair<int, AreaExtOperator>(area.ID, op);
                }
            }
            else
            {
                var existingAreaIndex = newAreaList.FindIndex(x => x.Key == area.ID);
                if (existingAreaIndex < 0)
                {
                    return new AreaExt(areaExtID);
                }
                else
                {
                    newAreaList.RemoveAll(x => x.Key == area.ID);
                    if (newAreaList.Count == 0)
                    {
                        return null;
                    }
                }
            }

            return new AreaExt(new AreaExtID(areaManager.map.uniqueID, newAreaList));
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

        public void Update()
        {
            Map map = Find.Maps.Find(x => x.uniqueID == areaExtID.MapID);
            this.areaManager = map.areaManager;

            var innerAreas = InnerAreas;

            BoolGrid innerGrid = (BoolGrid)FieldInfos.innerGrid.GetValue(this);
            bool[] arr = (bool[])FieldInfos.boolGridArr.GetValue(innerGrid);
            int length = arr.Length;

            BitArray arrBA = new BitArray(arr);
            if (innerAreas.Count > 0 && innerAreas[0].Value == AreaExtOperator.Inclusion)
            {
                arrBA.SetAll(false);
            }
            else
            {
                arrBA.SetAll(true);
            }

            foreach (var kv in innerAreas)
            {
                Area area = kv.Key;
                AreaExtOperator op = kv.Value;
                bool[] targetArr = (bool[])FieldInfos.boolGridArr.GetValue(FieldInfos.innerGrid.GetValue(area));
                if (op == AreaExtOperator.Inclusion)
                {
                    if (targetArr.Length == length)
                    {
                        BitArray targetArrBA = new BitArray(targetArr);
                        arrBA = arrBA.Or(targetArrBA);
                    }
                    else
                    {
                        Log.Warning(string.Format("Area Inclusion is skipped since array size is not match {0}, {1} != {2}", this.GetUniqueLoadID(), targetArr.Length, length));
                    }
                }
                else if (op == AreaExtOperator.Exclusion)
                {
                    if (targetArr.Length == length)
                    {
                        BitArray targetArrBA = new BitArray(targetArr).Not();
                        arrBA = arrBA.And(targetArrBA);
                    }
                    else
                    {
                        Log.Warning(string.Format("Area Exclusion is skipped since array size is not match {0}, {1} != {2}", this.GetUniqueLoadID(), targetArr.Length, length));
                    }
                }
            }

            arrBA.CopyTo(arr, 0);
            FieldInfos.boolGridArr.SetValue(innerGrid, arr);
            FieldInfos.boolGridTrueCount.SetValue(innerGrid, arr.Count(x => x));

            var builder = new StringBuilder();
            for (int i = 0; i < innerAreas.Count; ++i)
            {
                if (i > 0 && innerAreas[i].Value == AreaExtOperator.Inclusion)
                {
                    builder.Append("+");
                }

                if (innerAreas[i].Value == AreaExtOperator.Exclusion)
                {
                    builder.Append("-");
                }

                builder.Append(innerAreas[i].Key.Label);
            }

            cachedLabel = builder.ToString();

            if (innerAreas.Count == 1)
            {
                cachedColor = innerAreas[0].Key.Color;
            }
            else
            {
                cachedColor = Color.black;
            }

            initialized = true;
        }

        public void OnAreaEdited(Area area)
        {
            if (areaExtID.Areas.Any(x => x.Key == area.ID))
            {
                Update();
            }
        }

        public void OnAreaRemoved(Area area)
        {
            if (areaExtID.Areas.Any(x => x.Key == area.ID))
            {
                areaExtID.Areas.RemoveAll(x => x.Key == area.ID);
                Update();
            }
        }
    }
}
