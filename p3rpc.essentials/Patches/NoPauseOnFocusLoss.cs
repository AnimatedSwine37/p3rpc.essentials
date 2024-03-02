using p3rpc.essentials.Configuration;
using p3rpc.essentials.Utilities;
using Reloaded.Hooks.Definitions;
using Reloaded.Hooks.ReloadedII.Interfaces;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using static p3rpc.essentials.Utilities.Native;
using static p3rpc.essentials.Utils;
using IReloadedHooks = Reloaded.Hooks.ReloadedII.Interfaces.IReloadedHooks;

namespace p3rpc.essentials.Patches;
/// <summary>
/// Patch that disables pause on focus loss.
/// </summary>
internal static class NoPauseOnFocusLoss
{
    private static IReloadedHooks _hooks = null!;
    private static WndProcHook _wndProcHook = null!;
    private static IHook<SetupWindowDelegate> _setupWindowHook;

    public unsafe static void Activate(IReloadedHooks hooks)
    {
        _hooks = hooks;

        SigScan("4C 8B DC 53 55 56 41 54 41 55 41 56", "SetupWindow", address =>
        {
            _setupWindowHook = _hooks.CreateHook<SetupWindowDelegate>(SetupWindow, address).Activate();
        });
    }

    private static unsafe void SetupWindow(WindowInfo* info, nuint param_2, nuint param_3, nuint param_4, nuint param_5)
    {
        _setupWindowHook.OriginalFunction(info, param_2, param_3, param_4, param_5);
        Log("Got Window, Hooking WndProc.");
        var wndProcHandlerPtr = (IntPtr)_hooks.Utilities.GetFunctionPointer(typeof(NoPauseOnFocusLoss), nameof(WndProcImpl));
        _wndProcHook = WndProcHook.Create(_hooks, info->hWnd, Unsafe.As<IntPtr, WndProcFn>(ref wndProcHandlerPtr));
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

    [StructLayout(LayoutKind.Explicit)]
    private struct WindowInfo
    {
        [FieldOffset(0x28)]
        public nint hWnd;
    }

    private unsafe delegate void SetupWindowDelegate(WindowInfo* info, nuint param_2, nuint param_3, nuint param_4, nuint param_5);

}