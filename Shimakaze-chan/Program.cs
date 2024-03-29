using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Lavalink;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shimakaze.Logger;

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
            ShimakazeBot.Client.Logger.Log(
                LogLevel.Information,
                LogSources.COMMAND_EXECUTION_EVENT,
                $"Executing {ctx.Message.Content} from {ctx.User.Username} in {ctx.Guild?.Name ?? "DM"}");
            return Task.CompletedTask;
        }
    }


    class Program
    {
        static void Main(string[] args) => MainAsync(args).ConfigureAwait(false).GetAwaiter().GetResult();

        static async Task MainAsync(string[] args)
        {
            ShimakazeBot.Config = ShimaConfig.LoadConfig();
            if (ShimakazeBot.Config.Equals(null) ||
                (ShimakazeBot.Config.settings.liveToken == null &&
                ShimakazeBot.Config.settings.testToken == null))
            {
                throw new System.ArgumentNullException("", "Config ain't set up properly (:");
            }

            ShimaLoggerFactory loggerFactory = new ShimaLoggerFactory();
            loggerFactory.AddProvider(new ShimaLoggerProvider());

            ShimakazeBot.Client = new DiscordClient(new DiscordConfiguration
            {
                Token = ShimakazeBot.Config.settings.isTest ?
                        ShimakazeBot.Config.settings.testToken :
                        ShimakazeBot.Config.settings.liveToken,
                TokenType = TokenType.Bot,
                MinimumLogLevel = LogLevel.Information,
                LoggerFactory = loggerFactory,
                Intents = DiscordIntents.All
            });
            
            ShimakazeBot.DbCtx = new ShimaContext();

            await ShimakazeBot.DbCtx.Database.MigrateAsync();

            ShimakazeBot.FetchPrefixes();
            ShimakazeBot.FetchStreamingRoles();
            ShimakazeBot.FetchSelfAssignLimits();
            ShimakazeBot.FetchPermissionLevels();

            CommandsNextConfiguration commandConfig = new CommandsNextConfiguration
            {
                PrefixResolver = (msg) =>
                {
                    return Task.Run(() =>
                    {
                        var guild = msg.Channel.Guild;
                        if (guild is null)
                        {
                            return msg.GetStringPrefixLength(ShimakazeBot.DefaultPrefix);
                        }
                        return msg.GetStringPrefixLength(ShimakazeBot.CustomPrefixes.ContainsKey(guild.Id)
                            ? ShimakazeBot.CustomPrefixes[guild.Id]
                            : ShimakazeBot.DefaultPrefix);
                    });
                }
            };

            CommandsNextExtension commandsNextExtension = ShimakazeBot.Client.UseCommandsNext(commandConfig);
            commandsNextExtension.RegisterCommands<DebugCommands>();
            commandsNextExtension.RegisterCommands<InfoCommands>();
            commandsNextExtension.RegisterCommands<GeneralCommands>();
            commandsNextExtension.RegisterCommands<FunCommands>();
            commandsNextExtension.RegisterCommands<VoiceCommands>();
            commandsNextExtension.RegisterCommands<CustomizationCommands>();
            commandsNextExtension.RegisterCommands<ManagementCommands>();
            commandsNextExtension.RegisterCommands<EventCommands>();

            ShimakazeBot.Client.UseLavalink();
           
            ShimakazeBot.events.LoadEvents();

            await ShimakazeBot.Client.ConnectAsync();
            await Task.Delay(-1);
        }
    }
}
