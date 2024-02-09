using p3rpc.essentials.Template.Configuration;
using System.ComponentModel;

namespace p3rpc.essentials.Configuration;
public class Config : Configurable<Config>
{
    [DisplayName("Render In Background")]
    [Description("Makes the game continue running when not in focus.")]
    [DefaultValue(false)]
    public bool RenderInBackground { get; set; } = false;
}

/// <summary>
/// Allows you to override certain aspects of the configuration creation process (e.g. create multiple configurations).
/// Override elements in <see cref="ConfiguratorMixinBase"/> for finer control.
/// </summary>
public class ConfiguratorMixin : ConfiguratorMixinBase
{
    // 
}