using System;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.CommandsNext;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus.Entities;
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
