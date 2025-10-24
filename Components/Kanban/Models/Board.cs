using System.ComponentModel.DataAnnotations;

namespace kairos.Components.Kanban.Models;

public class Board
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    
    [Required(ErrorMessage = "O título do quadro é obrigatório")]
    [StringLength(100, MinimumLength = 1, ErrorMessage = "O título deve ter entre 1 e 100 caracteres")]
    public string Title { get; set; } = string.Empty;
    
    [Range(0, int.MaxValue, ErrorMessage = "A ordem deve ser um número positivo")]
    public int Order { get; set; }
    
    public List<Card> Cards { get; set; } = new();
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime LastModified { get; set; } = DateTime.UtcNow;

    public bool IsValid()
    {
        return !string.IsNullOrWhiteSpace(Title) && 
               Title.Length <= 100 && 
               Order >= 0;
    }

    public void UpdateLastModified()
    {
        LastModified = DateTime.UtcNow;
    }
}