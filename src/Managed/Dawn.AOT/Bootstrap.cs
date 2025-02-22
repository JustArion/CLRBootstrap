﻿namespace Dawn.AOT;

using System.Runtime.InteropServices;

[StructLayout(LayoutKind.Sequential)]
public readonly struct LoaderInformation
{
    public readonly HINSTANCE Module;
    public readonly uint MainThreadId;
    public readonly THREAD_PRIORITY MainThreadPriority;

    public static unsafe LoaderInformation FromPointer(nint pointer) 
        => Unsafe.AsRef<LoaderInformation>(pointer.ToPointer());
}