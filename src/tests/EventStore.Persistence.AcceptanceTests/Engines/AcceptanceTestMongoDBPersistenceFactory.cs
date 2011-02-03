﻿namespace EventStore.Persistence.AcceptanceTests.Engines
{
	using EventStore.Persistence.MongoDBPersistence;
	using MongoPersistence;
	using Serialization;

	public class AcceptanceTestMongoDBPersistenceFactory : MongoDBPersistenceFactory
	{
		public AcceptanceTestMongoDBPersistenceFactory()
			: base("MongoDB", new BinarySerializer())
		{
		}
		protected override string TransformConnectionString(string connectionString)
		{
			return connectionString
				.Replace("[HOST]", "host".GetSetting() ?? "localhost")
				.Replace("[PORT]", "port".GetSetting() ?? string.Empty)
				.Replace("[DATABASE]", "database".GetSetting() ?? "EventStore2b")
				.Replace("[USER]", "user".GetSetting() ?? string.Empty)
				.Replace("[PASSWORD]", "password".GetSetting() ?? string.Empty);
		}
	}
}