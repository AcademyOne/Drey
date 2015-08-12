﻿using Drey.Server.Services;
using Nancy;
using System.Collections.Generic;
using System.Linq;

namespace Drey.Server.Modules.well_known
{
    public class ReleasesModule : NancyModule
    {
        readonly IPackageService _packageService;

        public ReleasesModule(IPackageService packageService) : base("/.well-known/releases")
        {
            _packageService = packageService;

            Get["/{packageId}"] = GetReleases;
            Get["/download/{sha}"] = GetRelease;
            Post["/{packageId}"] = StoreRelease;
            Delete["/{packageId}"] = DeleteRelease;
        }

        private dynamic StoreRelease(dynamic arg)
        {
            if (!Request.Files.Any())
            {
                return ((Response)"Must upload a file.").StatusCode = HttpStatusCode.BadRequest;
            }

            var file = Request.Files.First();
            var packageId = (string)arg.packageId;

            if (_packageService.CreateRelease(packageId, file.Name, file.Value))
            {
                return HttpStatusCode.Created;
            };
            return ((Response)"Your release was not created").StatusCode = HttpStatusCode.BadRequest;
        }

        private dynamic GetReleases(dynamic args)
        {
            try
            {
                return _packageService.GetReleases(args.packageId);
            }
            catch (KeyNotFoundException ex)
            {
                return ((Response)ex.Message).StatusCode = HttpStatusCode.NotFound;
            }
        }

        private dynamic GetRelease(dynamic arg)
        {
            var file = _packageService.GetRelease((string)arg.sha);
            var response = Response.FromStream(file.FileContents, file.MimeType);
            response.Headers.Add("Content-Disposition", "attachment; filename=\"" + file.Filename + "\"");
            return response;
        }

        private dynamic DeleteRelease(dynamic arg)
        {
            throw new System.NotImplementedException();
        }
    }
}