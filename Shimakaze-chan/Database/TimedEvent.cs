using Shimakaze;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public class TimedEvent
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }
    public EventType Type { get; set; }
    public DateTime EventTime { get; set; }
    public string Message { get; set; }
    public ulong ChannelId { get; set; }
    public ulong UserId { get; set; }
}