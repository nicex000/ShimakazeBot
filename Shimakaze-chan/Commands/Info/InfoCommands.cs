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
            await ctx.RespondAsync("This bot serves as a temporary hotfix compliment to the original Shimakaze's broken voicechat functions." +
                " We will notify You when we're done with rewriting mainline Shimakaze and bring all of her functionality back up. At least " +
                "the parts that were actively used, we will leave out some useless shit like cleverbot.");
        }

        [Command("prefix")]
        [Description("Displays the current prefix, if you\'re that confused.")]
        public async Task DisplayPrefix(CommandContext ctx)
        {
            await ctx.RespondAsync("This server\'s prefix is: **" +
                             (ShimakazeBot.CustomPrefixes.ContainsKey(ctx.Guild.Id) ? ShimakazeBot.CustomPrefixes[ctx.Guild.Id] : ShimakazeBot.DefaultPrefix) +
                             "**\n You can change the prefix with **cprefix**");
        }
    }
}
