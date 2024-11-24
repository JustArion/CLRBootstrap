#define ulong unsigned long
#define EXPORT extern "C" __declspec(dllexport)
#define var const auto
#include <exception>
#include <string>
#include <windows.h>

typedef struct
{
    HINSTANCE Module;
    ulong MainThreadId;
    int MainThreadPriority;
} LOADER_INFORMATION, *PLOADER_INFORMATION;

typedef void (*InitFunc)(PLOADER_INFORMATION);

typedef struct
{
    InitFunc CLREntryPoint;
    PLOADER_INFORMATION PLoaderInfo;
} CLRInitFunc, *PCLRInitFunc;

void Cleanup(const PLOADER_INFORMATION lpParameter, const HANDLE mainThread)
{
    if (mainThread != nullptr)
    {
        SetThreadPriority(mainThread, lpParameter->MainThreadPriority);
        SetThreadPriority(GetCurrentThread(), THREAD_PRIORITY_NORMAL);
        ResumeThread(mainThread);
        CloseHandle(mainThread);    
    }
    delete lpParameter;
}

ulong WINAPI DllThread(const PLOADER_INFORMATION lpParameter)
{
    var loaderInfo = *lpParameter;
    var mainThread = OpenThread(THREAD_SUSPEND_RESUME, false, loaderInfo.MainThreadId);

    if (mainThread != nullptr)
    {
        SuspendThread(mainThread);
    }

    var address = GetProcAddress(loaderInfo.Module, "Init");

    if (address == nullptr)
    {
        Cleanup(lpParameter, mainThread);
        return 1;
    }

    var init = reinterpret_cast<InitFunc>(address);  // NOLINT(clang-diagnostic-cast-function-type-strict)
    try
    {
        init(lpParameter);
    }
    catch (const std::exception &e)
    {
        MessageBoxA(nullptr, (std::string("Error during loading Mod Loader: ") + e.what()).c_str(), "Error", MB_OK | MB_ICONERROR);
    }

    Cleanup(lpParameter, mainThread);
    return 0;
}

EXPORT bool APIENTRY DllMain(const HMODULE hModule, const ulong fdwReason)
{
    if (fdwReason != DLL_PROCESS_ATTACH)
        return true;

    DisableThreadLibraryCalls(hModule);
    const LPDWORD threadId = nullptr;
    var loaderInfo = new(LOADER_INFORMATION)
    {
        hModule, GetCurrentThreadId(), GetThreadPriority(GetCurrentThread())
    };
    // ReSharper disable once CppTooWideScopeInitStatement
    var dllThread = CreateThread(nullptr,  0, reinterpret_cast<LPTHREAD_START_ROUTINE>(DllThread), loaderInfo, 0, threadId);  // NOLINT(clang-diagnostic-cast-function-type-strict)

    if (dllThread != nullptr)
    {
        SetThreadPriority(dllThread, THREAD_PRIORITY_TIME_CRITICAL);
        SetThreadPriority(GetCurrentThread(), THREAD_PRIORITY_IDLE);  
    }
    
    return true;
}