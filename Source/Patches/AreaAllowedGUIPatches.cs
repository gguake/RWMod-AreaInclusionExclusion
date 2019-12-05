using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

using Harmony;
using UnityEngine;
using Verse;
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

        public static bool DoAreaSelectorPrefix(Rect rect, Pawn p, Area area)
        {
            bool dragging = (bool)FieldInfos.dragging.GetValue(null);
            rect = rect.ContractedBy(1f);
            GUI.DrawTexture(rect, (area == null) ? BaseContent.GreyTex : area.ColorTexture);
            Text.Anchor = TextAnchor.MiddleLeft;
            string text = AreaUtility.AreaAllowedLabel_Area(area);
            Rect rect2 = rect;
            rect2.xMin += 3f;
            rect2.yMin += 2f;
            Widgets.Label(rect2, text);
            
            Area currentArea = p.playerSettings.AreaRestriction;
            AreaExt currentAreaExt = p.playerSettings.AreaRestriction as AreaExt;
            if (area != null && currentAreaExt != null)
            {
                AreaExtOperator areaOp = currentAreaExt.GetAreaOperator(area.ID);
                if (areaOp == AreaExtOperator.Inclusion)
                {
                    GUI.color = Color.green;
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
            
            if ((editMode == AreaAllowedEditMode.AddExclusion && Event.current.button == 0 && Event.current.rawType == EventType.mouseDown) ||
                (editMode == AreaAllowedEditMode.AddInclusion && Event.current.button == 1 && Event.current.rawType == EventType.mouseDown))
            {
                dragging = false;
                editMode = AreaAllowedEditMode.None;
            }
            else if (Event.current.rawType == EventType.mouseUp)
            {
                dragging = false;
                editMode = AreaAllowedEditMode.None;
            }

            if (Mouse.IsOver(rect))
            {
                if (area != null)
                {
                    area.MarkForDraw();
                }

                if (Event.current.type == EventType.MouseDown)
                {
                    if (Event.current.button == 0 ^ Event.current.button == 1)
                    {
                        dragging = true;
                    }
                }
                
                if (dragging)
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
                        }
                        else if (currentAreaExt != null)
                        {
                            if (currentAreaExt.GetAreaOperator(area.ID) != AreaExtOperator.Inclusion)
                            {
                                p.playerSettings.AreaRestriction = currentAreaExt.CloneWithOperationArea(AreaExtOperator.Inclusion, area);
                            }
                        }
                        else
                        {
                            p.playerSettings.AreaRestriction = new AreaExt(area.Map, AreaExtOperator.Inclusion, area);
                        }
                    }

                    if (editMode == AreaAllowedEditMode.AddExclusion)
                    {
                        if (area == null)
                        {
                            p.playerSettings.AreaRestriction = null;
                        }
                        else if (currentAreaExt != null)
                        {
                            if (currentAreaExt.GetAreaOperator(area.ID) != AreaExtOperator.Exclusion)
                            {
                                p.playerSettings.AreaRestriction = currentAreaExt.CloneWithOperationArea(AreaExtOperator.Exclusion, area);
                            }
                        }
                        else
                        {
                            p.playerSettings.AreaRestriction = new AreaExt(area.Map, AreaExtOperator.Exclusion, area);
                        }
                    }

                    if (editMode == AreaAllowedEditMode.Remove)
                    {
                        if (currentAreaExt != null)
                        {
                            p.playerSettings.AreaRestriction = currentAreaExt.CloneWithOperationArea(AreaExtOperator.None, area);
                        }
                        else
                        {
                            if (currentArea != null)
                            {
                                p.playerSettings.AreaRestriction = null;
                            }
                        }
                    }
                }
            }

            return false;
        }
    }
}
