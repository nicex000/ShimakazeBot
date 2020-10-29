﻿using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.CommandsNext;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus;

namespace Shimakaze
{
    class DebugCommands : Commands
    {
        [Command("channelinfo")]
        [Description("Gets some debug info about user manage messages permissions for channel and server")]
        public async Task GetChannelInfo(CommandContext ctx)
        {
            await ctx.RespondAsync($"Channel id: {ctx.Channel.Id}\n" + 
                                   $"Server manage messages perms: {(ctx.Member.Guild.Permissions & Permissions.ManageMessages) != 0}\n" +
                                   $"Channel manage messages perms: {(ctx.Channel.PermissionsFor(ctx.Member) & Permissions.ManageMessages) != 0}");
        }

        [Command("debug")]
        [Attributes.RequireAdmin]
        [Aliases("supadebug", "sd", "スーパーデバッグモード")]
        public async Task DebugMode(CommandContext ctx)
        {
            bool toRemove = ShimakazeBot.guildDebugMode.Contains(ctx.Guild.Id);
            if (toRemove)
            {
                ShimakazeBot.guildDebugMode.Remove(ctx.Guild.Id);
            }
            else ShimakazeBot.guildDebugMode.Add(ctx.Guild.Id);

            await ctx.RespondAsync("スーパーデバッグモード" + 
                $" **{(!toRemove ? "enabled" : "disabled")}**" +
                $" for {ctx.Guild.Name} ({ctx.Guild.Id})") ;
        }

        [Command("debugchannel")]
        [Attributes.RequireShimaTeam]
        [Aliases("debugflag")]
        public async Task DebugChannelFlag(CommandContext ctx)
        {
            ShimakazeBot.shouldSendToDebugRoom = !ShimakazeBot.shouldSendToDebugRoom;
            await ctx.RespondAsync("Shima debug channel **" +
                $"{(ShimakazeBot.shouldSendToDebugRoom ? "enabled" : "disabled")}**");
        }

        [Command("status")]
        public async Task Status(CommandContext ctx)
        {
            string responseString = "⛺\n";

            responseString += "**LavalinkNodeConnection status**\n";

            responseString += "  - connected: " + (ShimakazeBot.lvn != null ?
                ShimakazeBot.lvn.IsConnected.ToString() : "false") + "\n";

            if (ShimakazeBot.lvn != null)
            {
                responseString += "  - node endpoint: " + ShimakazeBot.lvn.NodeEndpoint + "\n";


                responseString += "**LavalinkGuildConnection status**\n";

                responseString += "  - connected: " +
                    (ShimakazeBot.lvn.GetConnection(ctx.Guild) != null ?
                    ShimakazeBot.lvn.GetConnection(ctx.Guild).IsConnected.ToString() :
                    "**GetConnection Failed**") + "\n";

                if (ShimakazeBot.lvn.GetConnection(ctx.Guild) != null &&
                    ShimakazeBot.lvn.GetConnection(ctx.Guild).IsConnected)
                {
                    responseString += "  - channel: " +
                        (ShimakazeBot.lvn.GetConnection(ctx.Guild).Channel != null ?
                        ShimakazeBot.lvn.GetConnection(ctx.Guild).Channel.Name +
                        " (*" + ShimakazeBot.lvn.GetConnection(ctx.Guild).Channel.Id + "*)" :
                        "**NULL**") + "\n";

                    responseString += "  - current state: ";
                    if (ShimakazeBot.lvn.GetConnection(ctx.Guild).CurrentState != null)
                    {
                        responseString += "\n";
                        responseString += "        - track: " +
                            (ShimakazeBot.lvn.GetConnection(ctx.Guild).CurrentState.CurrentTrack != null ?
                            ShimakazeBot.lvn.GetConnection(ctx.Guild).CurrentState.CurrentTrack.Title :
                            "**not playing**") + "\n";

                        responseString += "        - position: " +
                            ShimakazeBot.lvn.GetConnection(ctx.Guild).CurrentState.PlaybackPosition.ToString() + "\n";

                        responseString += "        - last update: " +
                            ShimakazeBot.lvn.GetConnection(ctx.Guild).CurrentState.LastUpdate.ToString() + "\n";


                    }
                    else
                    {
                        responseString += "**NULL**";
                    }
                }
            }

            responseString += "**Playlist status**\n";
            responseString += "  - length: " + ShimakazeBot.playlists.Count() + "\n";
            responseString += "  - guild length: " +
                (ShimakazeBot.playlists.ContainsKey(ctx.Guild) ?
                ShimakazeBot.playlists[ctx.Guild].songRequests.Count().ToString() :
                "**no playlist**");

            await ctx.RespondAsync(responseString);
        }
    }
}
