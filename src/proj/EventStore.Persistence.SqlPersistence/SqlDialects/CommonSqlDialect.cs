namespace EventStore.Persistence.SqlPersistence.SqlDialects
{
	using System.Data;

	public abstract class CommonSqlDialect : ISqlDialect
	{
		public abstract string InitializeStorage { get; }

		public virtual string AppendSnapshotToCommit
		{
			get { return CommonSqlStatements.AppendSnapshotToCommit; }
		}
		public virtual string GetCommitsFromSnapshotUntilRevision
		{
			get { return CommonSqlStatements.GetCommitsFromSnapshotUntilRevision; }
		}
		public virtual string GetCommitsFromStartingRevision
		{
			get { return CommonSqlStatements.GetCommitsFromStartingRevision; }
		}
		public virtual string GetStreamsRequiringSnaphots
		{
			get { return CommonSqlStatements.GetStreamsRequiringSnaphots; }
		}
		public virtual string GetUndispatchedCommits
		{
			get { return CommonSqlStatements.GetUndispatchedCommits; }
		}
		public virtual string MarkCommitAsDispatched
		{
			get { return CommonSqlStatements.MarkCommitAsDispatched; }
		}
		public virtual string PersistCommitAttempt
		{
			get { return CommonSqlStatements.PersistCommitAttempt; }
		}

		public virtual string StreamId
		{
			get { return "@StreamId"; }
		}
		public virtual string StreamName
		{
			get { return "@StreamName"; }
		}
		public virtual string CommitId
		{
			get { return "@CommitId"; }
		}
		public virtual string CommitSequence
		{
			get { return "@CommitSequence"; }
		}
		public virtual string StreamRevision
		{
			get { return "@StreamRevision"; }
		}
		public virtual string Headers
		{
			get { return "@Headers"; }
		}
		public virtual string Payload
		{
			get { return "@Payload"; }
		}
		public virtual string Threshold
		{
			get { return "@Threshold"; }
		}

		public virtual IDbTransaction OpenTransaction(IDbConnection connection)
		{
			return null;
		}
		public virtual IDbStatement BuildStatement(IDbConnection connection, IDbTransaction transaction)
		{
			return new CommonDbStatement(connection, transaction);
		}
	}
}