using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Lavalink;
using Microsoft.EntityFrameworkCore;

namespace Shimakaze
{
    //lets use singletons because haHAA
    //but first the struct
    // Visco: Useless since we can use lavalink internal stuff
    // nice lie ^

    public class Commands : BaseCommandModule
    {
        public override Task BeforeExecutionAsync(CommandContext ctx)
        {
            ShimakazeBot.Client.DebugLogger.LogMessage(
                LogLevel.Info,
                LogMessageSources.COMMAND_EXECUTION_EVENT,
                $"Executing {ctx.Message.Content} from {ctx.User.Username} in {ctx.Guild.Name}",
                ctx.Message.Timestamp.DateTime);
            return Task.CompletedTask;
        }
    }


    class Program
    {
        static void Main(string[] args) => MainAsync(args).ConfigureAwait(false).GetAwaiter().GetResult();

        static async Task MainAsync(string[] args)
        {
            ShimakazeBot.Client = new DiscordClient(new DiscordConfiguration
            {
                // Token = "NDc2MTUxMjIwMDA0OTc4Njg5.DnXuqg.ANWX8zmMBLU5U7XLI9ZA-8E0nRQ", //test
                Token = "NjQyNDc4MDIzOTQ5NjgwNjYx.XtpHoA.P6U_GOWwkYOBML1lUCM5whbTN9s", //voice
                TokenType = TokenType.Bot,
                UseInternalLogHandler = true,
                LogLevel = LogLevel.Info
            });
            
            ShimakazeBot.DbCtx = new ShimaContext();

            ShimakazeBot.DbCtx.Database.Migrate();

            ShimakazeBot.FetchPrefixes();
            ShimakazeBot.FetchStreamingRoles();

            CommandsNextConfiguration commandConfig = new CommandsNextConfiguration
            {
                PrefixResolver = (msg) =>
                {
                    return Task.Run(() =>
                    {
                        var guild = msg.Channel.Guild;
                        return msg.GetStringPrefixLength(ShimakazeBot.CustomPrefixes.ContainsKey(guild.Id)
                            ? ShimakazeBot.CustomPrefixes[guild.Id]
                            : ShimakazeBot.DefaultPrefix);
                    });
                }
            };

            CommandsNextExtension commandsNextExtension = ShimakazeBot.Client.UseCommandsNext(commandConfig);
            commandsNextExtension.RegisterCommands<DebugCommands>();
            commandsNextExtension.RegisterCommands<InfoCommands>();
            commandsNextExtension.RegisterCommands<VoiceCommands>();
            commandsNextExtension.RegisterCommands<CustomizationCommands>();

            ShimakazeBot.Client.UseLavalink();

            Events events = new Events();
            events.LoadEvents();

            await ShimakazeBot.Client.ConnectAsync();
            await Task.Delay(-1);
        }
    }
}
