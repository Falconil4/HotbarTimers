using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.System.Framework;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using FFXIVClientStructs.FFXIV.Component.GUI;
using System.Collections.Generic;

namespace HotbarTimers
{
    public unsafe class ActionBarSkillBuilder
    {
        private static readonly string[] ActionBarNames = {
            "_ActionBar",
            "_ActionBar01",
            "_ActionBar02",
            "_ActionBar03",
            "_ActionBar04",
            "_ActionBar05",
            "_ActionBar06",
            "_ActionBar07",
            "_ActionBar08",
            "_ActionBar09",
            //"_ActionCross",
            //"_ActionDoubleCrossL",
            //"_ActionDoubleCrossR"
        };

        public static readonly int ActionBarSlotsCount = 12;

        private static HotBar* GetHotBar(int actionBarIndex)
        {
            return Framework.Instance()->GetUiModule()->GetRaptureHotbarModule()->HotBar[actionBarIndex];
        }
        private static AddonActionBarBase* GetActionBar(int actionBarIndex)
        {
            return (AddonActionBarBase*)AtkStage.GetSingleton()->RaptureAtkUnitManager->
                GetAddonByName(ActionBarNames[actionBarIndex]);
        }

        public static List<ActionBarSkill> Build()
        {
            List<ActionBarSkill> actionBarSkills = new();
            var actionManager = ActionManager.Instance();

            for (int actionBarIndex = 0; actionBarIndex < ActionBarNames.Length; actionBarIndex++)
            {
                var actionBar = GetActionBar(actionBarIndex)->ActionBarSlots;
                var hotBar = GetHotBar(actionBarIndex)->Slot;

                for (int slotIndex = 0; slotIndex < ActionBarSlotsCount; slotIndex++)
                {
                    var hotBarSlot = hotBar[slotIndex];
                    var actionBarSlot = &actionBar[slotIndex];

                    uint? id = GetActionId(hotBarSlot);
                    if (id == null) continue;

                    uint actionId = actionManager->GetAdjustedActionId((uint)id);
                    var name = HotbarTimers.GameActionsList?.GetRow(actionId)?.Name?.RawString;

                    if (name != null)
                    {
                        ActionBarSkill skill = new(actionBarSlot->Icon, name, actionBarIndex, slotIndex);
                        actionBarSkills.Add(skill);
                    }
                }
            }

            return actionBarSkills;
        }

        private static uint? GetActionId(HotBarSlot* hotBarSlot)
        {
            //sprint has id of 4 while in actions sheet it's a 3 - replace :)
            if (hotBarSlot->IconTypeB == HotbarSlotType.GeneralAction && hotBarSlot->IconB == 4)
            {
                return 3;
            }

            switch(hotBarSlot->CommandType)
            {
                case HotbarSlotType.Action:
                case HotbarSlotType.GeneralAction:
                case HotbarSlotType.Macro:
                    return hotBarSlot->IconB;
                default:
                    return null;
            }
        }
    }
}