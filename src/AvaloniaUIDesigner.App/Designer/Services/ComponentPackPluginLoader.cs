using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Text.Json;
using AvaloniaUIDesigner.App.Designer.Contracts;
using AvaloniaUIDesigner.App.Designer.Core;
using AvaloniaUIDesigner.App.Models;

namespace AvaloniaUIDesigner.App.Designer.Services;

public sealed class ComponentPackPluginLoader
{
    private static readonly Dictionary<string, Assembly> LoadedAssemblies = new(StringComparer.OrdinalIgnoreCase);
    private readonly ComponentPackLoader _componentPackLoader = new();

    public bool TryLoad(
        string assemblyPath,
        IComponentCatalog catalog,
        Func<string, bool> isDisplayNameAvailable,
        out ComponentPackLoadResult pack,
        out string error)
    {
        pack = default!;
        error = string.Empty;

        if (string.IsNullOrWhiteSpace(assemblyPath))
        {
            error = "Component pack plugin path is required.";
            return false;
        }

        string fullPath;
        try
        {
            fullPath = Path.GetFullPath(assemblyPath.Trim());
        }
        catch (Exception exception)
        {
            error = $"Component pack plugin path is invalid: {exception.Message}";
            return false;
        }

        if (!File.Exists(fullPath))
        {
            error = $"Component pack plugin was not found: {fullPath}";
            return false;
        }

        try
        {
            var assembly = LoadAssembly(fullPath);
            var pluginTypes = GetLoadableTypes(assembly)
                .Where(type => type is { IsClass: true, IsAbstract: false, IsPublic: true }
                    && typeof(IComponentPackPlugin).IsAssignableFrom(type))
                .ToList();
            if (pluginTypes.Count != 1)
            {
                error = pluginTypes.Count == 0
                    ? "Component pack plugin must expose one public IComponentPackPlugin implementation."
                    : "Component pack plugin must expose exactly one IComponentPackPlugin implementation.";
                return false;
            }

            if (pluginTypes[0].GetConstructor(Type.EmptyTypes) is null)
            {
                error = "Component pack plugin must have a public parameterless constructor.";
                return false;
            }

            var plugin = (IComponentPackPlugin)Activator.CreateInstance(pluginTypes[0])!;
            var document = plugin.CreatePack();
            if (document is null)
            {
                error = "Component pack plugin returned no pack.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(document.Name))
            {
                document.Name = string.IsNullOrWhiteSpace(plugin.Name)
                    ? Path.GetFileNameWithoutExtension(fullPath)
                    : plugin.Name.Trim();
            }

            var json = JsonSerializer.Serialize(document, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            });
            return _componentPackLoader.TryLoad(
                json,
                catalog,
                isDisplayNameAvailable,
                out pack,
                out error);
        }
        catch (ReflectionTypeLoadException exception)
        {
            var details = string.Join(
                " ",
                exception.LoaderExceptions
                    .Where(loaderException => loaderException is not null)
                    .Select(loaderException => loaderException!.Message));
            error = string.IsNullOrWhiteSpace(details)
                ? "Component pack plugin types could not be loaded."
                : $"Component pack plugin types could not be loaded: {details}";
            return false;
        }
        catch (Exception exception)
        {
            error = $"Component pack plugin could not be loaded: {exception.Message}";
            return false;
        }
    }

    private static Assembly LoadAssembly(string fullPath)
    {
        lock (LoadedAssemblies)
        {
            if (LoadedAssemblies.TryGetValue(fullPath, out var loadedAssembly))
            {
                return loadedAssembly;
            }

            var assembly = AssemblyLoadContext.Default.LoadFromAssemblyPath(fullPath);
            LoadedAssemblies[fullPath] = assembly;
            return assembly;
        }
    }

    private static IEnumerable<Type> GetLoadableTypes(Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException exception)
        {
            return exception.Types.Where(type => type is not null).Cast<Type>();
        }
    }
}
