﻿using Drey.Logging;

using System;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Drey.Nut
{
    class ProxyBase : MarshalByRefObject
    {
        static readonly ILog _Log = LogProvider.For<ProxyBase>();
        protected ILog Log { get { return _Log; } }

        readonly string _pathToAppPackage;

        public ProxyBase(string pathToAppPackage)
        {
            _pathToAppPackage = pathToAppPackage;
        }

        public Assembly ResolveAssemblyInDomain(object sender, ResolveEventArgs args)
        {
            var asmName = args.Name + ".dll";

            Log.Info(() => "Attempting to resolve " + asmName);

            var searchPaths = (new[] 
            { 
                    Path.GetFullPath(_pathToAppPackage), 
                    Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) 
            });

            Log.Debug(() => string.Format("Searching the following locations: {0}", searchPaths.Aggregate((s1, s2) => s1 + ";" + s2)));

            var resolvedDll = searchPaths
                .Select(fullPath => Path.Combine(fullPath, asmName))
                .Select(fullPath => File.Exists(fullPath) ? Assembly.LoadFrom(fullPath) : null)
                .Where(asm => asm != null)
                .FirstOrDefault();

            Log.Trace(() => "Has Dll been resolved? " + (resolvedDll != null ? "Yes" : "No"));

            return resolvedDll;
        }
    }
}