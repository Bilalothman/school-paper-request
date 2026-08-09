using System.ComponentModel.DataAnnotations;

namespace Backend.Models;

public class PaperService
{
    public int Id { get; set; }
    [MaxLength(100)] public required string Name { get; set; }
    [MaxLength(500)] public required string Description { get; set; }
    public ICollection<PaperRequest> Requests { get; set; } = [];
}
