using System.ComponentModel.DataAnnotations;

namespace kairos.Components.Kanban.Models;

public class KanbanData
{
    public List<Board> Boards { get; set; } = new();
    
    public DateTime LastModified { get; set; } = DateTime.UtcNow;
    
    public string Context { get; set; } = string.Empty;

    public bool IsValid()
    {
        // Validar que todos os quadros são válidos
        if (Boards.Any(board => !board.IsValid()))
            return false;

        // Validar que todos os cartões são válidos
        foreach (var board in Boards)
        {
            if (board.Cards.Any(card => !card.IsValid()))
                return false;
        }

        // Validar que as ordens dos quadros são sequenciais
        var boardOrders = Boards.Select(b => b.Order).OrderBy(o => o).ToList();
        for (int i = 0; i < boardOrders.Count; i++)
        {
            if (boardOrders[i] != i)
                return false;
        }

        // Validar que as ordens dos cartões dentro de cada quadro são sequenciais
        foreach (var board in Boards)
        {
            var cardOrders = board.Cards.Select(c => c.Order).OrderBy(o => o).ToList();
            for (int i = 0; i < cardOrders.Count; i++)
            {
                if (cardOrders[i] != i)
                    return false;
            }
        }

        return true;
    }

    public void UpdateLastModified()
    {
        LastModified = DateTime.UtcNow;
    }

    public Board? GetBoardById(string boardId)
    {
        return Boards.FirstOrDefault(b => b.Id == boardId);
    }

    public Card? GetCardById(string cardId)
    {
        foreach (var board in Boards)
        {
            var card = board.Cards.FirstOrDefault(c => c.Id == cardId);
            if (card != null)
                return card;
        }
        return null;
    }

    public void NormalizeOrders()
    {
        // Normalizar ordens dos quadros
        var sortedBoards = Boards.OrderBy(b => b.Order).ToList();
        for (int i = 0; i < sortedBoards.Count; i++)
        {
            sortedBoards[i].Order = i;
        }

        // Normalizar ordens dos cartões dentro de cada quadro
        foreach (var board in Boards)
        {
            var sortedCards = board.Cards.OrderBy(c => c.Order).ToList();
            for (int i = 0; i < sortedCards.Count; i++)
            {
                sortedCards[i].Order = i;
            }
        }

        UpdateLastModified();
    }
}