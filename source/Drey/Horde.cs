﻿using Drey.Nut;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Drey
{
    public class Horde : IDisposable
    {
        INutConfiguration _nutConfiguration = new ApplicationHostNutConfiguration();
        List<IShell> _shells = new List<IShell>();
        string _dreyConfigurationPackagePath;

        public void Startup()
        {
            DiscoverConfigurationPackage();

            var shell = ShellFactory.Create(_dreyConfigurationPackagePath);
            shell.ShellCallback += shell_ShellCallback;
            _shells.Add(shell);
        }

        void shell_ShellCallback(object sender, ShellEventArgs e)
        {
            
        }

        private void DiscoverConfigurationPackage()
        {
            // discover the main horde folder
#if DEBUG
            _dreyConfigurationPackagePath  = _nutConfiguration.HordeBaseDirectory;
#else
            var configurationPath = Path.Combine(_nutConfiguration.HordeBaseDirectory + "drey.configuration");

            // discover the latest version
            var versionFolders = Directory.GetDirectories(configurationPath).Select(dir => (new DirectoryInfo(dir)).Name);
            var versions = versionFolders.Select(ver => new Version(ver));
            var latestVersion = versionFolders.OrderByDescending(x => x).First();

            _dreyConfigurationPackagePath = Path.Combine(configurationPath, latestVersion.ToString());
#endif
        }

        public void Dispose()
        {
            foreach (var shell in _shells)
            {
                shell.ShellCallback -= shell_ShellCallback;
                shell.Dispose();
            }
        }
    }
}