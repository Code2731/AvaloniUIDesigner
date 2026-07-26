using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Dynamic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Avalonia.Controls;

namespace AvaloniaUIDesigner.App.Designer.Services;

public sealed record DesignerSampleDataDocument(
    string Json,
    DesignerSampleObject Root);

public sealed record DesignerSampleApplyResult(
    int AppliedCount,
    int MissingPathCount,
    int ConversionFailureCount)
{
    public static DesignerSampleApplyResult operator +(
        DesignerSampleApplyResult left,
        DesignerSampleApplyResult right)
        => new(
            left.AppliedCount + right.AppliedCount,
            left.MissingPathCount + right.MissingPathCount,
            left.ConversionFailureCount + right.ConversionFailureCount);
}

public sealed class DesignerSampleObject : DynamicObject, ICustomTypeDescriptor
{
    private readonly IReadOnlyDictionary<string, object?> _values;

    internal DesignerSampleObject(IReadOnlyDictionary<string, object?> values)
    {
        _values = values;
    }

    public bool TryGetValue(string name, out object? value)
        => _values.TryGetValue(name, out value);

    public int Count => _values.Count;

    public override bool TryGetMember(GetMemberBinder binder, out object? result)
        => _values.TryGetValue(binder.Name, out result);

    public override string ToString()
    {
        foreach (var key in new[] { "DisplayName", "Name", "Title", "Text" })
        {
            if (_values.TryGetValue(key, out var value) && value is not null)
            {
                return Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty;
            }
        }

        return $"Sample object ({_values.Count} properties)";
    }

    AttributeCollection ICustomTypeDescriptor.GetAttributes() => AttributeCollection.Empty;
    string? ICustomTypeDescriptor.GetClassName() => nameof(DesignerSampleObject);
    string? ICustomTypeDescriptor.GetComponentName() => null;
    TypeConverter ICustomTypeDescriptor.GetConverter() => new();
    EventDescriptor? ICustomTypeDescriptor.GetDefaultEvent() => null;
    PropertyDescriptor? ICustomTypeDescriptor.GetDefaultProperty() => null;
    object? ICustomTypeDescriptor.GetEditor(Type editorBaseType) => null;
    EventDescriptorCollection ICustomTypeDescriptor.GetEvents() => EventDescriptorCollection.Empty;
    EventDescriptorCollection ICustomTypeDescriptor.GetEvents(Attribute[]? attributes)
        => EventDescriptorCollection.Empty;
    PropertyDescriptorCollection ICustomTypeDescriptor.GetProperties()
        => GetPropertyDescriptors();
    PropertyDescriptorCollection ICustomTypeDescriptor.GetProperties(Attribute[]? attributes)
        => GetPropertyDescriptors();
    object ICustomTypeDescriptor.GetPropertyOwner(PropertyDescriptor? pd) => this;

    private PropertyDescriptorCollection GetPropertyDescriptors()
        => new(_values
            .Select(pair => new SamplePropertyDescriptor(pair.Key, pair.Value))
            .ToArray<PropertyDescriptor>());

    private sealed class SamplePropertyDescriptor : PropertyDescriptor
    {
        private readonly object? _value;

        public SamplePropertyDescriptor(string name, object? value)
            : base(name, null)
        {
            _value = value;
        }

        public override Type ComponentType => typeof(DesignerSampleObject);
        public override bool IsReadOnly => true;
        public override Type PropertyType => _value?.GetType() ?? typeof(object);
        public override bool CanResetValue(object component) => false;
        public override object? GetValue(object? component) => _value;
        public override void ResetValue(object component)
        {
        }

        public override void SetValue(object? component, object? value)
        {
        }

        public override bool ShouldSerializeValue(object component) => false;
    }
}

public static class DesignerSampleDataRuntime
{
    private static readonly ConditionalWeakTable<Control, ControlSampleState> States = new();
    private static readonly JsonSerializerOptions CanonicalJsonOptions = new()
    {
        WriteIndented = true,
    };

    public static bool TryParse(
        string json,
        out DesignerSampleDataDocument? document,
        out string error)
    {
        document = null;
        error = string.Empty;
        if (string.IsNullOrWhiteSpace(json))
        {
            return true;
        }

        try
        {
            using var parsed = JsonDocument.Parse(json, new JsonDocumentOptions
            {
                AllowTrailingCommas = true,
                CommentHandling = JsonCommentHandling.Skip,
            });
            if (parsed.RootElement.ValueKind != JsonValueKind.Object)
            {
                error = "Sample data root must be a JSON object.";
                return false;
            }

            var canonical = JsonSerializer.Serialize(parsed.RootElement, CanonicalJsonOptions);
            document = new DesignerSampleDataDocument(
                canonical,
                (DesignerSampleObject)ConvertElement(parsed.RootElement)!);
            return true;
        }
        catch (JsonException ex)
        {
            error = $"Sample data JSON is invalid: {ex.Message}";
            return false;
        }
    }

