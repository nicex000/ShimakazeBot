using System.ComponentModel.DataAnnotations;

public class ShimaGeneric
{
    [Key]
    [MaxLength(200)]
    public string Key { get; set; }
    public string Value { get; set; }
}