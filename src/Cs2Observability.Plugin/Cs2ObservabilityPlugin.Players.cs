using Cs2Observability.Core.Enums;
using Cs2Observability.Core.Events.Player;
using CounterStrikeSharp.API.Core;

namespace Cs2Observability.Plugin;

public sealed partial class Cs2ObservabilityPlugin
{
    private void RegisterPlayerHandlers()
    {
        // EventPlayerConnect fires before Steam auth; EventPlayerConnectFull fires once
        // the player is fully authenticated and has a valid SteamID.
        RegisterEventHandler<EventPlayerConnectFull>((e, _) =>
        {
            var player = e.Userid;
            if (player?.IsValid != true) return HookResult.Continue;

            _playerConnectTimes[player.SteamID] = DateTimeOffset.UtcNow;

            Dispatch(new PlayerConnectEvent(
                SteamId:         player.SteamID.ToString(),
                PlayerName:      player.PlayerName,
                IpAddress:       player.IpAddress ?? "unknown",
                CountryCode:     null,   // GeoIP is delegated to the OTel Collector pipeline
                AsnOrganization: null,
                PingMs:          player.Ping,
                OccurredAt:      DateTimeOffset.UtcNow));

            return HookResult.Continue;
        });

        RegisterEventHandler<EventPlayerDisconnect>((e, _) =>
        {
            var player = e.Userid;
            if (player?.IsValid != true) return HookResult.Continue;

            _playerConnectTimes.TryGetValue(player.SteamID, out var connectedAt);
            var duration = connectedAt != default
                ? DateTimeOffset.UtcNow - connectedAt
                : TimeSpan.Zero;
            _playerConnectTimes.Remove(player.SteamID);

            Dispatch(new PlayerDisconnectEvent(
                Player:          ToPlayerInfo(player),
                Reason:          e.Reason,
                SessionDuration: duration,
                OccurredAt:      DateTimeOffset.UtcNow));

            return HookResult.Continue;
        });

        RegisterEventHandler<EventPlayerChangename>((e, _) =>
        {
            var player = e.Userid;
            if (player?.IsValid != true) return HookResult.Continue;

            Dispatch(new PlayerNameChangeEvent(
                SteamId:    player.SteamID.ToString(),
                OldName:    e.Oldname,
                NewName:    e.Newname,
                OccurredAt: DateTimeOffset.UtcNow));

            return HookResult.Continue;
        });

        RegisterEventHandler<EventPlayerTeam>((e, _) =>
        {
            var player = e.Userid;
            if (player?.IsValid != true) return HookResult.Continue;

            Dispatch(new PlayerTeamChangeEvent(
                Player:     ToPlayerInfo(player),
                FromTeam:   (GameTeam)e.Oldteam,
                ToTeam:     (GameTeam)e.Team,
                OccurredAt: DateTimeOffset.UtcNow));

            return HookResult.Continue;
        });
    }
}
