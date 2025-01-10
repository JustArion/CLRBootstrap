namespace Dawn.AOT.CoreLib.X86.Threading;

using System.Collections.Concurrent;
using System.Runtime.InteropServices.ComTypes;

public sealed class Dispatcher : IDisposable
{
    public static Dispatcher MainThread { get; } = new(GetMainThreadId());
    
    private readonly SafeHHOOK _hook;
    private readonly ConcurrentQueue<Action> _taskQueue = new();
    private readonly uint _threadId;
    public Dispatcher(uint threadId)
    {
        _threadId = threadId;
        _hook = SetWindowsHookEx(HookType.WH_GETMESSAGE, HookProc, HINSTANCE.NULL, (int)threadId);
        
        if (_hook.IsInvalid)
            throw GetLastError().GetException()!;
    }

    private const uint WM_DISPATCH = WM_USER + 0x10;
    
    private unsafe nint HookProc(int nCode, nint wParam, nint lParam)
    {       
        if (nCode < 0 || (HC)nCode != HC.HC_ACTION)
            return CallNextHookEx(_hook, nCode, wParam, lParam);

        try
        {
            var msg = Unsafe.AsRef<MSG>(lParam.ToPointer());
            if (msg.message != WM_DISPATCH)
                return CallNextHookEx(_hook, nCode, wParam, lParam);
            
            while (_taskQueue.TryDequeue(out var act)) 
                act();
            
        }
        catch (Exception e)
        {
            Console.Error.WriteLine(e);
        }

        return CallNextHookEx(_hook, nCode, wParam, lParam);
    }
    
    private static uint GetMainThreadId()
    {
        using var hSnapshot = CreateToolhelp32Snapshot(TH32CS.TH32CS_SNAPTHREAD);

        if (hSnapshot.IsInvalid)
        {
            var le = GetLastError().GetException();
            throw le!;
        }

        var currentPid = Environment.ProcessId;
        // We only want our own process' threads
        var procThreads = hSnapshot.EnumThread32().Where(x => x.th32OwnerProcessID == currentPid);

        var earliestCreationTime = new FILETIME { dwLowDateTime = int.MaxValue, dwHighDateTime = int.MaxValue };
        var mainThreadId = 0U;
        
        // We're looking for the thread with the earliest creation time
        // There might be a way to do this without such heavy reliance on win32 apis but my mind is pulling a blank on this rn
        foreach (var thread in procThreads)
        {
            using var hThread = OpenThread(new ACCESS_MASK(ThreadAccess.THREAD_QUERY_INFORMATION), false, thread.th32ThreadID);

            if (hThread.IsNull || 
                !GetThreadTimes(hThread, out var creationTime, out _, out _, out _) || 
                CompareFileTime(creationTime, earliestCreationTime) >= 0) 
                continue;
            
            earliestCreationTime = creationTime;
            mainThreadId = thread.th32ThreadID;
        }

        if (mainThreadId != 0)
            return mainThreadId;
        
        throw GetLastError().GetException()!;
    }

    public void Post(Action act)
    {
        _taskQueue.Enqueue(act);
        PostThreadMessage(_threadId, WM_DISPATCH);
    }

    public async Task PostAsync(Func<Task> func)
    {
        var tcs = new TaskCompletionSource();
        _taskQueue.Enqueue(() =>
        {
            func().ContinueWith(t => 
                    tcs.SetException(t.Exception!), TaskContinuationOptions.OnlyOnFaulted)
                .ContinueWith(_ => 
                    tcs.SetResult(), TaskContinuationOptions.NotOnFaulted);
        });
        PostThreadMessage(_threadId, WM_DISPATCH);

        await tcs.Task;
    }

    ~Dispatcher() => Dispose();

    public void Dispose()
    {
        _hook.Dispose();
        GC.SuppressFinalize(this);
    }
}