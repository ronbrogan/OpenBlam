﻿using Microsoft.CodeAnalysis;
using System;
using System.Linq;

namespace OpenBlam.Serialization
{
    internal static class CodeAnalysisUtilities
    {
        public static INamedTypeSymbol GetTypeSymbol<T>(this Compilation compilation)
        {
            return compilation.GetTypeSymbol(typeof(T));
        }

        public static INamedTypeSymbol GetTypeSymbol(this Compilation compilation, Type t)
        {
            Type[] genericArgs = null;

            if(t.IsGenericType)
            {
                genericArgs = t.GetGenericArguments();
                t = t.GetGenericTypeDefinition();
            }

            var found = compilation.References.Select(compilation.GetAssemblyOrModuleSymbol)
                .OfType<IAssemblySymbol>()
                .Select(a => a.GetTypeByMetadataName(t.FullName))
                .Single(a => a != null);

            if(genericArgs?.Length > 0)
            {
                var genericSymbols = genericArgs.Select(a => GetTypeSymbol(compilation, a)).ToArray();
                found = found.Construct(genericSymbols);
            }

            return found;
        }
    }
}
