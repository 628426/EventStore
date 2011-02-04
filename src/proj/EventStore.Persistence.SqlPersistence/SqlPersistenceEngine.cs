namespace EventStore.Persistence.SqlPersistence
{
	using System;
	using System.Collections.Generic;
	using System.Data;
	using System.Linq;
	using System.Transactions;
	using Persistence;
	using Serialization;

	public class SqlPersistenceEngine : IPersistStreams
	{
		private readonly IConnectionFactory connectionFactory;
		private readonly ISqlDialect dialect;
		private readonly ISerialize serializer;

		public SqlPersistenceEngine(IConnectionFactory connectionFactory, ISqlDialect dialect, ISerialize serializer)
		{
			this.connectionFactory = connectionFactory;
			this.dialect = dialect;
			this.serializer = serializer;
		}

		public void Dispose()
		{
			this.Dispose(true);
			GC.SuppressFinalize(this);
		}
		protected virtual void Dispose(bool disposing)
		{
			// no-op
		}

		public virtual void Initialize()
		{
			this.ExecuteCommand(Guid.Empty, statement =>
				statement.ExecuteWithSuppression(this.dialect.InitializeStorage));
		}

		public virtual IEnumerable<Commit> GetFrom(Guid streamId, int minRevision, int maxRevision)
		{
			return this.ExecuteQuery(streamId, query =>
			{
				query.AddParameter(this.dialect.StreamId, streamId);
				query.AddParameter(this.dialect.StreamRevision, minRevision);
				query.AddParameter(this.dialect.MaxStreamRevision, maxRevision);
				return query.ExecuteWithQuery(this.dialect.GetCommitsFromStartingRevision, x => x.GetCommit(this.serializer));
			});
		}
		public virtual IEnumerable<Commit> GetFrom(DateTime start)
		{
			return this.ExecuteQuery(Guid.Empty, query =>
			{
				var statement = this.dialect.GetCommitsFromInstant;
				query.AddParameter(this.dialect.CommitStamp, start);
				return query.ExecuteWithQuery(statement, x => x.GetCommit(this.serializer));
			});
		}

		public virtual void Commit(Commit attempt)
		{
			this.ExecuteCommand(attempt.StreamId, cmd =>
			{
				cmd.AddParameter(this.dialect.StreamId, attempt.StreamId);
				cmd.AddParameter(this.dialect.StreamRevision, attempt.StreamRevision);
				cmd.AddParameter(this.dialect.Items, attempt.Events.Count);
				cmd.AddParameter(this.dialect.CommitId, attempt.CommitId);
				cmd.AddParameter(this.dialect.CommitSequence, attempt.CommitSequence);
				cmd.AddParameter(this.dialect.CommitStamp, attempt.CommitStamp);
				cmd.AddParameter(this.dialect.Headers, this.serializer.Serialize(attempt.Headers));
				cmd.AddParameter(this.dialect.Payload, this.serializer.Serialize(attempt.Events));

				var rowsAffected = cmd.Execute(this.dialect.PersistCommit);
				if (rowsAffected <= 0)
					throw new ConcurrencyException();
			});
		}

		public virtual IEnumerable<Commit> GetUndispatchedCommits()
		{
			return this.ExecuteQuery(Guid.Empty, query =>
				query.ExecuteWithQuery(this.dialect.GetUndispatchedCommits, x => x.GetCommit(this.serializer)));
		}
		public virtual void MarkCommitAsDispatched(Commit commit)
		{
			this.ExecuteCommand(commit.StreamId, cmd =>
			{
				cmd.AddParameter(this.dialect.StreamId, commit.StreamId);
				cmd.AddParameter(this.dialect.CommitSequence, commit.CommitSequence);
				cmd.ExecuteWithSuppression(this.dialect.MarkCommitAsDispatched);
			});
		}

		public virtual IEnumerable<StreamHead> GetStreamsToSnapshot(int maxThreshold)
		{
			return this.ExecuteQuery(Guid.Empty, query =>
			{
				var statement = this.dialect.GetStreamsRequiringSnaphots;
				query.AddParameter(this.dialect.Threshold, maxThreshold);
				return query.ExecuteWithQuery(statement, record => record.GetStreamToSnapshot());
			});
		}
		public Snapshot GetSnapshot(Guid streamId, int maxRevision)
		{
			return this.ExecuteQuery(streamId, query =>
			{
				var queryText = this.dialect.GetSnapshot;
				query.AddParameter(this.dialect.StreamId, streamId);
				query.AddParameter(this.dialect.StreamRevision, maxRevision);
				return query.ExecuteWithQuery(queryText, x => x.GetSnapshot(this.serializer)).FirstOrDefault();
			});
		}
		public bool AddSnapshot(Snapshot snapshot)
		{
			var rowsAffected = 0;
			this.ExecuteCommand(snapshot.StreamId, cmd =>
			{
				cmd.AddParameter(this.dialect.StreamId, snapshot.StreamId);
				cmd.AddParameter(this.dialect.StreamRevision, snapshot.StreamRevision);
				cmd.AddParameter(this.dialect.Payload, this.serializer.Serialize(snapshot.Payload));
				rowsAffected = cmd.ExecuteWithSuppression(this.dialect.AppendSnapshotToCommit);
			});
			return rowsAffected > 0;
		}

		protected virtual T ExecuteQuery<T>(Guid streamId, Func<IDbStatement, T> query)
		{
			var scope = new TransactionScope(TransactionScopeOption.Suppress);
			IDbConnection connection = null;
			IDbTransaction transaction = null;
			IDbStatement statement = null;

			try
			{
				connection = this.connectionFactory.OpenSlave(streamId);
				transaction = this.dialect.OpenTransaction(connection);
				statement = this.dialect.BuildStatement(connection, transaction, scope);
				return query(statement);
			}
			catch (Exception e)
			{
				if (statement != null)
					statement.Dispose();
				if (transaction != null)
					transaction.Dispose();
				if (connection != null)
					connection.Dispose();
				scope.Dispose();

				throw new StorageException(e.Message, e);
			}
		}
		protected virtual void ExecuteCommand(Guid streamId, Action<IDbStatement> command)
		{
			using (var scope = new TransactionScope(TransactionScopeOption.Suppress))
			using (var connection = this.connectionFactory.OpenMaster(streamId))
			using (var transaction = this.dialect.OpenTransaction(connection))
			using (var statement = this.dialect.BuildStatement(connection, transaction, scope))
			{
				try
				{
					command(statement);
					if (transaction != null)
						transaction.Commit();
				}
				catch (Exception e)
				{
					if (e is ConcurrencyException || e is DuplicateCommitException)
						throw;

					throw new StorageException(e.Message, e);
				}
			}
		}
	}
}