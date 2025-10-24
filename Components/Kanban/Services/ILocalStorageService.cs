namespace kairos.Components.Kanban.Services;

public interface ILocalStorageService
{
    /// <summary>
    /// Salva dados no LocalStorage
    /// </summary>
    /// <param name="key">Chave para armazenamento</param>
    /// <param name="data">Dados a serem salvos</param>
    /// <returns>Task representando a operação assíncrona</returns>
    Task SetItemAsync<T>(string key, T data);
    
    /// <summary>
    /// Carrega dados do LocalStorage
    /// </summary>
    /// <param name="key">Chave do item a ser carregado</param>
    /// <returns>Dados carregados ou default(T) se não encontrado</returns>
    Task<T?> GetItemAsync<T>(string key);
    
    /// <summary>
    /// Remove um item do LocalStorage
    /// </summary>
    /// <param name="key">Chave do item a ser removido</param>
    /// <returns>Task representando a operação assíncrona</returns>
    Task RemoveItemAsync(string key);
    
    /// <summary>
    /// Verifica se uma chave existe no LocalStorage
    /// </summary>
    /// <param name="key">Chave a ser verificada</param>
    /// <returns>True se a chave existe, false caso contrário</returns>
    Task<bool> ContainsKeyAsync(string key);
    
    /// <summary>
    /// Limpa todos os dados do LocalStorage
    /// </summary>
    /// <returns>Task representando a operação assíncrona</returns>
    Task ClearAsync();
    
    /// <summary>
    /// Obtém todas as chaves do LocalStorage
    /// </summary>
    /// <returns>Lista de chaves disponíveis</returns>
    Task<List<string>> GetKeysAsync();
}