﻿using Drey.Nut;

using System;
using System.Linq;
using System.Security.Permissions;

namespace Drey.Configuration.Services
{
    public class ConnectionStringsService : MarshalByRefObject, IConnectionStrings
    {
        readonly string _packageId;
        readonly Repositories.IConnectionStringRepository _connectionStringsRepository;

        /// <summary>
        /// Initializes a new instance of the <see cref="ConnectionStringsService"/> class.
        /// </summary>
        /// <param name="packageId">The package identifier.</param>
        /// <param name="connectionStringsRepository">The connection strings repository.</param>
        public ConnectionStringsService(string packageId, Repositories.IConnectionStringRepository connectionStringsRepository)
        {
            _packageId = packageId;
            _connectionStringsRepository = connectionStringsRepository;
        }

        /// <summary>
        /// Gets the <see cref="System.String"/> with the specified key.
        /// </summary>
        /// <value>
        /// The <see cref="System.String"/>.
        /// </value>
        /// <param name="key">The key.</param>
        /// <returns></returns>
        public string this[string key]
        {
            get { return _connectionStringsRepository.ByName(_packageId, key); }
        }

        /// <summary>
        /// Checks to see if a specific connection string exists within the repository, by its given name.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns></returns>
        public bool Exists(string name)
        {
            return _connectionStringsRepository.All().Any(cn => cn.PackageId.ToLower() == _packageId.ToLower() && cn.Name.ToLower() == name.ToLower());
        }

        /// <summary>
        /// Registers a connection string with the underlying repository.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="connectionString">The connection string.</param>
        /// <param name="providerName">Name of the provider.</param>
        public void Register(string name, string connectionString, string providerName = "")
        {
            var model = _connectionStringsRepository.Get(_packageId, name) ?? new DataModel.PackageConnectionString { PackageId = _packageId, Name = name };
            model.ConnectionString = connectionString;
            model.ProviderName = providerName;
            _connectionStringsRepository.Store(model);
        }
    }
}