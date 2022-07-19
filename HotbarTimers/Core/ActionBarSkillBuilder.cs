using FFXIVClientStructs.FFXIV.Client.Game;
using System.Collections.Generic;
using System.Diagnostics;

namespace HotbarTimers
{
    public unsafe class ActionBarSkillBuilder
    {
        public static List<ActionBarSkill> Build()
        {
            List<ActionBarSkill> actionBarSkills = new();

            var actionManager = ActionManager.Instance();
            for (int actionBarIndex = 0; actionBarIndex < ActionBars.Names.Length; actionBarIndex++)
            {
                var actionBar = ActionBars.GetActionBar(actionBarIndex)->ActionBarSlotsAction;
                if (actionBar == null) continue;
                
                for (int slotIndex = 0; slotIndex < ActionBars.SlotsCount; slotIndex++)
                {
                    var actionBarSlot = &actionBar[slotIndex];
                    var actionId = actionManager->GetAdjustedActionId((uint)actionBarSlot->ActionId);
                    var row = HotbarTimers.GameActionsList?.GetRow(actionId);
                    var name = row?.Name?.RawString;
                    
                    if (!IsSlotEmpty(actionBarSlot, row) && name != null)
                    {
                        var iconComponent = actionBarSlot->Icon;
                        ActionBarSkill skill = new(actionBarSlot, iconComponent, name, actionBarIndex, slotIndex);

                        actionBarSkills.Add(skill);
                    }
                }
            }

            return actionBarSkills;
        }

        private static bool IsSlotEmpty(ActionBarSlot* actionBarSlot, Lumina.Excel.GeneratedSheets.Action? row)
        {
            var name = row?.Name;
            var jobRow = row?.ClassJobCategory.Row;

            return actionBarSlot->ActionId <= 0 || actionBarSlot->PopUpHelpTextPtr == null || name == null || jobRow == 0;
        }
    }
}