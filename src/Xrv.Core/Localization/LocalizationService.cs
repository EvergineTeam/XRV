// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using Evergine.Common.Attributes;
using Evergine.Framework;
using Evergine.Framework.Services;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Resources;
using Xrv.Core.Messaging;

namespace Xrv.Core.Localization
{
    /// <summary>
    /// Service for UI localization.
    /// </summary>
    public class LocalizationService : Service
    {
        private const string ResourceExtension = ".resource";
        private const string NotFoundString = "<Not found>";
        private readonly Dictionary<string, ResourceManager> assemblyResources;
        private readonly HashSet<CultureInfo> availableCultures;

        [BindService]
        private XrvService xrvService = null;

        private PubSub pubSub;

        static LocalizationService()
        {
            SearchAssemblies = AppDomain.CurrentDomain
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
                .ToArray();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LocalizationService"/> class.
        /// </summary>
        public LocalizationService()
        {
            this.assemblyResources = new Dictionary<string, ResourceManager>();
            this.availableCultures = new HashSet<CultureInfo>();
            this.availableCultures.Add(CultureInfo.CreateSpecificCulture("en"));
            this.availableCultures.Add(CultureInfo.CreateSpecificCulture("es"));
        }

        /// <summary>
        /// Gets or sets current UI culture.
        /// </summary>
        public CultureInfo CurrentCulture
        {
            get => CultureInfo.CurrentUICulture;

            set
            {
                if (CultureInfo.CurrentUICulture != value)
                {
                    CultureInfo.CurrentUICulture = value;
                    CultureInfo.CurrentCulture = value;

                    this.pubSub?.Publish(new CurrentCultureChangeMessage(value));
                }
            }
        }

        public HashSet<CultureInfo> AvailableCultures { get => this.availableCultures; }

        internal static Assembly[] SearchAssemblies { get; private set; }

        /// <summary>
        /// Registers resource dictionaries for a given assembly.
        /// </summary>
        /// <param name="assembly">Target assembly.</param>
        public void RegisterAssembly(Assembly assembly)
        {
            var resources = assembly
                .GetManifestResourceNames()
                .Select(resource => resource.Substring(0, resource.Length - ResourceExtension.Length - 1))
                .ToArray();

            foreach (var resourceName in resources)
            {
                this.assemblyResources[resourceName] = new ResourceManager(resourceName, assembly);
            }
        }

        /// <summary>
        /// Gets a localized string from its dictionary name-key pair.
        /// </summary>
        /// <param name="dictionaryName">Dictionary name.</param>
        /// <param name="key">Dictionary entry key.</param>
        /// <returns>Matched string from dictionary. It relies on .NET
        /// resources dictionaries, following their rules. If a key is not
        /// found for specified dictionary, it returns an error string.
        /// </returns>
        /// <exception cref="ArgumentOutOfRangeException">Raised when provided dictionary name is not found.</exception>
        public string GetString(string dictionaryName, string key)
        {
            if (!this.assemblyResources.ContainsKey(dictionaryName))
            {
                throw new ArgumentOutOfRangeException(nameof(dictionaryName), $"Dictionary {dictionaryName} not found");
            }

            var manager = this.assemblyResources[dictionaryName];
            var resourceSet = manager.GetResourceSet(this.CurrentCulture, true, true);
            var entry = resourceSet.GetString(key);

            return entry ?? NotFoundString;
        }

        public string GetString<TDictionary>(Expression<Func<TDictionary>> expr)
            where TDictionary : class
        {
            var member = (expr.Body as MemberExpression).Member;
            var key = member.Name.Replace("_", ".");

            return this.GetString(member.DeclaringType.FullName, key);
        }

        internal void LoadSearchAssemblies()
        {
            foreach (var assembly in SearchAssemblies)
            {
                this.RegisterAssembly(assembly);
            }
        }

        internal IEnumerable<string> GetAllDictionaries() =>
            this.assemblyResources.Keys;

        internal IEnumerable<string> GetAllKeysByDictionary(string dictionaryName)
        {
            if (!this.assemblyResources.ContainsKey(dictionaryName))
            {
                throw new InvalidOperationException($"Dictionary {dictionaryName} not found");
            }

            var manager = this.assemblyResources[dictionaryName];
            var resourceSet = manager.GetResourceSet(CultureInfo.InvariantCulture, true, true);

            foreach (DictionaryEntry entry in resourceSet)
            {
                if (entry.Key is string key)
                {
                    yield return key;
                }
            }
        }

        /// <inheritdoc/>
        protected override void Start()
        {
            base.Start();
            this.LoadSearchAssemblies();
            this.pubSub = this.xrvService.PubSub;
        }
    }
}
