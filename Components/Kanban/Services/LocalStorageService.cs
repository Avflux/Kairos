using Microsoft.JSInterop;
using System.Text.Json;
using kairos.Components.Kanban.Exceptions;

namespace kairos.Components.Kanban.Services;

public class LocalStorageService : ILocalStorageService
{
    private readonly IJSRuntime _jsRuntime;
    private readonly JsonSerializerOptions _jsonOptions;

    public LocalStorageService(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };
    }

    public async Task SetItemAsync<T>(string key, T data)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Chave não pode ser vazia", nameof(key));

        if (data == null)
            throw new ArgumentNullException(nameof(data));

        try
        {
            var jsonData = JsonSerializer.Serialize(data, _jsonOptions);
            await _jsRuntime.InvokeVoidAsync("localStorage.setItem", key, jsonData);
        }
        catch (JSException ex)
        {
            throw new KanbanException($"Erro ao salvar item '{key}' no LocalStorage", ex);
        }
        catch (JsonException ex)
        {
            throw new KanbanException($"Erro ao serializar dados para a chave '{key}'", ex);
        }
        catch (Exception ex)
        {
            throw new KanbanException($"Erro inesperado ao salvar no LocalStorage: {ex.Message}", ex);
        }
    }

    public async Task<T?> GetItemAsync<T>(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Chave não pode ser vazia", nameof(key));

        try
        {
            var jsonData = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", key);
            
            if (string.IsNullOrEmpty(jsonData))
                return default(T);

            return JsonSerializer.Deserialize<T>(jsonData, _jsonOptions);
        }
        catch (JSException ex)
        {
            throw new KanbanException($"Erro ao carregar item '{key}' do LocalStorage", ex);
        }
        catch (JsonException ex)
        {
            throw new KanbanException($"Erro ao deserializar dados da chave '{key}'", ex);
        }
        catch (Exception ex)
        {
            throw new KanbanException($"Erro inesperado ao carregar do LocalStorage: {ex.Message}", ex);
        }
    }

    public async Task RemoveItemAsync(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Chave não pode ser vazia", nameof(key));

        try
        {
            await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", key);
        }
        catch (JSException ex)
        {
            throw new KanbanException($"Erro ao remover item '{key}' do LocalStorage", ex);
        }
        catch (Exception ex)
        {
            throw new KanbanException($"Erro inesperado ao remover do LocalStorage: {ex.Message}", ex);
        }
    }

    public async Task<bool> ContainsKeyAsync(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Chave não pode ser vazia", nameof(key));

        try
        {
            var value = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", key);
            return !string.IsNullOrEmpty(value);
        }
        catch (JSException ex)
        {
            throw new KanbanException($"Erro ao verificar existência da chave '{key}' no LocalStorage", ex);
        }
        catch (Exception ex)
        {
            throw new KanbanException($"Erro inesperado ao verificar LocalStorage: {ex.Message}", ex);
        }
    }

    public async Task ClearAsync()
    {
        try
        {
            await _jsRuntime.InvokeVoidAsync("localStorage.clear");
        }
        catch (JSException ex)
        {
            throw new KanbanException("Erro ao limpar LocalStorage", ex);
        }
        catch (Exception ex)
        {
            throw new KanbanException($"Erro inesperado ao limpar LocalStorage: {ex.Message}", ex);
        }
    }

    public async Task<List<string>> GetKeysAsync()
    {
        try
        {
            var length = await _jsRuntime.InvokeAsync<int>("eval", "localStorage.length");
            var keys = new List<string>();

            for (int i = 0; i < length; i++)
            {
                var key = await _jsRuntime.InvokeAsync<string>("localStorage.key", i);
                if (!string.IsNullOrEmpty(key))
                {
                    keys.Add(key);
                }
            }

            return keys;
        }
        catch (JSException ex)
        {
            throw new KanbanException("Erro ao obter chaves do LocalStorage", ex);
        }
        catch (Exception ex)
        {
            throw new KanbanException($"Erro inesperado ao obter chaves do LocalStorage: {ex.Message}", ex);
        }
    }
}