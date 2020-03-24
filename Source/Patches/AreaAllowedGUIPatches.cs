using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

using HarmonyLib;
using UnityEngine;
using Verse;
using Verse.Sound;
using RimWorld;

namespace AreaInclusionExclusion.Patches
{
    internal class AreaAllowedGUIPatches
    {
        internal enum AreaAllowedEditMode
        {
            None,
            AddInclusion,
            AddExclusion,
            Remove,
        }

        internal static class FieldInfos
        {
            public static FieldInfo dragging = AccessTools.Field(typeof(AreaAllowedGUI), "dragging");
        }

        private static AreaAllowedEditMode editMode = AreaAllowedEditMode.None;
        private delegate void DoAreaSelectorDelegate(Rect rect, Pawn p, Area area);
        private static DoAreaSelectorDelegate delegateDoAreaSelector = (DoAreaSelectorDelegate)AccessTools.Method(typeof(AreaAllowedGUI), "DoAreaSelector").CreateDelegate(typeof(DoAreaSelectorDelegate));

        public static IEnumerable<CodeInstruction> DoAllowedAreaSelectorsTranspiler(IEnumerable<CodeInstruction> codeInstructions, ILGenerator iLGenerator)
        {
            List<CodeInstruction> instructionList = codeInstructions.ToList();
            int w;
            
            w = 0;
            for (int i = 0; i < instructionList.Count; ++i)
            {
                if (instructionList[i].opcode == OpCodes.Call && (MethodInfo)instructionList[i].operand == AccessTools.Method(typeof(AreaAllowedGUI), "DoAreaSelector"))
                {
                    if (w == 0)
                    {
                        int j = i - 12;
                        Label label1 = iLGenerator.DefineLabel();
                        Label label2 = iLGenerator.DefineLabel();

                        instructionList[j].labels.Add(label1);

                        List<CodeInstruction> injections = new List<CodeInstruction>
                        {
                            new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(AreaAllowedGUIPatches), nameof(editMode))),
                            new CodeInstruction(OpCodes.Brtrue_S, label1),
                            new CodeInstruction(OpCodes.Call, AccessTools.PropertyGetter(typeof(Event), "current")),
                            new CodeInstruction(OpCodes.Callvirt, AccessTools.PropertyGetter(typeof(Event), "rawType")),
                            new CodeInstruction(OpCodes.Brtrue_S, label1),
                            new CodeInstruction(OpCodes.Ldarg_0),
                            new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Mouse), "IsOver")),
                            new CodeInstruction(OpCodes.Brfalse_S, label1),
                            new CodeInstruction(OpCodes.Call, AccessTools.PropertyGetter(typeof(Event), "current")),
                            new CodeInstruction(OpCodes.Callvirt, AccessTools.PropertyGetter(typeof(Event), "button")),
                            new CodeInstruction(OpCodes.Brfalse_S, label2),
                            new CodeInstruction(OpCodes.Call, AccessTools.PropertyGetter(typeof(Event), "current")),
                            new CodeInstruction(OpCodes.Callvirt, AccessTools.PropertyGetter(typeof(Event), "button")),
                            new CodeInstruction(OpCodes.Ldc_I4_1),
                            new CodeInstruction(OpCodes.Bne_Un_S, label1),
                            new CodeInstruction(OpCodes.Ldc_I4_1) { labels = new List<Label>() { label2 } },
                            new CodeInstruction(OpCodes.Stsfld, AccessTools.Field(typeof(AreaAllowedGUI), "dragging")),
                        };

