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
                        new()
                        {
                            Name = "Header",
                            Type = "String",
                            DisplayName = "Card header",
                            Category = "Content",
                            Description = "Heading shown above the primary value.",
                        },
                        new()
                        {
                            Name = "Value",
                            Type = "String",
                            DisplayName = "Primary value",
                            Category = "Content",
                        },
                        new()
                        {
                            Name = "Trend",
                            Type = "Enum",
                            DisplayName = "Trend direction",
                            Category = "Data",
                            Description = "Controls the trend indicator direction.",
                            Options = ["Up", "Flat", "Down"],
                        },
                        new()
                        {
                            Name = "ContentPadding",
                            Type = "Thickness",
                            DisplayName = "Content padding",
                            Category = "Layout",
                        },
                        new()
                        {
                            Name = "CardCorners",
                            Type = "CornerRadius",
                            DisplayName = "Card corners",
                            Category = "Appearance",
                        },
                        new()
                        {
                            Name = "PreferredTrack",
                            Type = "GridLength",
                            DisplayName = "Preferred track",
                            Category = "Layout",
                        },
                    ],
                    DefaultProperties = new Dictionary<string, string?>
                    {
                        ["Header"] = "Revenue",
                        ["Value"] = "$42K",
                        ["Trend"] = "Up",
                        ["ContentPadding"] = "16,12",
                        ["CardCorners"] = "8",
                        ["PreferredTrack"] = "2*",
                    },
                },
            ],
        };
}
