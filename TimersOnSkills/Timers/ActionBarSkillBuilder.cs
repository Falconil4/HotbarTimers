using Dalamud.Data;
using FFXIVClientStructs.FFXIV.Client.Game;
using Lumina.Excel;
using System.Collections.Generic;
using System.Linq;
using Action = Lumina.Excel.GeneratedSheets.Action;

namespace TimersOnSkills
{
    public unsafe class ActionBarSkillBuilder
    {
        private static List<ActionBarSkill> CachedActionBarSkills = new List<ActionBarSkill>();

        public static List<ActionBarSkill> Build(ExcelSheet<Action>? gameActionsList)
        {
            List<ActionBarSkill> actionBarSkills = new List<ActionBarSkill>();
            if (gameActionsList == null) return actionBarSkills;

            var actionManager = ActionManager.Instance();

            for (int actionBarIndex = 0; actionBarIndex < ActionBars.Names.Length; actionBarIndex++)
            {
                var actionBar = ActionBars.GetActionBar(actionBarIndex)->ActionBarSlotsAction;
                if (actionBar == null) continue;
                
                for (int slotIndex = 0; slotIndex < ActionBars.SlotsCount; slotIndex++)
                {
                    var actionBarSlot = &actionBar[slotIndex];
                    var actionId = actionManager->GetAdjustedActionId((uint)actionBarSlot->ActionId);
                    var name = gameActionsList.GetRow(actionId)?.Name?.RawString;
                    
                    ActionBarSkill? cachedActionBarSkill = CachedActionBarSkills.Find(s => 
                        s.ActionBarIndex == actionBarIndex 
                        && s.SlotIndex == slotIndex
                        && s.Name == name
                    );
                    
                    if (IsSlotEmpty(actionBarSlot, name))
                    {
                        if (cachedActionBarSkill != null) CachedActionBarSkills.Remove(cachedActionBarSkill);
                        continue;
                    }

                    if (cachedActionBarSkill == null)
                    {
                        var iconComponent = actionBarSlot->Icon;
                        cachedActionBarSkill = new ActionBarSkill(actionBarSlot, iconComponent, name!, actionBarIndex, slotIndex);
                    }
                    
                    actionBarSkills.Add(cachedActionBarSkill);
                }
            }

            CachedActionBarSkills = actionBarSkills;
            return actionBarSkills;
        }
        
        private static bool IsSlotEmpty(ActionBarSlot* actionBarSlot, string? name)
        {
            return actionBarSlot->ActionId <= 0 || actionBarSlot->PopUpHelpTextPtr == null || name == null;
        }
    }
}