using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using System;
using System.Collections.Generic;
using Shimakaze.Attributes;
using System.Threading.Tasks;
using System.Linq;
using DSharpPlus.Entities;
using DSharpPlus;
using DSharpPlus.SlashCommands;

namespace Shimakaze
{
    class ManagementCommands : Commands
    {
        [SlashCommand("shimasay", ":)")]
        [RequireShimaTeam]
        public async Task ShimaSay(InteractionContext ctx, 
            [Option("ch", ":)")][ChannelTypes(ChannelType.Text)] DiscordChannel channel = null,
            [Option("message", ":)")] string fullMessage = "")
        {
            if (fullMessage.Length == 0)
            {
                await SCTX.RespondSanitizedAsync(ctx, 
                    channel == null ? "Please write the guild, channel and message" : "Please write the message");
                return;
            }

            DiscordGuild guild = null;
            string message = "";

            if (channel == null)
            {
                if (!ExtractFromFullMessage(ctx, fullMessage, out guild, out channel, out message, out var errorMessage))
                {
                    await SCTX.RespondSanitizedAsync(ctx, errorMessage);
                    return;
                }
            }
            else
            {
                guild = channel.Guild;
                message = fullMessage;
            }

            

            // send message
            await CTX.SendSanitizedMessageAsync(channel, message);
            
            if (guild == null && string.IsNullOrWhiteSpace(channel.Name))
                await SCTX.RespondSanitizedAsync(ctx, $"_{message}_ sent in **DM**");
            else
                await SCTX.RespondSanitizedAsync(ctx, $"_{message}_ sent to **{channel.Name}** in **{guild.Name}**");
        }