    public static DesignerSampleApplyResult Apply(Control control, DesignerSampleObject root)
    {
        Clear(control);
        var bindings = DesignerBindingRuntime.ReadBindings(control);
        if (bindings.Count == 0)
        {
            return new DesignerSampleApplyResult(0, 0, 0);
        }

        var state = new ControlSampleState(control);
        var preparedProperties = new List<string>();
        var applied = 0;
        var missing = 0;
        var conversionFailures = 0;
        // Capture every original first because setting ItemsSource can reset selection properties.
        foreach (var binding in bindings
                     .OrderBy(binding => GetApplyOrder(binding.PropertyName))
                     .ThenBy(binding => binding.PropertyName, StringComparer.Ordinal))
        {
            object? value;
            if (!TryResolvePath(root, binding.Path, out value))
            {
                if (binding.FallbackValue is null)
                {
                    missing++;
                    continue;
                }

                value = binding.FallbackValue;
            }

            if (!state.TryPrepare(binding.PropertyName, value))
            {
                conversionFailures++;
                continue;
            }

            preparedProperties.Add(binding.PropertyName);
        }

        foreach (var propertyName in preparedProperties
                     .OrderBy(GetApplyOrder)
                     .ThenBy(name => name, StringComparer.Ordinal))
        {
            if (state.TryApplyPrepared(propertyName))
            {
                applied++;
            }
            else
            {
                conversionFailures++;
            }
        }

        if (state.HasValues)
        {
            States.Add(control, state);
            state.Attach();
        }

        return new DesignerSampleApplyResult(applied, missing, conversionFailures);
    }

    public static void Clear(Control control)
    {
        if (!States.TryGetValue(control, out var state))
        {
            return;
        }

        States.Remove(control);
        state.Restore();
    }

    public static bool IsApplied(Control control) => States.TryGetValue(control, out _);

    private static int GetApplyOrder(string propertyName)
        => propertyName switch
        {
            "Minimum" or "Maximum" => 0,
            "ItemsSource" => 1,
            "SelectedIndex" or "SelectedItem" => 2,
            _ => 1,
        };

    private static bool TryResolvePath(DesignerSampleObject root, string path, out object? value)
    {
        value = root;
        foreach (var segment in path.Split('.'))
        {
            if (value is not DesignerSampleObject sample
                || !sample.TryGetValue(segment, out value))
            {
                value = null;
                return false;
            }
        }

        return true;
    }

    private static object? ConvertElement(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.Object => new DesignerSampleObject(element.EnumerateObject()
                .ToDictionary(
                    property => property.Name,
                    property => ConvertElement(property.Value),
                    StringComparer.Ordinal)),
            JsonValueKind.Array => element.EnumerateArray().Select(ConvertElement).ToList(),
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Number when element.TryGetInt64(out var integer) => integer,
            JsonValueKind.Number when element.TryGetDecimal(out var number) => number,
            JsonValueKind.Number => element.GetDouble(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            _ => null,
        };
    }

    private sealed class ControlSampleState
    {
        private readonly Control _control;
        private readonly Dictionary<string, PropertySampleState> _values = new(StringComparer.Ordinal);
        private bool _isWriting;

        public ControlSampleState(Control control)
        {
            _control = control;
        }

        public bool HasValues => _values.Count > 0;

        public bool TryPrepare(string propertyName, object? value)
        {
            var property = _control.GetType().GetProperty(
                propertyName,
                BindingFlags.Instance | BindingFlags.Public);
            if (property is null || !property.CanRead || !property.CanWrite
                || !TryConvertValue(value, property.PropertyType, out var converted))
            {
                return false;
            }

            var propertyState = PropertySampleState.Capture(_control, property, converted);
            _values[propertyName] = propertyState;
            return true;
        }

        public bool TryApplyPrepared(string propertyName)
        {
            if (!_values.TryGetValue(propertyName, out var propertyState))
            {
                return false;
            }

            try
            {
                Write(propertyState, sampleValue: true);
                return true;
            }
            catch (Exception ex) when (ex is ArgumentException
                                       or InvalidOperationException
                                       or TargetInvocationException)
            {
                _values.Remove(propertyName);
                Write(propertyState, sampleValue: false);
                return false;
            }
        }

        public void Attach() => _control.PropertyChanged += OnControlPropertyChanged;

