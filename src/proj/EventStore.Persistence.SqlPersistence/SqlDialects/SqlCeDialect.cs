namespace EventStore.Persistence.SqlPersistence.SqlDialects
{
	using System.Collections.Generic;
	using System.Linq;

	public class SqlCeDialect : CommonSqlDialect
	{
		public override IEnumerable<string> InitializeStorage
		{
			get { return SqlCeStatements.InitializeStorage.SplitStatement(); }
		}
		public override IEnumerable<string> AppendSnapshotToCommit
		{
			get { return base.AppendSnapshotToCommit.First().SplitStatement(); }
		}
		public override IEnumerable<string> PersistCommitAttempt
		{
			get { return base.PersistCommitAttempt.First().SplitStatement(); }
		}
	}
}