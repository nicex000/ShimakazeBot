using System.ComponentModel.DataAnnotations;

public class GuildPrefix
{
    [Key]
    public ulong GuildId { get; set; }
    [MaxLength(16)]
    public string Prefix { get; set; }
}