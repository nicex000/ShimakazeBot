using System.ComponentModel.DataAnnotations;

public class GuildJoin
{
    [Key]
    public ulong GuildId { get; set; }
    public ulong ChannelId { get; set; }
}