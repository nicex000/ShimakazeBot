using DSharpPlus.CommandsNext;
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
                await CTX.RespondSanitizedAsync(ctx, "Please write the guild, channel and text");
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
                    await CTX.RespondSanitizedAsync(ctx, "Please **ALSO** write the channel and text, or use IDs for both guild and channel.");
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
                    await CTX.RespondSanitizedAsync(ctx, $"Guild **{guildName}** not found!");
                    return;
                }
            }
            if (guild == null)
            {
                if (guildId == 0)
                {
                    await CTX.RespondSanitizedAsync(ctx, $"Guild ID **{guildId}** is not a valid ID!");
                    return;
                }
                else
                {
                    guild = ShimakazeBot.Client.Guilds.Values.ToList().FirstOrDefault(item => item.Id == guildId);
                    if (guild == null)
                    {
                        await CTX.RespondSanitizedAsync(ctx, $"Guild with ID **{guildId}** not found!");
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
                    await CTX.RespondSanitizedAsync(ctx, $"Channel **{idArray[0]}** not found in **{guild.Name}**!");
                    return;
                }
            }
            if (channel == null)
            {
                if (channelId == 0)
                {
                    await CTX.RespondSanitizedAsync(ctx, $"Channel ID **{channelId}** is not a valid ID!");
                    return;
                }
                else
                {
                    channel = guild.Channels.Values.ToList().FirstOrDefault(item => item.Id == channelId);
                    if (channel == null)
                    {
                        await CTX.RespondSanitizedAsync(ctx, $"Channel with ID **{channelId}** not found in **{guild.Name}**!");
                        return;
                    }
                }
            }

            // remove the channel
            idArray = idArray.Skip(1).ToArray();
            if (idArray.Length == 0)
            {
                await CTX.RespondSanitizedAsync(ctx, "Please **ALSO** write the text");
                return;
            }

            // get message
            string message = string.Join(" ", idArray);

            // send message
            await CTX.SendSanitizedMessageAsync(channel, message);
            await CTX.RespondSanitizedAsync(ctx, $"_{message}_ sent to **{channel.Name}** in **{guild.Name}**");
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
                    await CTX.RespondSanitizedAsync(ctx, "Self assignable role isn't properly set up. " +
                        "Please contact an Admin to reset it in the customization command.");
                    return;
                }
            }

            if (selfAssignLimit == -1)
            {
                await CTX.RespondSanitizedAsync(ctx, $"I am not allowed to {(assign ? "assign" : "unassign")} roles on this server. " +
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

            await CTX.RespondSanitizedAsync(ctx, responseString);
        }
    }
}
