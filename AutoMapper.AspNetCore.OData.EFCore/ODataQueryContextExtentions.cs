#nullable enable

using Microsoft.AspNetCore.OData.Query;
using Microsoft.OData.Edm;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AutoMapper.AspNet.OData
{
    internal static class ODataQueryContextExtentions
    {
        public static OrderBySetting? FindSortableProperties(this ODataQueryContext context, Type type)
        {
            context = context ?? throw new ArgumentNullException(nameof(context));

            IEdmSchemaElement? schemaElement = GetSchemaElement();

            if (schemaElement is null)
                throw new InvalidOperationException($"The type '{type.FullName}' has not been declared in the entity data model.");

            return FindProperties(schemaElement);
            

            IEdmSchemaElement? GetSchemaElement() =>
                context.Model.SchemaElements
                    .FirstOrDefault(e => (e is IEdmEntityType || e is IEdmComplexType) && e.Name == type.Name);
            
            static OrderBySetting? FindProperties(IEdmSchemaElement schemaElement)
            {
                var propertyNames = schemaElement switch
                {
                    IEdmEntityType entityType when entityType.ContainsKey() => entityType.Key().Select(p => p.Name),
                    IEdmStructuredType structuredType => structuredType.GetSortableProperties().Take(1),
                    _ => throw new NotSupportedException("The EDM element type is not supported.")
                };

                var orderBySettings = new OrderBySetting();
                propertyNames.Aggregate(orderBySettings, (settings, name) =>
                {
                    if (settings.Name is null)
                    {
                        settings.Name = name;
                        return settings;
                    }
                    settings.ThenBy = new() { Name = name };
                    return settings.ThenBy;
                });
                return orderBySettings.Name is null ? null : orderBySettings;
            }                              
        }

        private static bool ContainsKey(this IEdmEntityType entityType)
            => entityType.Key().Any();

        private static IEnumerable<string> GetSortableProperties(this IEdmStructuredType structuredType) 
            => structuredType.StructuralProperties()
                .Where(p => p.Type.IsPrimitive() && !p.Type.IsStream())
                .Select(p => p.Name)
                .OrderBy(n => n);
    }
}
