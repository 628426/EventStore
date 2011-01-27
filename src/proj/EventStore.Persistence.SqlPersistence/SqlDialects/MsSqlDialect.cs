namespace EventStore.Persistence.SqlPersistence.SqlDialects
{
	public class MsSqlDialect : CommonSqlDialect
	{
		public override string InitializeStorage
		{
			get { return MsSqlStatements.InitializeStorage; }
		}

		public override string GetCommitsFromSnapshotUntilRevision
		{
			get { return MsSqlStatements.GetCommitsFromSnapshotUntilRevision; }
		}
	}
}