                        instructionList.InsertRange(j, injections);
                        i += injections.Count;
                    }

                    w++;
                }
            }

            w = 0;
            for (int i = 0; i < instructionList.Count; ++i)
            {
                if (instructionList[i].opcode == OpCodes.Ret)
                {
                    if (w == 1)
                    {
                        Label label1 = iLGenerator.DefineLabel();
                        Label label2 = iLGenerator.DefineLabel();

                        instructionList[i].labels.Add(label1);

                        List<CodeInstruction> injections = new List<CodeInstruction>
                        {
                            new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(AreaAllowedGUIPatches), nameof(editMode))),
                            new CodeInstruction(OpCodes.Brfalse_S, label1),
                            new CodeInstruction(OpCodes.Call, AccessTools.PropertyGetter(typeof(Event), "current")),
                            new CodeInstruction(OpCodes.Callvirt, AccessTools.PropertyGetter(typeof(Event), "rawType")),
                            new CodeInstruction(OpCodes.Ldc_I4_1),
                            new CodeInstruction(OpCodes.Bne_Un_S, label1),
                            new CodeInstruction(OpCodes.Call, AccessTools.PropertyGetter(typeof(Event), "current")),
                            new CodeInstruction(OpCodes.Callvirt, AccessTools.PropertyGetter(typeof(Event), "button")),
                            new CodeInstruction(OpCodes.Brfalse_S, label2),
                            new CodeInstruction(OpCodes.Call, AccessTools.PropertyGetter(typeof(Event), "current")),
                            new CodeInstruction(OpCodes.Callvirt, AccessTools.PropertyGetter(typeof(Event), "button")),
                            new CodeInstruction(OpCodes.Ldc_I4_1),
                            new CodeInstruction(OpCodes.Bne_Un_S, label1),
                            new CodeInstruction(OpCodes.Ldc_I4_0) { labels = new List<Label>() { label2 } },
                            new CodeInstruction(OpCodes.Stsfld, AccessTools.Field(typeof(AreaAllowedGUI), "dragging")),
                            new CodeInstruction(OpCodes.Ldc_I4_0),
                            new CodeInstruction(OpCodes.Stsfld, AccessTools.Field(typeof(AreaAllowedGUIPatches), nameof(editMode))),
                        };

                        instructionList.InsertRange(i, injections);
                        i += injections.Count;
                    }

                    w++;
                }
            }


            return instructionList;
        }

        public static bool DoAreaSelectorPrefix(Rect rect, Pawn p, Area area)
        {
            MouseoverSounds.DoRegion(rect);
            rect = rect.ContractedBy(1f);
            GUI.DrawTexture(rect, (area == null) ? BaseContent.GreyTex : area.ColorTexture);
            Text.Anchor = TextAnchor.MiddleLeft;
            string text = AreaUtility.AreaAllowedLabel_Area(area);
            Rect rect2 = rect;
            rect2.xMin += 3f;
            rect2.yMin += 2f;
            Widgets.Label(rect2, text);
            
            bool dragging = (bool)FieldInfos.dragging.GetValue(null);
            Area currentArea = p.playerSettings.AreaRestriction;
            AreaExt currentAreaExt = p.playerSettings.AreaRestriction as AreaExt;
            if (area != null && currentAreaExt != null)
            {
                AreaExtOperator areaOp = currentAreaExt.GetAreaOperator(area.ID);
                if (areaOp == AreaExtOperator.Inclusion)
                {
                    GUI.color = Color.white;
                    Widgets.DrawBox(rect, 2);
                    GUI.color = Color.white;
                }
                else if (areaOp == AreaExtOperator.Exclusion)
                {
                    GUI.color = Color.red;
                    Widgets.DrawBox(rect, 2);
                    GUI.color = Color.white;
                }
            }
            else
            {
                if (currentArea == area)
                    Widgets.DrawBox(rect, 2);
            }

            if (Mouse.IsOver(rect))
            {
                if (area != null)
                {
                    area.MarkForDraw();
                }
            }

            if (dragging && Mouse.IsOver(rect))
            {
                if (editMode == AreaAllowedEditMode.None)
                {
                    if (area != null && currentAreaExt != null)
                    {
                        var t = currentAreaExt.GetAreaOperator(area.ID);
                        if (t != AreaExtOperator.None)
                        {
                            if ((Event.current.button == 0 && t == AreaExtOperator.Inclusion) ||
                                (Event.current.button == 1 && t == AreaExtOperator.Exclusion))
                            {
                                editMode = AreaAllowedEditMode.Remove;
                            }
                            else
                            {
                                editMode = Event.current.button == 0 ? AreaAllowedEditMode.AddInclusion : AreaAllowedEditMode.AddExclusion;
                            }
                        }
                        else
                        {
                            editMode = Event.current.button == 0 ? AreaAllowedEditMode.AddInclusion : AreaAllowedEditMode.AddExclusion;
                        }
                    }
                    else
                    {
                        if (currentArea == area)
                        {
                            editMode = AreaAllowedEditMode.Remove;
                        }
                        else
                        {
                            if (Event.current.button == 0)
                            {
                                editMode = AreaAllowedEditMode.AddInclusion;
                            }
                            else if (Event.current.button == 1)
                            {
                                editMode = AreaAllowedEditMode.AddExclusion;
                            }
                        }
                    }
                }

                if (editMode == AreaAllowedEditMode.AddInclusion)
                {
                    if (area == null)
                    {
                        p.playerSettings.AreaRestriction = null;
                        SoundDefOf.Designate_DragStandard_Changed.PlayOneShotOnCamera(null);
                    }
                    else if (currentAreaExt != null)
                    {
                        if (currentAreaExt.GetAreaOperator(area.ID) != AreaExtOperator.Inclusion)
                        {
                            p.playerSettings.AreaRestriction = currentAreaExt.CloneWithOperationArea(AreaExtOperator.Inclusion, area);
                            SoundDefOf.Designate_DragStandard_Changed.PlayOneShotOnCamera(null);
                        }
                    }
                    else
                    {
                        p.playerSettings.AreaRestriction = new AreaExt(area.Map, AreaExtOperator.Inclusion, area);
                        SoundDefOf.Designate_DragStandard_Changed.PlayOneShotOnCamera(null);
                    }
                }

                if (editMode == AreaAllowedEditMode.AddExclusion)
                {
                    if (area == null)
                    {
                        p.playerSettings.AreaRestriction = null;
                        SoundDefOf.Designate_DragStandard_Changed.PlayOneShotOnCamera(null);
                    }
                    else if (currentAreaExt != null)
                    {
                        if (currentAreaExt.GetAreaOperator(area.ID) != AreaExtOperator.Exclusion)
                        {
                            p.playerSettings.AreaRestriction = currentAreaExt.CloneWithOperationArea(AreaExtOperator.Exclusion, area);
                            SoundDefOf.Designate_DragStandard_Changed.PlayOneShotOnCamera(null);
                        }
                    }
                    else
                    {
                        p.playerSettings.AreaRestriction = new AreaExt(area.Map, AreaExtOperator.Exclusion, area);
                        SoundDefOf.Designate_DragStandard_Changed.PlayOneShotOnCamera(null);
                    }
                }

                if (editMode == AreaAllowedEditMode.Remove)
                {
                    if (currentAreaExt != null)
                    {
                        if (area != null)
                        {
                            if (currentAreaExt.GetAreaOperator(area.ID) != AreaExtOperator.None)
                            {
                                p.playerSettings.AreaRestriction = currentAreaExt.CloneWithOperationArea(AreaExtOperator.None, area);
                                SoundDefOf.Designate_DragStandard_Changed.PlayOneShotOnCamera(null);
                            }
                        }
                    }
                    else
                    {
                        if (currentArea != null)
                        {
                            p.playerSettings.AreaRestriction = null;
                            SoundDefOf.Designate_DragStandard_Changed.PlayOneShotOnCamera(null);
                        }
                    }
                }
            }

            Text.Anchor = TextAnchor.UpperLeft;
            TooltipHandler.TipRegion(rect, text);

            return false;
        }
    }
}
