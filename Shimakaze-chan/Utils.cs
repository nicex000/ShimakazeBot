using DSharpPlus.Entities;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System;
using System.Threading.Tasks;

namespace Shimakaze
{
    class CTX
    {
        public static async Task<DiscordMessage> RespondSanitizedAsync(
            CommandContext ctx, string content = null, bool isTTS = false, DiscordEmbed embed = null,
            IEnumerable<IMention> mentions = null)
        {
            return await CTX.SendSanitizedMessageAsync(ctx.Channel, content, isTTS, embed, mentions);
        }

        public static async Task<DiscordMessage> SendSanitizedMessageAsync(
            DiscordChannel channel, string content = null, bool isTTS = false, DiscordEmbed embed = null,
            IEnumerable<IMention> mentions = null)
        {
            if (string.IsNullOrWhiteSpace(content) && embed == null)
            {
                return null;
            }

            if (mentions == null)
            {
                mentions = new List<IMention>() { RoleMention.All, UserMention.All };
            }

            return await channel.SendMessageAsync(content, isTTS, embed, mentions);
        }
    }
    class DebugString
    {
        private string value = "";
        public void AddWithDebug(string text, CommandContext ctx, bool condition = true)
        {
            if (condition && ShimakazeBot.guildDebugMode.Contains(ctx.Guild.Id))
            {
                value += text + "\n";
            }
        }
        public override string ToString()
        {
            return value;
        }
    }

    class Utils
    {
        public static bool MemberHasPermissions(DiscordMember member, Permissions permissions)
        {
            foreach (var role in member.Roles)
            {
                if (role.CheckPermission(permissions) == PermissionLevel.Allowed)
                {
                    return true;
                }
            }
            return false;
        }

        public static List<DiscordRole> GetRolesFromString(DiscordGuild guild, string roleString)
        {
            List<DiscordRole> roles = new List<DiscordRole>();

            if (string.IsNullOrWhiteSpace(roleString))
            {
                return roles;
            }

            string[] roleArray = roleString.Split(",");
            List<DiscordRole> guildRoles = guild.Roles.Values.ToList();
            ulong roleId;
            DiscordRole guildRole = null;

            foreach (var role in roleArray)
            {
                role.Trim();
                if (ulong.TryParse(role, out roleId))
                {
                    if (guild.Roles.ContainsKey(roleId))
                    {
                        roles.Add(guild.Roles[roleId]);
                    }
                    else
                    {
                        guildRole = guildRoles.FirstOrDefault(item => item.Name == role);
                        if (guildRole != null)
                        {
                            roles.Add(guildRole);
                        }
                    }
                }
            }

            return roles;
        }
    }

    public static class ThreadSafeRandom
    {
        [ThreadStatic] private static Random Local;

        public static Random ThisThreadsRandom
        {
            get 
            { 
                return Local ?? (Local = new Random(
                    unchecked(Environment.TickCount * 31 + Thread.CurrentThread.ManagedThreadId))); 
            }
        }
    }
}