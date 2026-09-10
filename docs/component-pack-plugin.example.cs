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
                    PropertyDefinitions =
                    [
                        new() { Name = "Header", Type = "String" },
                        new() { Name = "Value", Type = "String" },
                        new()
                        {
                            Name = "Trend",
                            Type = "Enum",
                            Options = ["Up", "Flat", "Down"],
                        },
                    ],
                    DefaultProperties = new Dictionary<string, string?>
                    {
                        ["Header"] = "Revenue",
                        ["Value"] = "$42K",
                        ["Trend"] = "Up",
                    },
                },
            ],
        };
}
