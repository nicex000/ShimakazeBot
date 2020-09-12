using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.CommandsNext;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Shimakaze.Attributes;
using DSharpPlus.Entities;

namespace Shimakaze
{
    class CustomizationCommands : Commands
    {
        [Command("streamrole")]
        [Description("Sets or removes the streaming role." +
                     "\nUsage: Role name / mention / id, leave empty to remove.")]
        [RequireAdmin]
        public async Task SetStreamingRole(CommandContext ctx, [RemainingText] string roleName)
        {
            if (string.IsNullOrWhiteSpace(roleName))
            {
                if (ShimakazeBot.StreamingEnabledGuilds.ContainsKey(ctx.Guild.Id))
                {
                    ShimakazeBot.StreamingEnabledGuilds.Remove(ctx.Guild.Id);
                    ShimakazeBot.DbCtx.StreamingGuild.RemoveRange(
                        ShimakazeBot.DbCtx.StreamingGuild.Where(g => g.GuildId == ctx.Guild.Id));
                    await ShimakazeBot.DbCtx.SaveChangesAsync();
                    await ctx.RespondAsync("Streaming role configuration removed.");
                }
                else
                {
                    await ctx.RespondAsync("No configuration to remove.");
                }
            }

            DiscordRole role = null;
            ulong roleId = 0;
            if (ctx.Message.MentionedRoles.Count > 0)
            {
                if (ctx.Message.MentionedRoles.Count() > 1)
                {
                    await ctx.RespondAsync("Please mention only 1 role.");
                    return;
                }
                else
                {
                    role = ctx.Message.MentionedRoles[0];
                }
            }
            else if (ulong.TryParse(roleName, out roleId))
            {
                if (ctx.Guild.Roles.ContainsKey(roleId))
                {
                    role = ctx.Guild.Roles[roleId];
                }
                else
                {
                    await ctx.RespondAsync($"Unable to find role with ID **{roleId}**");
                    return;
                }
            }
            else
            {
                role = ctx.Guild.Roles.Values.FirstOrDefault(item => item.Name == roleName);
            }

            if (role == null)
            {
                await ctx.RespondAsync($"Unable to find role **{roleName}**");
                return;
            }

            if (ShimakazeBot.StreamingEnabledGuilds.ContainsKey(ctx.Guild.Id))
            {
                ShimakazeBot.StreamingEnabledGuilds[ctx.Guild.Id] = roleId;
                var streamingGuild = ShimakazeBot.DbCtx.StreamingGuild.First(g => g.GuildId == ctx.Guild.Id);
                streamingGuild.RoleId = roleId;
                ShimakazeBot.DbCtx.StreamingGuild.Update(streamingGuild);
                await ctx.RespondAsync($"Streaming role configuration updated with role **{role.Name}**.");
            }
            else
            {
                ShimakazeBot.StreamingEnabledGuilds.Add(ctx.Guild.Id, roleId);
                await ShimakazeBot.DbCtx.StreamingGuild.AddAsync(
                    new StreamingGuild { GuildId = ctx.Guild.Id, RoleId = roleId });
                await ctx.RespondAsync($"Streaming role configuration added with  role **{role.Name}**.");
            }
            await ShimakazeBot.DbCtx.SaveChangesAsync();
        }

