namespace Dawn.AOT;

using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

[StructLayout(LayoutKind.Sequential)]
[SuppressMessage("ReSharper", "UnusedType.Global")]
public readonly struct BootstrapInformation
{
    public readonly nint HINSTANCE;
    public readonly ulong MainThreadId;
    public readonly int MainThreadPriority;
}