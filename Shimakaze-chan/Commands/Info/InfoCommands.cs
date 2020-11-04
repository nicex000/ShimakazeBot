using System;
using System.Collections.Generic;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.CommandsNext;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using Shimakaze.Attributes;

namespace Shimakaze
{
    class InfoCommands : Commands
    {
        [Command("info")]
        [Description("Quick tooltip regarding the purpose of this bot.")]
        public async Task DisplayInfo(CommandContext ctx)
        {
            var uptime = (DateTime.Now - ShimaConsts.applicationStartTime);
            var botInfo = new DiscordEmbedBuilder()
                .WithAuthor($"Shimakaze-chan", "", $"{ctx.Client.CurrentUser.AvatarUrl}")
                .WithColor(new DiscordColor("#3498db"))
                .WithTitle($"Running on ShimaEngine version {ShimaConsts.Version}")
                .WithUrl($"https://github.com/nicex000/ShimaTempVoice")
                .WithTimestamp(DateTime.Now)
                .AddField($"Servers connected", $"```{ctx.Client.Guilds.Count}```", true)
                .AddField($"Users known", $"```{ctx.Client.Guilds.Sum(guild => guild.Value.MemberCount)}```", true)
                .AddField($"Channels connected",
                    $"```{ctx.Client.Guilds.Sum(guild => guild.Value.Channels.Count)}```", true)
                .AddField($"Private channels", $"```{ctx.Client.PrivateChannels.Count}```", true)
                .AddField($"Owners", $@"{string.Join("\n",
                    from owner in ctx.Client.CurrentApplication.Owners.Reverse()
                    select owner.Mention)}", true)
                .WithFooter($"Online for {(DateTime.Now - ShimaConsts.applicationStartTime).Days} days, " +
                            $"{uptime.Hours} {(uptime.Hours is 1 ? "hour, " : "hours, ")}" +
                            $"{uptime.Minutes} {(uptime.Minutes is 1 ? "minute, " : "minutes, ")}" +
                            $"{uptime.Hours} {(uptime.Seconds is 1 ? "second." : "seconds.")}");
            await CTX.RespondSanitizedAsync(ctx, "This bot serves as a temporary hotfix compliment to the original Shimakaze's broken voicechat functions." +
                                                 " We will notify You when we're done with rewriting mainline Shimakaze and bring all of her functionality back up. At least " +
                                                 "the parts that were actively used, we will leave out some useless shit like cleverbot." +
                                                 $"\nRunning ShimaEngine v.{ShimaConsts.Version}",
                false, botInfo.Build());
        }

        [Command("server-info")]
        [CannotBeUsedInDM]
        [Description("I'll tell you some information about the server you're currently in.")]
        public async Task DisplayServerInfo(CommandContext ctx)
        {
            var textChannels = string.Join(", ",
                from channel in ctx.Guild.Channels.Values
                where channel.Type is ChannelType.Text
                select channel.Name);
            if (textChannels.Length > 1018)
            {
                textChannels = "Too many to list!";
            }
            var voiceChannels = string.Join(", ",
                from channel in ctx.Guild.Channels.Values
                where channel.Type is ChannelType.Voice
                select channel.Name);
            if (voiceChannels.Length > 1018)
            {
                voiceChannels = "Too many to list!";
            }
            var roles = string.Join(", ",
                from role in ctx.Guild.Roles.Values
                select role.Name);
            if (roles.Length > 1018)
            {
                roles = "Too many to list!";
            }
            var serverInfo = new DiscordEmbedBuilder()
                .WithAuthor($"Information requested by {ctx.Message.Author.Username}", "",
                    $"{ctx.Message.Author.AvatarUrl}")
                .WithTimestamp(DateTime.Now)
                .WithColor(new DiscordColor("#3498db"))
                .AddField($"Server name", $"{ctx.Guild.Name} [{ctx.Guild.Id}]")
                .AddField($"Server owner", $@"{ctx.Guild.Owner.Mention} [{ctx.Guild.Owner.Id}]")
                .AddField($"Members", $"```{ctx.Guild.Members.Count}```", true)
                .AddField($"Text Channels",
                    $"```{ctx.Guild.Channels.Values.Count(chn => chn.Type == ChannelType.Text)}```", true)
                .AddField($"Voice Channels",
                    $"```{ctx.Guild.Channels.Values.Count(chn => chn.Type == ChannelType.Voice)}```", true)
                .AddField($"Text Channels", $"```{textChannels}```")
                .AddField($"Voice Channels", $"```{voiceChannels}```")
                .AddField($"AFK-channel", $"```{ctx.Guild.AfkChannel.Name} [{ctx.Guild.AfkChannel.Id}]```")
                .AddField($"Current Region", $"```{ctx.Guild.VoiceRegion.Name}```", true)
                .AddField($"Total Roles", $"```{ctx.Guild.Roles.Count}```", true)
                .AddField($"Roles", $"```{roles}```")
                .WithThumbnail($"{ctx.Guild.IconUrl}");

            await CTX.RespondSanitizedAsync(ctx,"", false, serverInfo.Build());
        }

