﻿using Drey.Utilities;
using Drey.Nut;

using System;
using System.IO;
using System.Security.Permissions;

namespace Drey
{
    public class ApplicationHostNutConfiguration : MarshalByRefObject, INutConfiguration
    {
        IApplicationSettings _applicationSettings;
        IGlobalSettings _globalSettings;
        IConnectionStrings _connectionStrings;

        public ApplicationHostNutConfiguration()
        {
            var appSettings = new AppConfigApplicationSettings();
            _applicationSettings = appSettings;
            _globalSettings = appSettings;

            _connectionStrings = new AppConfigConnectionStrings();
        }

        public IGlobalSettings GlobalSettings
        {
            get { return _globalSettings; }
        }

        public IApplicationSettings ApplicationSettings
        {
            get { return _applicationSettings; }
        }

        public IConnectionStrings ConnectionStrings
        {
            get { return _connectionStrings; }
        }

        public string WorkingDirectory
        {
            get { return _applicationSettings["WorkingDirectory"].NormalizePathSeparator(); }
        }
        public string HoardeBaseDirectory
        {
            get { return Path.Combine(WorkingDirectory, "Hoarde").NormalizePathSeparator(); }
        }
        public string LogsDirectory { get { return Path.Combine(WorkingDirectory, "Logs").NormalizePathSeparator(); } }

        public ExecutionMode Mode { get; set; }

        public CertificateValidation.ICertificateValidation CertificateValidator
        {
            get 
            {
                var thumbprint = ApplicationSettings["server.sslthumbprint"] ?? string.Empty;
                if (string.IsNullOrWhiteSpace(thumbprint))
                {
                    return new CertificateValidation.AuthorityIssuedServerCertificateValidation();
                }
                return new CertificateValidation.SelfSignedServerCertificateValidation(thumbprint);
            }
        }

        [SecurityPermission(SecurityAction.Demand, Flags = SecurityPermissionFlag.Infrastructure)]
        public override object InitializeLifetimeService()
        {
            return null;
        }
    }
}