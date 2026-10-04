using System.Runtime.CompilerServices;
using Microsoft.JSInterop;

namespace NNPP;

public class AudioManager(IJSRuntime js) : IAsyncDisposable
{
    public enum AudioBus
    {
        Master,
        Sfx,
        Music
    }

    private readonly Dictionary<string, string> _map = new(StringComparer.OrdinalIgnoreCase);

    private readonly Lazy<Task<IJSObjectReference>> _mod = new(() =>
        js.InvokeAsync<IJSObjectReference>("import", "./audio.js").AsTask());

    public AudioManager Register(string key, string path)
    {
        _map[key] = path;
        return this;
    }

    private string Resolve(string key) =>
        _map.TryGetValue(key, out var path)
            ? path
            : throw new KeyNotFoundException($"Audio key '{key}' not registered. Skill issue.");

    private async ValueTask Call(string fn, params object?[] args) =>
        await (await _mod.Value).InvokeVoidAsync(fn, args);

    public ValueTask PreloadAllAsync() => Call("preload", (object)_map.Values.Distinct().ToArray());

    public ValueTask PreloadAsync(params string[] keys) =>
        Call("preload", (object)keys.Select(Resolve).ToArray());

    public ValueTask PlaySfxAsync(string key, double volume = 1, double pitch = 1, bool loop = false) =>
        Call("sfx", Resolve(key), volume, pitch, loop);

    public ValueTask PlayMusicAsync(string key, double volume = 1, double fadeSeconds = 0, bool loop = true) =>
        Call("playMusic", Resolve(key), volume, fadeSeconds, loop);

    public ValueTask StopMusicAsync(double fadeSeconds = 1) => Call("stopMusic", fadeSeconds);

    public ValueTask SetVolumeAsync(AudioBus bus, double volume) =>
        Call("setVolume", bus.ToString().ToLowerInvariant(), volume);
    
    private static string Norm(string key) => key.ToLowerInvariant();

    public ValueTask PlaySfxLoopAsync(string key, double volume = 1, double pitch = 1, double fadeSeconds = 0) =>
        Call("sfxLoop", Norm(key), Resolve(key), volume, pitch, fadeSeconds);

    public ValueTask StopSfxAsync(string key, double fadeSeconds = 0) =>
        Call("stopSfx", Norm(key), fadeSeconds);

    public ValueTask StopAllSfxAsync(double fadeSeconds = 0) =>
        Call("stopAllSfx", fadeSeconds);

    public async ValueTask<bool> IsSfxLoopingAsync(string key) =>
        await (await _mod.Value).InvokeAsync<bool>("isSfxLooping", Norm(key));

    public async ValueTask DisposeAsync()
    {
        if (_mod.IsValueCreated) await (await _mod.Value).DisposeAsync();
    }
}