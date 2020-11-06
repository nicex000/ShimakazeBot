using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Shimakaze
{
    class FunCommands : Commands
    {
        [Command("emote")]
        [Aliases("jumbo", "emoji")]
        public async Task Emote(CommandContext ctx, [RemainingText] string message)
        {
            try
            {
                await CTX.RespondSanitizedAsync(ctx,
                    DiscordEmoji.FromName(ShimakazeBot.Client, $":{message}:").Url);
                return;
            }
            catch { }

            ulong emoteId;
            try
            {
                if (ulong.TryParse(message, out emoteId))
                {
                    await CTX.RespondSanitizedAsync(ctx,
                    DiscordEmoji.FromGuildEmote(ShimakazeBot.Client, emoteId).Url);
                    return;
                }
            }
            catch { }

            if (string.IsNullOrWhiteSpace(message))
            {
                await CTX.RespondSanitizedAsync(ctx, "❓");
                return;
            }

            int index = message.LastIndexOf(":");
            if (index == -1)
            {
                await CTX.RespondSanitizedAsync(ctx, "That's not a discord emote, or one I can find.");
                return;
            }

            string endPart = message.Substring(index + 1);
            int endIndex = endPart.IndexOf(">");
            if (endIndex == -1 || !ulong.TryParse(endPart.Substring(0, endIndex), out emoteId))
            {
                await CTX.RespondSanitizedAsync(ctx, "That's not a discord emote.");
                return;
            }

            await CTX.RespondSanitizedAsync(ctx, "https://cdn.discordapp.com/emojis/" + $"{emoteId}.png");
        }

        [Command("8ball")]
        [Aliases("eightball")]
        public async Task EightBall(CommandContext ctx, [RemainingText] string ignoredQuestion)
        {
            if (string.IsNullOrWhiteSpace(ignoredQuestion))
            {
                await CTX.RespondSanitizedAsync(ctx, ctx.User.Mention +
                    ", I mean I can shake this 8ball all I want but without a question it's kinda dumb.");
                return;
            }
            //embed is better
            await CTX.RespondSanitizedAsync(ctx, $"The magic 8 ball says: **{FunConsts.Random8BallChoice()}**");
        }

        [Command("advice")]
        public async Task Advice(CommandContext ctx)
        {
            JObject response = await ShimaHttpClient.HttpGet("https://api.adviceslip.com/advice");
            if (response == null)
            {
                await CTX.RespondSanitizedAsync(ctx, "I've run out of advice ☹️");
                return;
            }
            //use embed for the advice #
            await CTX.RespondSanitizedAsync(ctx, response["slip"]?["advice"]?.Value<string>());
        }

        [Command("uselessfact")]
        [Aliases("fact")]
        public async Task UselessFact(CommandContext ctx)
        {
            JObject response = await ShimaHttpClient.HttpGet("https://uselessfacts.jsph.pl/random.json?language=en");
            if (response == null)
            {
                await CTX.RespondSanitizedAsync(ctx, "I've run out of useless facts ☹️");
                return;
            }

            await CTX.RespondSanitizedAsync(ctx, response["text"]?.Value<string>());
        }

        [Command("leetspeak")]
        [Aliases("leet", "leetspeech", "leetspeek")]
        [Description("Converts your text into leet speech")]
        public async Task LeetSpeak(CommandContext ctx, [RemainingText] string textToConvert)
        {
            if (string.IsNullOrWhiteSpace(textToConvert))
            {
                return;
            }

            var textArray = textToConvert.ToCharArray();
            string leetText = "";
            foreach (var character in textArray)
            {
                if (FunConsts.LeetKeyChars.ContainsKey(character))
                {
                    leetText += FunConsts.LeetKeyChars[character];
                }
                else
                {
                    leetText += character;
                }
            }

            await CTX.RespondSanitizedAsync(ctx, leetText);
        }

        [Command("catfact")]
        [Aliases("randomcat", "cat")]
        public async Task CatFact(CommandContext ctx)
        {
            JObject response = await ShimaHttpClient.HttpGet("https://catfact.ninja/fact");
            JObject image = await ShimaHttpClient.HttpGet("http://aws.random.cat/meow");
            if (response == null || image == null)
            {
                await CTX.RespondSanitizedAsync(ctx, "Kitties are gone ☹️");
                return;
            }
            // use embed for the image
            await CTX.RespondSanitizedAsync(ctx, response["fact"]?.Value<string>());
            //alternatively https://cataas.com/cat
            await CTX.RespondSanitizedAsync(ctx, image["file"]?.Value<string>());
        }

        [Command("dogfact")]
        [Aliases("randomdog", "dog")]
        public async Task DogFact(CommandContext ctx)
        {
            JObject response = await ShimaHttpClient.HttpGet("https://some-random-api.ml/facts/dog");
            JObject image = await ShimaHttpClient.HttpGet("https://random.dog/woof.json?filter=mp4,webm");
            if (response == null || image == null)
            {
                await CTX.RespondSanitizedAsync(ctx, "Doggos are gone ☹️");
                return;
            }
            // use embed for the image
            await CTX.RespondSanitizedAsync(ctx, response["fact"]?.Value<string>());
            await CTX.RespondSanitizedAsync(ctx, image["url"]?.Value<string>());
        }

        [Command("stroke")]
        [Aliases("ego", "strokeego")]
        [Description("Usage: stroke <first name> <last name>.\nOtherwise defaults to Chuck Norris.")]
        public async Task Stroke(CommandContext ctx, [RemainingText] string name)
        {
            string url = "https://api.icndb.com/jokes/random";
            if (!string.IsNullOrWhiteSpace(name))
            {
                var nameArray = name.Split(" ");
                if (nameArray.Length >= 2)
                {
                    url += $"?firstName={nameArray[0]}&lastName={nameArray[1]}";
                }
            }
            JObject response = await ShimaHttpClient.HttpGet(url);
            if (response == null)
            {
                await CTX.RespondSanitizedAsync(ctx, "Chuck Norris kicked the jokes away 👢");
                return;
            }

            await CTX.RespondSanitizedAsync(ctx, response["value"]?["joke"]?.Value<string>());
        }

        [Command("yomomma")]
        public async Task YoMomma(CommandContext ctx)
        {
            JObject response = await ShimaHttpClient.HttpGet("https://api.yomomma.info");
            if (response == null)
            {
                await CTX.RespondSanitizedAsync(ctx, "Yo momma so fat she broke the api.");
                return;
            }

            await CTX.RespondSanitizedAsync(ctx, response["joke"]?.Value<string>());
        }

        [Command("yesno")]
        public async Task YesNo(CommandContext ctx, [RemainingText] string choice)
        {
            JObject response = await ShimaHttpClient.HttpGet($"https://yesno.wtf/api/?force={choice}");
            if (response == null)
            {
                await CTX.RespondSanitizedAsync(ctx, "**No.**\nThe api broke.");
                return;
            }

            await CTX.RespondSanitizedAsync(ctx, response["image"]?.Value<string>());
        }

        [Command("fancyinsult")]
        [Aliases("insult")]
        public async Task FancyInsult(CommandContext ctx)
        {
            JObject response = await ShimaHttpClient.HttpGet($"http://quandyfactory.com/insult/json/");
            if (response == null)
            {
                await CTX.RespondSanitizedAsync(ctx, "Damned as thou art, thou hast broken the api.");
                return;
            }

            //embed image if keeping it
            await CTX.RespondSanitizedAsync(ctx, response["insult"]?.Value<string>());
            await CTX.RespondSanitizedAsync(ctx, "https://cdn.donmai.us/original/68/79/__kongou_kantai_collection_drawn_by_misumi_niku_kyu__68791ec3592899091779d132c06a0bba.jpg");
        }
    }
}
