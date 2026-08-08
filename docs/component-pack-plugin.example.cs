using System.Collections.Generic;
using AvaloniaUIDesigner.App.Designer.Contracts;
using AvaloniaUIDesigner.App.Models;

namespace Acme.DesignerPlugins;

public sealed class AcmeComponentPackPlugin : IComponentPackPlugin
{
    public string Name => "Acme Custom Controls";

    public ComponentPackDocument CreatePack()
        => new()
        {
            Name = Name,
            Components =
            [
                new ComponentPackComponent
                {
                    DisplayName = "Analytics Card",
                    AvaloniaTypeName = "Acme.Controls.AnalyticsCard",
                    DesignOnly = true,
                    PreviewText = "Analytics Card",
                    DefaultWidth = 320,
                    DefaultHeight = 120,
                    DefaultProperties = new Dictionary<string, string?>
                    {
                        ["Header"] = "Revenue",
                        ["Value"] = "$42K",
                    },
                },
            ],
        };
}
