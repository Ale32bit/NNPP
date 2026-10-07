using System.Text.Json;
using Microsoft.JSInterop;

namespace NNPP;

public class PersistentStorage(IJSRuntime js)
{
    public ValueTask SetAsync<T>(string key, T value)
    {
        return js.InvokeVoidAsync("localStorage.setItem", key, JsonSerializer.Serialize(value));
    }
    
    public async ValueTask<T?> GetAsync<T>(string key)
    {
        var json = await js.InvokeAsync<string>("localStorage.getItem", key);
        return json is null ? default : JsonSerializer.Deserialize<T>(json);
    }
    
    public ValueTask RemoveAsync(string key)
    {
        return js.InvokeVoidAsync("localStorage.removeItem", key);
    }
}