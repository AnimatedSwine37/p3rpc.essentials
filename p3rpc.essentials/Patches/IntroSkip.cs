using Reloaded.Hooks.Definitions;
using System.Runtime.InteropServices;
using static p3rpc.essentials.Configuration.Config;
using IReloadedHooks = Reloaded.Hooks.ReloadedII.Interfaces.IReloadedHooks;

namespace p3rpc.essentials.Patches;

internal enum ETitleState : byte
{
    TS_Caution = 0,
    TS_PhotosensitiveCaution = 1,
    TS_NetworkCheck = 2,
    TS_Logo = 3,
    TS_OP = 4,
    TS_PressWait = 5,
    TS_Select = 6,
    TS_NewGame = 7,
    TS_LoadGame = 8,
    TS_Config = 9,
    TS_Exit = 10,
    TS_ComeBackLoad = 11,
    TS_WaitGamerTag = 12,
    TS_ResidentReload = 13,
    TS_OP_Astrea = 14,
    TS_PressWait_Astrea = 15,
    TS_Select_Astrea = 16,
    TS_Num = 17,
    TS_MAX = 18,
}

internal unsafe class IntroSkip
{
    private static IReloadedHooks _hooks;

    private static IHook<GetStateDelegate>? _introHook;
    private static IHook<GetStateDelegate>? _cautionHook;
    private static IHook<GetStateDelegate>? _introHookAstrea;
    private static IHook<GetStateDelegate>? _introHookOP;
    private static IHook<GetStateDelegate>? _pressWaitHook;
    private static IHook<GetStateDelegate>? _selectHook;
    private static IHook<GetStateDelegate>? _selectHookAstrea;

    private static NextStateDelegate? _introNextState;
    private static NextStateDelegate? _pressWaitNextState;
    private static NextStateDelegate? _opNextState;
    private static NextStateDelegate? _introNextStateAstrea;

    private static bool JumpToLoadState = false;
    private static bool InitialLoad = false;

    public static void Activate(IReloadedHooks hooks)
    {
        _hooks = hooks;
        
        Utils.SigScan("48 89 5C 24 ?? 48 89 74 24 ?? 57 48 83 EC 30 80 B9 ?? ?? ?? ?? 00 48 8B D9", "Caution Skip", address =>
        {
            _cautionHook = hooks.CreateHook<GetStateDelegate>(Caution, address).Activate();
        });
    }

    private static ETitleState Caution(UTitleStateBase* self, float delta)
    {
        var res = _cautionHook.OriginalFunction(self, delta);

        var skip = Mod.Configuration.IntroSkip;
        if (skip == IntroPart.None) return res;

        var TitleActor = self->TitleActor;
        for (var i = 0; i < TitleActor->StateCount; i++)
        {
            var CurrState = TitleActor->StateAlloc[i].Value;
            switch (TitleActor->StateAlloc[i].Key)
            {
                case ETitleState.TS_Logo:
                    _introHook ??= _hooks.CreateHook<GetStateDelegate>(Intro, 
                        (long)CurrState->VTable->UpdateState).Activate();
                    break;
                case ETitleState.TS_PressWait:
                    _pressWaitHook ??= _hooks.CreateHook<GetStateDelegate>(PressWait, 
                        (long)CurrState->VTable->UpdateState).Activate();
                    break;
                case ETitleState.TS_OP:
                    _introHookOP ??= _hooks.CreateHook<GetStateDelegate>(IntroOP,
                        (long)CurrState->VTable->UpdateState).Activate();
                    break;
                case ETitleState.TS_Select:
                    _selectHook ??= _hooks.CreateHook<GetStateDelegate>(Select, 
                        (long)CurrState->VTable->UpdateState).Activate();
                    break;
                case ETitleState.TS_OP_Astrea:
                    _introHookAstrea ??= _hooks.CreateHook<GetStateDelegate>(IntroAstrea, 
                        (long)CurrState->VTable->UpdateState).Activate();
                    break;
                case ETitleState.TS_Select_Astrea:
                    _selectHookAstrea ??= _hooks.CreateHook<GetStateDelegate>(SelectAstrea, 
                        (long)CurrState->VTable->UpdateState).Activate();
                    break;
                default:
                    continue;
            }
        }

        if (!Mod.Configuration.NetworkSkip)
        {
            Utils.Log("Skipping to the network settings");
            return ETitleState.TS_NetworkCheck;
        }

        return ETitleState.TS_Logo; // Skip past network and caution
    }

    private static ETitleState Intro(UTitleStateBase* self, float delta)
    {
        var res = _introHook.OriginalFunction(self, delta);

        if (res != ETitleState.TS_Logo) return res;
        InitialLoad = true;
        switch (Mod.Configuration.IntroSkip)
        {
            case IntroPart.OpeningMovie:
                Utils.Log("Skipping to the opening movie");
                _introNextState ??= _hooks.CreateWrapper<NextStateDelegate>((long)self->VTable->NextState, out _);
                return _introNextState(self);
            case IntroPart.MainMenu:
            // case IntroPart.LoadMenu:
                Utils.Log("Skipping to the main menu");
                return ETitleState.TS_PressWait; 
            case IntroPart.LoadMenu:
                Utils.Log("Skipping to the load menu");
                return ETitleState.TS_LoadGame;
            default:
                return res;
        }
    }

