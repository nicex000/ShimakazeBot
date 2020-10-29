using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shimakaze.Attributes
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    class RequireShimaTeamAttribute : CheckBaseAttribute
    {
        public string failMessage;

        public RequireShimaTeamAttribute(string failMessage = "Only Shima Staff can use this command.")
        {
            this.failMessage = failMessage;
        }

        public override async Task<bool> ExecuteCheckAsync(CommandContext ctx, bool help)
        {
            var app = ctx.Client.CurrentApplication;
            if (app != null)
            {
                bool success = app.Owners.Any(x => x.Id == ctx.User.Id);
                if (!success && !string.IsNullOrWhiteSpace(failMessage))
                {
                    await CTX.RespondSanitizedAsync(ctx, failMessage);
                }

                return success;
            }

            var me = ctx.Client.CurrentUser;
            return ctx.User.Id == me.Id;
        }
    }
}
