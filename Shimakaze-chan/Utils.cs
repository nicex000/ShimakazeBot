﻿using DSharpPlus.Entities;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System;
using DSharpPlus.Net;
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

        public static List<ulong> GetIdListFromMessage(IReadOnlyList<DiscordUser> mentionedUsers, string userIDsString)
        {
            return GetIdListFromArray(mentionedUsers, userIDsString?.Split(" "));
        }

        public static List<ulong> GetIdListFromArray(IReadOnlyList<DiscordUser> mentionedUsers, string[] textArray)
        {
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
        
        public static DiscordEmbed BaseEmbedBuilder(
            CommandContext ctx, DiscordUser author = null, string title = null, DiscordColor? color = null,
            string footer = null, DateTime? timestamp = null)
        {
            if (author != null)
            {
                if (color == null && ctx.Guild != null && ctx.Guild.Members.ContainsKey(author.Id))
                {
                    color = ctx.Guild.Members[author.Id].Color;
                }
                return BaseEmbedBuilder(ctx,
                (ctx.Guild == null || !ctx.Guild.Members.ContainsKey(author.Id)) ?
                        $"{author.Username}#{author.Discriminator} ({author.Id})" :
                        $"{ctx.Guild.Members[author.Id].DisplayName} ({author.Id})",
                author.AvatarUrl,
                title, color, footer, timestamp);
            }
            else
            {
                return BaseEmbedBuilder(ctx, null, null, title, color, footer, timestamp);
            }
        }
        public static DiscordEmbed BaseEmbedBuilder(

            CommandContext ctx, string authorText = null, string authorUrl = null, string title = null,
            DiscordColor? color = null, string footer = null, DateTime? timestamp = null)
        {
            if (timestamp == null)
            {
                timestamp = DateTime.Now;
            }
            if (color == null)
            {
                color = ctx.Guild == null ?
                    new DiscordColor(ThreadSafeRandom.ThisThreadsRandom.Next(0, 16777216)) :
                        ctx.Guild.Members[ShimakazeBot.Client.CurrentUser.Id].Color;
            }

            DiscordEmbed baseEmbed = new DiscordEmbedBuilder()
              .WithTitle(title)
              .WithColor(color.Value)
              .WithFooter(footer)
              .WithTimestamp(timestamp);

            if (!string.IsNullOrWhiteSpace(authorText) || !string.IsNullOrWhiteSpace(authorUrl))
            {
                baseEmbed = new DiscordEmbedBuilder(baseEmbed).WithAuthor(authorText, null, authorUrl);
            }

            return baseEmbed;
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