using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace kairos.Components.Kanban.Extensions;

public static class DataTransferExtensions
{
    public static async Task SetDataAsync(this DataTransfer dataTransfer, IJSRuntime jsRuntime, string format, string data)
    {
        await jsRuntime.InvokeVoidAsync("kanbanDragDrop.setData", format, data);
    }

    public static async Task<string> GetDataAsync(this DataTransfer dataTransfer, IJSRuntime jsRuntime, string format)
    {
        return await jsRuntime.InvokeAsync<string>("kanbanDragDrop.getData", format);
    }

    public static void SetData(this DataTransfer dataTransfer, IJSRuntime jsRuntime, string format, string data)
    {
        _ = Task.Run(async () => await jsRuntime.InvokeVoidAsync("kanbanDragDrop.setData", format, data));
    }

    public static string GetData(this DataTransfer dataTransfer, IJSRuntime jsRuntime, string format)
    {
        try
        {
            return jsRuntime.InvokeAsync<string>("kanbanDragDrop.getData", format).GetAwaiter().GetResult();
        }
        catch
        {
            return string.Empty;
        }
    }
}