﻿using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using System;
using System.Collections.Generic;
using System.Text;
using Shimakaze.Attributes;
using System.Threading.Tasks;
using System.Linq;
using DSharpPlus.Entities;
using DSharpPlus;
using DSharpPlus.Exceptions;

namespace Shimakaze
{
    class ManagementCommands : Commands
    {
        [Command("shimasay")]
        [RequireShimaTeam]
        [Hidden]
        public async Task ShimaSay(CommandContext ctx, [RemainingText] string fullMessage)
        {
            if (fullMessage.Length == 0)
            {
                await ctx.RespondAsync("Please write the guild, channel and text");
                return;
            }
            string[] idArray = fullMessage.Split(" ");
            int channelIndex = fullMessage.IndexOf("#");

            DiscordGuild guild = null;
            DiscordChannel channel = null;


            // get guild
            ulong guildId = 0;
            if (!ulong.TryParse(idArray[0], out guildId))
            {
                if (channelIndex <= 0)
                {
                    await ctx.RespondAsync("Please **ALSO** write the channel and text, or use IDs for both guild and channel.");
                    return;
                }

                string guildName = fullMessage.Substring(0, channelIndex - 1);
                if (guildName[guildName.Length - 1] == ' ') //fixes channel mentions
                {
                    guildName = guildName.Substring(0, guildName.Length - 1);
                }

                guild = ShimakazeBot.Client.Guilds.Values.ToList().FirstOrDefault(item => item.Name == guildName);
                if (guild == null)
                {
                    await ctx.RespondAsync($"Guild **{guildName}** not found!");
                    return;
                }
            }
            if (guild == null)
            {
                if (guildId == 0)
                {
                    await ctx.RespondAsync($"Guild ID **{guildId}** is not a valid ID!");
                    return;
                }
                else
                {
                    guild = ShimakazeBot.Client.Guilds.Values.ToList().FirstOrDefault(item => item.Id == guildId);
                    if (guild == null)
                    {
                        await ctx.RespondAsync($"Guild with ID **{guildId}** not found!");
                        return;
                    }
                }
            }

            // remove the guild
            if (channelIndex > 0)
            {
                idArray = fullMessage.Substring(channelIndex + 1).Split(" ");
            }
            else
            {
                idArray = idArray.Skip(1).ToArray();
            }

            // get channel
            ulong channelId = 0;
            if (channelIndex > 0 &&
                idArray[0] == $"{ctx.Message.MentionedChannels.ToList()[0].Id}>")
            {
                channelId = ctx.Message.MentionedChannels.ToList()[0].Id;
            }
            else if (!ulong.TryParse(idArray[0], out channelId) && channelIndex > 0)
            {
                channel = guild.Channels.Values.ToList().FirstOrDefault(item => item.Name == idArray[0]);
                if (channel == null)
                {
                    await ctx.RespondAsync($"Channel **{idArray[0]}** not found in **{guild.Name}**!");
                    return;
                }
            }
            if (channel == null)
            {
                if (channelId == 0)
                {
                    await ctx.RespondAsync($"Channel ID **{channelId}** is not a valid ID!");
                    return;
                }
                else
                {
                    channel = guild.Channels.Values.ToList().FirstOrDefault(item => item.Id == channelId);
                    if (channel == null)
                    {
                        await ctx.RespondAsync($"Channel with ID **{channelId}** not found in **{guild.Name}**!");
                        return;
                    }
                }
            }

            // remove the channel
            idArray = idArray.Skip(1).ToArray();
            if (idArray.Length == 0)
            {
                await ctx.RespondAsync("Please **ALSO** write the text");
                return;
            }

            // get message
            string message = string.Join(" ", idArray);

            // send message
            await ShimakazeBot.Client.SendMessageAsync(channel, message);
            await ctx.RespondAsync($"_{message}_ sent to **{channel.Name}** in **{guild.Name}**");
        }

        [Command("addrole")]
        [Attributes.RequireBotPermissions(Permissions.ManageRoles, "I don't have enough permissions to do this!")]
        [Aliases("gibrole", "assignrole")]
        public async Task AddRole(CommandContext ctx, [RemainingText] string roleString)
        {
            await SetRole(ctx, roleString);
        }

        [Command("removerole")]
        [Attributes.RequireBotPermissions(Permissions.ManageRoles, "I don't have enough permissions to do this!")]
        [Aliases("takerole", "unassignrole")]
        public async Task RemoveRole(CommandContext ctx, [RemainingText] string roleString)
        {
            await SetRole(ctx, roleString, false);
        }

