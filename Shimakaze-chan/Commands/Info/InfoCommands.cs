using System;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.CommandsNext;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using Shimakaze.Attributes;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;

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
