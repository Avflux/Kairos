namespace kairos.Components.Kanban.Models;

public class DragDropData
{
    public string CardId { get; set; } = string.Empty;
    public string SourceBoardId { get; set; } = string.Empty;
    public int SourceOrder { get; set; }
    public string DragType { get; set; } = "card"; // "card" or "board"
    
    public DragDropData() { }
    
    public DragDropData(Card card)
    {
        CardId = card.Id;
        SourceBoardId = card.BoardId;
        SourceOrder = card.Order;
        DragType = "card";
    }
}

public static class DragDropConstants
{
    public const string CardDataType = "application/kanban-card";
    public const string BoardDataType = "application/kanban-board";
    public const string TextDataType = "text/plain";
}