﻿namespace EventStore.Persistence.MongoDBPersistence
{
	using System.Configuration;
	using MongoDB.Driver;
	using Serialization;

	public class MongoDBPersistenceFactory : IPersistenceFactory
	{
		private readonly string connectionName;
		private readonly ISerialize serializer;

		public MongoDBPersistenceFactory(string connectionName, ISerialize serializer)
		{
			this.connectionName = connectionName;
			this.serializer = serializer;
		}

		public virtual IPersistStreams Build()
		{
			var connectionString = this.TransformConnectionString(this.GetConnectionString());
			var database = MongoDatabase.Create(connectionString);
			return new MongoDBPersistenceEngine(database, this.serializer);
		}

		protected virtual string GetConnectionString()
		{
			return ConfigurationManager.ConnectionStrings[this.connectionName].ConnectionString;
		}

		protected virtual string TransformConnectionString(string connectionString)
		{
			return connectionString;
		}
	}
}