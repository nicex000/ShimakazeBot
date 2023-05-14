using DSharpPlus.SlashCommands;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Shimakaze.Attributes
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    class RequireShimaTeamAttribute : SlashCheckBaseAttribute
    {
        public string failMessage;

        public RequireShimaTeamAttribute(string failMessage = "Only Shima Staff can use this command.")
        {
            this.failMessage = failMessage;
        }

        public override async Task<bool> ExecuteChecksAsync(InteractionContext ctx)
        {
           var app = ctx.Client.CurrentApplication;
            if (app != null)
            {
                bool success = app.Owners.Any(x => x.Id == ctx.User.Id);
                if (!success && !string.IsNullOrWhiteSpace(failMessage))
                {
                    await SCTX.RespondSanitizedAsync(ctx, failMessage);
                }

                return success;
            }

            var me = ctx.Client.CurrentUser;
            return ctx.User.Id == me.Id;
        }
    }
}
