namespace EventStore.Dispatcher
{
	using Persistence;

	public class SynchronousDispatcher : IDispatchCommits
	{
		private readonly IPublishMessages bus;
		private readonly IPersistStreams persistence;

		public SynchronousDispatcher(IPublishMessages bus, IPersistStreams persistence)
		{
			this.bus = bus;
			this.persistence = persistence;
		}

		public virtual void Dispatch(Commit commit)
		{
			this.bus.Publish(commit);
			this.persistence.MarkCommitAsDispatched(commit);
		}
	}
}