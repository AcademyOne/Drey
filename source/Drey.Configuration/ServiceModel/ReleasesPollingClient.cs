﻿using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Drey.Configuration.ServiceModel
{
    class ReleasesPollingClient : IPollingClient
    {
        /// <summary>
        /// The delay, in milliseconds, between queries to the known-packages endpoint to see if updates are available.
        /// </summary>
        const int DELAY_TIME_SEC = 5;

        readonly Drey.Nut.INutConfiguration _configurationManager;
        readonly Services.IGlobalSettingsService _globalSettingsService;
        readonly Services.PackageService _packageService;
        readonly DataModel.RegisteredPackage _package;
        readonly Drey.IPackageEventBus _packageEventBus;

        Task _pollingClientTask;
        CancellationToken _ct;

        public ReleasesPollingClient(Drey.Nut.INutConfiguration configurationManager, Services.IGlobalSettingsService globalSettingsService,
            Services.PackageService packageService, Drey.IPackageEventBus packageEventBus, DataModel.RegisteredPackage package)
        {
            _configurationManager = configurationManager;
            _globalSettingsService = globalSettingsService;
            _packageService = packageService;
            _packageEventBus = packageEventBus;
            _package = package;
        }

        public void Start(CancellationToken ct)
        {
            _ct = ct;
            _pollingClientTask = new Task(executePollingLoop, ct);
            _pollingClientTask.Start();
        }

        async void executePollingLoop()
        {
            while (!_ct.IsCancellationRequested)
            {
                try
                {
                    // poll the releases endpoint
                    var webClient = _globalSettingsService.GetHttpClient();
                    var queryForReleases = await webClient.GetAsync("/.well-known/releases/" + _package.PackageId);
                    var discoveredReleases = await queryForReleases.Content.ReadAsAsync<IEnumerable<DataModel.Release>>();

                    // diff the response with the known releases.
                    var newReleases = _packageService.Diff(_package.PackageId, discoveredReleases);

                    // if the response has new:
                    if (newReleases.Any())
                    {
                        newReleases.Apply(x => x.Package = _package);

                        // determine latest
                        var releaseToDownload = _packageService.GetReleases(_package).Concat(newReleases).OrderByDescending(pkg => pkg.Ordinal).SingleOrDefault();

                        // download latest, based on SHA (storage in {hordebasedir}\packages
                        var fileResult = await webClient.GetAsync("/.well-known/releases/download/" + releaseToDownload.SHA1);

                        fileResult.EnsureSuccessStatusCode();

                        var fileName = fileResult
                            .Content
                            .Headers
                            .First(x => x.Key.Equals("Content-Disposition", StringComparison.InvariantCultureIgnoreCase))
                            .Value
                            .Single()
                            .Replace("attachment; filename=", string.Empty).Replace("\"", string.Empty);

                        var destinationFileNameAndPath = Path.Combine(_configurationManager.HordeBaseDirectory, "packages", fileName);
                        var destinationFolder = Path.GetDirectoryName(destinationFileNameAndPath);

                        if (Directory.Exists(destinationFolder))
                        {
                            Directory.Delete(destinationFolder, true);
                        }
                        Directory.CreateDirectory(destinationFolder);

                        Console.WriteLine("File will be stored at: '{0}'", destinationFileNameAndPath);

                        using (var fStream = File.OpenWrite(destinationFileNameAndPath))
                        {
                            await fileResult.Content.CopyToAsync(fStream);
                        }

                        // unzip to {hoardbasedir}\{filename} without the extension
                        var zipFileInfo = new FileInfo(fileName);
                        var zipFolderName = zipFileInfo.Name;
                        zipFolderName = zipFolderName.Substring(0, zipFolderName.Length - zipFileInfo.Extension.Length);
                        ZipFile.ExtractToDirectory(destinationFileNameAndPath, Path.Combine(_configurationManager.HordeBaseDirectory, zipFolderName));

                        // signal shutdown of current version
                        _packageEventBus.Publish(new PackageEvents.Shutdown { PackageId = _package.PackageId });

                        // signal startup of new version.
                        _packageEventBus.Publish(new PackageEvents.Load { ConfigurationManager = new Infrastructure.ConfigurationManagement.DbConfigurationSettings(_packageEventBus, _configurationManager.ApplicationSettings), PackageId = _package.PackageId });

                        _packageService.RecordReleases(newReleases);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }

                Console.WriteLine("Waiting {0} seconds before checking for new releases.", DELAY_TIME_SEC);
                await Task.Delay(TimeSpan.FromSeconds(DELAY_TIME_SEC), _ct);
            }
        }
    }
}