        [Command("purge")]
        [Attributes.RequirePermissions(Permissions.ManageMessages)]
        public async Task Purge(CommandContext ctx, [RemainingText] string suffix)
        {
            string[] suffixArray = suffix?.Split(" ");
            List<ulong> usersToPurge = new List<ulong>();
            string usersToPurgeString = "";
            if (suffixArray.Length > 1 )
            {
                usersToPurge = Utils.GetIdListFromArray(ctx.Message.MentionedUsers, suffixArray.Skip(1).ToArray());


                foreach (var id in usersToPurge)
                {
                    if (ctx.Guild.Members.ContainsKey(id))
                    {
                        usersToPurgeString += ctx.Guild.Members[id].DisplayName;
                    }
                    else
                    {
                        await ctx.RespondAsync($"Unable to find member with ID {id}");
                        return;
                    }
                }
            }

            int purgeAmount;
            if (!Int32.TryParse(suffixArray[0], out purgeAmount) || 
                purgeAmount <= 0 || purgeAmount > 100)
            {
                await ctx.RespondAsync($"{suffixArray[0]} is not a valid number between 1 and 100.");
                return;
            }
            
            if (usersToPurge.Count > 0)
            {
                DiscordMessage earliestMessage = ctx.Message;
                List<DiscordMessage> readMessages;
                List<DiscordMessage> messagesToDelete = new List<DiscordMessage>();
                do
                {
                    readMessages = (await ctx.Channel.GetMessagesBeforeAsync(earliestMessage.Id, 500)).ToList();
                    for (int i = 0; i < readMessages.Count; i++)
                    {
                        if (usersToPurge.Contains(readMessages[i].Author.Id))
                        {
                            messagesToDelete.Add(readMessages[i]);
                            if (messagesToDelete.Count == purgeAmount)
                            {
                                break;
                            }
                        }
                    }
                } while (readMessages.Count == 500 && messagesToDelete.Count < purgeAmount);

                await ctx.Channel.DeleteMessagesAsync(messagesToDelete);
            }
            else
            {
                await ctx.Channel.DeleteMessagesAsync(
                    await ctx.Channel.GetMessagesBeforeAsync(ctx.Message.Id, purgeAmount));
            }

            await ctx.RespondAsync($"Successfully purged **{purgeAmount}** messages" +
                (usersToPurge.Count > 0 ? $"from **{usersToPurgeString}**" : ""));
        }

        [Command("warn")]
        [RequireAdmin]
        public async Task Warn(CommandContext ctx, [RemainingText] string suffix)
        {
            await ModerateUser(ctx, suffix, ShimaConsts.ModerationType.WARN);
        }

        [Command("kick")]
        [RequireAdmin]
        public async Task Kick(CommandContext ctx, [RemainingText] string suffix)
        {
            await ModerateUser(ctx, suffix, ShimaConsts.ModerationType.KICK);
        }

        [Command("ban")]
        [RequireAdmin]
        public async Task Ban(CommandContext ctx, [RemainingText] string suffix)
        {
            await ModerateUser(ctx, suffix, ShimaConsts.ModerationType.BAN);
        }

        [Command("removewarn")]
        [RequireAdmin]
        public async Task RemoveWarn(CommandContext ctx, [RemainingText] string suffix)
        {
            List<ulong> userIds = Utils.GetIdListFromMessage(ctx.Message.MentionedUsers, suffix);
            if (userIds.Count == 0)
            {
                await ctx.RespondAsync("Please mention, type a user ID or ID to warn.");
                return;
            }
            if (ctx.Guild.Members.ContainsKey(userIds[0]))
            {
                ShimakazeBot.DbCtx.GuildWarn.RemoveRange(ShimakazeBot.DbCtx.GuildWarn.Where(g =>
                g.UserId == userIds[0] && g.GuildId == ctx.Guild.Id));

                await ctx.RespondAsync("Successfully removed all warnsings for " +
                    $"**{ctx.Guild.Members[userIds[0]].DisplayName}**.");
            }
            else
            {
                int id = (int)userIds[0];
                GuildWarn warn = await ShimakazeBot.DbCtx.GuildWarn.FindAsync(id);
                if (warn != null)
                {
                    ShimakazeBot.DbCtx.GuildWarn.Remove(warn);
                    await ShimakazeBot.DbCtx.SaveChangesAsync();
                    await ctx.RespondAsync($"Successfully removed warning with ID **{id}**");
                }
                else
                {
                    await ctx.RespondAsync($"Unable to find member or ID **{id}**");
                }
            }
        }

