using System;
using System.Collections.Generic;
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
    public enum AreaExtOperator
    {
        None,

        Inclusion,
        Exclusion,
    }

    public class AreaExtID
    {
        private static Regex decoder = new Regex(@"(\@\!\[Map_(\d*)\])?(([\+\-])(\d+))", RegexOptions.Compiled);

        public int MapID { get; private set; } = -1;
        public List<KeyValuePair<int, AreaExtOperator>> Areas { get; private set; }
        
        public AreaExtID(int mapID, IEnumerable<KeyValuePair<int, AreaExtOperator>> areas)
        {
            this.MapID = mapID;
            this.Areas = new List<KeyValuePair<int, AreaExtOperator>>(areas);
        }

        public AreaExtID(string id)
        {
            var matches = decoder.Matches(id);
            Areas = new List<KeyValuePair<int, AreaExtOperator>>(matches.Count);

            for (int i = 0; i < matches.Count; ++i)
            {
                if (i == 0)
                {
                    MapID = int.Parse(matches[i].Groups[2].Value);
                }

                Areas.Add(new KeyValuePair<int, AreaExtOperator>(int.Parse(matches[i].Groups[5].Value), matches[i].Groups[4].Value == "+" ? AreaExtOperator.Inclusion : AreaExtOperator.Exclusion));
            }
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendFormat("@![Map_{0}]", MapID);
            for (int i = 0; i < Areas.Count; ++i)
            {
                if (Areas[i].Value == AreaExtOperator.None)
                {
                    Log.Warning("Invalid Area operator detected: " + Areas[i].Value.ToString());
                    continue;
                }

                sb.Append(Areas[i].Value == AreaExtOperator.Inclusion ? "+" : "-");
                sb.Append(Areas[i].Key.ToString());
            }

            return sb.ToString();
        }
    }
}
