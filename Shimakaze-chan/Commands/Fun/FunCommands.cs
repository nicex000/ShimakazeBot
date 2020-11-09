using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using Newtonsoft.Json.Linq;
using System;
using System.Linq;
using System.Net.Http.Headers;
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
            await CTX.RespondSanitizedAsync(ctx, null, false,
                Utils.BaseEmbedBuilder(ctx, null as DiscordUser, "The magic 8 ball says")
                    .WithDescription($"{ FunConsts.Random8BallChoice()}")
                    .WithTimestamp(null));
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
            await CTX.RespondSanitizedAsync(ctx, null, false,
                Utils.BaseEmbedBuilder(ctx, null, response["slip"]?["advice"]?.Value<string>(), null,
                   $"#{response["slip"]?["id"]?.Value<string>()}")
                   .WithTimestamp(null));
        }

        [Command("dice")]
        [Aliases("roll", "rolz")]
        public async Task Dice(CommandContext ctx, [RemainingText] string suffix)
        {
            JObject response = await ShimaHttpClient.HttpGet($"https://rolz.org/api/?{suffix}.json");
            if (response == null)
            {
                await CTX.RespondSanitizedAsync(ctx, "The dice fell under the table...");
                return;
            }
            if (string.IsNullOrEmpty(response["result"].Value<string>()) ||
                response["result"].Value<string>().StartsWith("Error"))
            {
                await CTX.RespondSanitizedAsync(ctx, $"{response["result"].Value<string>()}" +
                    "\nYou probably want to use the website for that one: https://rolz.org");
                return;
            }

            DateTime timestamp = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                .AddSeconds(response["timestamp"].Value<long>());
            DiscordEmbedBuilder embed = Utils.BaseEmbedBuilder(ctx, null,
                null, null, null, timestamp)
                .WithAuthor(response["result"].Value<string>(), "https://rolz.org", "https://rolz.org/img/n3-d20.png")
                .AddField("Input", response["input"].Value<string>())
                .AddField("Details", response["details"].Value<string>());
            if (!string.IsNullOrWhiteSpace(response["code"].Value<string>()))
            {
                embed.AddField("Code", response["code"].Value<string>());
            }

            await CTX.RespondSanitizedAsync(ctx, null, false, embed);
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

        [Command("inspire")]
        [Aliases("inspireme", "inspirationalquote")]
        public async Task Inspire(CommandContext ctx)
        {
            JObject response = await ShimaHttpClient.HttpGet("https://inspirobot.me/api?generate=true");
            if (response == null)
            {
                await CTX.RespondSanitizedAsync(ctx, "help viscocchi");
                return;
            }

            await CTX.RespondSanitizedAsync(ctx, $"|| {response["data"]?.Value<string>()} ||");
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
            //alternatively https://cataas.com/cat
            await CTX.RespondSanitizedAsync(ctx, null, false,
                Utils.BaseEmbedBuilder(ctx, null as DiscordUser, response["fact"]?.Value<string>())
                   .WithTimestamp(null)
                   .WithImageUrl(image["file"]?.Value<string>()));
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
            await CTX.RespondSanitizedAsync(ctx, null, false,
                Utils.BaseEmbedBuilder(ctx, null as DiscordUser, response["fact"]?.Value<string>())
                   .WithTimestamp(null)
                   .WithImageUrl(image["url"]?.Value<string>()));
        }

        [Command("randommeme")]
        [Aliases("meme")]
        public async Task RandomMeme(CommandContext ctx)
        {
            JObject response = await ShimaHttpClient.HttpGet(
                $"https://api.imgur.com/3/g/memes/viral/{ThreadSafeRandom.ThisThreadsRandom.Next(1,9)}",
                new AuthenticationHeaderValue("Client-ID", ShimakazeBot.Config.apiKeys.imgurClientId));
            JToken item = null;
            if (response != null && response["data"].HasValues)
            {
                int size = response["data"].Children().Count();
                item = response["data"].Children().ToArray()[ThreadSafeRandom.ThisThreadsRandom.Next(0, size)];
            }
            if (item == null)
            {
                await CTX.RespondSanitizedAsync(ctx, "The meme factory has stopped working 😩");
                return;
            }

            DateTime timestamp = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                .AddSeconds(item["datetime"].Value<long>());
            DiscordEmbedBuilder embed = Utils.BaseEmbedBuilder(ctx, null, item["title"].Value<string>(), null,
                item["id"].Value<string>(), timestamp)
                .WithUrl(item["link"].Value<string>())
                .WithImageUrl(item["link"].Value<string>());
            if (!string.IsNullOrWhiteSpace(item["description"].Value<string>()))
            {
                embed.WithDescription(item["description"].Value<string>());
            }

            await CTX.RespondSanitizedAsync(ctx, null, false, embed);
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

        [Command("urbandictionary")]
        [Aliases("urban")]
        public async Task UrbanDictionary(CommandContext ctx, [RemainingText] string suffix)
        {
            if (string.IsNullOrWhiteSpace(suffix))
            {
                await CTX.RespondSanitizedAsync(ctx, ctx.User.Mention +
                    ", If you actually tell me what word you want to look up that'll be great.");
                return;
            }
            JObject response = await ShimaHttpClient.HttpGet(
                $"http://api.urbandictionary.com/v0/define?term={suffix}");
            JToken item = null;
            if (response != null && response["list"].HasValues)
            {
                var items = response["list"].Children().ToList().OrderByDescending(i => i["thumbs_up"].Value<int>());
                item = items.First();
            }
            if (item == null)
            {
                await CTX.RespondSanitizedAsync(ctx, "I burnt all the dictionaries because they were too slow 🔥🔥🔥");
                return;
            }
            DiscordEmbedBuilder embed = Utils.BaseEmbedBuilder(ctx, null, item["word"].Value<string>(), null,
                $"{item["thumbs_up"].Value<string>()}👍 - {item["thumbs_down"].Value<string>()}👎",
                item["written_on"].Value<DateTime>())
                .WithUrl(item["permalink"].Value<string>());

            if (item["definition"].Value<string>().Length > 2048)
            {
                embed.WithDescription(item["definition"].Value<string>().Substring(0, 2042) + " [...]");
            }
            else
            {
                embed.WithDescription(item["definition"].Value<string>());
            }
            if (!string.IsNullOrWhiteSpace(item["example"].Value<string>()))
            {
                if (item["example"].Value<string>().Length > 1024)
                {
                    embed.AddField("Example", item["example"].Value<string>().Substring(0, 1018) + " [...]");
                }
                else
                {
                    embed.AddField("Example", item["example"].Value<string>());
                }
            }

            await CTX.RespondSanitizedAsync(ctx, null, false, embed);
        }

        [Command("xkcd")]
        public async Task XKCD(CommandContext ctx, [RemainingText] string suffix)
        {
            JObject latestComic = await ShimaHttpClient.HttpGet("https://xkcd.com/info.0.json");

            if (latestComic == null)
            {
                await CTX.RespondSanitizedAsync(ctx, "The comic store is closed today ☹️");
                return;
            }
            int latestN = latestComic["num"].Value<int>();
            
            int searchN;
            if (string.IsNullOrWhiteSpace(suffix))
            {
                searchN = ThreadSafeRandom.ThisThreadsRandom.Next(0, latestN + 1);
            }
            else if (!int.TryParse(suffix, out searchN) || searchN > latestN)
            {
                await CTX.RespondSanitizedAsync(ctx, $"**{suffix}** is not a valid number between 0 and {latestN}.");
                return;
            }

            JObject response = await ShimaHttpClient.HttpGet($"https://xkcd.com/{searchN}/info.0.json");
            DateTime timestamp = new DateTime(
                response["year"].Value<int>(),
                response["month"].Value<int>(),
                response["day"].Value<int>(), 0, 0, 0, DateTimeKind.Utc);

            await CTX.RespondSanitizedAsync(ctx, null, false,
                Utils.BaseEmbedBuilder(ctx, null, response["title"]?.Value<string>(), null, $"{searchN}", timestamp)
                   .WithUrl($"https://xkcd.com/{searchN}")
                   .WithImageUrl(response["img"]?.Value<string>())
                   .WithDescription(response["alt"]?.Value<string>()));
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

            await CTX.RespondSanitizedAsync(ctx, null, false,
                Utils.BaseEmbedBuilder(ctx, null as DiscordUser, response["insult"]?.Value<string>())
                   .WithTimestamp(null)
                   .WithImageUrl(FunConsts.FancyInsultImage));
        }

        [Command("mememaker")]
        [Aliases("imgflip", "imgflipper")]
        public async Task ImgFlipper(CommandContext ctx, [RemainingText] string unusedSuffix)
        {
            await CTX.RespondSanitizedAsync(ctx, "Just use the website, it's so much easier than trying to type the " +
                "exact meme format you want\nhttps://imgflip.com/memegenerator");
        }
    }
}
