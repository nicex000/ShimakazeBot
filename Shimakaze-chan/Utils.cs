using System;
using DSharpPlus.Entities;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System;

namespace Shimakaze
{
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

        /// <summary>
        /// This gets BOTH the mentioned users and parses the message for ulong Ids such as UIDs
        /// </summary>
        /// <param name="mentionedUsers">ctx.Message.MentionedUsers</param>
        /// <param name="commandPayload">message string from [RemainingText]</param>
        /// <returns>List of ulong Ids</returns>
        public static List<ulong> GetIdListFromMessage(IReadOnlyList<DiscordUser> mentionedUsers, string commandPayload)
        {
            string[] textArray = commandPayload?.Split(" ");
            var idList = new List<ulong>();
            mentionedUsers.ToList().ForEach(user =>
            {
                if (!idList.Contains(user.Id))
                {
                    idList.Add(user.Id);
                }
            });
            if (textArray == null)
            {
                return idList;
            }
            foreach (var userId in textArray)
            {
                if (ulong.TryParse(userId, out ulong idFromText) &&
                    !idList.Contains(idFromText))
                {
                    idList.Add(idFromText);
                }
            }

            return idList;
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