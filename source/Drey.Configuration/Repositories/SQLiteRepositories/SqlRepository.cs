﻿using Drey.Nut;
using Drey.Utilities;

using Mono.Data.Sqlite;

using System;
using System.Data;
using System.IO;
using System.Security.Permissions;

namespace Drey.Configuration.Repositories.SQLiteRepositories
{
    public abstract class SqlRepository : MarshalByRefObject
    {
        const string CONNECTION_STRING_FORMAT = "Data Source=\"{0}\";Version=3;";
        const string CONFIG_FILE_NAME = "config.db3";

        INutConfiguration _configurationManager;

        Func<INutConfiguration, string> ConnectionStringBuilder = (config) => string.Format(CONNECTION_STRING_FORMAT, PathUtilities.MapPath(Path.Combine(config.WorkingDirectory, CONFIG_FILE_NAME), false));

        /// <summary>
        /// Initializes a new instance of the <see cref="SqlRepository"/> class.
        /// <remarks>Used by IoC container.</remarks>
        /// </summary>
        /// <param name="configurationManager">The configuration manager.</param>
        public SqlRepository(INutConfiguration configurationManager)
        {
            _configurationManager = configurationManager;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SqlRepository"/> class.
        /// <remarks>Used for integration testing.</remarks>
        /// </summary>
        /// <param name="databaseNameAndPath">The database name and path.</param>
        public SqlRepository(string databaseNameAndPath)
        {
            ConnectionStringBuilder = (config) => string.Format(CONNECTION_STRING_FORMAT, databaseNameAndPath);
        }

        /// <summary>
        /// Executes the specified work.
        /// </summary>
        /// <param name="work">The work.</param>
        protected void Execute(Action<IDbConnection> work)
        {
            using (var cn = SqliteFactory.Instance.CreateConnection())
            {
                cn.ConnectionString = ConnectionStringBuilder.Invoke(_configurationManager);
                cn.Open();

                work(cn);
            }
        }
        
        /// <summary>
        /// Executes the specified work.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="work">The work.</param>
        /// <returns></returns>
        protected T Execute<T>(Func<IDbConnection, T> work)
        {
            using (var cn = SqliteFactory.Instance.CreateConnection())
            {
                cn.ConnectionString = ConnectionStringBuilder.Invoke(_configurationManager);
                cn.Open();

                return work(cn);
            }
        }

        /// <summary>
        /// Executes the with transaction.
        /// </summary>
        /// <param name="work">The work.</param>
        protected void ExecuteWithTransaction(Action<IDbConnection> work)
        {
            Execute((cn) =>
            {
                using (var tx = cn.BeginTransaction())
                {
                    work(cn);
                    tx.Commit();
                }
            });
        }
    }
}