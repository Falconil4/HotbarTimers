using Dalamud.Logging;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using Lumina.Excel.GeneratedSheets;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using static FFXIVClientStructs.FFXIV.Client.UI.Misc.RaptureMacroModule;

namespace HotbarTimers
{
    public unsafe class ActionBarSkillBuilder
    {
        private static readonly List<Action>? PlayerActions = HotbarTimers.GameActionsList?.Where(a => a.IsPlayerAction).ToList();
        private static readonly ActionManager* ActionManagerInstance = ActionManager.Instance();
        public static List<ActionBarSkill> Build()
        {
            List<ActionBarSkill> actionBarSkills = new();

            for (int actionBarIndex = 0; actionBarIndex < ActionBars.Names.Length; actionBarIndex++)
            {
                var actionBar = ActionBars.GetActionBar(actionBarIndex)->ActionBarSlotsAction;
                if (actionBar == null) continue;

                for (int slotIndex = 0; slotIndex < ActionBars.SlotsCount; slotIndex++)
                {
                    var actionBarSlot = &actionBar[slotIndex];
                    var actionId = actionBarSlot->ActionId;
                    var adjustedActionId = ActionManagerInstance->GetAdjustedActionId((uint)actionId);

                    if (!IsSlotEmpty(actionBarSlot))
                    {
                        if (IsMacro(actionBarSlot))
                        {
                            uint? macroActionId = GetMacroActionId(actionId);
                            if (macroActionId != null) adjustedActionId = (uint)macroActionId;
                        }

                        var name = HotbarTimers.GameActionsList?.GetRow(adjustedActionId)?.Name?.RawString;
                        if (name != null)
                        {
                            ActionBarSkill skill = new(actionBarSlot, actionBarSlot->Icon, name, actionBarIndex, slotIndex);
                            actionBarSkills.Add(skill);
                        }
                    }
                }
            }

            return actionBarSkills;
        }

        private static bool IsMacro(ActionBarSlot* actionBarSlot)
        {
            return *(actionBarSlot->PopUpHelpTextPtr) == 0;
        }

        private static bool IsSlotEmpty(ActionBarSlot* actionBarSlot) => actionBarSlot->PopUpHelpTextPtr == null;
        

        private static uint? GetMacroActionId(int actionId)
        {
            var individual = GetMacroActionIdFromMacroIconName(RaptureMacroModule.Instance->Individual[actionId]);
            if (individual != null) return individual;

            var shared = GetMacroActionIdFromMacroIconName(RaptureMacroModule.Instance->Shared[actionId]);
            if (shared != null) return shared;

            return null;
        }

        private static readonly Regex Regex = new (@"^\/m(?:acro)?icon ""?(.+?)""?$", RegexOptions.IgnoreCase | RegexOptions.Multiline);
        private static uint? GetMacroActionIdFromMacroIconName(Macro* macro)
        {
            if (macro == null) return null;

            List<string> macroLines = new();
            for(int lineIndex = 0; lineIndex <= 14; lineIndex++)
            {
                string line = macro->Line[lineIndex]->ToString();
                if (string.IsNullOrEmpty(line))
                {
                    if (lineIndex == 0) return null;
                    break;
                }
                macroLines.Add(line);
            }

            string macroCode = string.Join("\n", macroLines);
            var match = Regex.Match(macroCode);
            if (!match.Success) return null;

            var skillName = match.Groups[1].Value;
            var rowId = PlayerActions?.FirstOrDefault(a => a.Name == skillName)?.RowId;
            if (rowId == null) return null;

            return ActionManagerInstance->GetAdjustedActionId((uint) rowId);
        }
    }
}