    private static ETitleState PressWait(UTitleStateBase* self, float delta)
    {
        if (Mod.Configuration.FastMenuNavigation)
            *(float*)((nint)self + 0x50) = 1.75f; // DT_TitleUI->PleaseWaitFadeInWaitTime
        return _pressWaitHook.OriginalFunction(self, delta);
        // var res = _pressWaitHook.OriginalFunction(self, delta);
        // if (res != ETitleState.TS_PressWait) return res;
        /*
        if (Mod.Configuration.IntroSkip == IntroPart.LoadMenu && !JumpToLoadState)
        { 
            JumpToLoadState = true;
            // _pressWaitNextState ??= _hooks.CreateWrapper<NextStateDelegate>((long)self->VTable->NextState, out _);
            // return _pressWaitNextState(self);
            return ETitleState.TS_LoadGame;
        }
        */
        // return res;
    }
    
    private static ETitleState SelectInner(UTitleStateBase* self, float delta, IHook<GetStateDelegate> Delegate)
    {
        if (Mod.Configuration.FastMenuNavigation && self->TitleActor != null && !self->TitleActor->Input.InputControl0)
        {
            var ActorInput = &self->TitleActor->Input;
            for (var i = 0; i < ActorInput->EntryCount; i++)
                ActorInput->Entries[i].ValCur = ActorInput->Entries[i].ValEnd;
            ActorInput->InputControl0 = true;
            ActorInput->InputControl1 = true;
        }
        return Delegate.OriginalFunction(self, delta);
    }

    private static ETitleState Select(UTitleStateBase* self, float delta)
        => SelectInner(self, delta, _selectHook);
    
    private static ETitleState SelectAstrea(UTitleStateBase* self, float delta)
        => SelectInner(self, delta, _selectHookAstrea);

    private static ETitleState IntroOP(UTitleStateBase* self, float delta)
    {
        var res = _introHookOP.OriginalFunction(self, delta);
        if (res != ETitleState.TS_OP || !Mod.Configuration.IntroSkipAstrea) return res;
        // return InitialLoad ? ETitleState.TS_ResidentReload : ETitleState.TS_PressWait;
        Utils.Log("Skipping to the main menu");
        _opNextState ??= _hooks.CreateWrapper<NextStateDelegate>((long)self->VTable->NextState, out _);
        return _opNextState(self);
    }

    private static ETitleState IntroAstrea(UTitleStateBase* self, float delta)
    {
        var res = _introHookAstrea.OriginalFunction(self, delta);
        if (res != ETitleState.TS_OP_Astrea || !Mod.Configuration.IntroSkipAstrea) return res;
        Utils.Log("Skipping to the main menu");
        _introNextStateAstrea ??= _hooks.CreateWrapper<NextStateDelegate>((long)self->VTable->NextState, out _);
        return _introNextStateAstrea(self);           
    }

    [StructLayout(LayoutKind.Explicit)]
    private struct UTitleStatBaseVtable
    {
        [FieldOffset(0x280)] internal nuint UpdateState;
        [FieldOffset(0x298)] internal nuint NextState;
    }
    
    [StructLayout(LayoutKind.Explicit)]
    private struct UTitleStateBase
    {
        [FieldOffset(0x0)] internal UTitleStatBaseVtable* VTable;
        [FieldOffset(0x30)] internal ATitleActor* TitleActor;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct TitleStateBase
    {
        internal ETitleState Key;
        internal UTitleStateBase* Value;
        internal int NextHashId;
        internal int HashIndex;
    }
    
    [StructLayout(LayoutKind.Explicit)]
    private struct ATitleActor
    {
        [FieldOffset(0x288)] internal TitleStateBase* StateAlloc;
        [FieldOffset(0x290)] internal int StateCount;
        [FieldOffset(0x478)] internal ATitleActorInput Input;
    }
    
    [StructLayout(LayoutKind.Explicit)]
    private struct ATitleActorInput
    {
        [FieldOffset(0x20)] internal ATitleActorInputEntry* Entries;
        [FieldOffset(0x28)] internal int EntryCount;
        [FieldOffset(0x85)] public bool InputControl0;
        [FieldOffset(0xb5)] public bool InputControl1;
    }
    
    [StructLayout(LayoutKind.Explicit, Size = 0x358)]
    private struct ATitleActorInputEntry
    {
        [FieldOffset(0x248)] internal float ValEnd;
        [FieldOffset(0x250)] internal float ValCur;
    }

    private delegate ETitleState GetStateDelegate(UTitleStateBase* self, float delta);
    private delegate ETitleState NextStateDelegate(UTitleStateBase* self);
}