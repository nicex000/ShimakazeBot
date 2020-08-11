using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using DSharpPlus.Lavalink;
using System.Collections.Generic;
using System.Linq;

namespace Shimakaze
{
    public static class ShimakazeBot
    {
        public static DiscordClient Client;
        public static ShimaContext DbCtx;
        public static Dictionary<ulong, string> CustomPrefixes = new Dictionary<ulong, string>();
        public static Dictionary<ulong, ulong> StreamingEnabledGuilds = new Dictionary<ulong, ulong>();
        public static string DefaultPrefix = "!";

        public static LavalinkNodeConnection lvn;
        public static Dictionary<DiscordGuild, GuildPlayer> playlists = new Dictionary<DiscordGuild, GuildPlayer>();

        public static List<ulong> guildDebugMode = new List<ulong>();
        public static bool shouldSendToDebugRoom = true;

        public static void FetchPrefixes()
        {
            var prefixes = DbCtx.GuildPrefix.ToList();
            prefixes.ForEach(g => CustomPrefixes.Add(g.GuildId, g.Prefix));
        }

        public static void FetchStreamingRoles()
        {
            var streamingRoles = DbCtx.StreamingGuild.ToList();
            streamingRoles.ForEach(g => StreamingEnabledGuilds.Add(g.GuildId, g.RoleId));
        }

        public static bool CheckDebugMode(ulong guildId)
        {
            return guildDebugMode.Contains(guildId);
        }

        public static string AddWithDebug(string text, CommandContext ctx, bool condition)
        {
            return (condition && guildDebugMode.Contains(ctx.Guild.Id)) ? (text + "\n") : "";
        }

        public async static void SendToDebugRoom(string text)
        {
            var channel = await Client.GetChannelAsync(421236403515686913);
            await channel.SendMessageAsync(text, true);
        }
    }
}
