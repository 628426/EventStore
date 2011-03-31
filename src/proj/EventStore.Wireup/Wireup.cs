namespace EventStore
{
	using System.Collections.Generic;
	using System.Linq;
	using Dispatcher;
	using Persistence;
	using Persistence.InMemoryPersistence;

	public class Wireup
	{
		private readonly Wireup inner;
		private readonly NanoContainer container;

		protected Wireup(NanoContainer container)
		{
			this.container = container;
		}
		protected Wireup(Wireup inner)
		{
			this.inner = inner;
		}

		public static Wireup Init()
		{
			var container = new NanoContainer();

			container.Register<IPersistStreams>(new InMemoryPersistenceEngine());
			container.Register(BuildEventStore);

			return new Wireup(container);
		}

		protected NanoContainer Container
		{
			get { return this.container ?? this.inner.Container; }
		}

		public virtual Wireup With<T>(T instance) where T : class
		{
			this.Container.Register(instance);
			return this;
		}

		public virtual IStoreEvents Build()
		{
			if (this.inner != null)
				return this.inner.Build();

			return this.Container.Resolve<IStoreEvents>();
		}

		private static IStoreEvents BuildEventStore(NanoContainer context)
		{
			var concurrentHook = new OptimisticPipelineHook();
			var dispatcherHook = new DispatchPipelineHook(context.Resolve<IDispatchCommits>());

			var pipelineHooks = context.Resolve<ICollection<IPipelineHook>>() ?? new IPipelineHook[0];
			pipelineHooks = new IPipelineHook[] { concurrentHook, dispatcherHook } .Concat(pipelineHooks).ToArray();

			return new OptimisticEventStore(context.Resolve<IPersistStreams>(), pipelineHooks);
		}
	}
}