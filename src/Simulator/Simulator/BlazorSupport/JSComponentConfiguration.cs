using Microsoft.AspNetCore.Components.Web;

namespace OpenSilver.Simulator.BlazorSupport
{
    public class JSComponentConfiguration : IJSComponentConfiguration
    {
        /// <inheritdoc />
        public JSComponentConfigurationStore JSComponents { get; } = new();
    }
}