        public bool ExtractFromFullMessage(InteractionContext ctx, string fullMessage,
            out DiscordGuild guild, out DiscordChannel channel, out string message, out string errorMessage)
        {
            string[] idArray = fullMessage.Split(" ");
            int channelIndex = fullMessage.IndexOf("#");

            guild = null;
            channel = null;
            message = "";
            errorMessage = "";

            // get guild
            ulong guildId = 0;
            if (!ulong.TryParse(idArray[0], out guildId))
            {
                if (channelIndex <= 0)
                {
                    errorMessage = "Please **ALSO** write the channel and text, or use IDs for both guild and channel." +
                                   "\nDon't forget the **#** on the channel name!";
                    return false;
                }

                string guildName = fullMessage.Substring(0, channelIndex - 1);
                if (guildName[guildName.Length - 1] == ' ') //fixes channel mentions
                {
                    guildName = guildName.Substring(0, guildName.Length - 1);
                }

                guild = ShimakazeBot.Client.Guilds.Values.ToList().FirstOrDefault(item => item.Name == guildName);
                if (guild == null)
                {
                    errorMessage = $"Guild **{guildName}** not found!";
                    return false;
                }
            }
            if (guild == null)
            {
                if (guildId == 0)
                {
                    errorMessage = $"Guild ID **{guildId}** is not a valid ID!";
                    return false;
                }
                else
                {
                    guild = ShimakazeBot.Client.Guilds.Values.ToList().FirstOrDefault(item => item.Id == guildId);
                    if (guild == null)
                    {
                        errorMessage = $"Guild with ID **{guildId}** not found!";
                        return false;
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
            if (channelIndex > 0 && ctx.ResolvedChannelMentions != null &&
                idArray[0] == $"{ctx.ResolvedChannelMentions.ToList()[0].Id}>")
            {
                channelId = ctx.ResolvedChannelMentions.ToList()[0].Id;
            }
            else if (!ulong.TryParse(idArray[0], out channelId) && channelIndex > 0)
            {
                channel = guild.Channels.Values.ToList().FirstOrDefault(item => item.Name == idArray[0]);
                if (channel == null)
                {
                    errorMessage = $"Channel **{idArray[0]}** not found in **{guild.Name}**!";
                    return false;
                }
            }
            if (channel == null)
            {
                if (channelId == 0)
                {
                    errorMessage = $"Channel ID **{channelId}** is not a valid ID!";
                    return false;
                }
                else
                {
                    channel = guild.Channels.Values.ToList().FirstOrDefault(item => item.Id == channelId);
                    if (channel == null)
                    {
                        errorMessage = $"Channel with ID **{channelId}** not found in **{guild.Name}**!";
                        return false;
                    }
                }
            }

            // remove the channel
            idArray = idArray.Skip(1).ToArray();
            if (idArray.Length == 0)
            {
                errorMessage = "Please **ALSO** write the message";
                return false;
            }

            // get message
            message = string.Join(" ", idArray);
            return true;
        }

        [SlashCommand("addrole", "):")]
        [Attributes.RequireBotPermissions(Permissions.ManageRoles, "I don't have enough permissions to do this!")]
        public async Task AddRole(InteractionContext ctx, [Option("role", "):")] DiscordRole role)
        {
            await SetRole(ctx, role);
        }

        [SlashCommand("removerole", "):")]
        [Attributes.RequireBotPermissions(Permissions.ManageRoles, "I don't have enough permissions to do this!")]
        public async Task RemoveRole(InteractionContext ctx, [Option("role", "):")] DiscordRole role)
        {
            await SetRole(ctx, role, false);
        }

        [Command("purge")]
         [Attributes.RequirePermissions(Permissions.ManageMessages)]
         [Description("Usage: purge amount <[user]>")]
         public async Task Purge(CommandContext ctx, [RemainingText] string suffix)
         {
             string[] suffixArray = suffix?.Split(" ");
             List<ulong> usersToPurge = new List<ulong>();
             string usersToPurgeString = "";
             if (suffixArray.Length > 1)
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
                         await CTX.RespondSanitizedAsync(ctx, $"Unable to find member with ID {id}");
                         return;
                     }
                 }
             }

             int purgeAmount;
             if (!int.TryParse(suffixArray[0], out purgeAmount) || purgeAmount <= 0)
             {
                 await CTX.RespondSanitizedAsync(ctx, $"{suffixArray[0]} is not a valid number between 1 and 100.");
                 return;
             }
             else if (purgeAmount > 100)
             {
                 await CTX.RespondSanitizedAsync(ctx, "I can only remove up to 100 messages at a time.");
                 return;
             }

             if (usersToPurge.Count > 0)
             {
                 DiscordMessage earliestMessage = ctx.Message;
                 List<DiscordMessage> readMessages;
                 List<DiscordMessage> messagesToDelete = new List<DiscordMessage>();
                 do
                 {
                     readMessages = (await ctx.Channel.GetMessagesBeforeAsync(
                         earliestMessage.Id, ShimaConsts.MaxMessageHistoryLoadCount)).ToList();
                     if (readMessages.Count > 0)
                     {
                         earliestMessage = readMessages.Last();
                     }
                     foreach (var message in readMessages.Where(message => usersToPurge.Contains(message.Author.Id)))
                     {
                         messagesToDelete.Add(message);
                         if (messagesToDelete.Count == purgeAmount)
                         {
                             break;
                         }
                     }
                 } while (readMessages.Count == ShimaConsts.MaxMessageHistoryLoadCount &&
                         messagesToDelete.Count < purgeAmount);

                 try
                 {
                     await ctx.Channel.DeleteMessagesAsync(messagesToDelete);
                 }
                 catch (Exception ex)
                 {
                     await CTX.RespondSanitizedAsync(ctx, $"Failed to delete the messages, reason:\n{ex.Message}");
                     return;
                 }
             }
             else
             {
                try
                 {
                    await ctx.Channel.DeleteMessagesAsync(
                         await ctx.Channel.GetMessagesBeforeAsync(ctx.Message.Id, purgeAmount));
                 }
                 catch (Exception ex)
                 {
                     await CTX.RespondSanitizedAsync(ctx, $"Failed to delete the messages, reason:\n{ex.Message}");
                     return;
                 }
             }

             await CTX.RespondSanitizedAsync(ctx, $"Successfully purged **{purgeAmount}** messages" +
                 (usersToPurge.Count > 0 ? $" from **{usersToPurgeString}**" : ""));
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
                 await CTX.RespondSanitizedAsync(ctx, "Please mention, type a user ID or a warn ID.");
                 return;
             }
             if (ctx.Guild.Members.ContainsKey(userIds[0]))
             {
                 ShimakazeBot.DbCtx.GuildWarn.RemoveRange(ShimakazeBot.DbCtx.GuildWarn.Where(g =>
                     g.UserId == userIds[0] && g.GuildId == ctx.Guild.Id));

                 await CTX.RespondSanitizedAsync(ctx, "Successfully removed all warnsings for " +
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
                     await CTX.RespondSanitizedAsync(ctx, $"Successfully removed warning with ID **{id}**");
                 }
                 else
                 {
                     await CTX.RespondSanitizedAsync(ctx, $"Unable to find member or ID **{id}**");
                 }
             }
         }

