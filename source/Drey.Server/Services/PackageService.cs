﻿using NuGet;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Drey.Server.Services
{
    public class PackageService : IPackageService
    {
        readonly IReleaseStore _releaseStore;
        readonly IFileService _fileService;

        public PackageService(IReleaseStore releaseStore, IFileService fileService)
        {
            _releaseStore = releaseStore;
            _fileService = fileService;
        }

        /// <summary>
        /// Gets an aggregate list of packages within the system.
        /// </summary>
        /// <returns></returns>
        public Task<IEnumerable<Models.Package>> GetPackagesAsync()
        {
            return _releaseStore.ListPackages();
        }

        /// <summary>
        /// Retrieves all known releases, based on a package id.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <returns></returns>
        public Task<IEnumerable<Models.Release>> GetReleasesAsync(string id)
        {
            return _releaseStore.ListByIdAsync(id);
        }

        /// <summary>
        /// Retrieves the nupkg based on its id and version, and prepares the nupkg for download by the client.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <param name="version">The version.</param>
        /// <returns></returns>
        /// <exception cref="System.IO.InvalidDataException"></exception>
        public async Task<Models.FileDownload> GetReleaseAsync(string id, string version)
        {
            var releaseInfo = await _releaseStore.GetAsync(id, version);

            if (releaseInfo == null) { throw new InvalidDataException(string.Format("Package {0} {1} does not exist.", id, version)); }

            var fileContent = await _fileService.DownloadBlobAsync(releaseInfo.RelativeUri);

            return new Models.FileDownload
            {
                FileContents = fileContent,
                MimeType = "application/zip",
                Filename = string.Format("{0}.{1}.nupkg", releaseInfo.Id, releaseInfo.Version)
            };
        }

        /// <summary>
        /// Syndicates the nupkg stream for use within the system.
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <returns></returns>
        public async Task<Models.Release> SyndicateAsync(Stream stream)
        {
            ZipPackage package = new ZipPackage(stream);
            var storageFilename = string.Format("{0}.{1}.nupkg", package.Id, package.Version);

            Models.Release release = await _releaseStore.GetAsync(package.Id, package.Version.ToString());

            if (release == null)
            {
                // assume we need to publish a new release.
                release = _releaseStore.Create();
            }

            var streamSHA = Utilities.CalculateChecksum(stream);
            if (!streamSHA.Equals(release.SHA1))
            {
                release.Description = package.Description;
                release.IconUrl = package.IconUrl;
                release.Id = package.Id;
                release.Listed = package.Listed;
                release.Published = DateTimeOffset.Now;
                release.ReleaseNotes = package.ReleaseNotes;
                release.Summary = package.Summary;
                release.Tags = package.Tags;
                release.Title = package.Title;
                release.Version = package.Version.ToString();
                release.SHA1 = streamSHA;

                // update store.
                await _fileService.DeleteAsync(storageFilename);

                release.RelativeUri = await _fileService.StoreAsync(storageFilename, package.GetStream());
            }

            return await _releaseStore.StoreAsync(release);
        }

        /// <summary>
        /// Deletes the requested package/version from storage.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <param name="version">The version.</param>
        /// <returns></returns>
        /// <exception cref="System.IO.FileNotFoundException">Package id/version does not exist.</exception>
        public async Task DeleteAsync(string id, string version)
        {
            var releaseInfo = await _releaseStore.GetAsync(id, version);
            if (releaseInfo == null) { throw new FileNotFoundException("Package id/version does not exist."); }

            await _releaseStore.DeleteAsync(id, version);
            await _fileService.DeleteAsync(releaseInfo.RelativeUri);
        }
    }
}