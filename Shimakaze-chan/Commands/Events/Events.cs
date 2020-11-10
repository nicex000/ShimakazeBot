using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Shimakaze
{
    public class Events
    {
        private DateTime socketCloseTime = new DateTime(0);
        private EventTimers timers = new EventTimers();
        public void LoadEvents()
        {
            ShimakazeBot.Client.Ready += DiscordReady;
            ShimakazeBot.Client.Resumed += DiscordResumed;

            ShimakazeBot.Client.PresenceUpdated += UserPresenceUpdated;

            ShimakazeBot.Client.SocketClosed += SocketClosed;
            ShimakazeBot.Client.SocketErrored += SocketErrored;
        }

        public async Task<int> AddTimerEvent(CommandContext ctx, EventType type, string message, DateTime eventTime,
            ulong channelId = 0)
        {
            return await timers.AddEvent(ctx, type, message, eventTime, channelId);
        }

        public EventInTimer GetTimerEvent(int id)
        {
            return timers.GetEvent(id);
        }

        public async Task<bool> RemoveTimerEvent(int id)
        {
            return await timers.RemoveEvent(id);
        }

        private Task SocketClosed(DiscordClient client, SocketCloseEventArgs e)
        {
            socketCloseTime = DateTime.Now;
            return Task.CompletedTask;
        }
        private Task SocketErrored(DiscordClient client, SocketErrorEventArgs e)
        {
            socketCloseTime = socketCloseTime.Ticks == 0 ? DateTime.Now : socketCloseTime;
            return Task.CompletedTask;
        }

        private async Task DiscordReady(DiscordClient client, ReadyEventArgs e)
        {
            ShimakazeBot.Client.Logger.Log(LogLevel.Information,
                LogSources.LAUNCHTIME_EVENT,
                "Ready" + (ShimakazeBot.Config.settings.isTest ?
                " - Using ShimaTest" : "") +
                $" on ShimaEngine v.{ShimaConsts.Version}");

            await ShimakazeBot.Client.UpdateStatusAsync(
                new DiscordActivity("out for abyssals with Rensouhou-chan",
                ActivityType.Watching),
                UserStatus.Online);

            timers.InitializeTimers();
        }

        private Task DiscordResumed(DiscordClient client, ReadyEventArgs e)
        {
            ShimakazeBot.Client.Logger.Log(LogLevel.Information,
                LogSources.WEBSOCKET_EVENT,
                "Gateway resumed" + (socketCloseTime.Ticks == 0 ? "" :
                $" - down for {(DateTime.Now - socketCloseTime).TotalMilliseconds}ms"));
            socketCloseTime = new DateTime(0);
            return Task.CompletedTask;
        }

        private async Task UserPresenceUpdated(DiscordClient client, PresenceUpdateEventArgs e)
        {
            bool streamStatusChanged = false;
            bool startedStreaming = false;
            var activitiesBefore = e.PresenceBefore?.Activities;
            var activitiesAfter = e.PresenceAfter?.Activities;

            //check if status changed
            if (activitiesBefore?.FirstOrDefault(activity =>
                activity.ActivityType == ActivityType.Streaming) != null)
            {
                if (activitiesAfter?.FirstOrDefault(activity =>
                    activity.ActivityType == ActivityType.Streaming) == null)
                {
                    streamStatusChanged = true;
                }
            }
            else if (activitiesAfter?.FirstOrDefault(activity =>
                     activity.ActivityType == ActivityType.Streaming) != null)
            {
                if (activitiesBefore?.FirstOrDefault(activity =>
                    activity.ActivityType == ActivityType.Streaming) == null)
                {
                    streamStatusChanged = true;
                    startedStreaming = true;
                }
            }

            if (!streamStatusChanged) return;

            //get all configured guilds
            var filteredGuilds = ShimakazeBot.Client.Guilds.Where(guild => {
                return ShimakazeBot.StreamingEnabledGuilds.ContainsKey(guild.Value.Id) ||
                    guild.Value.Roles.FirstOrDefault(role =>
                        role.Value.Name == "Now Streaming"
                    ).Value != null;
            });

            //filter ones for member and check perms
            string missingPerms = "";
            filteredGuilds = filteredGuilds.Where(guild =>
            {
                if (guild.Value.Members.ContainsKey(e.User.Id))
                {
                    if (Utils.MemberHasPermissions(
                        guild.Value.Members[ShimakazeBot.Client.CurrentUser.Id],
                        Permissions.ManageRoles))
                    {
                        return true;
                    }
                    else missingPerms += $"{guild.Value.Name} (**{guild.Value.Id}**) - ";
                }
                return false;
            });
            if (missingPerms.Length > 0)
            {
                ShimakazeBot.SendToDebugRoom("Shima is missing manage role perms for streaming in:" +
                    $"\n{missingPerms}");
                return;
            }

            //apply per guild
            foreach (var guild in filteredGuilds)
            {
                var role = ShimakazeBot.StreamingEnabledGuilds.ContainsKey(guild.Key) ?
                    guild.Value.Roles[ShimakazeBot.StreamingEnabledGuilds[guild.Key]] :
                    guild.Value.Roles.FirstOrDefault(guildRole =>
                        guildRole.Value.Name == "Now Streaming"
                    ).Value;
                try
                {
                    if (startedStreaming)
                    {
                        await guild.Value.Members[e.User.Id].GrantRoleAsync(role);
                    }
                    else
                    {
                        await guild.Value.Members[e.User.Id].RevokeRoleAsync(role);
                    }
                }
                //if role is too high
                catch
                {
                    ShimakazeBot.SendToDebugRoom($"Shima couldn't reach the streaming role {role.Name} " +
                        $"in {guild.Value.Name} (**{guild.Value.Id}**)");
                }
            }
        }
    }
}
