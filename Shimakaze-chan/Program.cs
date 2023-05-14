using System.Linq;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using DSharpPlus.Lavalink;
using DSharpPlus.SlashCommands;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shimakaze.Logger;

namespace Shimakaze
{
    //lets use singletons because haHAA
    //but first the struct
    // Visco: Useless since we can use lavalink internal stuff
    // nice lie ^

    public class Commands : ApplicationCommandModule
    {
        public override Task<bool> BeforeSlashExecutionAsync(InteractionContext ctx)
        {
            ShimakazeBot.Client.Logger.Log(
                LogLevel.Information,
                LogSources.SLASH_COMMAND_EXECUTION_EVENT,
                $"Executing {PrintInteractionData(ctx.Interaction.Data)} from {ctx.User.Username} in {ctx.Guild?.Name ?? "DM"}");
            return Task.FromResult(true);
        }

        public string PrintInteractionData(DiscordInteractionData ctxInteractionData)
        {
            string stringData = ctxInteractionData.Name;

            if (ctxInteractionData.Options != null && ctxInteractionData.Options.Any())
            {
                stringData += " [";
                foreach (DiscordInteractionDataOption option in ctxInteractionData.Options)
                {
                    stringData += $"{option.Name}={option.Value} ";
                }
                stringData += "]";
            }
            
            return stringData;
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

            SlashCommandsExtension slashCommandsExtension = ShimakazeBot.Client.UseSlashCommands();
            slashCommandsExtension.RegisterCommands<DebugCommands>();
            // slashCommandsExtension.RegisterCommands<InfoCommands>();
            // slashCommandsExtension.RegisterCommands<GeneralCommands>();
            // slashCommandsExtension.RegisterCommands<FunCommands>();
            // slashCommandsExtension.RegisterCommands<VoiceCommands>();
            // slashCommandsExtension.RegisterCommands<CustomizationCommands>();
            slashCommandsExtension.RegisterCommands<ManagementCommands>();
            // slashCommandsExtension.RegisterCommands<EventCommands>();

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
           // commandsNextExtension.RegisterCommands<ManagementCommands>();

            ShimakazeBot.Client.UseLavalink();
           
            ShimakazeBot.events.LoadEvents();

            await ShimakazeBot.Client.ConnectAsync();
            await Task.Delay(-1);
        }
    }
}
