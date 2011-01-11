namespace EventStore.Persistence
{
	using System;
	using System.Collections.Generic;

	/// <summary>
	/// Tracks the commits for a set of streams to determine if a particular commit has already
	/// been committed thus relaxing the requirements upon the persistence engine as well as
	/// reducing latency by avoiding needless database roundtrips through keeping the values which
	/// uniquely identify each commit in memory.
	/// </summary>
	/// <remarks>
	/// For storage engines with relaxed consistency guarantees, such as a document database,
	/// the CommitTracker prevents the need to query the persistence engine prior to a commit.
	/// For storage engines with stronger consistency guarantees, such as a relational database,
	/// the CommitTracker helps to avoid the increased latency incurred from extra roundtrips.
	/// </remarks>
	public class CommitTracker
	{
		private const int MaxCommitsTrackedPerStream = 1000;
		private readonly IDictionary<Guid, TrackedStream> streams = new Dictionary<Guid, TrackedStream>();
		private readonly int commitsToTrackPerStream;

		public CommitTracker()
			: this(MaxCommitsTrackedPerStream)
		{
		}
		public CommitTracker(int commitsToTrackPerStream)
		{
			this.commitsToTrackPerStream = commitsToTrackPerStream;
		}

		public virtual void Track(Commit committed)
		{
			TrackedStream stream;

			lock (this.streams)
				if (!this.streams.TryGetValue(committed.StreamId, out stream))
					this.streams[committed.StreamId] = stream = new TrackedStream(this.commitsToTrackPerStream);

			stream.Track(committed);
		}
		public virtual bool Contains(CommitAttempt attempt)
		{
			var stream = this.GetStream(attempt.StreamId);
			return stream != null && stream.Contains(attempt.CommitId);
		}
		public virtual Commit GetStreamHead(Guid streamId)
		{
			var stream = this.GetStream(streamId);
			return stream == null ? null : stream.Head;
		}

		private TrackedStream GetStream(Guid streamId)
		{
			lock (this.streams)
			{
				TrackedStream stream;
				this.streams.TryGetValue(streamId, out stream);
				return stream;
			}
		}

		private class TrackedStream
		{
			private readonly ICollection<Guid> lookup = new HashSet<Guid>();
			private readonly LinkedList<Guid> ordered = new LinkedList<Guid>();
			private readonly int commitsToTrack;

			public TrackedStream(int commitsToTrack)
			{
				this.commitsToTrack = commitsToTrack;
			}

			public Commit Head { get; private set; }

			public void Track(Commit committed)
			{
				if (this.lookup.Contains(committed.CommitId))
					return;

				lock (this.lookup)
				{
					if (this.lookup.Contains(committed.CommitId))
						return;

					if (this.Head == null || committed.CommitSequence == this.Head.CommitSequence + 1)
						this.Head = committed;

					this.lookup.Add(committed.CommitId);
					this.ordered.AddLast(committed.CommitId);

					if (this.ordered.Count <= this.commitsToTrack)
						return;

					var commitIdToRemove = this.ordered.First;
					this.ordered.RemoveFirst();
					this.lookup.Remove(commitIdToRemove.Value);
				}
			}
			public bool Contains(Guid commitId)
			{
				return this.lookup.Contains(commitId);
			}
		}
	}
}