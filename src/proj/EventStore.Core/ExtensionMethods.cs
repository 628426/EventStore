namespace EventStore.Core
{
	using System;
	using System.Collections.Generic;

	internal static class ExtensionMethods
	{
		public static void AddEventsOrClearOnSnapshot(
			this ICollection<object> events, Commit commit, object snapshot)
		{
			if (snapshot != null)
				events.Clear();
			else
				events.AddEvents(commit);
		}

		public static void AddEvents(this ICollection<object> @events, Commit commit)
		{
			foreach (var @event in commit.Events)
				events.Add(@event.Body);
		}

		public static bool IsValid(this CommitAttempt attempt)
		{
			if (attempt == null)
				throw new ArgumentNullException("attempt");

			if (!attempt.HasIdentifier())
				throw new ArgumentException("The commit must be uniquely identified.", "attempt");

			if (attempt.PreviousCommitSequence < 0)
				throw new ArgumentException("The commit sequence cannot be a negative number.", "attempt");

			if (attempt.PreviousStreamRevision < 0)
				throw new ArgumentException("The stream revision cannot be a negative number.", "attempt");

			return true;
		}

		public static bool HasIdentifier(this CommitAttempt attempt)
		{
			return attempt.StreamId != Guid.Empty && attempt.CommitId != Guid.Empty;
		}

		public static bool IsEmpty(this CommitAttempt attempt)
		{
			return attempt != null && attempt.Events.Count > 0;
		}
	}
}