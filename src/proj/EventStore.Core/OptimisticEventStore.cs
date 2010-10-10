namespace EventStore.Core
{
	using System;

	public class OptimisticEventStore : IStoreEvents
	{
		private readonly IStorageEngine storage;

		public OptimisticEventStore(IStorageEngine storage)
		{
			this.storage = storage;
		}

		public CommittedEventStream Read(Guid id, long maxStartingVersion)
		{
			return this.storage.LoadById(id, maxStartingVersion);
		}

		public void Write(UncommittedEventStream stream)
		{
			if (!CanWrite(stream))
				throw new ArgumentException(ExceptionMessages.NoWork, "stream");

			if (!ValidStream(stream))
				throw new ArgumentException(ExceptionMessages.MalformedStream, "stream");

			try
			{
				this.storage.Save(stream);
			}
			catch (DuplicateKeyException e)
			{
				this.WrapAndThrow(stream, e);
			}
		}
		private static bool CanWrite(UncommittedEventStream stream)
		{
			return stream != null
				&& (stream.Snapshot != null || (stream.Events != null && stream.Events.Count > 0));
		}
		private static bool ValidStream(UncommittedEventStream stream)
		{
			return stream.ExpectedVersion >= 0;
		}
		private void WrapAndThrow(UncommittedEventStream stream, Exception innerException)
		{
			var events = this.storage.LoadStartingAfter(stream.Id, stream.ExpectedVersion);
			if (events.Count > 0)
				throw new ConcurrencyException(ExceptionMessages.Concurrency, innerException, events);

			events = this.storage.LoadByCommandId(stream.CommandId);
			throw new DuplicateCommandException(ExceptionMessages.Duplicate, innerException, events);
		}
	}
}