         [Command("warns")]
         [CannotBeUsedInDM]
         public async Task Warns(CommandContext ctx, [RemainingText] string suffix)
         {
             List<ulong> userIds = string.IsNullOrWhiteSpace(suffix) ?
                 new List<ulong>() { ctx.User.Id } :
                 Utils.GetIdListFromMessage(ctx.Message.MentionedUsers, suffix);
             if (userIds.Count == 0)
             {
                 await CTX.RespondSanitizedAsync(ctx, "Please mention or type a user ID.");
                 return;
             }
             if (ctx.Guild.Members.ContainsKey(userIds[0]))
             {
                 RequireAdminAttribute adminCheck =
                     new RequireAdminAttribute("Only server admins are allowed to view warnings of other users.");
                 if (ctx.User.Id != userIds[0] && true)//(!await checkDM.ExecuteChecksAsync(ctx))!await adminCheck.ExecuteCheckAsync(ctx, false))
                 {
                     return;
                 }
                 DiscordEmbedBuilder warnEmbed = Utils.BaseEmbedBuilder(ctx,
                     $"Warnings for {ctx.Guild.Members[userIds[0]].DisplayName} ({userIds[0]})",
                     ctx.Guild.Members[userIds[0]].AvatarUrl,
                     null, ctx.Guild.Members[userIds[0]].Color);

                 var warns = ShimakazeBot.DbCtx.GuildWarn.Where(g =>
                         g.UserId == userIds[0] && g.GuildId == ctx.Guild.Id
                     ).ToList();

                 if (warns.Count() == 0)
                 {
                     warnEmbed.WithDescription($"{ctx.Guild.Members[userIds[0]].DisplayName} has no warnings.");
                 }
                 else
                 {
                     warns.ForEach(item => warnEmbed.AddField(item.TimeStamp.ToString(),
                         item.WarnMessage.Length > 1024 ? $"{item.WarnMessage.Take(1021)}..." : item.WarnMessage));
                 }

                 await CTX.RespondSanitizedAsync(ctx, null, false, warnEmbed);
             }
             else
             {
                 int id = (int)userIds[0];
                 GuildWarn warn = await ShimakazeBot.DbCtx.GuildWarn.FindAsync(id);
                 if (warn != null)
                 {
                     await CTX.RespondSanitizedAsync(ctx, $"{warn.TimeStamp} - {warn.WarnMessage}");
                 }
                 else
                 {
                     await CTX.RespondSanitizedAsync(ctx, $"Unable to find member or ID **{id}**");
                 }
             }
         }

         [Command("setnickname")]
         [Attributes.RequireBotPermissions(Permissions.ChangeNickname,
             "The server won't let me change my nickname :(")]
         [RequireAdmin("Only a server admin can use this command.", true)]
         [Aliases("setnick", "changenickname", "changenick")]
         public async Task SetNickname(CommandContext ctx, [RemainingText] string nickname)
         {
             var bot = await ctx.Guild.GetMemberAsync(ctx.Client.CurrentUser.Id);
             await bot.ModifyAsync((member) => member.Nickname = nickname);
             await CTX.RespondSanitizedAsync(ctx, "Nickname " + (string.IsNullOrWhiteSpace(nickname) ?
                 $"removed.": $"changed to **{nickname}**."));
         }

         [Command("leave-server")]
         [RequireAdmin]
         [Aliases("leaveserver")]
         public async Task LeaveServer(CommandContext ctx)
         {
             if (!ShimakazeBot.Client.CurrentApplication.Owners.Contains(ctx.User))
             {
                 await CTX.RespondSanitizedAsync(ctx,
                     "Ok, I understand... I'm no longer wanted here. I'm sorry 😢\n*Runs away*");
             }
             ShimakazeBot.SendToDebugRoom(
                 $"Left **{ctx.Guild.Name}** ({ctx.Guild.Id}). Triggered by **{ctx.User.Username}** ({ctx.User.Id})");

             await ctx.Guild.LeaveAsync();
         }

