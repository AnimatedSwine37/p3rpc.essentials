using Reloaded.Hooks.Definitions;
using System.Runtime.InteropServices;
using static p3rpc.essentials.Configuration.Config;
using IReloadedHooks = Reloaded.Hooks.ReloadedII.Interfaces.IReloadedHooks;

namespace p3rpc.essentials.Patches;
internal unsafe class IntroSkip
{
    private static IReloadedHooks _hooks;

    private static IHook<GetStateDelegate> _introHook;
    private static IHook<GetStateDelegate> _cautionHook;

    private static NextStateDelegate? _introNextState;

    public static void Activate(IReloadedHooks hooks)
    {
        _hooks = hooks;
        Utils.SigScan("48 8B C4 53 57 41 57 48 81 EC D0 00 00 00", "Intro Skip", address =>
        {
            _introHook = hooks.CreateHook<GetStateDelegate>(Intro, address).Activate();
        });

        Utils.SigScan("48 89 5C 24 ?? 48 89 74 24 ?? 57 48 83 EC 30 80 B9 ?? ?? ?? ?? 00 48 8B D9", "Caution Skip", address =>
        {
            _cautionHook = hooks.CreateHook<GetStateDelegate>(Caution, address).Activate();
        });
    }

    private static byte Caution(IntroPartInfo* info, float param_2)
    {
        var res = _cautionHook.OriginalFunction(info, param_2);

        var skip = Mod.Configuration.IntroSkip;
        if (skip == IntroPart.None) return res;

        if (!Mod.Configuration.NetworkSkip)
        {
            Utils.Log("Skipping to the network settings");
            return 2;
        }

        return 3; // Skip past network and caution
    }

    private static byte Intro(IntroPartInfo* info, float param2)
    {
        var res = _introHook.OriginalFunction(info, param2);
        if (res != 3) return res;

        var skip = Mod.Configuration.IntroSkip;
        if (skip == IntroPart.OpeningMovie)
        {
            Utils.Log("Skipping to the opening movie");
            if (_introNextState == null)
                _introNextState = _hooks.CreateWrapper<NextStateDelegate>((long)info->VTable->NextState, out _);

            return _introNextState(info);
        }
        else if (skip == IntroPart.MainMenu)
        {
            Utils.Log("Skipping to the main menu");
            return 5;
        }
        else if (skip == IntroPart.LoadMenu)
        {
            Utils.Log("Skipping to the load menu");
            return 8;
        }

        return res;
    }

    [StructLayout(LayoutKind.Explicit)]
    private struct IntroPartInfo
    {
        [FieldOffset(0)]
        internal IntroPartVTable* VTable;
    }

    [StructLayout(LayoutKind.Explicit)]
    private struct IntroPartVTable
    {
        [FieldOffset(0x298)]
        internal nuint NextState;
    }

    private delegate byte GetStateDelegate(IntroPartInfo* info, float param2);
    private delegate byte NextStateDelegate(IntroPartInfo* info);
}
