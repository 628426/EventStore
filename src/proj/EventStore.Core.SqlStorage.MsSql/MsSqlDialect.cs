namespace EventStore.Core.SqlStorage.MsSql
{
	using System;
	using System.Data;
	using System.Data.Common;
	using System.Data.SqlClient;

	public sealed class MsSqlDialect : BaseDialect
	{
		private const int PrimaryKeyViolation = 2627;
		private const int UniqueIndexViolation = 2601;

		public MsSqlDialect(IDbConnection connection, IDbTransaction transaction, Guid tenantId)
			: base(connection, transaction, tenantId)
		{
		}

		public override string SelectEvents
		{
			get { return MsSqlStatements.SelectEvents; }
		}
		public override string SelectEventsForCommand
		{
			get { return MsSqlStatements.SelectEventsForCommand; }
		}
		public override string SelectEventsForVersion
		{
			get { return MsSqlStatements.SelectEventsForVersion; }
		}
		public override string InsertEvent
		{
			get { return MsSqlStatements.InsertEvent; }
		}
		public override string InsertEvents
		{
			get { return MsSqlStatements.InsertEvents; }
		}

		public override bool IsDuplicateKey(DbException exception)
		{
			var sqlException = exception as SqlException;
			if (sqlException == null)
				return false;

			return sqlException.Number == PrimaryKeyViolation || sqlException.Number == UniqueIndexViolation;
		}
	}
}