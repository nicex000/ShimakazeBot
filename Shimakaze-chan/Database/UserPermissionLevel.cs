using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public class UserPermissionLevel
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }
    public ulong UserId { get; set; }
    public bool IsRole { get; set; }
    public ulong GuildId { get; set; }
    public int Level { get; set; }
}