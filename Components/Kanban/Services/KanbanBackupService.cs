using kairos.Components.Kanban.Models;
using kairos.Components.Kanban.Exceptions;
using System.Text.Json;

namespace kairos.Components.Kanban.Services;

public class KanbanBackupService : IKanbanBackupService
{
    private readonly ILocalStorageService _localStorage;
    private readonly IKanbanService _kanbanService;
    private const string STORAGE_KEY_PREFIX = "kanban_data_";

    public KanbanBackupService(ILocalStorageService localStorage, IKanbanService kanbanService)
    {
        _localStorage = localStorage;
        _kanbanService = kanbanService;
    }

    public async Task<string> CreateBackupAsync(string context)
    {
        if (string.IsNullOrWhiteSpace(context))
            throw new ArgumentException("Contexto não pode ser vazio", nameof(context));

        try
        {
            var data = await _kanbanService.LoadDataAsync(context);
            var backupData = new
            {
                Context = context,
                Data = data,
                BackupDate = DateTime.UtcNow,
                Version = "1.0"
            };

            return JsonSerializer.Serialize(backupData, new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
        }
        catch (Exception ex)
        {
            throw new KanbanException($"Erro ao criar backup do contexto '{context}'", ex);
        }
    }

    public async Task RestoreBackupAsync(string context, string backupData)
    {
        if (string.IsNullOrWhiteSpace(context))
            throw new ArgumentException("Contexto não pode ser vazio", nameof(context));

        if (string.IsNullOrWhiteSpace(backupData))
            throw new ArgumentException("Dados de backup não podem ser vazios", nameof(backupData));

        try
        {
            var backup = JsonSerializer.Deserialize<BackupContainer>(backupData);
            if (backup?.Data == null)
                throw new KanbanException("Dados de backup são inválidos ou corrompidos");

            // Atualizar contexto dos dados restaurados
            backup.Data.Context = context;
            backup.Data.UpdateLastModified();

            // Validar dados antes de restaurar
            if (!backup.Data.IsValid())
            {
                backup.Data.NormalizeOrders();
            }

            await _kanbanService.SaveDataAsync(backup.Data);
        }
        catch (JsonException ex)
        {
            throw new KanbanException("Erro ao deserializar dados de backup", ex);
        }
        catch (Exception ex)
        {
            throw new KanbanException($"Erro ao restaurar backup no contexto '{context}'", ex);
        }
    }

    public async Task<string> ExportAllDataAsync()
    {
        try
        {
            var contexts = await GetAvailableContextsAsync();
            var allData = new Dictionary<string, KanbanData>();

            foreach (var context in contexts)
            {
                var data = await _kanbanService.LoadDataAsync(context);
                allData[context] = data;
            }

            var exportData = new
            {
                ExportDate = DateTime.UtcNow,
                Version = "1.0",
                Contexts = allData
            };

            return JsonSerializer.Serialize(exportData, new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
        }
        catch (Exception ex)
        {
            throw new KanbanException("Erro ao exportar todos os dados", ex);
        }
    }

    public async Task ImportAllDataAsync(string importData)
    {
        if (string.IsNullOrWhiteSpace(importData))
            throw new ArgumentException("Dados de importação não podem ser vazios", nameof(importData));

        try
        {
            var import = JsonSerializer.Deserialize<ImportContainer>(importData);
            if (import?.Contexts == null)
                throw new KanbanException("Dados de importação são inválidos ou corrompidos");

            foreach (var kvp in import.Contexts)
            {
                var context = kvp.Key;
                var data = kvp.Value;

                // Atualizar contexto e timestamp
                data.Context = context;
                data.UpdateLastModified();

                // Validar e normalizar dados
                if (!data.IsValid())
                {
                    data.NormalizeOrders();
                }

                await _kanbanService.SaveDataAsync(data);
            }
        }
        catch (JsonException ex)
        {
            throw new KanbanException("Erro ao deserializar dados de importação", ex);
        }
        catch (Exception ex)
        {
            throw new KanbanException("Erro ao importar dados", ex);
        }
    }

    public async Task<List<string>> GetAvailableContextsAsync()
    {
        try
        {
            var allKeys = await _localStorage.GetKeysAsync();
            var contexts = allKeys
                .Where(key => key.StartsWith(STORAGE_KEY_PREFIX))
                .Select(key => key.Substring(STORAGE_KEY_PREFIX.Length))
                .Where(context => !string.IsNullOrWhiteSpace(context))
                .ToList();

            return contexts;
        }
        catch (Exception ex)
        {
            throw new KanbanException("Erro ao obter contextos disponíveis", ex);
        }
    }

    private class BackupContainer
    {
        public string Context { get; set; } = string.Empty;
        public KanbanData? Data { get; set; }
        public DateTime BackupDate { get; set; }
        public string Version { get; set; } = string.Empty;
    }

    private class ImportContainer
    {
        public DateTime ExportDate { get; set; }
        public string Version { get; set; } = string.Empty;
        public Dictionary<string, KanbanData>? Contexts { get; set; }
    }
}