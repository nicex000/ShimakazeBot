using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.CommandsNext;
using System.Linq;
using System.Threading.Tasks;

namespace Shimakaze
{
    class InfoCommands : Commands
    {
        [Command("info")]
        [Description("Quick tooltip regarding the purpose of this bot.")]
        public async Task DisplayInfo(CommandContext ctx)
        {
            await CTX.RespondSanitizedAsync(ctx, "This bot serves as a temporary hotfix compliment to the original Shimakaze's broken voicechat functions." +
                " We will notify You when we're done with rewriting mainline Shimakaze and bring all of her functionality back up. At least " +
                "the parts that were actively used, we will leave out some useless shit like cleverbot." +
                $"\nRunning ShimaEngine v.{ShimaConsts.Version}");
        }

        [Command("prefix")]
        [Description("Displays the current prefix, if you\'re that confused.")]
        public async Task DisplayPrefix(CommandContext ctx)
        {
            string prefix = ShimakazeBot.CustomPrefixes.ContainsKey(ctx.Guild.Id) ?
                            ShimakazeBot.CustomPrefixes[ctx.Guild.Id] :
                            ShimakazeBot.DefaultPrefix;
            await CTX.RespondSanitizedAsync(ctx, $"This server\'s prefix is: **{prefix}**" +
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
    }
}
