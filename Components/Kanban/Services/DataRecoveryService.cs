using kairos.Components.Kanban.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace kairos.Components.Kanban.Services
{
    public interface IDataRecoveryService
    {
        Task<KanbanData> RecoverDataAsync(string context);
        Task<bool> CreateEmergencyBackupAsync(KanbanData data, string context);
        Task<List<KanbanData>> GetAvailableBackupsAsync(string context);
        Task<KanbanData> RestoreFromBackupAsync(string context, DateTime backupDate);
        Task<KanbanData> ValidateAndRepairDataAsync(KanbanData data);
        Task<bool> CanRecoverDataAsync(string context);
        Task CleanupOldBackupsAsync(string context, int keepDays = 30);
    }

    public class DataRecoveryService : IDataRecoveryService
    {
        private readonly ILocalStorageService _localStorage;
        private readonly IKanbanBackupService _backupService;
        private readonly ILogger<DataRecoveryService> _logger;
        private readonly IUserFeedbackService _feedbackService;

        public DataRecoveryService(
            ILocalStorageService localStorage,
            IKanbanBackupService backupService,
            ILogger<DataRecoveryService> logger,
            IUserFeedbackService feedbackService)
        {
            _localStorage = localStorage;
            _backupService = backupService;
            _logger = logger;
            _feedbackService = feedbackService;
        }

        public async Task<KanbanData> RecoverDataAsync(string context)
        {
            _logger.LogInformation("Iniciando recuperação de dados para contexto: {Context}", context);
            
            try
            {
                await _feedbackService.ShowLoadingAsync("Recuperando dados...", "data-recovery");

                // Step 1: Try to load from primary storage
                try
                {
                    var primaryData = await _localStorage.GetItemAsync<KanbanData>($"kanban_data_{context}");
                    if (primaryData != null)
                    {
                        var validatedData = await ValidateAndRepairDataAsync(primaryData);
                        if (validatedData != null)
                        {
                            _logger.LogInformation("Dados recuperados do armazenamento primário");
                            await _feedbackService.HideLoadingAsync("data-recovery");
                            await _feedbackService.ShowSuccessAsync("Dados recuperados com sucesso!");
                            return validatedData;
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Falha ao carregar do armazenamento primário");
                }

                // Step 2: Try to recover from backup
                try
                {
                    var backups = await GetAvailableBackupsAsync(context);
                    if (backups.Any())
                    {
                        _logger.LogInformation("Tentando recuperar do backup mais recente");
                        await _feedbackService.ShowInfoAsync("Recuperando do backup...");
                        
                        var latestBackup = backups.OrderByDescending(b => b.LastModified).First();
                        var validatedBackup = await ValidateAndRepairDataAsync(latestBackup);
                        
                        if (validatedBackup != null)
                        {
                            // Restore to primary storage
                            await _localStorage.SetItemAsync($"kanban_data_{context}", validatedBackup);
                            
                            _logger.LogInformation("Dados recuperados do backup");
                            await _feedbackService.HideLoadingAsync("data-recovery");
                            await _feedbackService.ShowSuccessAsync("Dados recuperados do backup!");
                            return validatedBackup;
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Falha ao recuperar do backup");
                }

                // Step 3: Create default data as last resort
                _logger.LogWarning("Criando dados padrão como último recurso");
                await _feedbackService.ShowWarningAsync("Criando dados padrão...");
                
                var defaultData = CreateDefaultData(context);
                await _localStorage.SetItemAsync($"kanban_data_{context}", defaultData);
                
                // Create initial backup
                await CreateEmergencyBackupAsync(defaultData, context);
                
                await _feedbackService.HideLoadingAsync("data-recovery");
                await _feedbackService.ShowInfoAsync("Dados padrão criados. Você pode começar a usar o sistema.");
                
                return defaultData;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro crítico na recuperação de dados");
                await _feedbackService.HideLoadingAsync("data-recovery");
                await _feedbackService.ShowErrorAsync("Erro crítico na recuperação de dados.");
                throw;
            }
        }

        public async Task<bool> CreateEmergencyBackupAsync(KanbanData data, string context)
        {
            try
            {
                _logger.LogInformation("Criando backup de emergência para contexto: {Context}", context);
                
                var backupKey = $"emergency_backup_{context}_{DateTime.UtcNow:yyyyMMdd_HHmmss}";
                await _localStorage.SetItemAsync(backupKey, data);
                
                // Also use the regular backup service
                await _backupService.CreateBackupAsync(context);
                
                _logger.LogInformation("Backup de emergência criado com sucesso");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao criar backup de emergência");
                return false;
            }
        }

        public async Task<List<KanbanData>> GetAvailableBackupsAsync(string context)
        {
            var backups = new List<KanbanData>();
            
            try
            {
                // Get regular backups - using available contexts as proxy
                var contexts = await _backupService.GetAvailableContextsAsync();
                // Note: This is a simplified approach since GetBackupsAsync doesn't exist

                // Get emergency backups
                var emergencyBackups = await GetEmergencyBackupsAsync(context);
                backups.AddRange(emergencyBackups);

                _logger.LogInformation("Encontrados {Count} backups para contexto {Context}", backups.Count, context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao obter backups disponíveis");
            }

            return backups.OrderByDescending(b => b.LastModified).ToList();
        }

        public async Task<KanbanData> RestoreFromBackupAsync(string context, DateTime backupDate)
        {
            try
            {
                _logger.LogInformation("Restaurando backup de {BackupDate} para contexto {Context}", backupDate, context);
                
                await _feedbackService.ShowLoadingAsync("Restaurando backup...", "restore-backup");

                var backups = await GetAvailableBackupsAsync(context);
                var targetBackup = backups.FirstOrDefault(b => 
                    Math.Abs((b.LastModified - backupDate).TotalMinutes) < 1);

                if (targetBackup == null)
                {
                    throw new InvalidOperationException($"Backup não encontrado para a data {backupDate}");
                }

                var validatedData = await ValidateAndRepairDataAsync(targetBackup);
                if (validatedData == null)
                {
                    throw new InvalidOperationException("Backup está corrompido e não pode ser restaurado");
                }

                // Create backup of current data before restoring
                var currentData = await _localStorage.GetItemAsync<KanbanData>($"kanban_data_{context}");
                if (currentData != null)
                {
                    await CreateEmergencyBackupAsync(currentData, $"{context}_pre_restore");
                }

                // Restore the backup
                await _localStorage.SetItemAsync($"kanban_data_{context}", validatedData);

                _logger.LogInformation("Backup restaurado com sucesso");
                await _feedbackService.HideLoadingAsync("restore-backup");
                await _feedbackService.ShowSuccessAsync("Backup restaurado com sucesso!");

                return validatedData;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao restaurar backup");
                await _feedbackService.HideLoadingAsync("restore-backup");
                await _feedbackService.ShowErrorAsync($"Erro ao restaurar backup: {ex.Message}");
                throw;
            }
        }

        public async Task<KanbanData> ValidateAndRepairDataAsync(KanbanData data)
        {
            if (data == null)
            {
                _logger.LogWarning("Dados são nulos, não é possível validar");
                return null;
            }

            try
            {
                _logger.LogInformation("Validando e reparando dados para contexto: {Context}", data.Context);

                var repairedData = new KanbanData
                {
                    Context = data.Context ?? "unknown",
                    LastModified = data.LastModified == default ? DateTime.UtcNow : data.LastModified,
                    Boards = new List<Board>()
                };

                // Validate and repair boards
                if (data.Boards != null)
                {
                    var validBoards = new List<Board>();
                    var boardOrder = 0;

                    foreach (var board in data.Boards)
                    {
                        if (board != null && !string.IsNullOrWhiteSpace(board.Title))
                        {
                            var repairedBoard = new Board
                            {
                                Id = string.IsNullOrWhiteSpace(board.Id) ? Guid.NewGuid().ToString() : board.Id,
                                Title = board.Title.Trim(),
                                Order = boardOrder++,
                                CreatedAt = board.CreatedAt == default ? DateTime.UtcNow : board.CreatedAt,
                                LastModified = board.LastModified == default ? DateTime.UtcNow : board.LastModified,
                                Cards = new List<Card>()
                            };

                            // Validate and repair cards
                            if (board.Cards != null)
                            {
                                var validCards = new List<Card>();
                                var cardOrder = 0;

                                foreach (var card in board.Cards)
                                {
                                    if (card != null && !string.IsNullOrWhiteSpace(card.Title))
                                    {
                                        var repairedCard = new Card
                                        {
                                            Id = string.IsNullOrWhiteSpace(card.Id) ? Guid.NewGuid().ToString() : card.Id,
                                            Title = card.Title.Trim(),
                                            Description = card.Description?.Trim() ?? string.Empty,
                                            BoardId = repairedBoard.Id,
                                            Order = cardOrder++,
                                            CreatedAt = card.CreatedAt == default ? DateTime.UtcNow : card.CreatedAt,
                                            LastModified = card.LastModified == default ? DateTime.UtcNow : card.LastModified
                                        };

                                        validCards.Add(repairedCard);
                                    }
                                }

                                repairedBoard.Cards = validCards;
                            }

                            validBoards.Add(repairedBoard);
                        }
                    }

                    repairedData.Boards = validBoards;
                }

                // Ensure at least one board exists
                if (!repairedData.Boards.Any())
                {
                    _logger.LogWarning("Nenhum quadro válido encontrado, criando quadros padrão");
                    repairedData.Boards.AddRange(CreateDefaultBoards());
                }

                _logger.LogInformation("Dados validados e reparados: {BoardCount} quadros, {CardCount} cartões", 
                    repairedData.Boards.Count, repairedData.Boards.Sum(b => b.Cards.Count));

                return repairedData;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao validar e reparar dados");
                return null;
            }
        }

        public async Task<bool> CanRecoverDataAsync(string context)
        {
            try
            {
                // Check if primary storage has data
                var primaryData = await _localStorage.GetItemAsync<KanbanData>($"kanban_data_{context}");
                if (primaryData != null)
                {
                    return true;
                }

                // Check if backups are available
                var backups = await GetAvailableBackupsAsync(context);
                return backups.Any();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao verificar se dados podem ser recuperados");
                return false;
            }
        }

        public async Task CleanupOldBackupsAsync(string context, int keepDays = 30)
        {
            try
            {
                _logger.LogInformation("Limpando backups antigos para contexto: {Context}, mantendo {KeepDays} dias", context, keepDays);

                var cutoffDate = DateTime.UtcNow.AddDays(-keepDays);
                
                // Clean up emergency backups
                var allKeys = await _localStorage.GetKeysAsync();
                var emergencyBackupKeys = allKeys.Where(k => k.StartsWith($"emergency_backup_{context}_")).ToList();

                foreach (var key in emergencyBackupKeys)
                {
                    try
                    {
                        // Extract date from key format: emergency_backup_{context}_{yyyyMMdd_HHmmss}
                        var datePart = key.Split('_').LastOrDefault();
                        if (DateTime.TryParseExact(datePart, "yyyyMMdd_HHmmss", null, 
                            System.Globalization.DateTimeStyles.None, out var backupDate))
                        {
                            if (backupDate < cutoffDate)
                            {
                                await _localStorage.RemoveItemAsync(key);
                                _logger.LogInformation("Backup de emergência removido: {Key}", key);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Erro ao processar chave de backup: {Key}", key);
                    }
                }

                // Note: CleanupOldBackupsAsync doesn't exist in current interface
                // This would need to be implemented in the backup service

                _logger.LogInformation("Limpeza de backups concluída");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro durante limpeza de backups");
            }
        }

        private async Task<List<KanbanData>> GetEmergencyBackupsAsync(string context)
        {
            var backups = new List<KanbanData>();

            try
            {
                var allKeys = await _localStorage.GetKeysAsync();
                var emergencyBackupKeys = allKeys.Where(k => k.StartsWith($"emergency_backup_{context}_")).ToList();

                foreach (var key in emergencyBackupKeys)
                {
                    try
                    {
                        var backup = await _localStorage.GetItemAsync<KanbanData>(key);
                        if (backup != null)
                        {
                            backups.Add(backup);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Erro ao carregar backup de emergência: {Key}", key);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao obter backups de emergência");
            }

            return backups;
        }

        private KanbanData CreateDefaultData(string context)
        {
            return new KanbanData
            {
                Context = context,
                LastModified = DateTime.UtcNow,
                Boards = CreateDefaultBoards()
            };
        }

        private List<Board> CreateDefaultBoards()
        {
            return new List<Board>
            {
                new Board
                {
                    Id = Guid.NewGuid().ToString(),
                    Title = "A Fazer",
                    Order = 0,
                    CreatedAt = DateTime.UtcNow,
                    LastModified = DateTime.UtcNow,
                    Cards = new List<Card>()
                },
                new Board
                {
                    Id = Guid.NewGuid().ToString(),
                    Title = "Em Progresso",
                    Order = 1,
                    CreatedAt = DateTime.UtcNow,
                    LastModified = DateTime.UtcNow,
                    Cards = new List<Card>()
                },
                new Board
                {
                    Id = Guid.NewGuid().ToString(),
                    Title = "Concluído",
                    Order = 2,
                    CreatedAt = DateTime.UtcNow,
                    LastModified = DateTime.UtcNow,
                    Cards = new List<Card>()
                }
            };
        }
    }
}