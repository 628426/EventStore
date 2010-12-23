namespace EventStore.Persistence.RavenPersistence
{
	using System;
	using System.Transactions;
	using Raven.Client;

	public class RavenInitializer : IInitializeRaven
	{
		public virtual void Initialize(IDocumentStore store)
		{
			try
			{
				using (new TransactionScope(TransactionScopeOption.Suppress))
					TryInitialize(store);
			}
			catch (Exception e)
			{
				throw new PersistenceEngineException(e.Message, e);
			}
		}
		private static void TryInitialize(IDocumentStore store)
		{
			// TODO: define conventions
			AssignDocumentKeyGenerator(store);

			// TODO: create indexes
			store.Initialize();
		}

		private static void AssignDocumentKeyGenerator(IDocumentStore store)
		{
			var generator = store.Conventions.DocumentKeyGenerator;
			store.Conventions.DocumentKeyGenerator = entity =>
				AssignIdentity(entity as Commit) ?? generator(entity);
		}
		private static string AssignIdentity(Commit commit)
		{
			return commit == null ? null : commit.Id();
		}
	}
}