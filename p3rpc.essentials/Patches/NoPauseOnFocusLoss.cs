using p3rpc.essentials.Configuration;
using p3rpc.essentials.Utilities;
using Reloaded.Hooks.ReloadedII.Interfaces;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using static p3rpc.essentials.Utilities.Native;
using static p3rpc.essentials.Utils;

namespace p3rpc.essentials.Patches;
/// <summary>
/// Patch that disables pause on focus loss.
/// </summary>
internal static class NoPauseOnFocusLoss
{
    private static IReloadedHooks _hooks = null!;
    private static WndProcHook _wndProcHook = null!;

    public static void Activate(IReloadedHooks hooks)
    {
        _hooks = hooks;

        _ = Task.Run(async () =>
        {
            await TryHookWndProc("UnrealWindow");
        });
    }

    private static async Task TryHookWndProc(string windowClass)
    {
        while (true)
        {
            var window = FindWindow(windowClass, null);
            if (window == IntPtr.Zero)
            {
                await Task.Delay(1000);
                continue;
            }

            unsafe
            {
                Log("Found Window, Hooking WndProc.");
                var wndProcHandlerPtr = (IntPtr)_hooks.Utilities.GetFunctionPointer(typeof(NoPauseOnFocusLoss), nameof(WndProcImpl));
                _wndProcHook = WndProcHook.Create(_hooks, window, Unsafe.As<IntPtr, WndProcFn>(ref wndProcHandlerPtr));
                return;
            }

        }
    }

    [UnmanagedCallersOnly]
    private static unsafe IntPtr WndProcImpl(IntPtr hWnd, uint uMsg, IntPtr wParam, IntPtr lParam)
    {
        if (!Mod.Configuration.RenderInBackground)
            return _wndProcHook.Hook.OriginalFunction.Value.Invoke(hWnd, uMsg, wParam, lParam);

        switch (uMsg)
        {
            case WM_ACTIVATE:
            case WM_ACTIVATEAPP:
                if (wParam == IntPtr.Zero)
                    return IntPtr.Zero;

                break;

            case WM_KILLFOCUS:
                return IntPtr.Zero;
        }

        return _wndProcHook.Hook.OriginalFunction.Value.Invoke(hWnd, uMsg, wParam, lParam);
    }
}