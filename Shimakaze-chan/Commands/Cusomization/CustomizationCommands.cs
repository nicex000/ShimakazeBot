using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.CommandsNext;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shimakaze
{
    class CustomizationCommands : Commands
    {
        [Command("streamrole")]
        [Description("Sets or removes the streaming role.")]
        public async Task SetStreamingRole(CommandContext ctx, [RemainingText] string roleName)
        {
            ulong roleId = 0;
            if (ctx.Message.MentionedRoles.Count > 0)
            {
                roleId = ctx.Message.MentionedRoles[0].Id;
            }
            else if (!string.IsNullOrWhiteSpace(roleName))
            {
                var role = ctx.Guild.Roles.Values.FirstOrDefault(item => item.Name == roleName);
                roleId = role != null ? role.Id : 0;
            }
            else
            {
                if (ShimakazeBot.StreamingEnabledGuilds.ContainsKey(ctx.Guild.Id))
                {
                    ShimakazeBot.StreamingEnabledGuilds.Remove(ctx.Guild.Id);
                    ShimakazeBot.DbCtx.StreamingGuild.RemoveRange(ShimakazeBot.DbCtx.StreamingGuild.Where(g => g.GuildId == ctx.Guild.Id));
                    ShimakazeBot.DbCtx.SaveChanges();
                    await ctx.RespondAsync("Streaming role configuration removed.");
                }
                else
                {
                    await ctx.RespondAsync("Please mention or write the role name.");
                }
                return;
            }

            if (roleId == 0)
            {
                await ctx.RespondAsync("Invalid role.");
                return;
            }

            if (ShimakazeBot.StreamingEnabledGuilds.ContainsKey(ctx.Guild.Id))
            {
                ShimakazeBot.StreamingEnabledGuilds[ctx.Guild.Id] = roleId;
                var streamingGuild = ShimakazeBot.DbCtx.StreamingGuild.First(g => g.GuildId == ctx.Guild.Id);
                streamingGuild.RoleId = roleId;
                ShimakazeBot.DbCtx.StreamingGuild.Update(streamingGuild);
                ShimakazeBot.DbCtx.SaveChanges();
                await ctx.RespondAsync("streaming role configuration updated.");
            }
            else
            {
                ShimakazeBot.StreamingEnabledGuilds.Add(ctx.Guild.Id, roleId);
                ShimakazeBot.DbCtx.StreamingGuild.Add(new StreamingGuild { GuildId = ctx.Guild.Id, RoleId = roleId });
                ShimakazeBot.DbCtx.SaveChanges();
                await ctx.RespondAsync("streaming role configuration added.");
            }
        }

        [Command("cprefix")]
        [Description("Changes the prefix.")]
        public async Task CustomizePrefix(CommandContext ctx, [RemainingText] string newPrefix)
        {
            if (ctx.Member != ctx.Guild.Owner)
            {
                await ctx.RespondAsync("Only the server owner can change the prefix.");
                return;
            }

            if (ShimakazeBot.CustomPrefixes.ContainsKey(ctx.Guild.Id) || newPrefix == ShimakazeBot.DefaultPrefix)
            {
                if (string.IsNullOrWhiteSpace(newPrefix) || newPrefix == ShimakazeBot.DefaultPrefix)
                {
                    ShimakazeBot.CustomPrefixes.Remove(ctx.Guild.Id);
                    ShimakazeBot.DbCtx.GuildPrefix.RemoveRange(ShimakazeBot.DbCtx.GuildPrefix.Where(p => p.GuildId == ctx.Guild.Id));
                    ShimakazeBot.DbCtx.SaveChanges();
                    await ctx.RespondAsync($"Prefix reset to default: **{ShimakazeBot.DefaultPrefix}**");
                }
                else
                {
                    ShimakazeBot.CustomPrefixes[ctx.Guild.Id] = newPrefix;
                    var guildPrefix = ShimakazeBot.DbCtx.GuildPrefix.First(p => p.GuildId == ctx.Guild.Id);
                    guildPrefix.Prefix = newPrefix;
                    ShimakazeBot.DbCtx.GuildPrefix.Update(guildPrefix);
                    ShimakazeBot.DbCtx.SaveChanges();
                    await ctx.RespondAsync($"Prefix updated to: **{newPrefix}**");
                }
            }
            else
            {
                if (string.IsNullOrWhiteSpace(newPrefix))
                {
                    await ctx.RespondAsync("Default prefix is: **" + ShimakazeBot.DefaultPrefix + "**\nProvide a prefix to change it.");
                }
                else
                {
                    ShimakazeBot.CustomPrefixes.Add(ctx.Guild.Id, newPrefix);
                    ShimakazeBot.DbCtx.GuildPrefix.Add(new GuildPrefix { GuildId = ctx.Guild.Id, Prefix = newPrefix });
                    ShimakazeBot.DbCtx.SaveChanges();
                    await ctx.RespondAsync("Prefix updated to: **" + newPrefix + "**");
                }
            }
        }
    }
}
