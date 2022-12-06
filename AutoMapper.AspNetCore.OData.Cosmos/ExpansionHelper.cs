﻿#nullable enable

using LogicBuilder.Expressions.Utils;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.OData.Edm;
using Microsoft.OData.UriParser;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;


namespace AutoMapper.AspNet.OData;

internal static class ExpansionHelper
{
    public static List<List<ODataExpansionOptions>> GetExpansions<TModel>(this ODataQueryOptions<TModel> options)
    {
        var complexTypeNames = options.Context
            .GetEdmSchemaElementOfType<IEdmComplexType>()
            .Select(e => e.FullTypeName())
            .ToArray();
       
        var test = options.GetSelectedItems().GetExpansions<TModel>(complexTypeNames);

        return test;
    }

    private static List<List<ODataExpansionOptions>> GetExpansions<TModel>(
        this IEnumerable<SelectItem> selectedItems, IReadOnlyList<string> complexTypeNames)
    {
        Type parentType = typeof(TModel);
        List<List<ODataExpansionOptions>>? rootIncludes = null;

        // If there are no selects or only selects for expanded entities,
        // we need to expand the complex types on the root entity.
        if (!selectedItems.Any() || !selectedItems.Any(s => s is PathSelectItem))
        {
            rootIncludes = new();
            rootIncludes.ExpandComplex(null, parentType, complexTypeNames, null);

            // These types will be expanded later, ignore them for now.
            //var typesToIgnore = selectedItems
            //    .OfType<ExpandedNavigationSelectItem>()
            //    .SelectMany(s => s.PathToNavigationProperty)
            //    .Select(s => s.Identifier)
            //    .ToArray();
            //
            //var parentComplexMembers = parentType
            //    .GetComplexMembers(complexTypeNames)
            //    .Where(m => !typesToIgnore.Contains(m.Name));
        }

        var expansions = selectedItems.GetExpansions(parentType, complexTypeNames);

        if (rootIncludes is not null)
            expansions.InsertRange(0, rootIncludes);

        return expansions;
    }

    private static List<List<ODataExpansionOptions>> GetExpansions(
        this IEnumerable<SelectItem> selectedItems, Type parentType, IReadOnlyList<string> complexTypeNames)
    {
        return selectedItems.Aggregate(new List<List<ODataExpansionOptions>>(), (expansions, next) =>
        {
            Type currentParentType = parentType;
            List<ODataExpansionOptions> includes = new();

            foreach (var (pathSegment, index) in next.GetPathSegments().Select((p, i) => (p, i)))
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
                    FilterOptions = GetFilter()!,
                    QueryOptions = GetQuery()!,
                    Selects = GetSelects()
                });

                // Add any complex type expansions.
                expansions.ExpandComplex(new(includes), elementType, complexTypeNames, includes.Last().Selects);            
                currentParentType = elementType;


                FilterOptions? GetFilter()
                {
                    if (TryGetNavigationSelectItem(out var item) 
                        && memberType.IsList() && item.FilterOption is not null)
                    {
                        return new FilterOptions(item.FilterOption);
                    }
                    return null;
                }

                QueryOptions? GetQuery()
                {
                    if (TryGetNavigationSelectItem(out var item)
                        && (item.OrderByOption is not null || item.SkipOption.HasValue || item.TopOption.HasValue))
                    {
                        return new QueryOptions(item.OrderByOption!, (int?)item.SkipOption, (int?)item.TopOption);
                    }
                    return null;
                }

                List<string> GetSelects()
                {
                    if (TryGetNavigationSelectItem(out var item))
                        return item.SelectAndExpand.GetSelects();

                    return new();
                }

                bool TryGetNavigationSelectItem([MaybeNullWhen(false)] out ExpandedNavigationSelectItem item)
                {
                    if (pathSegment is NavigationPropertySegment)
                    {
                        item = (ExpandedNavigationSelectItem)next;
                        return true;
                    }
                    item = null;
                    return false;
                }
            }

            var navigationItem = next as ExpandedNavigationSelectItem;
            var navigationItems = navigationItem?.GetNestedExpansions(currentParentType, complexTypeNames).Select
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

