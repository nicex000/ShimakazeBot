using System.ComponentModel.DataAnnotations;

public class StreamingGuild
{
    [Key]
    public ulong GuildId { get; set; }
    public ulong RoleId { get; set; }
}