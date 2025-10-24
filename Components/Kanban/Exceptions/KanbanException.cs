namespace kairos.Components.Kanban.Exceptions;

public class KanbanException : Exception
{
    public KanbanException(string message) : base(message)
    {
    }
    
    public KanbanException(string message, Exception innerException) : base(message, innerException)
    {
    }
}

public class BoardNotFoundException : KanbanException
{
    public string BoardId { get; }
    
    public BoardNotFoundException(string boardId) 
        : base($"Quadro com ID '{boardId}' não foi encontrado")
    {
        BoardId = boardId;
    }
}

public class CardNotFoundException : KanbanException
{
    public string CardId { get; }
    
    public CardNotFoundException(string cardId) 
        : base($"Cartão com ID '{cardId}' não foi encontrado")
    {
        CardId = cardId;
    }
}

public class InvalidOperationKanbanException : KanbanException
{
    public InvalidOperationKanbanException(string message) : base(message)
    {
    }
}

public class ValidationKanbanException : KanbanException
{
    public List<string> ValidationErrors { get; }
    
    public ValidationKanbanException(string message, List<string> validationErrors) 
        : base(message)
    {
        ValidationErrors = validationErrors;
    }
    
    public ValidationKanbanException(List<string> validationErrors) 
        : base("Erro de validação nos dados do Kanban")
    {
        ValidationErrors = validationErrors;
    }
}