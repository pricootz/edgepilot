using System.IO.Pipes;
using System.Security.Cryptography;
using System.Text;

namespace EdgePilot.Platform;

/// <summary>One instance per user. A second launch opens the existing settings window.</summary>
internal sealed class SingleInstance : IDisposable
{
    private readonly Mutex _mutex;
    private readonly CancellationTokenSource _stop = new();
    private readonly string _name;
    private readonly bool _ownsMutex;
    private static readonly object Gate = new();
    private static Action? _activate;
    private static bool _pending;

    public SingleInstance()
    {
        var user = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        _name = "EdgePilot-" + Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(user)))[..20];
        _mutex = new Mutex(false, _name);
        try { _ownsMutex = _mutex.WaitOne(0); }
        catch (AbandonedMutexException) { _ownsMutex = true; }
        if (_ownsMutex) _ = ListenAsync(_stop.Token);
    }

    public bool IsPrimary => _ownsMutex;

    public static void Bind(Action activate)
    {
        bool pending;
        lock (Gate) { _activate = activate; pending = _pending; _pending = false; }
        if (pending) activate();
    }

    private static void Activate()
    {
        Action? callback;
        lock (Gate) { callback = _activate; if (callback is null) _pending = true; }
        callback?.Invoke();
    }

    public bool NotifyPrimary()
    {
        for (var attempt = 0; attempt < 15; attempt++)
        {
            try
            {
                using var pipe = new NamedPipeClientStream(".", _name, PipeDirection.Out, PipeOptions.CurrentUserOnly);
                pipe.Connect(300);
                using var writer = new StreamWriter(pipe) { AutoFlush = true };
                writer.WriteLine("settings");
                return true;
            }
            catch (Exception ex) when (ex is IOException or TimeoutException)
            { Thread.Sleep(100); }
        }
        return false;
    }

    private async Task ListenAsync(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            try
            {
                using var pipe = new NamedPipeServerStream(_name, PipeDirection.In, 1,
                    PipeTransmissionMode.Byte, PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly);
                await pipe.WaitForConnectionAsync(token).ConfigureAwait(false);
                using var reader = new StreamReader(pipe);
                if (await reader.ReadLineAsync(token).ConfigureAwait(false) == "settings") Activate();
            }
            catch (OperationCanceledException) { break; }
            catch (IOException ex)
            {
                System.Diagnostics.Trace.WriteLine(ex);
                try { await Task.Delay(250, token).ConfigureAwait(false); }
                catch (OperationCanceledException) { break; }
            }
        }
    }

    public void Dispose()
    {
        _stop.Cancel();
        if (_ownsMutex) _mutex.ReleaseMutex();
        _mutex.Dispose();
        _stop.Dispose();
    }
}
