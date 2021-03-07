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
            AreaExtOperator currentAreaExtOp = currentAreaExt?.GetAreaOperator(area?.ID ?? -1) ?? AreaExtOperator.None;

            // null area인 경우, 현재 구역의 첫 구역이 exclusion이면 inclusion으로 선택된것처럼 출력해준다. (실제로는 아님)
            if (area == null)
            {
                if (currentArea == null || (currentAreaExt != null && currentAreaExt.IsWholeExclusive))
                {
                    GUI.color = Color.white;
                    Widgets.DrawBox(rect, 2);
                    GUI.color = Color.white;
                }
            }
            else
            {
                if (currentAreaExt != null)
                {
                    if (currentAreaExtOp == AreaExtOperator.Inclusion)
                    {
                        GUI.color = Color.white;
                        Widgets.DrawBox(rect, 2);
                        GUI.color = Color.white;
                    }
                    else if (currentAreaExtOp == AreaExtOperator.Exclusion)
                    {
                        GUI.color = Color.red;
                        Widgets.DrawBox(rect, 2);
                        GUI.color = Color.white;
                    }
                }
                else
                {
                    if (currentArea == area)
                    {
                        Widgets.DrawBox(rect, 2);
                    }
                }
            }

            bool mouseOver = Mouse.IsOver(rect);
            bool leftButton = Event.current.button == 0;
            bool rightButton = Event.current.button == 1;

            if (mouseOver && area != null)
            {
                area.MarkForDraw();
            }

            if (editMode == AreaAllowedEditMode.None)
            {
                if (mouseOver && dragging)
                {
                    if (leftButton)
                    {
                        // 구역이 null이 아니면서, 현재 선택되어있다면 Remove, 아니면 Inclusion
                        if (area != null && (currentArea == area || currentAreaExtOp == AreaExtOperator.Inclusion))
                        {
                            editMode = AreaAllowedEditMode.Remove;
                        }
                        else
                        {
                            editMode = AreaAllowedEditMode.AddInclusion;
                        }
                    }
                    else if (rightButton)
                    {
                        // 이미 Exclusion으로 선택된것만 Remove, 아니면 Exclusion으로 덮어쓰거나 수정하지 않음
                        if (currentAreaExtOp == AreaExtOperator.Exclusion)
                        {
                            editMode = AreaAllowedEditMode.Remove;
                        }
                        else if (area != null)
                        {
                            editMode = AreaAllowedEditMode.AddExclusion;
                        }
                    }
                }
            }

            if (editMode == AreaAllowedEditMode.AddInclusion)
            {
                if (mouseOver && dragging)
                {
                    if (currentArea != area)
                    {
                        if (area == null)
                        {
                            p.playerSettings.AreaRestriction = null;
                        }
                        else
                        {
                            if (currentArea == null)
                            {
                                p.playerSettings.AreaRestriction = area;
                            }
                            else
                            {
                                if (currentAreaExt != null)
                                {
                                    if (!currentAreaExt.Contains(area, AreaExtOperator.Inclusion))
                                    {
                                        p.playerSettings.AreaRestriction = currentAreaExt.CloneWithOperationArea(AreaExtOperator.Inclusion, area);
                                    }
                                }
                                else
                                {
                                    p.playerSettings.AreaRestriction = new AreaExt(currentArea.Map, AreaExtOperator.Inclusion, currentArea).CloneWithOperationArea(AreaExtOperator.Inclusion, area);
                                }
                            }
                        }
                    }
                }
                else if (!dragging)
                {
                    editMode = AreaAllowedEditMode.None;
                }
            }
            else if (editMode == AreaAllowedEditMode.AddExclusion)
            {
                if (mouseOver && dragging)
                {
                    if (currentArea != area)
                    {
                        if (area == null)
                        {
                            p.playerSettings.AreaRestriction = null;
                        }
                        else
                        {
                            if (currentArea == null)
                            {
                                p.playerSettings.AreaRestriction = new AreaExt(area.Map, AreaExtOperator.Exclusion, area);
                            }
                            else
                            {
                                if (currentAreaExt != null)
                                {
                                    if (!currentAreaExt.Contains(area, AreaExtOperator.Exclusion))
                                    {
                                        p.playerSettings.AreaRestriction = currentAreaExt.CloneWithOperationArea(AreaExtOperator.Exclusion, area);
                                    }
                                }
                                else
                                {
                                    p.playerSettings.AreaRestriction = new AreaExt(currentArea.Map, AreaExtOperator.Inclusion, currentArea).CloneWithOperationArea(AreaExtOperator.Exclusion, area);
                                }
                            }
                        }
                    }
                }
                else if (!dragging)
                {
                    editMode = AreaAllowedEditMode.None;
                }
            }
            else if (editMode == AreaAllowedEditMode.Remove)
            {
                if (mouseOver && dragging)
                {
                    if (currentArea != null && area != null)
                    {
                        if (currentArea == area)
                        {
                            p.playerSettings.AreaRestriction = null;
                        }
                        else if (currentAreaExt != null && currentAreaExt.Contains(area))
                        {
                            p.playerSettings.AreaRestriction = currentAreaExt.CloneWithOperationArea(AreaExtOperator.None, area);
                        }
                    }
                }
                else if (!dragging)
                {
                    editMode = AreaAllowedEditMode.None;
                }
            }

            Text.Anchor = TextAnchor.UpperLeft;
            TooltipHandler.TipRegion(rect, text);

            return false;
        }
    }
}
