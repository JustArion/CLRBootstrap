Allows the CLR to safely initialize on DllMain as `Init` on a new thread

Example:

```cs
using Dawn.AOT;

internal static class EntryPoint
{   
    internal static LoaderInformation _loaderInfo;
    
    [UnmanagedCallersOnly(EntryPoint = nameof(Init))]
    public static unsafe void Init(LoaderInformation* loaderInfo)
    {
        _loaderInfo = *loaderInfo;
    }
}
```

Build from Source Requirements:
- cl
- dotnet 9 sdk