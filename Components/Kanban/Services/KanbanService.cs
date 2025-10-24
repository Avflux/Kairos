using kairos.Components.Kanban.Models;
using kairos.Components.Kanban.Exceptions;

namespace kairos.Components.Kanban.Services;

public class KanbanService : IKanbanService
{
    private readonly ILocalStorageService _localStorage;
    private readonly Dictionary<string, KanbanData> _cache = new();
    private const string STORAGE_KEY_PREFIX = "kanban_data_";

    public KanbanService(ILocalStorageService localStorage)
    {
        _localStorage = localStorage;
    }

    public async Task<KanbanData> LoadDataAsync(string context)
    {
        if (string.IsNullOrWhiteSpace(context))
            throw new ArgumentException("Contexto não pode ser vazio", nameof(context));

        try
        {
            // Verificar cache primeiro
            if (_cache.ContainsKey(context))
                return _cache[context];

            // Tentar carregar do LocalStorage
            var storageKey = STORAGE_KEY_PREFIX + context;
            var data = await _localStorage.GetItemAsync<KanbanData>(storageKey);

            if (data == null)
            {
                data = CreateDefaultData(context);
            }

            // Validar e normalizar dados
            if (!data.IsValid())
            {
                data.NormalizeOrders();
            }

            data.Context = context;
            _cache[context] = data;
            return data;
        }
        catch (KanbanException)
        {
            // Re-throw KanbanExceptions (incluindo erros do LocalStorage)
            throw;
        }
        catch (Exception ex)
        {
            throw new KanbanException($"Erro inesperado ao carregar dados do contexto '{context}'", ex);
        }
    }

    public async Task SaveDataAsync(KanbanData data)
    {
        if (data == null)
            throw new ArgumentNullException(nameof(data));

        if (string.IsNullOrWhiteSpace(data.Context))
            throw new ArgumentException("Contexto dos dados não pode ser vazio");

        // Validar dados antes de salvar
        var validationResult = ValidateKanbanData(data);
        if (!validationResult.IsValid)
        {
            throw new ValidationKanbanException(validationResult.Errors);
        }

        try
        {
            data.UpdateLastModified();
            data.NormalizeOrders();

            var storageKey = STORAGE_KEY_PREFIX + data.Context;
            await _localStorage.SetItemAsync(storageKey, data);
            
            // Atualizar cache
            _cache[data.Context] = data;
        }
        catch (KanbanException)
        {
            // Re-throw KanbanExceptions (incluindo erros do LocalStorage)
            throw;
        }
        catch (Exception ex)
        {
            throw new KanbanException($"Erro inesperado ao salvar dados do contexto '{data.Context}'", ex);
        }
    }

    public async Task<Board> CreateBoardAsync(string context, string title)
    {
        if (string.IsNullOrWhiteSpace(context))
            throw new ArgumentException("Contexto não pode ser vazio", nameof(context));

        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Título do quadro não pode ser vazio", nameof(title));

        var data = await LoadDataAsync(context);
        
        var board = new Board
        {
            Title = title.Trim(),
            Order = data.Boards.Count
        };

        // Validar o quadro
        if (!board.IsValid())
            throw new ValidationKanbanException("Dados do quadro são inválidos", new List<string> { "Título inválido ou muito longo" });

        data.Boards.Add(board);
        await SaveDataAsync(data);

        return board;
    }

    public async Task DeleteBoardAsync(string context, string boardId)
    {
        if (string.IsNullOrWhiteSpace(context))
            throw new ArgumentException("Contexto não pode ser vazio", nameof(context));

        if (string.IsNullOrWhiteSpace(boardId))
            throw new ArgumentException("ID do quadro não pode ser vazio", nameof(boardId));

        var data = await LoadDataAsync(context);
        var board = data.GetBoardById(boardId);

        if (board == null)
            throw new BoardNotFoundException(boardId);

        data.Boards.Remove(board);
        data.NormalizeOrders();
        await SaveDataAsync(data);
    }

    public async Task<Card> CreateCardAsync(string context, string boardId, string title)
    {
        if (string.IsNullOrWhiteSpace(context))
            throw new ArgumentException("Contexto não pode ser vazio", nameof(context));

        if (string.IsNullOrWhiteSpace(boardId))
            throw new ArgumentException("ID do quadro não pode ser vazio", nameof(boardId));

        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Título do cartão não pode ser vazio", nameof(title));

        var data = await LoadDataAsync(context);
        var board = data.GetBoardById(boardId);

        if (board == null)
            throw new BoardNotFoundException(boardId);

        var card = new Card
        {
            Title = title.Trim(),
            BoardId = boardId,
            Order = board.Cards.Count
        };

        // Validar o cartão
        if (!card.IsValid())
            throw new ValidationKanbanException("Dados do cartão são inválidos", new List<string> { "Título inválido ou muito longo" });

        board.Cards.Add(card);
        board.UpdateLastModified();
        await SaveDataAsync(data);

        return card;
    }

