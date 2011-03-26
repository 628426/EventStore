namespace EventStore
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using Persistence;

	public class OptimisticEventStore : IStoreEvents, ICommitEvents
	{
		private readonly IPersistStreams persistence;
		private readonly IEnumerable<IHookCommitAttempts> commitHooks;
		private readonly IEnumerable<IHookCommitSelects> selectHooks;

		private bool disposed;

		public OptimisticEventStore(
			IPersistStreams persistence,
			IEnumerable<IHookCommitAttempts> commitHooks,
			IEnumerable<IHookCommitSelects> selectHooks)
		{
			this.persistence = persistence;
			this.commitHooks = commitHooks ?? new IHookCommitAttempts[0];
			this.selectHooks = selectHooks ?? new IHookCommitSelects[0];
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
			this.persistence.Dispose();
		}

		public virtual IEventStream CreateStream(Guid streamId)
		{
			return new OptimisticEventStream(streamId, this);
		}
		public virtual IEventStream OpenStream(Guid streamId, int minRevision, int maxRevision)
		{
			maxRevision = maxRevision <= 0 ? int.MaxValue : maxRevision;
			return new OptimisticEventStream(streamId, this, minRevision, maxRevision);
		}
		public virtual IEventStream OpenStream(Snapshot snapshot, int maxRevision)
		{
			maxRevision = maxRevision <= 0 ? int.MaxValue : maxRevision;
			return new OptimisticEventStream(snapshot, this, maxRevision);
		}

		IEnumerable<Commit> ICommitEvents.GetFrom(Guid streamId, int minRevision, int maxRevision)
		{
			var commits = this.persistence.GetFrom(streamId, minRevision, maxRevision);
			foreach (var commit in commits)
			{
				foreach (var hook in this.selectHooks)
				{
					var filtered = hook.Select(commit);
					if (filtered == null)
						continue;
				}

				yield return commit;
			}
		}

		void ICommitEvents.Commit(Commit attempt)
		{
			if (!attempt.IsValid() || attempt.IsEmpty())
				return;

			if (this.commitHooks.Any(x => !x.PreCommit(attempt)))
				return;

			this.persistence.Commit(attempt);

			foreach (var hook in this.commitHooks)
				hook.PostCommit(attempt);
		}

		public virtual Snapshot GetSnapshot(Guid streamId, int maxRevision)
		{
			// TODO: add to some kind of cache
			return this.persistence.GetSnapshot(streamId, maxRevision);
		}
		public virtual bool AddSnapshot(Snapshot snapshot)
		{
			// TODO: update the cache here
			return this.persistence.AddSnapshot(snapshot);
		}
	}
}