         private async Task SetRole(InteractionContext ctx, DiscordRole role, bool assign = true)
         {
             int selfAssignLimit = -1;
             if (ShimakazeBot.SelfAssignRoleLimit.ContainsKey(ctx.Guild.Id))
             {
                 if (ctx.Guild.Roles.ContainsKey(ShimakazeBot.SelfAssignRoleLimit[ctx.Guild.Id]))
                 {
                     selfAssignLimit = ctx.Guild.Roles[ShimakazeBot.SelfAssignRoleLimit[ctx.Guild.Id]].Position;
                 }
                 else
                 {
                     await SCTX.RespondSanitizedAsync(ctx, "Self assignable role isn't properly set up. " +
                         "Please contact an Admin to reset it in the customization command.");
                     return;
                 }
             }

             if (selfAssignLimit == -1)
             {
                 await SCTX.RespondSanitizedAsync(ctx,
                     $"I am not allowed to {(assign ? "assign" : "unassign")} roles on this server. " +
                     $"Please contact an Admin to {(assign ? "add" : "remove")} your role.");
                 return;
             }

             int highestRolePos = ctx.Guild.Members[ShimakazeBot.Client.CurrentUser.Id].Roles.
                 OrderByDescending(role => role.Position).First().Position;

             string responseString = "";

             if (role != null)
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
                     catch
                     {
                         responseString += $"Failed to {(assign ? "assign" : "remove")} role **{role.Name}** " +
                             $"{(assign ? "to" : "from")} **{ctx.Member.DisplayName}**";
                     }
                 }
             }
             else
            {
                responseString += "Please provide a role.";
            }

             await SCTX.RespondSanitizedAsync(ctx, responseString);
         }

         private async Task ModerateUser(CommandContext ctx, string suffix, ShimaConsts.ModerationType type)
         {
             int userIndex = string.IsNullOrWhiteSpace(suffix) ? -1 : suffix.IndexOf(" ");
             string userIdString = userIndex > 0 ? suffix.Substring(0, userIndex) : suffix;

             //get user to moderate
             List<ulong> userToModerateInList =
                 Utils.GetIdListFromArray(ctx.Message.MentionedUsers, new string[] { userIdString });
             if (userToModerateInList.Count == 0)
             {
                 await CTX.RespondSanitizedAsync(ctx,
                     $"Please mention or type a user ID to {type.ToString().ToLower()}.");
                 return;
             }
             ulong userToModerate = userToModerateInList[0];

             if (!ctx.Guild.Members.ContainsKey(userToModerate))
             {
                 await CTX.RespondSanitizedAsync(ctx, $"Unable to find member with ID **{userToModerate}**");
                 return;
             }

             DiscordMember exMember = ctx.Guild.Members[userToModerate];

             //get message
             string message = userIndex > 0 ? suffix.Substring(userIndex).TrimStart() : "";
             DiscordEmbedBuilder embed;
             //moderate
             switch (type)
             {
                 case ShimaConsts.ModerationType.WARN:
                     if (message.Length > 1024)
                     {
                         await CTX.RespondSanitizedAsync(ctx, "Warning message must be under 1024 characters.");
                         return;
                     }

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
                         embed.AddField("Message", message);
                     }

                     await CTX.RespondSanitizedAsync(ctx, null, false, embed);
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
                             embed.AddField("Reason", message);
                         }

                         await CTX.RespondSanitizedAsync(ctx, "Do you think they'll learn their lesson?", false, embed);
                     }
                     catch
                     {
                         await CTX.RespondSanitizedAsync(ctx,
                             $"Failed to kick **{exMember.DisplayName}** ({userToModerate})");
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
                             embed.AddField("Reason", message);
                         }

                         await CTX.RespondSanitizedAsync(ctx, "Good riddance.", false, embed);
                     }
                     catch
                     {
                         await CTX.RespondSanitizedAsync(ctx,
                             $"Failed to ban **{exMember.DisplayName}** ({userToModerate})");
                     }
                     break;
             }
         }
    }
}
