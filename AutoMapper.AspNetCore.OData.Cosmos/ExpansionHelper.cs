#nullable enable

using LogicBuilder.Expressions.Utils;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.OData.Edm;
using Microsoft.OData.UriParser;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

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
            rootIncludes = ExpandComplex(parentType, complexTypeNames);

        var pathSegments = selectedItems.ToExpansionSegments();
        var expansions = pathSegments.GetExpansions(parentType, complexTypeNames);

        if (rootIncludes is not null)
            expansions.InsertRange(0, rootIncludes);

        return expansions;
    }

    private static List<List<ODataExpansionOptions>> GetExpansions(
        this ISet<ExpansionSegments> selectedItems, Type parentType, IReadOnlyList<string> complexTypeNames)
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

                if (!pathSegment.IsNavigationPropertySegment || (pathSegment.IsNavigationPropertySegment && !includes.Last().Selects.Any()))
                {
                    var complexExpansions = ExpandComplex(elementType, complexTypeNames).Select
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

            ExpandedNavigationSelectItem? navigationItem = pathSegments;
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


    /// <summary>
    /// Finds all of the complex types in the object hierarchy and builds paths from the base object 
    /// (the object whose declaring type is the same as 'parentType') to the complex type. 
    /// These paths are then used to include those complex objects in the SQL query.
    /// </summary>
    /// 
    /// <remarks>   
    /// Given the classes below with the 'parentType' set to typeof(Entity)
    /// the following include paths will be returned:
    /// 
    /// [0]: FirstComplex/SecondComplex/ThirdComplex
    /// [1]: FirstComplex/FourthComplex    
    /// 
    /// The first segment of both paths (FirstComplex) is the "base object"
    /// whose declaring type is equal to typeof(Entity).
    /// </remarks>
    /// 
    /// <example>        
    /// <![CDATA[
    /// class FourthComplex {}
    /// 
    /// class ThirdComplex {}
    /// 
    /// class SecondComplex
    /// {
    ///     public ThirdComplex ThirdComplex {get; set;} 
    /// }
    /// 
    /// class FirstComplex
    /// {
    ///     public SecondComplex SecondComplex {get; set;}
    ///     public FourthComplex FourthComplex {get; set;}
    /// }
    /// 
    /// class Entity 
    /// {
    ///     public Guid Id {get; set;}
    ///     public FirstComplex FirstComplex {get; set;}
    /// }
    /// ]]>
    /// </example>
    private static List<List<ODataExpansionOptions>> ExpandComplex(Type parentType, IEnumerable<string> complexTypeNames)
    {
        List<List<ODataExpansionOptions>> expansions = new();
        return ExpandComplexInternal(expansions, new(), parentType, complexTypeNames);

        static List<List<ODataExpansionOptions>> ExpandComplexInternal(List<List<ODataExpansionOptions>> expansions, List<ODataExpansionOptions> currentExpansions,
            Type parentType, IEnumerable<string> complexTypeNames, int depth = 0)
        {
            var members = parentType.GetComplexMembers(complexTypeNames);            

            for (int i = 0; i < members.Count; ++i)
            {
                var member = members[i];
                var memberType = member.GetMemberType().GetCurrentType();

                var nextExpansions = AddExpansion
                (
                    member, memberType, i == 0 ? currentExpansions : new(currentExpansions.Take(depth))
                );

                ExpandComplexInternal(expansions, nextExpansions, memberType, complexTypeNames, depth + 1);

                if (!nextExpansions.Equals(currentExpansions) || depth == 0)
                    expansions.Add(nextExpansions);
            }

            return expansions;
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

    private static List<List<ODataExpansionOptions>> GetNestedExpansions(this ExpandedNavigationSelectItem node, Type parentType, IReadOnlyList<string> complexTypeNames)
    {
        if (node is null)
            return new();

        var pathSegments = node.SelectAndExpand.SelectedItems.ToExpansionSegments();
        return pathSegments.GetExpansions(parentType, complexTypeNames);
    }

    private static ISet<ExpansionSegments> ToExpansionSegments(
        this IEnumerable<SelectItem> selectItems) => selectItems
            .Where(s => s is ExpandedNavigationSelectItem)
            .Concat(selectItems.OfType<PathSelectItem>())
            .Select(s => new ExpansionSegments(s))
            .ToHashSet();

    private static IEnumerable<SelectItem> GetSelectedItems<TModel>(this ODataQueryOptions<TModel> options) =>
        options.SelectExpand?.SelectExpandClause?.SelectedItems ?? Enumerable.Empty<SelectItem>();
}
