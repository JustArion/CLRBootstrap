Allows the CLR to safely initialize on DllMain as `Init` on a new thread

Example:

```cs
using Dawn.AOT;

internal static class EntryPoint
{   
    internal static BootstrapInformation _loaderInfo;
    
    [UnmanagedCallersOnly(EntryPoint = nameof(Init))]
    public static unsafe void Init(BootstrapInformation* loaderInfo)
    {
        _loaderInfo = *loaderInfo;
    }
}
```