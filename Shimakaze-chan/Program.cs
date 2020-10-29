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
                $"Executing {ctx.Message.Content} from {ctx.User.Username} in {ctx.Guild?.Name ?? "DM"}",
                ctx.Message.Timestamp.DateTime);
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

            ShimakazeBot.Client = new DiscordClient(new DiscordConfiguration
            {
                Token = ShimakazeBot.Config.settings.isTest ?
                        ShimakazeBot.Config.settings.testToken :
                        ShimakazeBot.Config.settings.liveToken,
                TokenType = TokenType.Bot,
                UseInternalLogHandler = true,
                LogLevel = LogLevel.Info
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
            commandsNextExtension.RegisterCommands<VoiceCommands>();
            commandsNextExtension.RegisterCommands<CustomizationCommands>();
            commandsNextExtension.RegisterCommands<ManagementCommands>();

            ShimakazeBot.Client.UseLavalink();

            Events events = new Events();
            events.LoadEvents();

            await ShimakazeBot.Client.ConnectAsync();
            await Task.Delay(-1);
        }
    }
}
