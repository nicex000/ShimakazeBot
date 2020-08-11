using DSharpPlus.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Shimakaze
{
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
            if (levelList[0].guildId == 0) return levelList[0];

            return levelList.FirstOrDefault(item => item.guildId == guildId);
        }

        public static int GetUserLevel(List<UserLevels> levelList, ulong guildId)
        {
            var levelItem = GetUserLevelItem(levelList, guildId);

            return levelItem != null ? levelItem.level : ShimakazeBot.DefaultLevel;
        }

        public static int GetLevel(ulong userId, ulong guildId)
        {
            if (ShimakazeBot.IsUserShimaTeam(userId)) return 999;

            if (ShimakazeBot.UserLevelList.ContainsKey(userId))
            {
                return GetUserLevel(ShimakazeBot.UserLevelList[userId].levelList, guildId);
            }

            return ShimakazeBot.DefaultLevel;
        }

        public static int GetMemberLevel(DiscordMember member)
        {
            if (ShimakazeBot.IsUserShimaTeam(member.Id)) return 999;


            int level = ShimakazeBot.DefaultLevel;
            int lv;
            member.Roles.ToList().ForEach(role => {
                lv = GetLevel(role.Id, member.Guild.Id);
                if (lv > ShimakazeBot.DefaultLevel && lv > level) level = lv;
            });

            lv = GetLevel(member.Id, member.Guild.Id);

            if (lv < ShimakazeBot.DefaultLevel || lv > level)
                level = lv;

            return level;
        }

        public static bool SetLevel(ulong userId, ulong guildId, bool isRole, int level)
        {
            if (ShimakazeBot.IsUserShimaTeam(userId)) return false;

            if (ShimakazeBot.UserLevelList.ContainsKey(userId))
            {
                var levelItem = GetUserLevelItem(ShimakazeBot.UserLevelList[userId].levelList, guildId);

                if (levelItem != null)
                {
                    if (level == levelItem.level) return false;

                    if (level == ShimakazeBot.DefaultLevel)
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
                        ShimakazeBot.DbCtx.SaveChanges();

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
                        ShimakazeBot.DbCtx.SaveChanges();
                    }
                }
                else
                {
                    //add
                    ShimakazeBot.DbCtx.UserPermissionLevel.Add(new UserPermissionLevel
                    { 
                        UserId = userId, 
                        IsRole = isRole, 
                        GuildId = guildId, 
                        Level = level
                    });

                    ShimakazeBot.DbCtx.SaveChanges();
                    int key = ShimakazeBot.DbCtx.UserPermissionLevel.ToList().Find(g =>
                        g.UserId == userId && g.GuildId == guildId).Id;

                    if (guildId == 0)
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
                if (level == ShimakazeBot.DefaultLevel) return false;
                //add and add
                ShimakazeBot.DbCtx.UserPermissionLevel.Add(new UserPermissionLevel
                {
                    UserId = userId,
                    IsRole = isRole,
                    GuildId = guildId,
                    Level = level
                });

                ShimakazeBot.DbCtx.SaveChanges();
                int key = ShimakazeBot.DbCtx.UserPermissionLevel.ToList().Find(g =>
                    g.UserId == userId && g.GuildId == guildId).Id;

                ShimakazeBot.UserLevelList.Add(userId,
                    new LevelListContainer(isRole,
                        new List<UserLevels>() { new UserLevels(key, guildId, level) }));
            }

            return true;
        }
    }

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
}
