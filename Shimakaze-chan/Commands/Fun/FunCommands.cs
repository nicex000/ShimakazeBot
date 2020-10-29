using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
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
    }
}
