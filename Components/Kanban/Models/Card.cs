using System.ComponentModel.DataAnnotations;

namespace kairos.Components.Kanban.Models;

public class Card
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    
    [Required(ErrorMessage = "O título do cartão é obrigatório")]
    [StringLength(200, MinimumLength = 1, ErrorMessage = "O título deve ter entre 1 e 200 caracteres")]
    public string Title { get; set; } = string.Empty;
    
    [StringLength(1000, ErrorMessage = "A descrição não pode exceder 1000 caracteres")]
    public string Description { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "O ID do quadro é obrigatório")]
    public string BoardId { get; set; } = string.Empty;
    
    [Range(0, int.MaxValue, ErrorMessage = "A ordem deve ser um número positivo")]
    public int Order { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime LastModified { get; set; } = DateTime.UtcNow;

    public bool IsValid()
    {
        return !string.IsNullOrWhiteSpace(Title) && 
               Title.Length <= 200 && 
               !string.IsNullOrWhiteSpace(BoardId) && 
               Order >= 0;
    }

    public void UpdateLastModified()
    {
        LastModified = DateTime.UtcNow;
    }
}