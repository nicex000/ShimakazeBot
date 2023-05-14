using DSharpPlus.SlashCommands;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Shimakaze.Attributes
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    class RequireLevelAttribute : SlashCheckBaseAttribute
    {
        public int level;
        public string failMessage;

        public RequireLevelAttribute(int level = 1, string failMessage = "")
        {
            this.level = level;
            this.failMessage = failMessage;
        }

        public override async Task<bool> ExecuteChecksAsync(InteractionContext ctx)
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
                    await SCTX.RespondSanitizedAsync(ctx, failMessage);
                }
                return false;
            }
            return true;
        }
    }
}
