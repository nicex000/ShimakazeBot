using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.CommandsNext;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus;
using System.Collections.Generic;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using DSharpPlus.SlashCommands.Attributes;

namespace Shimakaze
{
    class DebugCommands : Commands
    {
        [SlashCommand("channelinfo", "Gets some debug info about user manage messages permissions for channel and server")]
        public async Task GetChannelInfo(InteractionContext ctx)
        {
            await SCTX.RespondSanitizedAsync(ctx, $"Channel id: {ctx.Channel.Id}\n" + 
                $"Server manage messages perms:" +
                $"{(ctx.Member.Guild.Permissions & Permissions.ManageMessages) != 0}\n" +
                $"Channel manage messages perms:" +
                $"{(ctx.Channel.PermissionsFor(ctx.Member) & Permissions.ManageMessages) != 0}");
        }

        [SlashCommand("debug", "Enables or disables debug mode")]
        [Attributes.RequireAdmin]
        public async Task DebugMode(InteractionContext ctx,
            [Option("Enable", "true to enable")] bool enable = true)
        {
            bool toRemove = ShimakazeBot.guildDebugMode.Contains(ctx.Guild.Id);
            if (toRemove && !enable)
            {
                ShimakazeBot.guildDebugMode.Remove(ctx.Guild.Id);
            }
            else if (enable)
            {
                ShimakazeBot.guildDebugMode.Add(ctx.Guild.Id);
            }

            await SCTX.RespondSanitizedAsync(ctx, "スーパーデバッグモード" + 
                $" **{(enable ? "enabled" : "disabled")}**" +
                $" for {ctx.Guild.Name} ({ctx.Guild.Id})") ;
        }
        #region Aliases
        [SlashCommand("supadebug", "Enables or disables debug mode")]
        [Attributes.RequireAdmin]
        public async Task DebugMode_supadebug(InteractionContext ctx,
            [Option("Enable", "true to enable")] bool enable = true)
        {
            await this.DebugMode(ctx, enable);
        }

        [SlashCommand("スーパーデバッグモード", "Enables or disables debug mode")]
        [Attributes.RequireAdmin]
        public async Task DebugMode_jp(InteractionContext ctx,
            [Option("Enable", "true to enable")] bool enable = true)
        {
            await this.DebugMode(ctx, enable);
        }
        #endregion


        [SlashCommand("debugchannel", "Enables or disables the debug channel")]
        [Attributes.RequireShimaTeam]
        //[Aliases("debugflag")]
        public async Task SetDebugChannel(InteractionContext ctx, 
            [Option("Enable", "true to enable")] bool enable = true)
        {
            ShimakazeBot.shouldSendToDebugRoom = enable;
            await SCTX.RespondSanitizedAsync(ctx, "Shima debug channel **" +
                $"{(ShimakazeBot.shouldSendToDebugRoom ? "enabled" : "disabled")}**");
        }

        [SlashCommand("status", "Disaplays the current status of vc related stuff (lavalink, channel, playlist)")]
        public async Task Status(InteractionContext ctx)
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
                    (ShimakazeBot.lvn.GetGuildConnection(ctx.Guild) != null ?
                    ShimakazeBot.lvn.GetGuildConnection(ctx.Guild).IsConnected.ToString() :
                    "**GetGuildConnection Failed**") + "\n";

                if (ShimakazeBot.lvn.GetGuildConnection(ctx.Guild) != null &&
                    ShimakazeBot.lvn.GetGuildConnection(ctx.Guild).IsConnected)
                {
                    responseString += "  - channel: " +
                        (ShimakazeBot.lvn.GetGuildConnection(ctx.Guild).Channel != null ?
                        ShimakazeBot.lvn.GetGuildConnection(ctx.Guild).Channel.Name +
                        " (*" + ShimakazeBot.lvn.GetGuildConnection(ctx.Guild).Channel.Id + "*)" :
                        "**NULL**") + "\n";

                    responseString += "  - current state: ";
                    if (ShimakazeBot.lvn.GetGuildConnection(ctx.Guild).CurrentState != null)
                    {
                        responseString += "\n";
                        responseString += "        - track: " +
                            (ShimakazeBot.lvn.GetGuildConnection(ctx.Guild).CurrentState.CurrentTrack != null ?
                            ShimakazeBot.lvn.GetGuildConnection(ctx.Guild).CurrentState.CurrentTrack.Title :
                            "**not playing**") + "\n";

                        responseString += "        - position: " +
                            ShimakazeBot.lvn.GetGuildConnection(ctx.Guild).CurrentState.PlaybackPosition.ToString() +
                            "\n";

                        responseString += "        - last update: " +
                            ShimakazeBot.lvn.GetGuildConnection(ctx.Guild).CurrentState.LastUpdate.ToString() +
                            "\n";


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

            await SCTX.RespondSanitizedAsync(ctx, responseString);
        }
    }
}
