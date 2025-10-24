using Microsoft.JSInterop;
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace kairos.Components.Kanban.Services
{
    public interface IUserFeedbackService
    {
        Task ShowSuccessAsync(string message, int durationMs = 3000);
        Task ShowErrorAsync(string message, int durationMs = 5000);
        Task ShowWarningAsync(string message, int durationMs = 4000);
        Task ShowInfoAsync(string message, int durationMs = 3000);
        Task ShowLoadingAsync(string message, string operationId = null);
        Task HideLoadingAsync(string operationId = null);
        Task ShowProgressAsync(string message, int percentage, string operationId = null);
        Task HideProgressAsync(string operationId = null);
        Task ProvideHapticFeedbackAsync(HapticFeedbackType type = HapticFeedbackType.Light);
        Task AnimateElementAsync(string elementId, AnimationType animation);
        Task HighlightElementAsync(string elementId, int durationMs = 2000);
    }

    public enum HapticFeedbackType
    {
        Light,
        Medium,
        Heavy,
        Success,
        Warning,
        Error
    }

    public enum AnimationType
    {
        Bounce,
        Shake,
        Pulse,
        FadeIn,
        FadeOut,
        SlideUp,
        SlideDown,
        SlideLeft,
        SlideRight,
        Zoom,
        Flip
    }

    public class UserFeedbackService : IUserFeedbackService, IDisposable
    {
        private readonly IJSRuntime _jsRuntime;
        private readonly ConcurrentDictionary<string, CancellationTokenSource> _loadingOperations = new();
        private readonly ConcurrentDictionary<string, CancellationTokenSource> _progressOperations = new();
        private bool _disposed = false;

        public UserFeedbackService(IJSRuntime jsRuntime)
        {
            _jsRuntime = jsRuntime;
        }

        public async Task ShowSuccessAsync(string message, int durationMs = 3000)
        {
            try
            {
                await _jsRuntime.InvokeVoidAsync("kanbanFeedback.showNotification", 
                    message, "success", durationMs);
                await ProvideHapticFeedbackAsync(HapticFeedbackType.Success);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao mostrar notificação de sucesso: {ex.Message}");
            }
        }

        public async Task ShowErrorAsync(string message, int durationMs = 5000)
        {
            try
            {
                await _jsRuntime.InvokeVoidAsync("kanbanFeedback.showNotification", 
                    message, "error", durationMs);
                await ProvideHapticFeedbackAsync(HapticFeedbackType.Error);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao mostrar notificação de erro: {ex.Message}");
            }
        }

        public async Task ShowWarningAsync(string message, int durationMs = 4000)
        {
            try
            {
                await _jsRuntime.InvokeVoidAsync("kanbanFeedback.showNotification", 
                    message, "warning", durationMs);
                await ProvideHapticFeedbackAsync(HapticFeedbackType.Warning);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao mostrar notificação de aviso: {ex.Message}");
            }
        }

        public async Task ShowInfoAsync(string message, int durationMs = 3000)
        {
            try
            {
                await _jsRuntime.InvokeVoidAsync("kanbanFeedback.showNotification", 
                    message, "info", durationMs);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao mostrar notificação de informação: {ex.Message}");
            }
        }

        public async Task ShowLoadingAsync(string message, string operationId = null)
        {
            operationId ??= Guid.NewGuid().ToString();
            
            // Cancel any existing loading for this operation
            if (_loadingOperations.TryGetValue(operationId, out var existingCts))
            {
                existingCts.Cancel();
                existingCts.Dispose();
            }

            var cts = new CancellationTokenSource();
            _loadingOperations[operationId] = cts;

            try
            {
                await _jsRuntime.InvokeVoidAsync("kanbanFeedback.showLoading", 
                    message, operationId);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao mostrar loading: {ex.Message}");
            }
        }

        public async Task HideLoadingAsync(string operationId = null)
        {
            try
            {
                if (operationId != null && _loadingOperations.TryRemove(operationId, out var cts))
                {
                    cts.Cancel();
                    cts.Dispose();
                }

                await _jsRuntime.InvokeVoidAsync("kanbanFeedback.hideLoading", operationId);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao esconder loading: {ex.Message}");
            }
        }

        public async Task ShowProgressAsync(string message, int percentage, string operationId = null)
        {
            operationId ??= Guid.NewGuid().ToString();
            
            // Cancel any existing progress for this operation
            if (_progressOperations.TryGetValue(operationId, out var existingCts))
            {
                existingCts.Cancel();
                existingCts.Dispose();
            }

            var cts = new CancellationTokenSource();
            _progressOperations[operationId] = cts;

            try
            {
                percentage = Math.Max(0, Math.Min(100, percentage));
                await _jsRuntime.InvokeVoidAsync("kanbanFeedback.showProgress", 
                    message, percentage, operationId);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao mostrar progresso: {ex.Message}");
            }
        }

        public async Task HideProgressAsync(string operationId = null)
        {
            try
            {
                if (operationId != null && _progressOperations.TryRemove(operationId, out var cts))
                {
                    cts.Cancel();
                    cts.Dispose();
                }

                await _jsRuntime.InvokeVoidAsync("kanbanFeedback.hideProgress", operationId);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao esconder progresso: {ex.Message}");
            }
        }

        public async Task ProvideHapticFeedbackAsync(HapticFeedbackType type = HapticFeedbackType.Light)
        {
            try
            {
                await _jsRuntime.InvokeVoidAsync("kanbanFeedback.hapticFeedback", type.ToString().ToLower());
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao fornecer feedback háptico: {ex.Message}");
            }
        }

        public async Task AnimateElementAsync(string elementId, AnimationType animation)
        {
            try
            {
                await _jsRuntime.InvokeVoidAsync("kanbanFeedback.animateElement", 
                    elementId, animation.ToString().ToLower());
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao animar elemento: {ex.Message}");
            }
        }

        public async Task HighlightElementAsync(string elementId, int durationMs = 2000)
        {
            try
            {
                await _jsRuntime.InvokeVoidAsync("kanbanFeedback.highlightElement", 
                    elementId, durationMs);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao destacar elemento: {ex.Message}");
            }
        }

        public void Dispose()
        {
            if (_disposed) return;

            // Cancel all pending operations
            foreach (var cts in _loadingOperations.Values)
            {
                cts.Cancel();
                cts.Dispose();
            }
            _loadingOperations.Clear();

            foreach (var cts in _progressOperations.Values)
            {
                cts.Cancel();
                cts.Dispose();
            }
            _progressOperations.Clear();

            _disposed = true;
        }
    }
}