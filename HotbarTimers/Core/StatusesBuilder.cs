using Dalamud.Game.ClientState.Objects;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Game.ClientState.Statuses;
using System.Collections.Generic;

namespace HotbarTimers
{
    class StatusesBuilder
    {
        public static List<Status> GetCurrentStatuses(PlayerCharacter player, TargetManager targetManager)
        {
            List<Status> statuses = new();
            if (player.StatusList != null) statuses.AddRange(player.StatusList);

            var target = targetManager.Target;
            if (target is BattleChara targetCharacter)
            {
                statuses.AddRange(targetCharacter.StatusList);
            }

            return statuses;
        }
    }
}
