﻿using Drey.Logging;

using System;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

namespace Drey.CertificateValidation
{
    [Serializable]
    public class SelfSignedServerCertificateValidation : ICertificateValidation
    {
        static ILog _log = LogProvider.For<SelfSignedServerCertificateValidation>();

        readonly string _thumbprint;

        public SelfSignedServerCertificateValidation(string thumbprint)
        {
            _log.InfoFormat("Trusted SSL Certificate (thumbprint): {thumbprint}", thumbprint);
            _thumbprint = thumbprint;
        }

        /// <summary>
        /// Initializes the certificate validation callback.
        /// </summary>
        public void Initialize()
        {
            ServicePointManager.ServerCertificateValidationCallback = ValidateServerCertificate;
        }

        /// <summary>
        /// Validates the remote server's ssl certificate, to ensure we are connecting to the right signalr hub, etc.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="certificate">The certificate.</param>
        /// <param name="chain">The chain.</param>
        /// <param name="sslPolicyErrors">The SSL policy errors.</param>
        /// <returns></returns>
        public bool ValidateServerCertificate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            var cert2 = certificate as X509Certificate2;

            if (cert2 == null) { return false; }

            var effectiveDateString = cert2.GetEffectiveDateString();
            var expirationDateString = cert2.GetExpirationDateString();

            if (!string.IsNullOrWhiteSpace(effectiveDateString))
            {
                var dteEffectiveDate = DateTime.Parse(effectiveDateString);
                if (dteEffectiveDate > DateTime.UtcNow) { return false; }
            }
            else
            {
                _log.Warn("No effective date available on server certificate.");
            }

            if (!string.IsNullOrWhiteSpace(expirationDateString))
            {
                var dteExpirationDate = DateTime.Parse(expirationDateString);
                if (dteExpirationDate <= DateTime.UtcNow) { return false; }
            }
            else
            {
                _log.Warn("Expiration Date unavailable on certificate.");
            }

            _log.DebugFormat("Certificate effective date: {date}", cert2.GetEffectiveDateString());
            _log.DebugFormat("Certificate expiration date: {date}", cert2.GetExpirationDateString());

            return _thumbprint.Equals(cert2.Thumbprint, StringComparison.OrdinalIgnoreCase);
        }
    }
}
