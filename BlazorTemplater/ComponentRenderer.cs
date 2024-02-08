using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

namespace BlazorTemplater;

/*
 * Adapted from ParameterViewBuilder.cs in Egil Hansen's Genzor
 * https://github.com/egil/genzor/blob/main/src/genzor/ParameterViewBuilder.cs
 * Thanks for the suggestion, Egil!
 */

public class ComponentRenderer
{
    private const string ChildContent = nameof(ChildContent);
    private readonly Type TComponentType;

    private readonly Dictionary<string, object> parameters = new(StringComparer.Ordinal);
    private readonly Templater templater;

    #region Ctor

    /// <summary>
    /// Create a new renderer
    /// </summary>
    public ComponentRenderer(Type type)
    {
        templater = new Templater();
        TComponentType = type;
    }

    #endregion Ctor

    #region Services

    /// <summary>
    /// Add a service provider
    /// </summary>
    /// <param name="serviceProvider">a new IServiceProvider used when resolving dependencies</param>
    /// <returns></returns>
    public ComponentRenderer AddServiceProvider(IServiceProvider serviceProvider)
    {
        templater.AddServiceProvider(serviceProvider);
        return this;
    }

    /// <summary>
    /// Fluent add-service with contract and implementation
    /// </summary>
    /// <typeparam name="TContract"></typeparam>
    /// <typeparam name="TImplementation"></typeparam>
    /// <param name="implementation"></param>
    /// <returns></returns>
    public ComponentRenderer AddService<TContract, TImplementation>(TImplementation implementation) where TImplementation : TContract

    {
        templater.AddService<TContract, TImplementation>(implementation);
        return this;
    }

    /// <summary>
    /// Fluent add-service with implemention
    /// </summary>
    /// <typeparam name="TImplementation"></typeparam>
    /// <param name="implementation"></param>
    /// <returns></returns>
    public ComponentRenderer AddService<TImplementation>(TImplementation implementation)

    {
        templater.AddService<TImplementation>(implementation);
        return this;
    }

    #endregion Services

    #region Set Parameters

    /// <summary>
    /// Sets the <paramref name="value"/> to the parameter selected with the <paramref name="parameterSelector"/>.
    /// </summary>
    /// <typeparam name="TValue">Type of <paramref name="value"/>.</typeparam>
    /// <param name="parameterSelector">A lambda function that selects the parameter.</param>
    /// <param name="value">The value to pass to <typeparamref name="TComponent"/>.</param>
    /// <returns>This <see cref="ComponentRenderer{TComponent}"/> so that additional calls can be chained.</returns>
    public ComponentRenderer Set<TValue>(Expression<Func<object, TValue>> parameterSelector, TValue value)
    {
        if (value is null)
            throw new ArgumentNullException(nameof(value));

        parameters.Add(GetParameterName(parameterSelector), value);
        return this;
    }

    private string GetParameterName<TValue>(Expression<Func<object, TValue>> parameterSelector)
    {
        if (parameterSelector is null)
            throw new ArgumentNullException(nameof(parameterSelector));

        if (parameterSelector.Body is not MemberExpression { Member: PropertyInfo propInfoCandidate })
            throw new ArgumentException($"The parameter selector '{parameterSelector}' does not resolve to a public property on the component '{TComponentType}'.", nameof(parameterSelector));

        var propertyInfo = propInfoCandidate.DeclaringType != TComponentType
            ? TComponentType.GetProperty(propInfoCandidate.Name, propInfoCandidate.PropertyType)
            : propInfoCandidate;

        var paramAttr = propertyInfo?.GetCustomAttribute<ParameterAttribute>(inherit: true);

        if (propertyInfo is null || paramAttr is null)
            throw new ArgumentException($"The parameter selector '{parameterSelector}' does not resolve to a public property on the component '{TComponentType}' with a [Parameter] or [CascadingParameter] attribute.", nameof(parameterSelector));

        return propertyInfo.Name;
    }

    #endregion Set Parameters

    #region Layout

    /// <summary>
    /// Set the layout to use when rendering
    /// </summary>
    /// <typeparam name="TLayout">The Layout type - should inherit LayoutComponentBase</typeparam>
    /// <returns>self</returns>
    public ComponentRenderer UseLayout<TLayout>() where TLayout : LayoutComponentBase
    {
        templater.UseLayout<TLayout>();
        return this;
    }

    /// <summary>
    /// Set the layout to use when rendering
    /// </summary>
    /// <typeparam name="layoutType">The Layout type - should inherit LayoutComponentBase</typeparam>
    /// <returns>self</returns>
    public ComponentRenderer UseLayout(Type layoutType)
    {
        templater.UseLayout(layoutType);
        return this;
    }

    #endregion Layout

    /// <summary>
    /// Render the component to HTML
    /// </summary>
    /// <returns></returns>
    public string Render()
    {
        // renders the component and returns the markup HTML
        return templater.RenderComponent(TComponentType, parameters);
    }
}