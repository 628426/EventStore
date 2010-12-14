namespace EventStore
{
	using System;
	using System.Collections.Generic;

	/// <summary>
	/// Represents a series of events which have been fully committed as a single unit and which apply to the stream indicated.
	/// </summary>
	public class Commit
	{
		/// <summary>
		/// Initializes a new instance of the Commit class.
		/// </summary>
		/// <param name="streamId">The value which uniquely identifies the stream to which the commit belongs.</param>
		/// <param name="commitId">The value which uniquely identifies the commit within the stream.</param>
		/// <param name="streamRevision">The value which indicates the revision of the most recent event in the stream to which this commit applies.</param>
		/// <param name="commitSequence">The value which indicates the sequence (or position) in the stream to which this commit applies.</param>
		/// <param name="headers">The metadata which provides additional, unstructured information about this commit.</param>
		/// <param name="events">The collection of event messages to be committed as a single unit.</param>
		/// <param name="snapshot">The snapshot, if any, which represents a materialization of the stream at the last event of the commit.</param>
		public Commit(
			Guid streamId,
			Guid commitId,
			long streamRevision,
			long commitSequence,
			IDictionary<string, object> headers,
			ICollection<EventMessage> events,
			object snapshot)
		{
			this.StreamId = streamId;
			this.CommitId = commitId;
			this.StreamRevision = streamRevision;
			this.CommitSequence = commitSequence;
			this.Headers = headers ?? new Dictionary<string, object>();
			this.Events = events ?? new LinkedList<EventMessage>();
			this.Snapshot = snapshot;
		}

		/// <summary>
		/// Gets the value which uniquely identifies the stream to which the commit belongs.
		/// </summary>
		public Guid StreamId { get; private set; }

		/// <summary>
		/// Gets the value which uniquely identifies the commit within the stream.
		/// </summary>
		public Guid CommitId { get; private set; }

		/// <summary>
		/// Gets the value which indicates the revision of the most recent event in the stream to which this commit applies.
		/// </summary>
		public long StreamRevision { get; private set; }

		/// <summary>
		/// Gets the value which indicates the sequence (or position) in the stream to which this commit applies.
		/// </summary>
		public long CommitSequence { get; private set; }

		/// <summary>
		/// Gets the metadata which provides additional, unstructured information about this commit.
		/// </summary>
		public IDictionary<string, object> Headers { get; private set; }

		/// <summary>
		/// Gets the collection of event messages to be committed as a single unit.
		/// </summary>
		public ICollection<EventMessage> Events { get; private set; }

		/// <summary>
		/// Gets the snapshot, if any, which represents a materialization of the stream at the last event of the commit.
		/// </summary>
		public object Snapshot { get; private set; }
	}
}