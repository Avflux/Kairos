using kairos.Components.Kanban.Models;

namespace kairos.Components.Kanban.Services;

public interface IKanbanBackupService
{
    /// <summary>
    /// Cria um backup dos dados de um contexto específico
    /// </summary>
    /// <param name="context">Contexto dos dados</param>
    /// <returns>Dados de backup em formato JSON</returns>
    Task<string> CreateBackupAsync(string context);
    
    /// <summary>
    /// Restaura dados de um backup
    /// </summary>
    /// <param name="context">Contexto onde os dados serão restaurados</param>
    /// <param name="backupData">Dados de backup em formato JSON</param>
    /// <returns>Task representando a operação assíncrona</returns>
    Task RestoreBackupAsync(string context, string backupData);
    
    /// <summary>
    /// Exporta todos os dados de todos os contextos
    /// </summary>
    /// <returns>Dados completos em formato JSON</returns>
    Task<string> ExportAllDataAsync();
    
    /// <summary>
    /// Importa dados completos (todos os contextos)
    /// </summary>
    /// <param name="importData">Dados completos em formato JSON</param>
    /// <returns>Task representando a operação assíncrona</returns>
    Task ImportAllDataAsync(string importData);
    
    /// <summary>
    /// Lista todos os contextos disponíveis
    /// </summary>
    /// <returns>Lista de contextos com dados</returns>
    Task<List<string>> GetAvailableContextsAsync();
}