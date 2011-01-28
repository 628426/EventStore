namespace EventStore.Persistence.SqlPersistence
{
	using System;
	using System.Data;

	public interface ISqlDialect
	{
		string InitializeStorage { get; }

		string GetCommitsFromStartingRevision { get; }
		string GetCommitsFromInstant { get; }

		string PersistCommit { get; }

		string GetStreamsRequiringSnaphots { get; }
		string GetSnapshot { get; }
		string AppendSnapshotToCommit { get; }

		string GetUndispatchedCommits { get; }
		string MarkCommitAsDispatched { get; }

		string StreamId { get; }
		string StreamRevision { get; }
		string Items { get; }
		string CommitId { get; }
		string CommitSequence { get; }
		string CommitStamp { get; }
		string Headers { get; }
		string Payload { get; }
		string Snapshot { get; }
		string Threshold { get; }

		IDbTransaction OpenTransaction(IDbConnection connection);
		IDbStatement BuildStatement(IDbConnection connection, IDbTransaction transaction, params IDisposable[] resources);
	}
}