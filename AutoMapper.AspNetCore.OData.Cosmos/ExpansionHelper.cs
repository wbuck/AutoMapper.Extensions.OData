#nullable enable

using LogicBuilder.Expressions.Utils;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.OData.Edm;
using Microsoft.OData.UriParser;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace AutoMapper.AspNet.OData;

internal static class ExpansionHelper
{
    public static List<List<ODataExpansionOptions>> GetExpansions<TModel>(this ODataQueryOptions<TModel> options)
    {
        var complexTypeNames = options.Context
            .GetEdmSchemaElementOfType<IEdmComplexType>()
            .Select(e => e.Name)
            .ToArray();
       
        return options.GetSelectedItems().GetExpansions<TModel>(complexTypeNames);
    }

    private static List<List<ODataExpansionOptions>> GetExpansions<TModel>(
        this IEnumerable<SelectItem> selectedItems, IReadOnlyList<string> complexTypeNames)
    {
        Type parentType = typeof(TModel);
        List<List<ODataExpansionOptions>>? rootIncludes = null;

        // If there are no selects or only selects for expanded entities,
        // we need to expand the complex types on the root entity.
        if (!selectedItems.Any() || !selectedItems.Any(s => s is PathSelectItem))        
            rootIncludes = parentType.ExpandComplexTypes(complexTypeNames);

        var pathSegments = selectedItems.ToExpansionSegments();
        var expansions = pathSegments.GetExpansions(parentType, complexTypeNames);

        if (rootIncludes is not null)
            expansions.InsertRange(0, rootIncludes);

        return expansions;
    }

    private static List<List<ODataExpansionOptions>> GetExpansions(
        this IImmutableSet<SelectPathSegments> selectedItems, Type parentType, IReadOnlyList<string> complexTypeNames)
    {
        return selectedItems.Aggregate(new List<List<ODataExpansionOptions>>(), (expansions, pathSegments) =>
        {
            Type currentParentType = parentType;
            List<ODataExpansionOptions> includes = new();

            foreach (var pathSegment in pathSegments)
            {
                Type memberType = currentParentType.GetMemberInfo(pathSegment.Identifier).GetMemberType();
                Type elementType = memberType.GetCurrentType();

                if (elementType.IsLiteralType())                
                    continue;

                includes.Add(new()
                {
                    MemberType = memberType,
                    ParentType = currentParentType,
                    MemberName = pathSegment.Identifier,
                    FilterOptions = pathSegment.GetFilter(memberType)!,
                    QueryOptions = pathSegment.GetQuery()!,
                    Selects = pathSegment.GetSelects()
                });

                if (!pathSegment.IsNavigationPropertySegment || (pathSegment.IsNavigationPropertySegment && !pathSegment.HasSelects()))
                {
                    var complexExpansions = elementType.ExpandComplexTypes(complexTypeNames).Select
                    (
                        expansions =>
                        {
                            expansions.InsertRange(0, includes);
                            return expansions;
                        }
                    ).ToList();

                    if (complexExpansions.Any())
                        expansions.AddRange(complexExpansions);
                }

                currentParentType = elementType;
            }

            var navigationItems = pathSegments.GetNestedExpansions(currentParentType, complexTypeNames).Select
            (
                expansions =>
                {
                    if (includes.Any())
                        expansions.InsertRange(0, includes);
                
                    return expansions;
                }
            ).ToList();

            if (navigationItems is not null && navigationItems.Any())
                expansions.AddRange(navigationItems);
            else if (includes.Any())
                expansions.Add(includes);

            return expansions;            
        });
    }

    private static List<List<ODataExpansionOptions>> GetNestedExpansions(in this SelectPathSegments pathSegments, Type parentType, IReadOnlyList<string> complexTypeNames)
    {
        var selects = pathSegments.GetNestedExpansionSegments();
        return selects.GetExpansions(parentType, complexTypeNames);
    }

    private static IImmutableSet<SelectPathSegments> ToExpansionSegments(
        this IEnumerable<SelectItem> selectItems) => selectItems
            .OrderByDescending(s => s is ExpandedNavigationSelectItem)
            .Select(s => new SelectPathSegments(s))
            .ToImmutableHashSet();

    private static IEnumerable<SelectItem> GetSelectedItems<TModel>(this ODataQueryOptions<TModel> options) =>
        options.SelectExpand?.SelectExpandClause?.SelectedItems ?? Enumerable.Empty<SelectItem>();
}
