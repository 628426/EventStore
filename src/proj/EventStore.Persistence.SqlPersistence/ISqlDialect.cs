namespace EventStore.Persistence.SqlPersistence
{
	using System;
	using System.Data;

	public interface ISqlDialect
	{
		string InitializeStorage { get; }
		string AppendSnapshotToCommit { get; }
		string GetCommitsFromSnapshotUntilRevision { get; }
		string GetCommitsFromStartingRevision { get; }
		string GetStreamsRequiringSnaphots { get; }
		string GetUndispatchedCommits { get; }
		string MarkCommitAsDispatched { get; }
		string PersistCommitAttempt { get; }

		string StreamId { get; }
		string StreamName { get; }
		string CommitId { get; }
		string CommitSequence { get; }
		string StreamRevision { get; }
		string Headers { get; }
		string Payload { get; }
		string Threshold { get; }

		DbType GuidType { get; }

		bool IsDuplicateException(Exception exception);
	}
}