        public void Restore()
        {
            _control.PropertyChanged -= OnControlPropertyChanged;
            foreach (var value in _values.Values
                         .OrderBy(state => GetApplyOrder(state.Property.Name)))
            {
                Write(value, sampleValue: false);
            }

            _values.Clear();
        }

        private void OnControlPropertyChanged(object? sender, Avalonia.AvaloniaPropertyChangedEventArgs e)
        {
            if (_isWriting || !_values.TryGetValue(e.Property.Name, out var state))
            {
                return;
            }

            state.CaptureExternalValue(_control);
            Write(state, sampleValue: true);
        }

        private void Write(PropertySampleState state, bool sampleValue)
        {
            _isWriting = true;
            try
            {
                if (state.Property.Name == "ItemsSource" && _control is ItemsControl itemsControl)
                {
                    itemsControl.ItemsSource = null;
                    itemsControl.Items.Clear();
                    var source = sampleValue ? state.SampleValue : state.OriginalValue;
                    if (source is not null)
                    {
                        itemsControl.ItemsSource = (IEnumerable)source;
                    }
                    else if (!sampleValue && state.OriginalItems is not null)
                    {
                        foreach (var item in state.OriginalItems)
                        {
                            itemsControl.Items.Add(item);
                        }
                    }

                    return;
                }

                state.Property.SetValue(
                    _control,
                    sampleValue ? state.SampleValue : state.OriginalValue);
            }
            finally
            {
                _isWriting = false;
            }
        }
    }

    private sealed class PropertySampleState
    {
        private PropertySampleState(
            PropertyInfo property,
            object? originalValue,
            object? sampleValue,
            IReadOnlyList<object?>? originalItems)
        {
            Property = property;
            OriginalValue = originalValue;
            SampleValue = sampleValue;
            OriginalItems = originalItems;
        }

        public PropertyInfo Property { get; }
        public object? OriginalValue { get; private set; }
        public object? SampleValue { get; }
        public IReadOnlyList<object?>? OriginalItems { get; private set; }

        public static PropertySampleState Capture(
            Control control,
            PropertyInfo property,
            object? sampleValue)
        {
            var originalValue = property.GetValue(control);
            IReadOnlyList<object?>? originalItems = null;
            if (property.Name == "ItemsSource"
                && control is ItemsControl itemsControl
                && originalValue is null)
            {
                originalItems = itemsControl.Items.Cast<object?>().ToList();
            }

            return new PropertySampleState(property, originalValue, sampleValue, originalItems);
        }

        public void CaptureExternalValue(Control control)
        {
            OriginalValue = Property.GetValue(control);
            if (Property.Name == "ItemsSource"
                && control is ItemsControl itemsControl
                && OriginalValue is null)
            {
                OriginalItems = itemsControl.Items.Cast<object?>().ToList();
            }
        }
    }

    private static bool TryConvertValue(object? value, Type targetType, out object? converted)
    {
        var actualType = Nullable.GetUnderlyingType(targetType) ?? targetType;
        if (value is null)
        {
            converted = null;
            return !targetType.IsValueType || Nullable.GetUnderlyingType(targetType) is not null;
        }

        if (targetType.IsInstanceOfType(value))
        {
            converted = value;
            return true;
        }

        if (typeof(IEnumerable).IsAssignableFrom(targetType)
            && value is IEnumerable enumerable
            && value is not string)
        {
            converted = enumerable;
            return true;
        }

        try
        {
            if (actualType == typeof(string))
            {
                converted = Convert.ToString(value, CultureInfo.InvariantCulture);
                return true;
            }

            if (actualType == typeof(DateTimeOffset)
                && DateTimeOffset.TryParse(
                    Convert.ToString(value, CultureInfo.InvariantCulture),
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.RoundtripKind,
                    out var date))
            {
                converted = date;
                return true;
            }

            if (actualType == typeof(TimeSpan)
                && TimeSpan.TryParse(
                    Convert.ToString(value, CultureInfo.InvariantCulture),
                    CultureInfo.InvariantCulture,
                    out var time))
            {
                converted = time;
                return true;
            }

            if (actualType.IsEnum
                && Enum.TryParse(
                    actualType,
                    Convert.ToString(value, CultureInfo.InvariantCulture),
                    ignoreCase: true,
                    out var enumValue))
            {
                converted = enumValue;
                return true;
            }

            converted = Convert.ChangeType(value, actualType, CultureInfo.InvariantCulture);
            return true;
        }
        catch (Exception ex) when (ex is FormatException or InvalidCastException or OverflowException)
        {
            converted = null;
            return false;
        }
    }
}
