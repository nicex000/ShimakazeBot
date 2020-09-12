using System.ComponentModel.DataAnnotations;

public class GuildSelfAssign
{
    [Key]
    public ulong GuildId { get; set; }
    public ulong RoleId { get; set; }
}