        [Command("cprefix")]
        [Description("Changes the prefix.")]
        [RequireAdmin("Only a server admin can change the prefix.")]
        public async Task CustomizePrefix(CommandContext ctx, [RemainingText] string newPrefix)
        {
            if (ShimakazeBot.CustomPrefixes.ContainsKey(ctx.Guild.Id) || newPrefix == ShimakazeBot.DefaultPrefix)
            {
                if (string.IsNullOrWhiteSpace(newPrefix) || newPrefix == ShimakazeBot.DefaultPrefix)
                {
                    ShimakazeBot.CustomPrefixes.Remove(ctx.Guild.Id);
                    ShimakazeBot.DbCtx.GuildPrefix.RemoveRange(ShimakazeBot.DbCtx.GuildPrefix.Where(p => p.GuildId == ctx.Guild.Id));
                    await ShimakazeBot.DbCtx.SaveChangesAsync();
                    await ctx.RespondAsync($"Prefix reset to default: **{ShimakazeBot.DefaultPrefix}**");
                }
                else
                {
                    ShimakazeBot.CustomPrefixes[ctx.Guild.Id] = newPrefix;
                    var guildPrefix = ShimakazeBot.DbCtx.GuildPrefix.First(p => p.GuildId == ctx.Guild.Id);
                    guildPrefix.Prefix = newPrefix;
                    ShimakazeBot.DbCtx.GuildPrefix.Update(guildPrefix);
                    await ShimakazeBot.DbCtx.SaveChangesAsync();
                    await ctx.RespondAsync($"Prefix updated to: **{newPrefix}**");
                }
            }
            else
            {
                if (string.IsNullOrWhiteSpace(newPrefix))
                {
                    await ctx.RespondAsync($"Default prefix is: **{ShimakazeBot.DefaultPrefix}**\nProvide a prefix to change it.");
                }
                else
                {
                    ShimakazeBot.CustomPrefixes.Add(ctx.Guild.Id, newPrefix);
                    await ShimakazeBot.DbCtx.GuildPrefix.AddAsync(new GuildPrefix { GuildId = ctx.Guild.Id, Prefix = newPrefix });
                    await ShimakazeBot.DbCtx.SaveChangesAsync();
                    await ctx.RespondAsync($"Prefix updated to: **{newPrefix}**");
                }
            }
        }

        [Command("setlevel")]
        [Description("Sets the user level." +
                     "\nUsage: level mention/id")]
        [RequireGuild]
        [RequireLevel(3, "You need a higher level to set levels.")]
        public async Task SetMemberLevel(CommandContext ctx, [RemainingText] string text)
        {
            int requesterLevel = UserLevels.GetLevel(ctx.User.Id, ctx.Guild.Id);

            var textArray = text.Split(" ");
            int level;
            if (!Int32.TryParse(textArray[0], out level))
            {
                await ctx.RespondAsync($"{textArray[0]} is not a valid level.");
                return;
            }

            if (level >= requesterLevel)
            {
                await ctx.RespondAsync("You cannot assign a level higher than your own");
                return;
            }

            Dictionary<ulong, bool> idList = PrepareUserIdList(ctx.Message.MentionedUsers, textArray);

            ctx.Message.MentionedRoles.ToList().ForEach(role =>
            {
                if (!idList.ContainsKey(role.Id)) idList.Add(role.Id, true);
            });

            await SetLevelsFromList(ctx, idList, level, requesterLevel);
        }

        [Command("setgloballevel")]
        [Description("Sets the user level." +
                     "\nUsage: level mention/id")]
        [RequireShimaTeam]
        public async Task SetGlobalLevel(CommandContext ctx, [RemainingText] string text)
        {
            var textArray = text.Split(" ");
            int level;
            if (!Int32.TryParse(textArray[0], out level))
            {
                await ctx.RespondAsync($"{textArray[0]} is not a valid level.");
                return;
            }

            await SetLevelsFromList(ctx, PrepareUserIdList(ctx.Message.MentionedUsers, textArray),
                level, (int)ShimaConsts.UserPermissionLevel.SHIMA_TEAM);
        }

