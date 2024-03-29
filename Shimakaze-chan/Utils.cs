using System;
using DSharpPlus.Entities;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Net.Http;
using Newtonsoft.Json.Linq;
using System.Net.Http.Headers;

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

            DiscordMessageBuilder builder = new DiscordMessageBuilder();
            builder.WithContent(content);
            builder.WithAllowedMentions(mentions);

            if (embed != null)
            {
                builder.AddEmbed(embed);
            }

            return await channel.SendMessageAsync(builder);
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

        /// <summary>
        /// This gets BOTH the mentioned users and parses the message for ulong Ids such as UIDs
        /// </summary>
        /// <param name="mentionedUsers">ctx.Message.MentionedUsers</param>
        /// <param name="userIDsString">string containing the list of user IDs separated by spaces</param>
        /// <returns>List of ulong Ids</returns>
        public static List<ulong> GetIdListFromMessage(IReadOnlyList<DiscordUser> mentionedUsers, string userIDsString)
        {
            return GetIdListFromArray(mentionedUsers, userIDsString?.Split(" "));
        }

        /// <summary>
        /// This gets BOTH the mentioned users and parses the message for ulong Ids such as UIDs
        /// </summary>
        /// <param name="mentionedUsers">ctx.Message.MentionedUsers</param>
        /// <param name="textArray">Array containing the list of user IDs as strings</param>
        /// <returns>List of ulong Ids</returns>
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
        
        public static DiscordEmbedBuilder BaseEmbedBuilder(
            CommandContext ctx, DiscordUser author = null, string title = null, DiscordColor? color = null,
            string footer = null, DateTime? timestamp = null)
        {
            if (author == null)
            {
                return BaseEmbedBuilder(ctx, null, null, title, color, footer, timestamp);
            }

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

        public static DiscordEmbedBuilder BaseEmbedBuilder(
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

            DiscordEmbedBuilder baseEmbedBuilder = new DiscordEmbedBuilder()
              .WithTitle(title)
              .WithColor(color.Value)
              .WithFooter(footer)
              .WithTimestamp(timestamp);

            if (!string.IsNullOrWhiteSpace(authorText) || !string.IsNullOrWhiteSpace(authorUrl))
            {
                baseEmbedBuilder.WithAuthor(authorText, null, authorUrl);
            }

            return baseEmbedBuilder;
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

    public static class ShimaHttpClient
    {
        private static HttpClient Client = new HttpClient();

        public static async Task<JObject> HttpGet(string url, AuthenticationHeaderValue authentication = null)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                return null;
            }
            try
            {
                using (var requestMessage = new HttpRequestMessage(HttpMethod.Get, url))
                {
                    if (authentication != null)
                    {
                        requestMessage.Headers.Authorization = authentication;
                    }
                    var response = await Client.SendAsync(requestMessage);
                    response.EnsureSuccessStatusCode();
                    string objectString = await response.Content.ReadAsStringAsync();
                    if (!objectString.StartsWith("{") && !objectString.EndsWith("}"))
                    {
                        objectString = $"{{\"data\":\"{objectString}\"}}";
                    }
                    return JObject.Parse(objectString);
                }
            }
            catch (Exception e)
            {
                ShimakazeBot.SendToDebugRoom($"Http GET failed.\nURL: **{url}**\nError: **{e.Message}**");
                return null;
            }
        }

        public static async Task<JObject> HttpPost(string url, AuthenticationHeaderValue authentication = null)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                return null;
            }
            try
            {
                using (var requestMessage = new HttpRequestMessage(HttpMethod.Post, url))
                {
                    if (authentication != null)
                    {
                        requestMessage.Headers.Authorization = authentication;
                    }
                    requestMessage.Content = new StringContent("grant_type=client_credentials");
                    requestMessage.Content.Headers.ContentType = new MediaTypeHeaderValue("application/x-www-form-urlencoded");
                    var response = await Client.SendAsync(requestMessage);
                    response.EnsureSuccessStatusCode();
                    string objectString = await response.Content.ReadAsStringAsync();
                    if (!objectString.StartsWith("{") && !objectString.EndsWith("}"))
                    {
                        objectString = $"{{\"data\":\"{objectString}\"}}";
                    }
                    return JObject.Parse(objectString);
                }
            }
            catch (Exception e)
            {
                ShimakazeBot.SendToDebugRoom($"Http POST failed.\nURL: **{url}**\nError: **{e.Message}**");
                return null;
            }
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