    public async Task DeleteCardAsync(string context, string cardId)
    {
        if (string.IsNullOrWhiteSpace(context))
            throw new ArgumentException("Contexto não pode ser vazio", nameof(context));

        if (string.IsNullOrWhiteSpace(cardId))
            throw new ArgumentException("ID do cartão não pode ser vazio", nameof(cardId));

        var data = await LoadDataAsync(context);
        var card = data.GetCardById(cardId);

        if (card == null)
            throw new CardNotFoundException(cardId);

        var board = data.GetBoardById(card.BoardId);
        if (board != null)
        {
            board.Cards.Remove(card);
            board.UpdateLastModified();
            data.NormalizeOrders();
        }

        await SaveDataAsync(data);
    }    public
 async Task MoveCardAsync(string context, string cardId, string targetBoardId, int newOrder)
    {
        if (string.IsNullOrWhiteSpace(context))
            throw new ArgumentException("Contexto não pode ser vazio", nameof(context));

        if (string.IsNullOrWhiteSpace(cardId))
            throw new ArgumentException("ID do cartão não pode ser vazio", nameof(cardId));

        if (string.IsNullOrWhiteSpace(targetBoardId))
            throw new ArgumentException("ID do quadro de destino não pode ser vazio", nameof(targetBoardId));

        if (newOrder < 0)
            throw new ArgumentException("Nova ordem deve ser um número positivo", nameof(newOrder));

        var data = await LoadDataAsync(context);
        var card = data.GetCardById(cardId);
        var targetBoard = data.GetBoardById(targetBoardId);

        if (card == null)
            throw new CardNotFoundException(cardId);

        if (targetBoard == null)
            throw new BoardNotFoundException(targetBoardId);

        // Remover cartão do quadro atual
        var currentBoard = data.GetBoardById(card.BoardId);
        if (currentBoard != null)
        {
            currentBoard.Cards.Remove(card);
            currentBoard.UpdateLastModified();
        }

        // Atualizar cartão e adicionar ao quadro de destino
        card.BoardId = targetBoardId;
        card.Order = Math.Min(newOrder, targetBoard.Cards.Count);
        card.UpdateLastModified();

        // Inserir na posição correta
        if (card.Order >= targetBoard.Cards.Count)
        {
            targetBoard.Cards.Add(card);
        }
        else
        {
            targetBoard.Cards.Insert(card.Order, card);
        }

        targetBoard.UpdateLastModified();
        data.NormalizeOrders();
        await SaveDataAsync(data);
    }

    public async Task MoveBoardAsync(string context, string boardId, int newOrder)
    {
        if (string.IsNullOrWhiteSpace(context))
            throw new ArgumentException("Contexto não pode ser vazio", nameof(context));

        if (string.IsNullOrWhiteSpace(boardId))
            throw new ArgumentException("ID do quadro não pode ser vazio", nameof(boardId));

        if (newOrder < 0)
            throw new ArgumentException("Nova ordem deve ser um número positivo", nameof(newOrder));

        var data = await LoadDataAsync(context);
        var board = data.GetBoardById(boardId);

        if (board == null)
            throw new BoardNotFoundException(boardId);

        // Remover quadro da posição atual
        data.Boards.Remove(board);

        // Inserir na nova posição
        var targetOrder = Math.Min(newOrder, data.Boards.Count);
        if (targetOrder >= data.Boards.Count)
        {
            data.Boards.Add(board);
        }
        else
        {
            data.Boards.Insert(targetOrder, board);
        }

        board.UpdateLastModified();
        data.NormalizeOrders();
        await SaveDataAsync(data);
    }

