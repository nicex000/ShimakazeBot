﻿using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using DSharpPlus.Lavalink;
using System.Collections.Generic;
using System.Linq;

namespace Shimakaze
{
    public static class ShimaConsts
    {
        public enum UserPermissionLevel
        {
            DEFAULT = 1,
            DEFAULT_SERVER_OWNER = 5,
            SHIMA_TEAM = 999
        }

        public const int GlobalLevelGuild = 0;
    }

    public static class ShimakazeBot
    {
        public static DiscordClient Client;
        public static ShimaContext DbCtx;
        public static Dictionary<ulong, string> CustomPrefixes = new Dictionary<ulong, string>();
        public static Dictionary<ulong, ulong> StreamingEnabledGuilds = new Dictionary<ulong, ulong>();
        public static Dictionary<ulong, LevelListContainer> UserLevelList = new Dictionary<ulong, LevelListContainer>();
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

        public static void FetchPermissionLevels()
        {
            var permissionLevels = DbCtx.UserPermissionLevel.ToList();
            permissionLevels.ForEach(g => {

                UserLevels uLevel = new UserLevels(g.Id, g.GuildId, g.Level);

                if (UserLevelList.ContainsKey(g.UserId))
                {
                    if (g.GuildId == 0)
                    {
                        UserLevelList[g.UserId].levelList.Insert(0, uLevel);
                    }
                    else
                    {
                        UserLevelList[g.UserId].levelList.Add(uLevel);
                    }
                }
                else
                {
                    UserLevelList.Add(g.UserId, new LevelListContainer(g.IsRole, new List<UserLevels>() { uLevel }));
                }
            });
        }

        public static bool IsUserShimaTeam(ulong userId)
        {
            return Client.CurrentApplication.Owners.Any(user => user.Id == userId);
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
