using kairos.Components.Kanban.Models;
using kairos.Components.Kanban.Exceptions;

namespace kairos.Components.Kanban.Services;

public class KanbanDataMigrationService
{
    private readonly IKanbanService _kanbanService;
    private readonly ILocalStorageService _localStorage;

    public KanbanDataMigrationService(IKanbanService kanbanService, ILocalStorageService localStorage)
    {
        _kanbanService = kanbanService;
        _localStorage = localStorage;
    }

    /// <summary>
    /// Inicializa dados padrão para um contexto se não existirem
    /// </summary>
    /// <param name="context">Contexto a ser inicializado</param>
    /// <param name="customBoards">Quadros personalizados (opcional)</param>
    /// <returns>Task representando a operação assíncrona</returns>
    public async Task InitializeContextAsync(string context, List<string>? customBoards = null)
    {
        if (string.IsNullOrWhiteSpace(context))
            throw new ArgumentException("Contexto não pode ser vazio", nameof(context));

        try
        {
            var storageKey = $"kanban_data_{context}";
            var hasData = await _localStorage.ContainsKeyAsync(storageKey);

            if (!hasData)
            {
                var boardTitles = customBoards ?? GetDefaultBoardTitles(context);
                var data = new KanbanData
                {
                    Context = context,
                    Boards = boardTitles.Select((title, index) => new Board
                    {
                        Title = title,
                        Order = index
                    }).ToList()
                };

                await _kanbanService.SaveDataAsync(data);
            }
        }
        catch (Exception ex)
        {
            throw new KanbanException($"Erro ao inicializar contexto '{context}'", ex);
        }
    }

    /// <summary>
    /// Migra dados antigos para o novo formato (se necessário)
    /// </summary>
    /// <param name="context">Contexto a ser migrado</param>
    /// <returns>True se migração foi necessária, false caso contrário</returns>
    public async Task<bool> MigrateDataAsync(string context)
    {
        if (string.IsNullOrWhiteSpace(context))
            throw new ArgumentException("Contexto não pode ser vazio", nameof(context));

        try
        {
            var data = await _kanbanService.LoadDataAsync(context);
            
            bool needsMigration = false;

            // Verificar se precisa normalizar ordens
            if (!data.IsValid())
            {
                data.NormalizeOrders();
                needsMigration = true;
            }

            // Verificar se cartões têm BoardId correto
            foreach (var board in data.Boards)
            {
                foreach (var card in board.Cards)
                {
                    if (card.BoardId != board.Id)
                    {
                        card.BoardId = board.Id;
                        card.UpdateLastModified();
                        needsMigration = true;
                    }
                }
            }

            // Verificar se há timestamps ausentes
            foreach (var board in data.Boards)
            {
                if (board.CreatedAt == default)
                {
                    board.CreatedAt = DateTime.UtcNow.AddDays(-1); // Data padrão no passado
                    needsMigration = true;
                }

                foreach (var card in board.Cards)
                {
                    if (card.CreatedAt == default)
                    {
                        card.CreatedAt = DateTime.UtcNow.AddDays(-1);
                        needsMigration = true;
                    }
                }
            }

            if (needsMigration)
            {
                await _kanbanService.SaveDataAsync(data);
            }

            return needsMigration;
        }
        catch (Exception ex)
        {
            throw new KanbanException($"Erro ao migrar dados do contexto '{context}'", ex);
        }
    }

    /// <summary>
    /// Limpa dados corrompidos e reinicializa com dados padrão
    /// </summary>
    /// <param name="context">Contexto a ser limpo</param>
    /// <returns>Task representando a operação assíncrona</returns>
    public async Task CleanAndReinitializeAsync(string context)
    {
        if (string.IsNullOrWhiteSpace(context))
            throw new ArgumentException("Contexto não pode ser vazio", nameof(context));

        try
        {
            var storageKey = $"kanban_data_{context}";
            await _localStorage.RemoveItemAsync(storageKey);
            await InitializeContextAsync(context);
        }
        catch (Exception ex)
        {
            throw new KanbanException($"Erro ao limpar e reinicializar contexto '{context}'", ex);
        }
    }

    private static List<string> GetDefaultBoardTitles(string context)
    {
        return context.ToLowerInvariant() switch
        {
            "civil" => new List<string> { "Planejamento", "Em Execução", "Revisão", "Concluído" },
            "eletromecanica" => new List<string> { "Análise", "Desenvolvimento", "Testes", "Implementado" },
            "spcs" => new List<string> { "Backlog", "Em Progresso", "Validação", "Finalizado" },
            _ => new List<string> { "A Fazer", "Em Progresso", "Concluído" }
        };
    }
}