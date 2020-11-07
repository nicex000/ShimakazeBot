using DSharpPlus;
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
    class RequireAdminAttribute : CheckBaseAttribute
    {
        public string failMessage;
        public bool skipDMMessage;

        public RequireAdminAttribute(string failMessage = "Only a server admin can use this command.",
            bool skipDMMessage = false)
        {
            this.failMessage = failMessage;
            this.skipDMMessage = skipDMMessage;
        }

        public override async Task<bool> ExecuteCheckAsync(CommandContext ctx, bool help)
        {
            CannotBeUsedInDMAttribute checkDM =
                skipDMMessage ? new CannotBeUsedInDMAttribute("") : new CannotBeUsedInDMAttribute();
            if (!await checkDM.ExecuteCheckAsync(ctx, help))
            {
                return false;
            }

            var user = ctx.Member;
            if (user == null)
            {
                return false;
            }

            var app = ctx.Client.CurrentApplication;
            if (app == null)
            {
                return false;
            }

            if (help || user.Id == ctx.Guild.Owner.Id || app.Owners.Any(x => x.Id == ctx.User.Id))
            {
                return true;
            }

            var pUser = ctx.Channel.PermissionsFor(user);
            if ((pUser & Permissions.Administrator) != 0)
            {
                return true;
            }

            if (!string.IsNullOrWhiteSpace(failMessage))
            {
                await CTX.RespondSanitizedAsync(ctx, failMessage);
            }
            
            return false;
        }
    }
}