        [Command("warns")]
        public async Task Warns(CommandContext ctx, [RemainingText] string suffix)
        {
            List<ulong> userIds = string.IsNullOrWhiteSpace(suffix) ? 
                new List<ulong>() { ctx.User.Id } :
                Utils.GetIdListFromMessage(ctx.Message.MentionedUsers, suffix);
            if (userIds.Count == 0)
            {
                await ctx.RespondAsync("Please mention or type a user ID.");
                return;
            }
            if (ctx.Guild.Members.ContainsKey(userIds[0]))
            {
                RequireAdminAttribute adminCheck = 
                    new RequireAdminAttribute("Only server admins are allowed to view warnings of other users.");
                if (ctx.User.Id != userIds[0] && !await adminCheck.ExecuteCheckAsync(ctx, false))
                {
                    return;
                }
                DiscordEmbed warnEmbed = Utils.BaseEmbedBuilder(ctx,
                    $"Warnings for {ctx.Guild.Members[userIds[0]].DisplayName} ({userIds[0]})",
                    ctx.Guild.Members[userIds[0]].AvatarUrl,
                    null, ctx.Guild.Members[userIds[0]].Color);

                var warns = ShimakazeBot.DbCtx.GuildWarn.Where(g =>
                        g.UserId == userIds[0] && g.GuildId == ctx.Guild.Id
                    ).ToList();

                if (warns.Count() == 0)
                {
                    warnEmbed = new DiscordEmbedBuilder(warnEmbed)
                        .WithDescription($"{ctx.Guild.Members[userIds[0]].DisplayName} has no warnings.");
                }
                else
                {
                    warns.ForEach(item =>
                        warnEmbed = new DiscordEmbedBuilder(warnEmbed).AddField(item.TimeStamp.ToString(),
                            item.WarnMessage)
                    );
                }

                await ctx.RespondAsync(null, false, warnEmbed);
            }
            else
            {
                int id = (int)userIds[0];
                GuildWarn warn = await ShimakazeBot.DbCtx.GuildWarn.FindAsync(id);
                if (warn != null)
                {
                    await ctx.RespondAsync($"{warn.TimeStamp} - {warn.WarnMessage}");
                }
                else
                {
                    await ctx.RespondAsync($"Unable to find member or ID **{id}**");
                }
            }
        }

        private async Task SetRole(CommandContext ctx, string roleString, bool assign = true)
        {
            int selfAssignLimit = 0;
            if (ShimakazeBot.SelfAssignRoleLimit.ContainsKey(ctx.Guild.Id))
            {
                if (ShimakazeBot.SelfAssignRoleLimit[ctx.Guild.Id] == 0)
                {
                    selfAssignLimit = -1;
                }
                else if (ctx.Guild.Roles.ContainsKey(ShimakazeBot.SelfAssignRoleLimit[ctx.Guild.Id]))
                {
                    selfAssignLimit = ctx.Guild.Roles[ShimakazeBot.SelfAssignRoleLimit[ctx.Guild.Id]].Position;
                }
                else
                {
                    await ctx.RespondAsync("Self assignable role isn't properly set up. " +
                        "Please contact an Admin to reset it in the customization command.");
                    return;
                }
            }

            if (selfAssignLimit == -1)
            {
                await ctx.RespondAsync($"I am not allowed to {(assign ? "assign" : "unassign")} roles on this server. " +
                    $"Please contact an Admin to {(assign ? "add" : "remove")} your role.");
                return;
            }

            List<DiscordRole> roles = Utils.GetRolesFromString(ctx.Guild, roleString);
            roles = roles.Concat(ctx.Message.MentionedRoles).ToList();

            int highestRolePos = ctx.Guild.Members[ShimakazeBot.Client.CurrentUser.Id].Roles.
                OrderByDescending(role => role.Position).First().Position;

            string responseString = "";

            foreach (var role in roles)
            {
                if (role.Position == 0 || role.Name.StartsWith("@")) //ignore
                { }
                else if (selfAssignLimit > 0 && role.Position >= selfAssignLimit)
                {
                    responseString += $"**{role.Name}** is not a self-assignable role\n";
                }
                else if (role.Position >= highestRolePos)
                {
                    responseString += $"**{role.Name}** is too high for me to reach\n";
                }
                else
                {
                    try
                    {
                        if (assign)
                        {
                            await ctx.Member.GrantRoleAsync(role);
                        }
                        else
                        {
                            await ctx.Member.RevokeRoleAsync(role);
                        }
                        responseString += $"**{role.Name}** was successfully " +
                            $"{(assign ? "assigned to" : "removed from")} **{ctx.Member.DisplayName}**";
                    }
                    catch (UnauthorizedException ex)
                    {
                        responseString += $"Failed to {(assign ? "assign" : "remove")} role **{role.Name}** " +
                            $"{(assign ? "to" : "from")} **{ctx.Member.DisplayName}**";
                    }
                }
            }

            await ctx.RespondAsync(responseString);
        }

