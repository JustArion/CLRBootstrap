```cs
    [StructLayout(LayoutKind.Sequential)]
    public readonly struct LoaderInformation
    {
        public readonly HINSTANCE Module;
        public readonly ulong MainThreadId;
        public readonly THREAD_PRIORITY MainThreadPriority;
    }

    [UnmanagedCallersOnly(EntryPoint = nameof(Init))]
    internal static unsafe void Init(LoaderInformation* loaderInfo)
    {
        // Main thread is frozen until the current function (Init) finishes
        Task.Run(()=> { Console.WriteLine("Runtime initialized successfully!") });
    }
```

### Note:
- The `export` "Init" has to be named Init.
- While `LoaderInformation*` is not necessary, it is a good way to get the HINSTANCE