    private static void ExpandComplex(
        this List<List<ODataExpansionOptions>> expansions, List<ODataExpansionOptions>? currentExpansions, Type parentType, IEnumerable<string> complexTypeNames, List<string>? selects)
    {
        int start = currentExpansions is null ? 0 : 1;
        ExpandComplexInternal(expansions, currentExpansions, parentType, complexTypeNames, start, start, selects);

        static void ExpandComplexInternal(List<List<ODataExpansionOptions>> expansions, List<ODataExpansionOptions>? currentExpansions,
            Type parentType, IEnumerable<string> complexTypeNames, int depth, in int start, List<string>? selects)
        {
            currentExpansions = currentExpansions is null ? new() : currentExpansions;
            var members = parentType.GetComplexMembers(complexTypeNames, selects);

            for (int i = 0; i < members.Count; ++i)
            {
                var member = members[i];
                var memberType = member.GetMemberType().GetCurrentType();

                var nextExpansions = AddExpansion(member, memberType, i == 0 ? currentExpansions : new(currentExpansions.Take(depth)));
                ExpandComplexInternal(expansions, nextExpansions, memberType, complexTypeNames, depth + 1, start, selects);

                if (!nextExpansions.Equals(currentExpansions) || depth == start)
                    expansions.Add(nextExpansions);
            }            
        }

        static List<ODataExpansionOptions> AddExpansion(MemberInfo member, Type memberType, List<ODataExpansionOptions> expansions)
        {
            expansions.Add(new()
            {
                MemberName = member.Name,
                MemberType = memberType,
                ParentType = member.DeclaringType,
                Selects = new()
            });
            return expansions;
        }
    }

#if false

    private static void ExpandComplex(
        this List<List<ODataExpansionOptions>> expansions, List<ODataExpansionOptions>? currentExpansions, Type parentType, IEnumerable<string> complexTypeNames, int depth, int start = 0)
    {        
        currentExpansions = currentExpansions is null ? new() : currentExpansions;
        var members = parentType.GetComplexMembers(complexTypeNames);

        for (int i = 0; i < members.Count; ++i)
        {
            var member = members[i];
            var memberType = member.GetMemberType().GetCurrentType();

            var nextExpansions = AddExpansion(member, memberType, i == 0 ? currentExpansions : new(currentExpansions.Take(depth)));
            expansions.ExpandComplex(nextExpansions, memberType, complexTypeNames, depth + 1);

            if (!nextExpansions.Equals(currentExpansions) || depth == start)
                expansions.Add(nextExpansions);
        }

        

        static List<ODataExpansionOptions> AddExpansion(MemberInfo member, Type memberType, List<ODataExpansionOptions> expansions)
        {
            expansions.Add(new()
            {
                MemberName = member.Name,
                MemberType = memberType,
                ParentType = member.DeclaringType,
                Selects = new()
            });
            return expansions;
        }

    }

#endif

    private static void ExpandComplexTypeHierarchy2(this List<ODataExpansionOptions> expansions, MemberInfo complexMember, IEnumerable<string> complexTypeNames)
    {
        var actualType = complexMember.GetMemberType().GetCurrentType();

        expansions.Add(new ODataExpansionOptions
        {
            MemberName = complexMember.Name,
            MemberType = actualType,
            ParentType = complexMember.DeclaringType,
            Selects = new()
        });

        foreach (var nextMember in actualType.GetComplexMembers(complexTypeNames))
            expansions.ExpandComplexTypeHierarchy2(nextMember, complexTypeNames);
    }

    private static List<List<ODataExpansionOptions>> GetNestedExpansions(this ExpandedNavigationSelectItem node, Type parentType, IReadOnlyList<string> complexTypeNames)
    {
        if (node is null)
            return new();

        return node.SelectAndExpand.SelectedItems
            .GetExpansions(parentType, complexTypeNames);
    }


    private static List<string> GetSelects(this SelectExpandClause clause)
    {
        if (clause == null)
            return new List<string>();

        return clause.SelectedItems
            .OfType<PathSelectItem>()
            .Select(item => item.SelectedPath.FirstSegment.Identifier)//Only first segment is necessary because of the new syntax $expand=Builder($expand=City) vs $expand=Builder/City
            .ToList();
    }

    private static IEnumerable<ODataPathSegment> GetPathSegments(this SelectItem selectItem) => 
        selectItem switch
        {
            PathSelectItem item => item.SelectedPath,
            ExpandedNavigationSelectItem item => item.PathToNavigationProperty,
            _ => throw new NotImplementedException()
        };

    private static IEnumerable<SelectItem> GetSelectedItems<TModel>(this ODataQueryOptions<TModel> options) =>
        options.SelectExpand?.SelectExpandClause?.SelectedItems ?? Enumerable.Empty<SelectItem>();
}
