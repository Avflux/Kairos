using kairos.Components.Kanban.Models;

namespace kairos.Components.Kanban.Services;

public interface IKanbanService
{
    /// <summary>
    /// Carrega os dados do Kanban para um contexto específico
    /// </summary>
    /// <param name="context">Contexto da página (civil, eletromecanica, spcs)</param>
    /// <returns>Dados do Kanban para o contexto especificado</returns>
    Task<KanbanData> LoadDataAsync(string context);
    
    /// <summary>
    /// Salva os dados do Kanban para um contexto específico
    /// </summary>
    /// <param name="data">Dados do Kanban a serem salvos</param>
    /// <returns>Task representando a operação assíncrona</returns>
    Task SaveDataAsync(KanbanData data);
    
    /// <summary>
    /// Cria um novo quadro
    /// </summary>
    /// <param name="context">Contexto da página</param>
    /// <param name="title">Título do quadro</param>
    /// <returns>O quadro criado</returns>
    Task<Board> CreateBoardAsync(string context, string title);
    
    /// <summary>
    /// Remove um quadro existente
    /// </summary>
    /// <param name="context">Contexto da página</param>
    /// <param name="boardId">ID do quadro a ser removido</param>
    /// <returns>Task representando a operação assíncrona</returns>
    Task DeleteBoardAsync(string context, string boardId);
    
    /// <summary>
    /// Cria um novo cartão em um quadro específico
    /// </summary>
    /// <param name="context">Contexto da página</param>
    /// <param name="boardId">ID do quadro onde o cartão será criado</param>
    /// <param name="title">Título do cartão</param>
    /// <returns>O cartão criado</returns>
    Task<Card> CreateCardAsync(string context, string boardId, string title);
    
    /// <summary>
    /// Remove um cartão existente
    /// </summary>
    /// <param name="context">Contexto da página</param>
    /// <param name="cardId">ID do cartão a ser removido</param>
    /// <returns>Task representando a operação assíncrona</returns>
    Task DeleteCardAsync(string context, string cardId);
    
    /// <summary>
    /// Move um cartão para outro quadro ou posição
    /// </summary>
    /// <param name="context">Contexto da página</param>
    /// <param name="cardId">ID do cartão a ser movido</param>
    /// <param name="targetBoardId">ID do quadro de destino</param>
    /// <param name="newOrder">Nova posição do cartão no quadro de destino</param>
    /// <returns>Task representando a operação assíncrona</returns>
    Task MoveCardAsync(string context, string cardId, string targetBoardId, int newOrder);
    
    /// <summary>
    /// Move um quadro para uma nova posição
    /// </summary>
    /// <param name="context">Contexto da página</param>
    /// <param name="boardId">ID do quadro a ser movido</param>
    /// <param name="newOrder">Nova posição do quadro</param>
    /// <returns>Task representando a operação assíncrona</returns>
    Task MoveBoardAsync(string context, string boardId, int newOrder);
    
    /// <summary>
    /// Atualiza o título de um quadro
    /// </summary>
    /// <param name="context">Contexto da página</param>
    /// <param name="boardId">ID do quadro</param>
    /// <param name="newTitle">Novo título do quadro</param>
    /// <returns>Task representando a operação assíncrona</returns>
    Task UpdateBoardTitleAsync(string context, string boardId, string newTitle);
    
    /// <summary>
    /// Atualiza o título de um cartão
    /// </summary>
    /// <param name="context">Contexto da página</param>
    /// <param name="cardId">ID do cartão</param>
    /// <param name="newTitle">Novo título do cartão</param>
    /// <returns>Task representando a operação assíncrona</returns>
    Task UpdateCardTitleAsync(string context, string cardId, string newTitle);
    
    /// <summary>
    /// Atualiza a descrição de um cartão
    /// </summary>
    /// <param name="context">Contexto da página</param>
    /// <param name="cardId">ID do cartão</param>
    /// <param name="newDescription">Nova descrição do cartão</param>
    /// <returns>Task representando a operação assíncrona</returns>
    Task UpdateCardDescriptionAsync(string context, string cardId, string newDescription);
}