using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using Shimakaze.Attributes;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Shimakaze
{
    class GeneralCommands : Commands
    {
        [Command("say")]
        [Description("I'll repeat what you said, if I want to.")]
        [RequireLevel(2, "I won't say anything you tell me to, not with that pleb level.")]
        public async Task Say(CommandContext ctx, [RemainingText] string message)
        {
            await CTX.RespondSanitizedAsync(ctx, message);
        }

        [Command("hello")]
        [Description("I'll say hi back because I'm not rude.")]
        [Aliases("hi")]
        public async Task Hello(CommandContext ctx)
        {
            await CTX.RespondSanitizedAsync(ctx, 
                $"Hi {(ctx.Guild == null ? ctx.User.Username : ctx.Member.DisplayName)}! Let's race!");
        }

        [Command("autodelete")]
        [Aliases("d")]
        // no dm attribute here tyvm
        public async Task AutoDelete(CommandContext ctx, [RemainingText] string suffix)
        {
            int index = string.IsNullOrWhiteSpace(suffix) ? -1 : suffix.IndexOf(" ");
            if (index > -1)
            {
                double timeout;
                if (double.TryParse(suffix.Substring(0, index), out timeout))
                {
                    await Task.Delay((int)(timeout * 1000d));
                    await ctx.Message.DeleteAsync("Autodelete");
                    return;
                }
            }
            await ctx.Message.DeleteAsync("Autodelete bad syntax");
            await ctx.Member.SendMessageAsync(
                "You messed up the autodelete syntax, here's your deleted message:\n```" +
                $"{suffix}\n```");
        }

        [Command("goodmorning")]
        [Aliases("gm", "morning")]
        public async Task GoodMorning(CommandContext ctx, [RemainingText] string message)
        {
            DiscordEmoji goodMorningEmoji = DiscordEmoji.FromGuildEmote(
                ShimakazeBot.Client, ShimaConsts.GoodMorningEmojiId);

            if (!string.IsNullOrWhiteSpace(message))
            {
                await CTX.RespondSanitizedAsync(ctx, $"{goodMorningEmoji}/ {message}!", false, null,
                    new List<IMention> { });
                return;
            }

            if (ctx.User.Id == 155038222794227712)
            {
                await CTX.RespondSanitizedAsync(ctx,
                    $"We have finally awoken, that was slow, wasn't it?\n{goodMorningEmoji}/ everyone!");
                return;
            }

            if (ThreadSafeRandom.ThisThreadsRandom.Next(0, 2) == 1)
            {
                await CTX.RespondSanitizedAsync(ctx, $"You're finally awake? You're too slow! {goodMorningEmoji}/");
            }
            else
            {
                await CTX.RespondSanitizedAsync(ctx, $"{goodMorningEmoji}/ Wanna race? I won't lose!");
            }
        }

        [Command("goodnight")]
        [Aliases("gn", "night")]
        public async Task GoodNight(CommandContext ctx, [RemainingText] string message)
        {
            DiscordEmoji goodNightEmoji = DiscordEmoji.FromGuildEmote(
                ShimakazeBot.Client, ShimaConsts.GoodNightEmojiId);

            if (!string.IsNullOrWhiteSpace(message))
            {
                await CTX.RespondSanitizedAsync(ctx, $"{goodNightEmoji}/ {message}!", false, null,
                    new List<IMention> { });
                return;
            }

            if (ctx.User.Id == 155038222794227712)
            {
                await CTX.RespondSanitizedAsync(ctx,
                    $"{goodNightEmoji}/ everyone! {ctx.User.Username} and I are going to bed now.");
                return;
            }

            await CTX.RespondSanitizedAsync(ctx, $"Go sleep already! You're so slow! {goodNightEmoji}/");
        }

        [Command("hug")]
        [Description("I guess i can give you a friendly hug...")]
        public async Task Hug(CommandContext ctx)
        {
            if (ctx.User.Id == 155038222794227712)
            {
                await CTX.RespondSanitizedAsync(ctx, $"*hugs {ctx.User.Mention} with lots of love ♥♥♥*");
            }
            else
            {
                await CTX.RespondSanitizedAsync(ctx, $"*hugs {ctx.User.Mention} in a friendly manner*");
            }
        }

        [Command("kiss")]
        [Description("I'll kiss my loved one ♥")]
        public async Task Kiss(CommandContext ctx)
        {
            if (ctx.User.Id == 155038222794227712)
            {
                await CTX.RespondSanitizedAsync(ctx, $"*gives {ctx.User.Mention} a lovers kiss ♥♥♥*");
            }
            else if (ThreadSafeRandom.ThisThreadsRandom.Next(0, 1000) < 5)
            {
                await CTX.RespondSanitizedAsync(ctx, $"{ctx.User.Mention} you Baka!" +
                    "\n*gives him a small kiss on the cheek ♥*");
            }
            else
            {
                await CTX.RespondSanitizedAsync(ctx, "I'm not going to kiss you!");
            }
        }
    }
}