        private async Task ModerateUser(CommandContext ctx, string suffix, ShimaConsts.ModerationType type)
        {
            string[] suffixArray = string.IsNullOrWhiteSpace(suffix) ? new string[] { } : suffix.Split(" ");

            //get user to moderate
            List<ulong> userToModerateInList =
                Utils.GetIdListFromArray(ctx.Message.MentionedUsers, suffixArray.Take(1).ToArray());
            if (userToModerateInList.Count == 0)
            {
                await ctx.RespondAsync($"Please mention or type a user ID to {type.ToString().ToLower()}.");
                return;
            }
            ulong userToModerate = userToModerateInList[0];
            
            if (!ctx.Guild.Members.ContainsKey(userToModerate))
            {
                await ctx.RespondAsync($"Unable to find member with ID **{userToModerate}**");
                return;
            }

            DiscordMember exMember = ctx.Guild.Members[userToModerate];

            //get message
            string message = suffixArray.Length > 1 ? string.Join(" ", suffixArray.Skip(1)) : "";
            DiscordEmbed embed;
            //moderate
            switch (type)
            {
                case ShimaConsts.ModerationType.WARN:
                    GuildWarn warn = (await ShimakazeBot.DbCtx.GuildWarn.AddAsync(new GuildWarn
                    {
                        GuildId = ctx.Guild.Id,
                        UserId = userToModerate,
                        WarnMessage = message,
                        TimeStamp = DateTime.UtcNow
                    })).Entity;
                    await ShimakazeBot.DbCtx.SaveChangesAsync();

                    embed = Utils.BaseEmbedBuilder(ctx,
                            $"Added new warning for {exMember.DisplayName} ({userToModerate})",
                            exMember.AvatarUrl,
                            null, exMember.Color, $"warning ID: {warn.Id}");
                    if (!string.IsNullOrWhiteSpace(message))
                    {
                        embed = new DiscordEmbedBuilder(embed).AddField("Message", message);
                    }

                    await ctx.RespondAsync(null, false, embed);
                    break;
                case ShimaConsts.ModerationType.KICK:
                    try
                    {
                        await exMember.RemoveAsync(message);

                        embed = Utils.BaseEmbedBuilder(ctx,
                            $"Kicked {exMember.DisplayName} ({userToModerate})",
                            exMember.AvatarUrl, null, exMember.Color);
                        if (!string.IsNullOrWhiteSpace(message))
                        {
                            embed = new DiscordEmbedBuilder(embed).AddField("Reason", message);
                        }

                        await ctx.RespondAsync("Do you think they'll learn their lesson?", false, embed);
                    }
                    catch
                    {
                        await ctx.RespondAsync($"Failed to kick **{exMember.DisplayName}** ({userToModerate})");
                    }
                    break;
                case ShimaConsts.ModerationType.BAN:
                    try
                    {
                        await exMember.BanAsync(0, message);

                        embed = Utils.BaseEmbedBuilder(ctx,
                               $"Banned {exMember.DisplayName} ({userToModerate})",
                               exMember.AvatarUrl, null, exMember.Color);
                        if (!string.IsNullOrWhiteSpace(message))
                        {
                            embed = new DiscordEmbedBuilder(embed).AddField("Reason", message);
                        }

                        await ctx.RespondAsync("Good riddance.", false, embed);
                    }
                    catch
                    {
                        await ctx.RespondAsync($"Failed to ban **{exMember.DisplayName}** ({userToModerate})");
                    }
                    break;
            }

        }
    }
}
