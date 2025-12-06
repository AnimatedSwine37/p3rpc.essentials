using p3rpc.essentials.Template.Configuration;
using System.ComponentModel;

namespace p3rpc.essentials.Configuration;
public class Config : Configurable<Config>
{
    [DisplayName("Render In Background")]
    [Description("Makes the game continue running when not in focus.")]
    [DefaultValue(false)]
    public bool RenderInBackground { get; set; } = false;

    [DisplayName("Intro Skip")]
    [Category("Intro Skip")]
    [Description("Skip to the specified part of the intro.")]
    [DefaultValue(IntroPart.None)]
    public IntroPart IntroSkip { get; set; } = IntroPart.None;

    [DisplayName("Skip Network")]
    [Category("Intro Skip")]
    [Description("Skips the section asking you whether you want network features.\nDoing this will cause network features to be off.")]
    [DefaultValue(false)]
    public bool NetworkSkip { get; set; } = false;
    
    [DisplayName("Intro Skip (Episode Aigis)")]
    [Category("Intro Skip")]
    [Description("Skip to the main menu for Episode Aigis.")]
    [DefaultValue(false)]
    public bool IntroSkipAstrea { get; set; } = false;
    // public IntroPartAstrea IntroSkipAstrea { get; set; } = IntroPartAstrea.None;
    
    [DisplayName("Fast Menu Navigation")]
    [Category("Intro Skip")]
    [Description("Let the user make inputs in the title menu immediately.")]
    [DefaultValue(false)]
    public bool FastMenuNavigation { get; set; } = false;

    public enum IntroPart
    {
        None,
        OpeningMovie,
        MainMenu,
        LoadMenu
    }
}

/// <summary>
/// Allows you to override certain aspects of the configuration creation process (e.g. create multiple configurations).
/// Override elements in <see cref="ConfiguratorMixinBase"/> for finer control.
/// </summary>
public class ConfiguratorMixin : ConfiguratorMixinBase
{
    // 
}