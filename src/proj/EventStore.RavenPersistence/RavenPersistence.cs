namespace EventStore.RavenPersistence
{
	using System;
	using System.Collections.Generic;
	using Persistence;
	using Raven.Client;
	using Raven.Client.Exceptions;

	public class RavenPersistence : IPersistStreams
	{
		private readonly IDocumentStore store;

		public RavenPersistence(IDocumentStore store)
		{
			this.store = store;
			this.store.Initialize();
		}

		public virtual IEnumerable<Commit> GetUntil(Guid streamId, long maxRevision)
		{
			return null;
		}
		public virtual IEnumerable<Commit> GetFrom(Guid streamId, long minRevision)
		{
			return null;
		}
		public virtual void Persist(CommitAttempt uncommitted)
		{
			using (var session = this.store.OpenSession())
			{
				session.Advanced.UseOptimisticConcurrency = true;
				session.Store(uncommitted.ToRavenCommit());

				try
				{
					session.SaveChanges();
				}
				catch (ConflictException e)
				{
					throw new ConcurrencyException(e.Message, e);
				}
				catch (NonUniqueObjectException e)
				{
					throw new ConcurrencyException(e.Message, e);
				}
			}
		}

		public virtual IEnumerable<Commit> GetUndispatchedCommits()
		{
			return null;
		}
		public virtual void MarkCommitAsDispatched(Commit commit)
		{
			// patches a commit to flag it as dispatched
		}

		public virtual IEnumerable<Guid> GetStreamsToSnapshot(int maxThreshold)
		{
			return null;
		}
		public virtual void AddSnapshot(Guid streamId, long commitSequence, object snapshot)
		{
			// inserts a snapshot document *between* two commits
		}
	}
}