#define ulong unsigned long
#define EXPORT EXTERN_C __MIDL_DECLSPEC_DLLEXPORT
#include <exception>
#include <iostream>
#include <ostream>
#include <string>
#include <windows.h>
#include <tlhelp32.h>

typedef struct
{
    HINSTANCE Module;
    DWORD MainThreadId;
    int MainThreadPriority;
} LOADER_INFORMATION, *PLOADER_INFORMATION;

typedef void (*InitFunc)(PLOADER_INFORMATION);

typedef struct
{
    InitFunc CLREntryPoint;
    PLOADER_INFORMATION PLoaderInfo;
} CLRInitFunc, *PCLRInitFunc;


DWORD WINAPI DllThread(const PLOADER_INFORMATION lpParameter)
{
    const auto loaderInfo = *lpParameter;
    // ReSharper disable once CppFunctionResultShouldBeUsed
    SetThreadDescription(GetCurrentThread(), L"DllMain");
    
    const auto address = GetProcAddress(loaderInfo.Module, "Init");

    if (address == nullptr)
    {
        delete lpParameter;
        return 1;
    }

    const auto init = reinterpret_cast<InitFunc>(address);  // NOLINT(clang-diagnostic-cast-function-type-strict)
    try
    {
        init(lpParameter);
    }
    catch (const std::exception &e)
    {
        MessageBoxA(nullptr, (std::string("Error during loading Mod Loader: ") + e.what()).c_str(), "Error", MB_OK | MB_ICONERROR);
    }

    delete lpParameter;
    return 0;
}

DWORD GetMainThreadId()
{
    const auto processId = GetCurrentProcessId();
    DWORD mainThreadId = 0;
    FILETIME earliestCreationTime = { MAXDWORD, MAXDWORD };

    const auto hSnapshot = CreateToolhelp32Snapshot(TH32CS_SNAPTHREAD, 0);
    if (hSnapshot != INVALID_HANDLE_VALUE)
    {
        THREADENTRY32 te;
        te.dwSize = sizeof(te);

        if (Thread32First(hSnapshot, &te))
        {
            do
            {
                if (te.th32OwnerProcessID == processId)
                {
                    const auto hThread = OpenThread(THREAD_QUERY_INFORMATION, FALSE, te.th32ThreadID);
                    if (hThread)
                    {
                        FILETIME creationTime, exitTime, kernelTime, userTime;
                        if (GetThreadTimes(hThread, &creationTime, &exitTime, &kernelTime, &userTime))
                        {
                            if (CompareFileTime(&creationTime, &earliestCreationTime) < 0)
                            {
                                earliestCreationTime = creationTime;
                                mainThreadId = te.th32ThreadID;
                            }
                        }
                        CloseHandle(hThread);
                    }
                }
            } while (Thread32Next(hSnapshot, &te));
        }
        CloseHandle(hSnapshot);
    }

    return mainThreadId;
}

EXPORT bool cdecl DllMain(const HMODULE hModule, const ulong fdwReason, LPVOID _) noexcept
{
    if (fdwReason != DLL_PROCESS_ATTACH)
        return true;

    DisableThreadLibraryCalls(hModule);
    // const auto loaderInfo = new(LOADER_INFORMATION)
    // {
    //     hModule, GetCurrentThreadId(), GetThreadPriority(GetCurrentThread())
    // };

    int threadPriority = THREAD_PRIORITY_NORMAL;
    const auto mainThreadId = GetMainThreadId();
    if (mainThreadId != 0)
    {
        const auto hMainThread = OpenThread(THREAD_QUERY_INFORMATION, false, mainThreadId);
        threadPriority = GetThreadPriority(hMainThread);
        CloseHandle(hMainThread);
    }

    
    const auto loaderInfo = new(LOADER_INFORMATION)
    {
        hModule, mainThreadId, threadPriority
    };
    
    CreateThread(nullptr,  0, reinterpret_cast<LPTHREAD_START_ROUTINE>(DllThread), loaderInfo, 0, nullptr);  // NOLINT(clang-diagnostic-cast-function-type-strict)
    
    return true;
}
