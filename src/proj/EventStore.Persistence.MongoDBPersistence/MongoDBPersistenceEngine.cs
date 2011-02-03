﻿namespace EventStore.Persistence.MongoDBPersistence
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using MongoDB.Bson;
	using MongoDB.Driver;
	using MongoDB.Driver.Builders;
	using Serialization;

	public class MongoDBPersistenceEngine : IPersistStreams
	{
		private const string ConcurrencyException = "E1100";
		private readonly MongoDatabase store;
		private readonly ISerialize serializer;
		private bool disposed;

		public MongoDBPersistenceEngine(MongoDatabase store, ISerialize serializer)
		{
			this.store = store;
			this.serializer = serializer;
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
		}

		private MongoCollection<MongoDBCommit> PersistedCommits
		{
			get { return this.store.GetCollection<MongoDBCommit>("Commit"); }
		}

		private MongoCollection<MongoDBSnapshot> PersistedSnapshots
		{
			get { return this.store.GetCollection<MongoDBSnapshot>("Snapshot"); }
		}

		private MongoCollection<MongoDBStreamHead> PersistedStreamHeads
		{
			get { return this.store.GetCollection<MongoDBStreamHead>("StreamHead"); }
		}

		public virtual void Initialize()
		{
			this.PersistedCommits.EnsureIndex(
				IndexKeys.Ascending("Dispatched").Ascending("CommitStamp"),
				IndexOptions.SetName("Dispatched_Index").SetUnique(false));

			this.PersistedCommits.EnsureIndex(
				IndexKeys.Ascending("_id.StreamId", "StartingStreamRevision", "StreamRevision"),
				IndexOptions.SetName("GetFrom_Index").SetUnique(true));

			this.PersistedCommits.EnsureIndex(
				IndexKeys.Ascending("CommitStamp"),
				IndexOptions.SetName("CommitStamp_Index").SetUnique(false));
		}

		public virtual IEnumerable<Commit> GetFrom(Guid streamId, int minRevision, int maxRevision)
		{
			try
			{
				var query = Query.And(
					Query.EQ("_id.StreamId", streamId),
					Query.GTE("StreamRevision", minRevision),
					Query.LTE("StartingStreamRevision", maxRevision));

				return
					this.PersistedCommits.Find(query).SetSortOrder("StartingStreamRevision").Select(mc => mc.ToCommit(this.serializer));
			}
			catch (Exception e)
			{
				throw new StorageException(e.Message, e);
			}
		}

		public virtual IEnumerable<Commit> GetFrom(DateTime start)
		{
			try
			{
				var query = Query.GTE("CommitStamp", start);

				return this.PersistedCommits.Find(query).SetSortOrder("CommitStamp").Select(mc => mc.ToCommit(this.serializer));
			}
			catch (Exception e)
			{
				throw new StorageException(e.Message, e);
			}
		}

		public virtual void Commit(Commit attempt)
		{
			var commit = attempt.ToMongoDBCommit(this.serializer);

			try
			{
				// for concurrency / duplicate commit detection safe mode is required
				this.PersistedCommits.Insert(commit, SafeMode.True);

				this.SaveStreamHeadAsync(new MongoDBStreamHead(commit.Id.StreamId, commit.StreamRevision, 0));
			}
			catch (MongoException e)
			{
				if (!e.Message.Contains(ConcurrencyException))
					throw new StorageException(e.Message, e);

				var committed = this.PersistedCommits.FindOne(commit.ToMongoDBCommitIdQuery());
				if (committed == null || committed.CommitId == commit.CommitId)
					throw new DuplicateCommitException();

				var conflictRevision = attempt.StreamRevision - attempt.Events.Count + 1;
				throw new ConcurrencyException(this.GetFrom(attempt.StreamId, conflictRevision, int.MaxValue));
			}
		}

		public virtual IEnumerable<Commit> GetUndispatchedCommits()
		{
			var query = Query.EQ("Dispatched", false);

			return this.PersistedCommits.Find(query).SetSortOrder("CommitStamp").Select(mc => mc.ToCommit(this.serializer));
		}

		public virtual void MarkCommitAsDispatched(Commit commit)
		{
			var query = commit.ToMongoDBCommitIdQuery();
			var update = Update.Set("Dispatched", true);
			this.PersistedCommits.Update(query, update);
		}

		public virtual IEnumerable<StreamHead> GetStreamsToSnapshot(int maxThreshold)
		{
			var query = Query.Where(BsonJavaScript.Create("this.HeadRevision >= this.SnapshotRevision + " + maxThreshold));
			return this.PersistedStreamHeads.Find(query).ToArray().Select(sh => sh.ToStreamHead());
		}

		public virtual Snapshot GetSnapshot(Guid streamId, int maxRevision)
		{
			var query =
				Query.GT("_id",
					Query.And(
						Query.EQ("StreamId", streamId),
						Query.EQ("StreamRevision", BsonNull.Value)
						).ToBsonDocument()
					).LTE(
						Query.And(
							Query.EQ("StreamId", streamId),
							Query.EQ("StreamRevision", maxRevision)
							).ToBsonDocument()
					);

			return this.PersistedSnapshots
				.Find(query)
				.SetSortOrder(SortBy.Descending("_id"))
				.SetLimit(1)
				.Select(mc => mc.ToSnapshot(this.serializer))
				.FirstOrDefault();
		}

		public virtual bool AddSnapshot(Snapshot snapshot)
		{
			if (snapshot == null)
				return false;

			try
			{
				var mongoSnapshot = snapshot.ToMongoDBSnapshot(this.serializer);
				this.PersistedSnapshots.Insert(mongoSnapshot);

				this.SaveStreamHeadAsync(new MongoDBStreamHead(snapshot.StreamId, snapshot.StreamRevision, snapshot.StreamRevision));

				return true;
			}
			catch (MongoException e)
			{
				if (!e.Message.StartsWith(ConcurrencyException))
					throw new StorageException(e.Message, e);

				return false;
			}
		}

		private void SaveStreamHeadAsync(MongoDBStreamHead streamHead)
		{
			// ThreadPool.QueueUserWorkItem(item => this.PersistedStreamHeads.Save(item as StreamHead), streamHead);

			var query = Query.EQ("_id", streamHead.StreamId);
			var update = Update.Set("HeadRevision", streamHead.HeadRevision)
				.Set("SnapshotRevision", streamHead.SnapshotRevision);
			this.PersistedStreamHeads.Update(query, update, UpdateFlags.Upsert);
		}
	}
}