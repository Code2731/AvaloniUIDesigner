using System;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;

namespace AvaloniaUIDesigner.App.Designer.Services;

public sealed record DesignerPreviewInteraction(
    string ControlName,
    string EventName,
    string HandlerName);

public static class DesignerPreviewInteractionRuntime
{
    public static int Wire(
        Control control,
        string controlName,
        Action<DesignerPreviewInteraction> report)
    {
        var handlers = DesignerEventHandlerRuntime.Read(control);
        var wired = 0;

        void Subscribe(string eventName, Action<Action> subscribe)
        {
            if (!handlers.TryGetValue(eventName, out var handlerName))
            {
                return;
            }

            subscribe(() => report(new DesignerPreviewInteraction(
                controlName,
                eventName,
                handlerName)));
            wired++;
        }

        Subscribe("AttachedToVisualTree", callback =>
            control.AttachedToVisualTree += (_, _) => callback());
        Subscribe("DetachedFromVisualTree", callback =>
            control.DetachedFromVisualTree += (_, _) => callback());
        Subscribe("DataContextChanged", callback =>
            control.DataContextChanged += (_, _) => callback());
        Subscribe("Initialized", callback =>
            control.Initialized += (_, _) => callback());
        Subscribe("LayoutUpdated", callback =>
            control.LayoutUpdated += (_, _) => callback());
        Subscribe("SizeChanged", callback =>
            control.SizeChanged += (_, _) => callback());
        Subscribe("Unloaded", callback =>
            control.Unloaded += (_, _) => callback());

        Subscribe("GotFocus", callback =>
            control.GotFocus += (_, _) => callback());
        Subscribe("LostFocus", callback =>
            control.LostFocus += (_, _) => callback());
        Subscribe("KeyDown", callback =>
            control.KeyDown += (_, _) => callback());
        Subscribe("KeyUp", callback =>
            control.KeyUp += (_, _) => callback());
        Subscribe("PointerCaptureLost", callback =>
            control.PointerCaptureLost += (_, _) => callback());
        Subscribe("PointerEntered", callback =>
            control.PointerEntered += (_, _) => callback());
        Subscribe("PointerExited", callback =>
            control.PointerExited += (_, _) => callback());
        Subscribe("PointerMoved", callback =>
            control.PointerMoved += (_, _) => callback());
        Subscribe("PointerPressed", callback =>
            control.PointerPressed += (_, _) => callback());
        Subscribe("PointerReleased", callback =>
            control.PointerReleased += (_, _) => callback());
        Subscribe("PointerWheelChanged", callback =>
            control.PointerWheelChanged += (_, _) => callback());
        Subscribe("Tapped", callback =>
            control.Tapped += (_, _) => callback());
        Subscribe("DoubleTapped", callback =>
            control.DoubleTapped += (_, _) => callback());
        Subscribe("TextInput", callback =>
            control.TextInput += (_, _) => callback());
        Subscribe("ContextRequested", callback =>
            control.ContextRequested += (_, _) => callback());

        if (control is Button button)
        {
            Subscribe("Click", callback =>
                button.Click += (_, _) => callback());
        }

        if (control is TextBox textBox)
        {
            Subscribe("TextChanged", callback =>
                textBox.TextChanged += (_, _) => callback());
        }

        if (control is SelectingItemsControl selectingItemsControl)
        {
            Subscribe("SelectionChanged", callback =>
                selectingItemsControl.SelectionChanged += (_, _) => callback());
        }

        if (control is RangeBase rangeBase)
        {
            Subscribe("ValueChanged", callback =>
                rangeBase.ValueChanged += (_, _) => callback());
        }

        if (control is NumericUpDown numericUpDown)
        {
            Subscribe("ValueChanged", callback =>
                numericUpDown.ValueChanged += (_, _) => callback());
        }

        if (control is ToggleButton toggleButton)
        {
            if (handlers.ContainsKey("Checked")
                || handlers.ContainsKey("Unchecked")
                || handlers.ContainsKey("Indeterminate"))
            {
                toggleButton.IsCheckedChanged += (_, _) =>
                {
                    var eventName = toggleButton.IsChecked switch
                    {
                        true => "Checked",
                        false => "Unchecked",
                        _ => "Indeterminate",
                    };
                    if (handlers.TryGetValue(eventName, out var handlerName))
                    {
                        report(new DesignerPreviewInteraction(
                            controlName,
                            eventName,
                            handlerName));
                    }
                };
                wired++;
            }
        }

        if (control is Expander expander)
        {
            Subscribe("Expanded", callback =>
                expander.Expanded += (_, _) => callback());
            Subscribe("Collapsed", callback =>
                expander.Collapsed += (_, _) => callback());
        }

        if (control is DatePicker datePicker)
        {
            Subscribe("SelectedDateChanged", callback =>
                datePicker.SelectedDateChanged += (_, _) => callback());
        }

        if (control is CalendarDatePicker calendarDatePicker)
        {
            Subscribe("SelectedDateChanged", callback =>
                calendarDatePicker.SelectedDateChanged += (_, _) => callback());
        }

        return wired;
    }
}
