using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Game.ClientState.Statuses;
using System;
using System.Collections.Generic;
using System.Linq;

namespace HotbarTimers
{
    class StatusesBuilder
    {
        public static List<Status> GetCurrentStatuses()
        {
            List<Status> statuses = new();
            if (HotbarTimers.TargetManager == null) return statuses;

            var player = HotbarTimers.Player;
            if (player == null) return statuses;
            var playerId = player.ObjectId;

            statuses.AddRange(player.StatusList.Where(status => status.SourceID == playerId));

            var target = HotbarTimers.TargetManager.Target;
            if (target is BattleChara targetCharacter) statuses.AddRange(targetCharacter.StatusList.Where(status => status.SourceID == playerId));

            return statuses;
        }
    }
}
