// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using Evergine.Common.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Evergine.Xrv.Core.Themes.Texts
{
    internal static class TextStylesRegister
    {
        static TextStylesRegister()
        {
            var textStyles = new Dictionary<string, TextStyle>();
            var registrationType = typeof(ITextStyleRegistration);
            var allRegistrations = AppDomain.CurrentDomain
                .GetAssemblies()
                .Where(assembly => assembly
                    .GetCustomAttributes()
                    .Any(attribute =>
                    {
                        if (attribute is EvergineAssembly evergine)
                        {
                            return evergine.Type == EvergineAssemblyUsage.UserProject
                                || evergine.Type == EvergineAssemblyUsage.Extension;
                        }

                        return false;
                    }))
                .SelectMany(assembly => assembly.GetTypes())
                .Where(type => type.IsClass && registrationType.IsAssignableFrom(type))
                .ToArray();

            foreach (var regType in allRegistrations)
            {
                var registration = (ITextStyleRegistration)Activator.CreateInstance(regType);
                registration.Register(textStyles);
            }

            TextStyles = textStyles;
        }

        public static Dictionary<string, TextStyle> TextStyles { get; private set; }
    }
}