        [Command("userinfo")]
        [Description("Displays information about the specified user account or the sender")]
        [CannotBeUsedInDM()]
        public async Task DisplayUserInfo(CommandContext ctx, [RemainingText] string commandPayload)
        {
            var members = new List<DiscordMember>();
            var uidList = Utils.GetIdListFromMessage(ctx.Message.MentionedUsers, commandPayload);
            foreach (var id in uidList)
            {
                if (ctx.Guild.Members.ContainsKey(id))
                {
                    members.Add(ctx.Guild.Members[id]);
                }
                else
                {
                    await CTX.RespondSanitizedAsync(ctx, $"Unable to find user with ID {id} on the server");
                    return;
                }
            }

            if (members.Count == 0)
            {
                members.Add(ctx.Member);
            }
            foreach (var member in members)
            {
                var userLevel = UserLevels.GetLevel(member.Id, ctx.Guild.Id);
                var globalUserLevel = UserLevels.GetMemberLevel(member);
                var roles = string.Join(", ",
                    from role in member.Roles
                    select role.Mention);
                var customStatus = string.Concat(
                    member.Presence.Activity.CustomStatus?.Emoji ?? "",
                    member.Presence.Activity.CustomStatus?.Name ?? "");
                if (string.IsNullOrEmpty(customStatus))
                {
                    customStatus = null;
                }
                // Header
                var userInfo = new DiscordEmbedBuilder()
                    .WithAuthor($"{member.Username}#{member.Discriminator} ({member.Id})",
                        "", member.AvatarUrl)
                    .WithTimestamp(DateTime.Now)
                    .WithColor(new DiscordColor("#3498db"))
                    .WithThumbnail(member.AvatarUrl)
                    .WithUrl(member.AvatarUrl)
                    .AddField($"Status", $"```\n{member.Presence.Status}```", true);
                // Activities
                string streams = null;
                string games = null;
                foreach (var act in member.Presence.Activities)
                {
                    if (act.ActivityType is ActivityType.Custom)
                    {
                        continue;
                    }
                    // So basically, am very tiny. In case you really need to know i just need a matching type lol.
                    // It's discarded anyway and a .ToString() still executes.
                    // Should a C style switch not have this problem it could be more elegant, if verbose af.
                    _ = act.ActivityType switch
                    {
                        ActivityType.Watching => userInfo
                            .AddField($"Watching", $"```{act.Name}```").ToString(),
                        ActivityType.ListeningTo => userInfo
                            .AddField($"Listening to", $"```{act.Name}```").ToString(),
                        ActivityType.Playing => games += $"```{act.Name}```",
                        ActivityType.Streaming => streams += $"```{act.RichPresence.Details}```\n{act.StreamUrl}",
                        _ => throw new NotImplementedException()
                    };
                }

                if (games?.Length > 0)
                {
                    userInfo.AddField($"Playing", $"{games} ");
                }

                if (streams?.Length > 0)
                {
                    userInfo.AddField($"Streaming", $"{streams} ");
                }
                // Account info
                userInfo
                    .AddField($"Custom status", $"\n{customStatus ?? "No custom status set"}")
                    .AddField($"Account Creation",
                        $"```\n{member.CreationTimestamp.UtcDateTime} UTC```", false)
                    .AddField($"Joined on", $"```\n{member.JoinedAt.UtcDateTime} UTC```")
                    .AddField($"Server access level",
                        $@"```{
                                userLevel switch
                                {
                                    (int) ShimaConsts.UserPermissionLevel.DEFAULT =>
                                        $"Default ({(int) ShimaConsts.UserPermissionLevel.DEFAULT})",
                                    (int) ShimaConsts.UserPermissionLevel.DEFAULT_SERVER_OWNER =>
                                        $"Server owner ({(int) ShimaConsts.UserPermissionLevel.DEFAULT_SERVER_OWNER})",
                                    (int) ShimaConsts.UserPermissionLevel.SHIMA_TEAM => "Bot owner",
                                    _ => userLevel.ToString()
                                }
                            }```", true)
                    .AddField($"Global access level",
                        $@"```{
                                globalUserLevel switch
                                {
                                    (int) ShimaConsts.UserPermissionLevel.DEFAULT =>
                                        $"Default ({(int) ShimaConsts.UserPermissionLevel.DEFAULT})",
                                    (int) ShimaConsts.UserPermissionLevel.SHIMA_TEAM => "Bot owner",
                                    _ => globalUserLevel.ToString()
                                }
                            }```",
                        true)
                    .AddField($"Roles", $"\n {roles}");

                await CTX.RespondSanitizedAsync(ctx,"", false, userInfo.Build());
            }
        }

        [Command("prefix")]
        [Description("Displays the current prefix, if you\'re that confused.")]
        public async Task DisplayPrefix(CommandContext ctx)
        {
            string prefix = ShimakazeBot.CustomPrefixes.ContainsKey(ctx.Guild.Id) ?
                            ShimakazeBot.CustomPrefixes[ctx.Guild.Id] :
                            ShimakazeBot.DefaultPrefix;
            await CTX.RespondSanitizedAsync(ctx,$"This server\'s prefix is: **{prefix}**" +
                "\n You can change the prefix with **cprefix**");
        }

        [Command("ping")]
        [Description("I'll reply to you with pong!")]
        public async Task Ping(CommandContext ctx)
        {
            var message = await CTX.RespondSanitizedAsync(ctx, "Pong!");
            await message.ModifyAsync("Pong! Time taken: " +
                $"{(message.CreationTimestamp - ctx.Message.CreationTimestamp).TotalMilliseconds}ms.");            
        }

        [Command("join-server")]
        [Aliases("joinserver", "invite", "invite-link", "invitelink")]
        public async Task JoinServer(CommandContext ctx)
        {
            await CTX.RespondSanitizedAsync(ctx, $"Here's an invite link: {ShimakazeBot.Config.settings.oauth}\n" +
                "An admin of the server will need to use this link to let me join the server.\n" +
                "*Can't wait to join a new server~~*  **Hurry up!**");
        }
    }
}
