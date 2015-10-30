﻿using Drey.Logging;
using Drey.Nut;

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Drey.Configuration.ServiceModel
{
    class ListLogsService : RemoteInvocationService<DomainModel.Request<DomainModel.Empty>, DomainModel.Empty, DomainModel.Response<IEnumerable<string>>, IEnumerable<string>>
    {
        readonly INutConfiguration _configurationManager;

        public ListLogsService(INutConfiguration configurationManager) : base("BeginListLogFiles", "EndListLogFiles")
        {
            _configurationManager = configurationManager;
        }
        protected override Task<DomainModel.Response<IEnumerable<string>>> ProcessAsync(DomainModel.Request<DomainModel.Empty> request)
        {
            var absoluteLogsPath = Drey.Utilities.PathUtilities.MapPath(_configurationManager.LogsDirectory);

            if (!Directory.Exists(absoluteLogsPath)) { Directory.CreateDirectory(absoluteLogsPath); }

            Log.DebugFormat("Listing logs from {directory}.", absoluteLogsPath);

            var files = Directory.EnumerateFiles(absoluteLogsPath, "*.*", SearchOption.AllDirectories);

            Log.DebugFormat("Found {count} logs.", files.Count());

            return Task.FromResult(DomainModel.Response<IEnumerable<string>>.Success(request.Token, files));
        }
    }
}
