﻿#nullable enable

using LogicBuilder.Expressions.Utils;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.OData.Edm;
using Microsoft.OData.UriParser;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

namespace AutoMapper.AspNet.OData;

internal static partial class ExpansionHelper
{
    public static List<List<PathSegment>> GetSelects<TModel>(this ODataQueryOptions<TModel> options)
    {
        Type parentType = typeof(TModel);
        IEdmModel edmModel = options.Context.Model;

        var selects = options.GetSelects<PathSelectItem>();

        if (!selects.Any())
        {
            // If there are no selects or only selects for expanded entities,
            // we need to expand the complex types on the root entity.
            return parentType.GetLiteralAndComplexSelects(edmModel);
        }        

        return selects.ToList().BuildSelectPaths(parentType, edmModel, new(), new());
    }

    private static List<PathSegment> ToNewList(this IEnumerable<PathSegment> pathSegments, IEdmModel edmModel) =>
        new(pathSegments.Select
        (
            p => new PathSegment
            (
                p.Member,
                p.ParentType,
                p.MemberType,
                p.EdmTypeKind,
                edmModel
            ))
        );

    public static List<List<PathSegment>> GetExpansions<TModel>(this ODataQueryOptions<TModel> options)
    {
        Type parentType = typeof(TModel);
        IEdmModel edmModel = options.Context.Model;
      
        return options.GetSelects<ExpandedNavigationSelectItem>()
            .ToList().BuildExpansionPaths(parentType, edmModel, new(), new());
    }

    private static List<List<PathSegment>> BuildExpansionPaths(
        this IReadOnlyList<ExpandedNavigationSelectItem> selectItems,
        Type parentType,
        IEdmModel edmModel,
        List<List<PathSegment>> paths,
        List<PathSegment> currentExpansionPath,
        int depth = 0)
    {
        if (!selectItems.Any())
            return paths;

        List<PathSegment> newExpansionPath = depth switch
        {
            > 0 => currentExpansionPath.ToNewList(edmModel),
            _ => currentExpansionPath
        };

        BuildPathSegments(selectItems.First(), newExpansionPath, depth);

        if (depth == 0 || !currentExpansionPath.Equals(newExpansionPath))
            paths.Add(newExpansionPath);

        foreach (var selectItem in selectItems.Skip(1))
        {
            paths.Add(BuildPathSegments(selectItem,
                newExpansionPath.Take(depth).ToNewList(edmModel), depth));
        }

        return paths;

        List<PathSegment> BuildPathSegments(ExpandedNavigationSelectItem pathSegments, List<PathSegment> path, int depth)
        {
            Type rootType = parentType;
            foreach (var pathSegment in pathSegments.PathToNavigationProperty)
            {
                MemberInfo member = rootType.GetMemberInfo(pathSegment.Identifier);
                Type memberType = member.GetMemberType();
                Type elementType = memberType.GetCurrentType();

                path.Add(new
                (                    
                    member,
                    rootType,
                    memberType,
                    pathSegment.EdmType.TypeKind,
                    edmModel,
                    pathSegment.GetFilter(pathSegments),
                    pathSegment.GetQuery(pathSegments),
                    pathSegment.GetSelects(pathSegments, elementType, edmModel)
                ));

                rootType = elementType;
            }
            
            pathSegments.GetSelects<ExpandedNavigationSelectItem>()
                .ToList().BuildExpansionPaths(rootType, edmModel, paths, path, depth + 1);

            return path;
        }       
    }