    public async Task UpdateBoardTitleAsync(string context, string boardId, string newTitle)
    {
        if (string.IsNullOrWhiteSpace(context))
            throw new ArgumentException("Contexto não pode ser vazio", nameof(context));

        if (string.IsNullOrWhiteSpace(boardId))
            throw new ArgumentException("ID do quadro não pode ser vazio", nameof(boardId));

        if (string.IsNullOrWhiteSpace(newTitle))
            throw new ArgumentException("Novo título não pode ser vazio", nameof(newTitle));

        var data = await LoadDataAsync(context);
        var board = data.GetBoardById(boardId);

        if (board == null)
            throw new BoardNotFoundException(boardId);

        board.Title = newTitle.Trim();
        board.UpdateLastModified();

        // Validar o quadro após atualização
        if (!board.IsValid())
            throw new ValidationKanbanException("Título do quadro é inválido", new List<string> { "Título muito longo ou vazio" });

        await SaveDataAsync(data);
    }

    public async Task UpdateCardTitleAsync(string context, string cardId, string newTitle)
    {
        if (string.IsNullOrWhiteSpace(context))
            throw new ArgumentException("Contexto não pode ser vazio", nameof(context));

        if (string.IsNullOrWhiteSpace(cardId))
            throw new ArgumentException("ID do cartão não pode ser vazio", nameof(cardId));

        if (string.IsNullOrWhiteSpace(newTitle))
            throw new ArgumentException("Novo título não pode ser vazio", nameof(newTitle));

        var data = await LoadDataAsync(context);
        var card = data.GetCardById(cardId);

        if (card == null)
            throw new CardNotFoundException(cardId);

        card.Title = newTitle.Trim();
        card.UpdateLastModified();

        // Validar o cartão após atualização
        if (!card.IsValid())
            throw new ValidationKanbanException("Título do cartão é inválido", new List<string> { "Título muito longo ou vazio" });

        var board = data.GetBoardById(card.BoardId);
        if (board != null)
        {
            board.UpdateLastModified();
        }

        await SaveDataAsync(data);
    }

    public async Task UpdateCardDescriptionAsync(string context, string cardId, string newDescription)
    {
        if (string.IsNullOrWhiteSpace(context))
            throw new ArgumentException("Contexto não pode ser vazio", nameof(context));

        if (string.IsNullOrWhiteSpace(cardId))
            throw new ArgumentException("ID do cartão não pode ser vazio", nameof(cardId));

        var data = await LoadDataAsync(context);
        var card = data.GetCardById(cardId);

        if (card == null)
            throw new CardNotFoundException(cardId);

        card.Description = newDescription?.Trim() ?? string.Empty;
        card.UpdateLastModified();

        // Validar o cartão após atualização
        if (!card.IsValid())
            throw new ValidationKanbanException("Descrição do cartão é inválida", new List<string> { "Descrição muito longa" });

        var board = data.GetBoardById(card.BoardId);
        if (board != null)
        {
            board.UpdateLastModified();
        }

        await SaveDataAsync(data);
    }

    private static KanbanData CreateDefaultData(string context)
    {
        return new KanbanData
        {
            Context = context,
            Boards = new List<Board>
            {
                new Board { Title = "A Fazer", Order = 0 },
                new Board { Title = "Em Progresso", Order = 1 },
                new Board { Title = "Concluído", Order = 2 }
            }
        };
    }

    private static ValidationResult ValidateKanbanData(KanbanData data)
    {
        var result = new ValidationResult { IsValid = true };

        if (data == null)
        {
            result.AddError("Dados do Kanban não podem ser nulos");
            return result;
        }

        if (string.IsNullOrWhiteSpace(data.Context))
        {
            result.AddError("Contexto dos dados não pode ser vazio");
        }

        // Validar quadros
        foreach (var board in data.Boards)
        {
            if (!board.IsValid())
            {
                result.AddError($"Quadro '{board.Title}' contém dados inválidos");
            }

            // Validar cartões do quadro
            foreach (var card in board.Cards)
            {
                if (!card.IsValid())
                {
                    result.AddError($"Cartão '{card.Title}' no quadro '{board.Title}' contém dados inválidos");
                }

                if (card.BoardId != board.Id)
                {
                    result.AddError($"Cartão '{card.Title}' tem referência incorreta ao quadro");
                }
            }
        }

        // Validar IDs únicos dos quadros
        var boardIds = data.Boards.Select(b => b.Id).ToList();
        if (boardIds.Count != boardIds.Distinct().Count())
        {
            result.AddError("Existem quadros com IDs duplicados");
        }

        // Validar IDs únicos dos cartões
        var allCards = data.Boards.SelectMany(b => b.Cards).ToList();
        var cardIds = allCards.Select(c => c.Id).ToList();
        if (cardIds.Count != cardIds.Distinct().Count())
        {
            result.AddError("Existem cartões com IDs duplicados");
        }

        return result;
    }
}