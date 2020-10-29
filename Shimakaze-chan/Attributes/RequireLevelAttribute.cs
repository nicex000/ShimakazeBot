using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using Shimakaze;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Shimakaze.Attributes
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    class RequireLevelAttribute : CheckBaseAttribute
    {
        public int level;
        public string failMessage;

        public RequireLevelAttribute(int level = 1, string failMessage = "")
        {
            this.level = level;
            this.failMessage = failMessage;
        }

        public override async Task<bool> ExecuteCheckAsync(CommandContext ctx, bool help)
        {
            var app = ctx.Client.CurrentApplication;
            if (app == null)
            {
                return false;
            }

            if (app.Owners.Any(x => x.Id == ctx.User.Id))
            {
                return true;
            }

            var userLevel = ctx.Member != null ? 
                UserLevels.GetMemberLevel(ctx.Member) : 
                UserLevels.GetLevel(ctx.User.Id, ctx.Guild.Id);

            if (ctx.Member == ctx.Guild.Owner && userLevel < (int)ShimaConsts.UserPermissionLevel.DEFAULT_SERVER_OWNER)
            {
                userLevel = (int)ShimaConsts.UserPermissionLevel.DEFAULT_SERVER_OWNER;
            }

            if (userLevel < level)
            {
                if (!string.IsNullOrWhiteSpace(failMessage))
                {
                    await CTX.RespondSanitizedAsync(ctx, failMessage);
                }
                return false;
            }
            return true;
        }
    }
}