    private static List<List<PathSegment>> BuildSelectPaths(
        this IReadOnlyList<PathSelectItem> selectedPaths,        
        Type parentType,
        IEdmModel edmModel,
        List<List<PathSegment>> paths,
        List<PathSegment> currentPath,
        int depth = 0)
    {
        for (int i = 0; i < selectedPaths.Count; ++i)
        {
            List<PathSegment> segments = i == 0 ? currentPath : currentPath.Take(depth).ToList();
            segments = BuildPathSegments
            (
                selectedPaths[i], 
                segments, 
                depth
            );

            if (depth == 0 || !segments.Equals(currentPath))
            {
                if (!segments.Last().IsComplex)
                    paths.Add(segments);
                else
                {
                    PathSegment pathSegment = segments.Last();
                    Type memberType = pathSegment.ElementType;

                    var memberSelects = memberType.GetLiteralTypeMembers()
                        .Select(m => AddExpansion(m, EdmTypeKind.Primitive, new(segments)));

                    var complexPaths = edmModel.GetComplexTypeSelects(memberType).Select
                    (
                        paths =>
                        {
                            paths.InsertRange(0, segments);
                            return paths;
                        }
                    );

                    paths.AddRange(memberSelects.Concat(complexPaths));

                    List<PathSegment> AddExpansion(MemberInfo member, EdmTypeKind edmTypeKind, List<PathSegment> pathSegments)
                    {
                        pathSegments.Add(new
                        (
                            member,
                            member.DeclaringType!,
                            member.GetMemberType().GetCurrentType(),
                            edmTypeKind,
                            edmModel
                        ));
                        return pathSegments;
                    }
                }
            }
        }

        return paths;

        List<PathSegment> BuildPathSegments(PathSelectItem pathSegments, List<PathSegment> path, int depth)
        {
            Type rootType = parentType;
            foreach (var pathSegment in pathSegments.SelectedPath)
            {
                MemberInfo member = rootType.GetMemberInfo(pathSegment.Identifier);
                Type memberType = member.GetMemberType();
                Type elementType = memberType.GetCurrentType();

                path.Add(new
                (
                    member,
                    rootType,
                    memberType,
                    pathSegment.EdmType.AsElementType().TypeKind,
                    edmModel,
                    pathSegment.GetFilter(pathSegments),
                    pathSegment.GetQuery(pathSegments)
                ));

                rootType = elementType;
            }            

            pathSegments.GetSelects<PathSelectItem>().ToList()
                .BuildSelectPaths(rootType, edmModel, paths, path, depth + 1);

            return path;
        }        
    }

    private static List<List<PathSegment>> GetLiteralAndComplexSelects(this Type parentType, IEdmModel edmModel) =>
        parentType.GetLiteralSelects(edmModel).Concat(edmModel.GetComplexTypeSelects(parentType)).ToList();

    private static List<List<PathSegment>>? GetSelects(this ODataPathSegment pathSegment, ExpandedNavigationSelectItem pathSegments, Type parentType, IEdmModel edmModel)
    {
        if (pathSegments.PathToNavigationProperty.Last().Identifier.Equals(pathSegment.Identifier))
        {
            return pathSegments.GetSelects<PathSelectItem>().ToList() switch
            {
                var selects when selects.Any() => selects.BuildSelectPaths(parentType, edmModel, new(), new()),
                _ => parentType.GetLiteralAndComplexSelects(edmModel)
            };
        }
        return null;
    }

    private static FilterOptions? GetFilter(this ODataPathSegment pathSegment, ExpandedNavigationSelectItem pathSegments)
    {
        if (pathSegments.PathToNavigationProperty.Last().Identifier.Equals(pathSegment.Identifier)
            && pathSegments.FilterOption is not null)
        {
            return new(pathSegments.FilterOption);
        }
        return null;
    }

    private static QueryOptions? GetQuery(this ODataPathSegment pathSegment, ExpandedNavigationSelectItem pathSegments)
    {
        if (!pathSegments.PathToNavigationProperty.Last().Identifier.Equals(pathSegment.Identifier))
            return null;

        if (pathSegments.OrderByOption is not null || pathSegments.SkipOption.HasValue || pathSegments.TopOption.HasValue)
            return new(pathSegments.OrderByOption!, (int?)pathSegments.SkipOption, (int?)pathSegments.TopOption);

        return null;
    }

    private static FilterOptions? GetFilter(this ODataPathSegment pathSegment, PathSelectItem pathSegments)
    {
        if (pathSegments.SelectedPath.Last().Identifier.Equals(pathSegment.Identifier)
            && pathSegments.FilterOption is not null)
        {
            return new(pathSegments.FilterOption);
        }

        return null;
    }

    private static QueryOptions? GetQuery(this ODataPathSegment pathSegment, PathSelectItem pathSegments)
    {
        if (!pathSegments.SelectedPath.Last().Identifier.Equals(pathSegment.Identifier))
            return null;

        if (pathSegments.OrderByOption is not null || pathSegments.SkipOption.HasValue || pathSegments.TopOption.HasValue)
            return new(pathSegments.OrderByOption!, (int?)pathSegments.SkipOption, (int?)pathSegments.TopOption);

        return null;
    }

    private static IEnumerable<TPathType> GetSelects<TPathType>(this ExpandedNavigationSelectItem item) where TPathType : SelectItem =>
        item.SelectAndExpand?.SelectedItems.OfType<TPathType>() ?? Enumerable.Empty<TPathType>();

    private static IEnumerable<TPathType> GetSelects<TPathType>(this PathSelectItem item) where TPathType : SelectItem =>
        item.SelectAndExpand?.SelectedItems.OfType<TPathType>() ?? Enumerable.Empty<TPathType>();

    private static IEnumerable<TPathType> GetSelects<TPathType>(this ODataQueryOptions options) where TPathType : SelectItem =>
        options.SelectExpand?.SelectExpandClause?.SelectedItems.OfType<TPathType>() ?? Enumerable.Empty<TPathType>();
}
