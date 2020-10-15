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
            var botInfo = new DiscordEmbedBuilder()
                .WithAuthor($"Shimakaze-chan", "", $"{ctx.Client.CurrentUser.AvatarUrl}")
                .WithColor(new DiscordColor("#3498db"))
                .WithTitle($"Running on ShimaEngine version {ShimaConsts.Version}")
                .WithUrl($"https://github.com/nicex000/ShimaTempVoice")
                .WithTimestamp(DateTime.Now)
                .AddField($"Servers connected",$"```{ctx.Client.Guilds.Count}```", true)
                .AddField($"Users known", $"```{ ctx.Client.Guilds.Sum(guild => guild.Value.MemberCount) }```", true)
                .AddField($"Channels connected", 
                    $"```{ctx.Client.Guilds.Sum(guild => guild.Value.Channels.Count)}```",true)
                .AddField($"Private channels", $"```{ctx.Client.PrivateChannels.Count}```",true)
                .AddField($"Owners", $@"{ctx.Client.CurrentApplication.Owners.Reverse()
                    .Aggregate("", (current, owner) => current + $"{owner.Mention}\n")}", true)
                .WithFooter($"Online for {(DateTime.Now - ShimaConsts.applicationStartTime).Days} days, " +
                            $"{(DateTime.Now - ShimaConsts.applicationStartTime).Hours} hours, " +
                            $"{(DateTime.Now - ShimaConsts.applicationStartTime).Minutes} minutes, " +
                            $"{(DateTime.Now - ShimaConsts.applicationStartTime).Seconds} seconds.");
            await ctx.RespondAsync("This bot serves as a temporary hotfix compliment to the original Shimakaze's broken voicechat functions." +
                " We will notify You when we're done with rewriting mainline Shimakaze and bring all of her functionality back up. At least " +
                "the parts that were actively used, we will leave out some useless shit like cleverbot.",
                false, botInfo.Build());
        }

        [Command("server-info")]
        [CannotBeUsedInDM]
        [Description("I'll tell you some information about the server you're currently in.")]
        public async Task DisplayServerInfo(CommandContext ctx)
        {
            var textChannels = ctx.Guild.Channels.Values
                .Aggregate("", (current, channel) =>
                {
                    if (channel.Type == ChannelType.Text)
                    {
                        current += $"{channel.Name}, ";
                    }
                    return current;
                });
            var voiceChannels = ctx.Guild.Channels.Values
                .Aggregate("", (current, channel) =>
                {
                    if (channel.Type == ChannelType.Voice)
                    {
                        current += $"{channel.Name}, ";
                    }
                    return current;
                });
            var roles = ctx.Guild.Roles.Values.Aggregate("", (current, role) => current + $"{role.Name}, ");
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
                .AddField($"Text Channels", $"```{textChannels.Remove(textChannels.Length - 2, 2)}```")
                .AddField($"Voice Channels", $"```{voiceChannels.Remove(voiceChannels.Length - 2, 2)}```")
                .AddField($"AFK-channel", $"```{ctx.Guild.AfkChannel.Name} [{ctx.Guild.AfkChannel.Id}]```")
                .AddField($"Current Region", $"```{ctx.Guild.VoiceRegion.Name}```", true)
                .AddField($"Total Roles", $"```{ctx.Guild.Roles.Count}```", true)
                .AddField($"Roles", $"```{roles.Remove(roles.Length - 2, 2)}```")
                .WithThumbnail($"{ctx.Guild.IconUrl}");
            await ctx.RespondAsync("", false, serverInfo.Build());
        }

        [Command("userinfo")]
        [Description("Displays information about specified user account or the sender")]
        [CannotBeUsedInDM()]
        public async Task DisplayUserInfo(CommandContext ctx, [RemainingText] string commandPayload )
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
                    await ctx.RespondAsync($"Unable to find user with ID {id} on the server");
                    return;
                }
            }
            if (members.Count == 0)
            {
                members.Add(ctx.Member);
            }

            if (members.Count > 0)
            {
                foreach (var member in members)
                {
                    var userLevel = UserLevels.GetLevel(member.Id, ctx.Guild.Id);
                    var globalUserLevel = UserLevels.GetMemberLevel(member);
                    var roles = member.Roles
                        .Aggregate("", (current, role) => current + $"{role.Mention}, ");
                    string customStatus = null;
                    if (member.Presence.Activity.CustomStatus?.Emoji != null)
                    {
                        customStatus += member.Presence.Activity.CustomStatus.Emoji + " ";
                    }
                    if (member.Presence.Activity.CustomStatus?.Name != null)
                    {
                        customStatus += member.Presence.Activity.CustomStatus.Name;
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
                        .AddField($"Custom status",$"\n{customStatus ?? "No custom status set"}")
                        .AddField($"Account Creation",
                            $"```\n{member.CreationTimestamp.UtcDateTime} UTC```", false)
                        .AddField($"Joined on", $"```\n{member.JoinedAt.UtcDateTime} UTC```")
                        .AddField($"Server access level",
                            $@"```{
                                userLevel switch
                                {
                                    1 => "Default (1)",
                                    4 => "Server owner (4)",
                                    999 => "Bot owner",
                                    _ => userLevel.ToString()
                                }
                                }```", true)
                        .AddField($"Global access level",
                            $@"```{
                                globalUserLevel switch
                                {
                                    1 => "Default (1)",
                                    999 => "Bot owner",
                                    _ => globalUserLevel.ToString()
                                }
                                }```",
                            true)
                        .AddField($"Roles", $"\n {roles.Remove(roles.Length - 2, 2)}");

                    await ctx.RespondAsync("", false, userInfo.Build());
                }
            }
        }

        [Command("prefix")]
        [Description("Displays the current prefix, if you\'re that confused.")]
        public async Task DisplayPrefix(CommandContext ctx)
        {
            string prefix = ShimakazeBot.CustomPrefixes.ContainsKey(ctx.Guild.Id) ?
                            ShimakazeBot.CustomPrefixes[ctx.Guild.Id] :
                            ShimakazeBot.DefaultPrefix;
            await ctx.RespondAsync($"This server\'s prefix is: **{prefix}**" +
                "\n You can change the prefix with **cprefix**");
        }

        [Command("ping")]
        [Description("I'll reply to you with pong!")]
        public async Task Ping(CommandContext ctx)
        {
            var message = await ctx.RespondAsync("Pong!");
            await message.ModifyAsync("Pong! Time taken: " +
                $"{(message.CreationTimestamp - ctx.Message.CreationTimestamp).TotalMilliseconds}ms.");            
        }
    }
}
