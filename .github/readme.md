Allows the CLR to safely initialize on DllMain as `Init` on a new thread

Example:

```cs
internal static class EntryPoint
{
    [StructLayout(LayoutKind.Sequential)]
    public readonly struct LoaderInformation
    {
        public readonly HINSTANCE Module;
        public readonly ulong MainThreadId;
        public readonly int MainThreadPriority;
    }
    
    internal static LoaderInformation _loaderInfo;
    
    [UnmanagedCallersOnly(EntryPoint = nameof(Init))]
    public static unsafe void Init(LoaderInformation* loaderInfo)
    {
        _loaderInfo = *loaderInfo;
    }
}
```