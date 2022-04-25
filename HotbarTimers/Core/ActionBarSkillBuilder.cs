using FFXIVClientStructs.FFXIV.Client.Game;
using Lumina.Excel;
using System.Collections.Generic;
using Action = Lumina.Excel.GeneratedSheets.Action;

namespace HotbarTimers
{
    public unsafe class ActionBarSkillBuilder
    {
        private static List<ActionBarSkill> CachedActionBarSkills = new List<ActionBarSkill>();

        public static List<ActionBarSkill> Build(ExcelSheet<Action>? gameActionsList, Configuration configuration, bool rebuild = false)
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

                    if (cachedActionBarSkill == null || rebuild)
                    {
                        var iconComponent = actionBarSlot->Icon;
                        cachedActionBarSkill = new ActionBarSkill(actionBarSlot, iconComponent, name!, actionBarIndex, slotIndex, configuration);
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