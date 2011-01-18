namespace EventStore.Persistence.RavenPersistence
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Transactions;
	using Persistence;
	using Raven.Client;

	public class RavenPersistenceEngine : IPersistStreams
	{
		private const string ToDispatch = "ToDispatch";
		private readonly IDocumentStore store;
		private readonly IInitializeRaven initializer;
		private bool disposed;

		public RavenPersistenceEngine(IDocumentStore store, IInitializeRaven initializer)
		{
			this.store = store;
			this.initializer = initializer;
		}

		public void Dispose()
		{
			this.Dispose(true);
			GC.SuppressFinalize(this);
		}
		protected virtual void Dispose(bool disposing)
		{
			if (!disposing || this.disposed)
				return;

			this.disposed = true;
			this.store.Dispose();
		}

		public virtual void Initialize()
		{
			this.initializer.Initialize(this.store);
		}

		public virtual IEnumerable<Commit> GetUntil(Guid streamId, int maxRevision)
		{
			using (new TransactionScope(TransactionScopeOption.Suppress))
			{
				return null;
			}
		}
		public virtual IEnumerable<Commit> GetFrom(Guid streamId, int minRevision)
		{
			using (new TransactionScope(TransactionScopeOption.Suppress))
			using (var session = this.store.OpenSession())
			{
				try
				{
					return session.Query<Commit>()
						.Where(x => x.StreamId == streamId && x.StreamRevision >= minRevision)
						.ToArray();
				}
				catch (Exception e)
				{
					throw new PersistenceEngineException(e.Message, e);
				}
			}
		}
		public virtual void Persist(CommitAttempt uncommitted)
		{
			var commit = uncommitted.ToCommit();

			using (new TransactionScope(TransactionScopeOption.Suppress))
			using (var session = this.store.OpenSession())
			{
				session.Advanced.UseOptimisticConcurrency = true;
				session.Advanced.OnEntityConverted += (entity, doc, meta) => doc.Add(ToDispatch, true);

				if (uncommitted.PreviousCommitSequence == 0)
					session.Store(new StreamHead(uncommitted.StreamId, uncommitted.StreamRevision, 0));
				else
				{
					var patch = commit.StreamId.UpdateStream(uncommitted.StreamRevision);
					session.Advanced.DatabaseCommands.Batch(new[] { patch });
				}

				session.Store(commit);

				try
				{
					session.SaveChanges();
				}
				catch (Raven.Http.Exceptions.ConcurrencyException e)
				{
					var committed = session.Load<Commit>(commit.Id());
					if (committed.CommitId == commit.CommitId)
						throw new DuplicateCommitException();

					if (session.Query<Commit>()
						.Where(x => x.StreamId == commit.StreamId && x.CommitId == commit.CommitId).Any())
						throw new DuplicateCommitException();

					throw new ConcurrencyException(e.Message, e);
				}
				catch (Exception e)
				{
					throw new PersistenceEngineException(e.Message, e);
				}
			}
		}

		public virtual IEnumerable<Commit> GetFrom(DateTime start)
		{
			throw new NotImplementedException();
		}

		public virtual IEnumerable<Commit> GetUndispatchedCommits()
		{
			using (new TransactionScope(TransactionScopeOption.Suppress))
			using (var session = this.store.OpenSession())
			{
				try
				{
					return session.Advanced.LuceneQuery<Commit>()
						.WhereContains(ToDispatch, true).ToList();
				}
				catch (Exception e)
				{
					throw new PersistenceEngineException(e.Message, e);
				}
			}
		}
		public virtual void MarkCommitAsDispatched(Commit commit)
		{
			using (new TransactionScope(TransactionScopeOption.Suppress))
			using (var session = this.store.OpenSession())
			{
				var patch = commit.RemoveProperty(ToDispatch);
				session.Advanced.DatabaseCommands.Batch(new[] { patch });
				session.SaveChanges();
			}
		}

		public virtual IEnumerable<StreamHead> GetStreamsToSnapshot(int maxThreshold)
		{
			using (new TransactionScope(TransactionScopeOption.Suppress))
			using (var session = this.store.OpenSession())
			{
				// TODO: paging
				var streams = session.Query<StreamHead>()
					.Where(x => x.HeadRevision > x.SnapshotRevision)
					.ToArray();

				return streams.Where(x => x.HeadRevision >= x.SnapshotRevision + 1);
			}
		}
		public virtual void AddSnapshot(Guid streamId, int streamRevision, object snapshot)
		{
			using (new TransactionScope(TransactionScopeOption.Suppress))
			using (var session = this.store.OpenSession())
			{
				// TODO
			}
		}
	}
}