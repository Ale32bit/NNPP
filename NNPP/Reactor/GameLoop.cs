using System.Diagnostics;

namespace NNPP.Reactor;

public class GameLoop : IAsyncDisposable
{
    private readonly Action<double> _update;
    private readonly Action _render;

    private readonly TimeSpan _step;
    private readonly PeriodicTimer _timer;

    private readonly CancellationTokenSource _cts = new();
    private Task? _task;

    public GameLoop(int tickRate, Action<double> update, Action render)
    {
        _update = update;
        _render = render;

        _step = TimeSpan.FromSeconds(1.0 / tickRate);
        _timer = new PeriodicTimer(_step);
    }

    public void Start()
    {
        _task = RunAsync(_cts.Token);
    }

    private async Task RunAsync(CancellationToken token)
    {
        var stopwatch = Stopwatch.StartNew();
        var last = stopwatch.Elapsed;
        var acc = TimeSpan.Zero;
        var maxCatchUp = _step * 10;

        try
        {
            while (await _timer.WaitForNextTickAsync(token))
            {
                var now = stopwatch.Elapsed;
                acc += now - last;
                last = now;

                if (acc > maxCatchUp)
                {
                    acc = maxCatchUp;
                }

                while (acc >= _step)
                {
                    _update(_step.TotalSeconds);
                    acc -= _step;
                }

                _render();
            }
        }
        catch (OperationCanceledException)
        {
        }
    }

    public async ValueTask DisposeAsync()
    {
        _cts.Cancel();
        _timer.Dispose();
        if (_task is not null) await _task;
        _cts.Dispose();
    }
}