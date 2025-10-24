using kairos.Components.Kanban.Services;
using Microsoft.Extensions.DependencyInjection;

namespace kairos.Components.Kanban.Extensions;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registra todos os serviços necessários para o sistema Kanban
    /// </summary>
    /// <param name="services">Coleção de serviços</param>
    /// <returns>Coleção de serviços para encadeamento</returns>
    public static IServiceCollection AddKanbanServices(this IServiceCollection services)
    {
        // Registrar serviços principais
        services.AddScoped<ILocalStorageService, LocalStorageService>();
        services.AddScoped<IKanbanService, KanbanService>();
        services.AddScoped<IKanbanBackupService, KanbanBackupService>();
        services.AddScoped<KanbanDataMigrationService>();
        
        // Registrar serviços de performance e feedback
        services.AddScoped<IPerformanceService, PerformanceService>();
        services.AddScoped<IUserFeedbackService, UserFeedbackService>();
        
        // Registrar serviços de tratamento de erros e recuperação
        // Temporariamente comentado para evitar dependências circulares
        // services.AddScoped<IErrorHandlingService, ErrorHandlingService>();
        // services.AddScoped<IDataRecoveryService, DataRecoveryService>();

        return services;
    }
}