        [Command("setselfassign")]
        [Description("Sets or removes the role that acts as a hierarchy limit for self assignable roles." +
                     "\nUsage: Role name / mention / id, leave empty to remove.")]
        [Aliases("setselfassignlimit", "selfassign", "selfassignlimit")]
        [RequireAdmin]
        public async Task SetSelfAssign(CommandContext ctx, [RemainingText] string roleName)
        {
            // removes config
            if (string.IsNullOrWhiteSpace(roleName))
            {
                if (ShimakazeBot.SelfAssignRoleLimit.ContainsKey(ctx.Guild.Id))
                {
                    ShimakazeBot.SelfAssignRoleLimit.Remove(ctx.Guild.Id);
                    ShimakazeBot.DbCtx.GuildSelfAssign.RemoveRange(
                        ShimakazeBot.DbCtx.GuildSelfAssign.Where(p => p.GuildId == ctx.Guild.Id));
                    await ShimakazeBot.DbCtx.SaveChangesAsync();
                    await ctx.RespondAsync("Successfully removed the configuration.");
                }
                else
                {
                    await ctx.RespondAsync("No configuration to remove.");
                }
                return;
            }

            DiscordRole role = null;
            ulong roleId = 0;
            if (ctx.Message.MentionedRoles.Count() > 0)
            {
                if (ctx.Message.MentionedRoles.Count() > 1)
                {
                    await ctx.RespondAsync("Please mention only 1 role.");
                    return;
                }
                else
                {
                    role = ctx.Message.MentionedRoles[0];
                }
            }
            else if (ulong.TryParse(roleName, out roleId))
            {
                if (ctx.Guild.Roles.ContainsKey(roleId))
                {
                    role = ctx.Guild.Roles[roleId];
                }
                else
                {
                    await ctx.RespondAsync($"Unable to find role with ID **{roleId}**");
                    return;
                }
            }
            else
            {
                role = ctx.Guild.Roles.Values.FirstOrDefault(item => item.Name == roleName);
            }

            if (role == null)
            {
                await ctx.RespondAsync($"Unable to find role **{roleName}**");
                return;
            }

            if (ShimakazeBot.SelfAssignRoleLimit.ContainsKey(ctx.Guild.Id))
            {
                ShimakazeBot.SelfAssignRoleLimit[ctx.Guild.Id] = role.Id;
                var guildSelfAssign = ShimakazeBot.DbCtx.GuildSelfAssign.First(p => p.GuildId == ctx.Guild.Id);
                guildSelfAssign.RoleId = role.Id;
                ShimakazeBot.DbCtx.GuildSelfAssign.Update(guildSelfAssign);
                await ctx.RespondAsync($"Successfully updated the configuration to use role **{role.Name}**.");
            }
            else
            {
                ShimakazeBot.SelfAssignRoleLimit.Add(ctx.Guild.Id, role.Id);
                await ShimakazeBot.DbCtx.GuildSelfAssign.AddAsync(
                    new GuildSelfAssign { GuildId = ctx.Guild.Id, RoleId = role.Id });
                await ctx.RespondAsync($"Successfully added the configuration with role **{role.Name}**.");
            }
            await ShimakazeBot.DbCtx.SaveChangesAsync();
        }

        private Dictionary<ulong, bool> PrepareUserIdList(IReadOnlyList<DiscordUser> mentionedUsers, string[] textArray)
        {
            Dictionary<ulong, bool> idList = new Dictionary<ulong, bool>();
            ulong idFromText;
            mentionedUsers.ToList().ForEach(user =>
            {
                if (!idList.ContainsKey(user.Id))
                {
                    idList.Add(user.Id, false);
                }
            });
            foreach (var userId in textArray.Skip(1))
            {
                if (ulong.TryParse(userId, out idFromText) &&
                    !idList.ContainsKey(idFromText))
                {
                    idList.Add(idFromText, false);
                }
            }

            return idList;
        }

        private async Task SetLevelsFromList(CommandContext ctx, Dictionary<ulong, bool> idList, int level, int requesterLevel)
        {
            List<ulong> failedIDs = new List<ulong>();
            bool isGlobal = requesterLevel == (int)ShimaConsts.UserPermissionLevel.SHIMA_TEAM;

            foreach (var item in idList)
            {
                if (isGlobal || UserLevels.GetLevel(item.Key, ctx.Guild.Id) < requesterLevel)
                {
                    if (!await UserLevels.SetLevel(item.Key, ctx.Guild.Id, item.Value, level))
                    {
                        failedIDs.Add(item.Key);
                    }
                }
            }

            string response = $"Successfully assigned level to {idList.Count() - failedIDs.Count()} IDs";
            if (failedIDs.Count() > 0)
            {
                response += $"\nFailed to assign level to: {String.Join(", ", failedIDs)}";
            }

            await ctx.RespondAsync(response);
        }
    }
}
