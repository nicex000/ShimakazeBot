using DSharpPlus.Entities;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Shimakaze
{
    public struct LevelListContainer
    {
        public bool isRole;
        public List<UserLevels> levelList;

        public LevelListContainer(bool isRole, List<UserLevels> levelList)
        {
            this.isRole = isRole;
            this.levelList = levelList;
        }
    }
    public class UserLevels
    {
        public int keyId;
        public ulong guildId;
        public int level;

        public UserLevels(int key, ulong guild, int level)
        {
            this.keyId = key;
            this.guildId = guild;
            this.level = level;
        }

        public static UserLevels GetUserLevelItem(List<UserLevels> levelList, ulong guildId)
        {
            if (levelList[0].guildId == ShimaConsts.GlobalLevelGuild)
            {
                return levelList[0];
            }

            return levelList.FirstOrDefault(item => item.guildId == guildId);
        }

        public static int GetUserLevel(List<UserLevels> levelList, ulong guildId)
        {
            var levelItem = GetUserLevelItem(levelList, guildId);

            return levelItem != null ? levelItem.level : (int)ShimaConsts.UserPermissionLevel.DEFAULT;
        }

        public static int GetLevel(ulong userId, ulong guildId)
        {
            if (ShimakazeBot.IsUserShimaTeam(userId))
            {
                return (int)ShimaConsts.UserPermissionLevel.SHIMA_TEAM;
            }

            if (ShimakazeBot.UserLevelList.ContainsKey(userId))
            {
                return GetUserLevel(ShimakazeBot.UserLevelList[userId].levelList, guildId);
            }

            return (int)ShimaConsts.UserPermissionLevel.DEFAULT;
        }

        public static int GetMemberLevel(DiscordMember member)
        {
            if (ShimakazeBot.IsUserShimaTeam(member.Id))
            {
                return (int)ShimaConsts.UserPermissionLevel.SHIMA_TEAM;
            }

            int level = (int)ShimaConsts.UserPermissionLevel.DEFAULT;
            int currentLevel;
            member.Roles.ToList().ForEach(role => {
                currentLevel = GetLevel(role.Id, member.Guild.Id);
                if (currentLevel > (int)ShimaConsts.UserPermissionLevel.DEFAULT && currentLevel > level)
                {
                    level = currentLevel;
                }
            });

            currentLevel = GetLevel(member.Id, member.Guild.Id);

            if (currentLevel < (int)ShimaConsts.UserPermissionLevel.DEFAULT || currentLevel > level)
            {
                level = currentLevel;
            }
            return level;
        }

        public static async Task<bool> SetLevel(ulong userId, ulong guildId, bool isRole, int level)
        {
            if (ShimakazeBot.IsUserShimaTeam(userId))
            {
                return false;
            }

            if (ShimakazeBot.UserLevelList.ContainsKey(userId))
            {
                var levelItem = GetUserLevelItem(ShimakazeBot.UserLevelList[userId].levelList, guildId);

                if (levelItem != null)
                {
                    if (level == levelItem.level)
                    {
                        return false;
                    }

                    if (level == (int)ShimaConsts.UserPermissionLevel.DEFAULT)
                    {
                        //remove
                        if (ShimakazeBot.UserLevelList[userId].levelList.Count > 1)
                        {
                            var deleteIndex = ShimakazeBot.UserLevelList[userId].levelList.FindIndex(del => 
                                del.guildId == guildId
                            );
                            if (deleteIndex > -1)
                                ShimakazeBot.UserLevelList[userId].levelList.RemoveAt(deleteIndex);
                        }
                        else
                        {
                            ShimakazeBot.UserLevelList.Remove(userId);
                        }
                        ShimakazeBot.DbCtx.UserPermissionLevel.RemoveRange(
                            ShimakazeBot.DbCtx.UserPermissionLevel.Where(g => g.Id == levelItem.keyId));
                        await ShimakazeBot.DbCtx.SaveChangesAsync();

                    }
                    else
                    {
                        //change
                        ShimakazeBot.UserLevelList[userId].levelList.Find(c => 
                            c.guildId == guildId
                        ).level = level;

                        var userPerm = ShimakazeBot.DbCtx.UserPermissionLevel.First(g =>
                            g.Id == levelItem.keyId
                        );
                        userPerm.Level = level;
                        ShimakazeBot.DbCtx.UserPermissionLevel.Update(userPerm);
                        await ShimakazeBot.DbCtx.SaveChangesAsync();
                    }
                }
                else
                {
                    //add
                    await ShimakazeBot.DbCtx.UserPermissionLevel.AddAsync(new UserPermissionLevel
                    { 
                        UserId = userId, 
                        IsRole = isRole, 
                        GuildId = guildId, 
                        Level = level
                    });

                    await ShimakazeBot.DbCtx.SaveChangesAsync();
                    int key = ShimakazeBot.DbCtx.UserPermissionLevel.ToList().Find(g =>
                        g.UserId == userId && g.GuildId == guildId).Id;

                    if (guildId == ShimaConsts.GlobalLevelGuild)
                    {
                        ShimakazeBot.UserLevelList[userId].levelList.Insert(0, new UserLevels(key, guildId, level));
                    }
                    else
                    {
                        ShimakazeBot.UserLevelList[userId].levelList.Add(new UserLevels(key, guildId, level));
                    }
                }

            }
            else
            {
                if (level == (int)ShimaConsts.UserPermissionLevel.DEFAULT)
                {
                    return false;
                }
                //add and add
                await ShimakazeBot.DbCtx.UserPermissionLevel.AddAsync(new UserPermissionLevel
                {
                    UserId = userId,
                    IsRole = isRole,
                    GuildId = guildId,
                    Level = level
                });

                await ShimakazeBot.DbCtx.SaveChangesAsync();
                int key = ShimakazeBot.DbCtx.UserPermissionLevel.ToList().Find(g =>
                    g.UserId == userId && g.GuildId == guildId).Id;

                ShimakazeBot.UserLevelList.Add(userId,
                    new LevelListContainer(isRole,
                        new List<UserLevels>() { new UserLevels(key, guildId, level) }));
            }

            return true;
        }
    }
}
