namespace EventStore.Serialization
{
	using System.IO;
	using System.Runtime.Serialization;
	using System.Runtime.Serialization.Formatters.Binary;

	public class BinarySerializer : ISerialize
	{
		private readonly IFormatter formatter = new BinaryFormatter();

		public virtual void Serialize(Stream output, object graph)
		{
			if (null != graph)
				this.formatter.Serialize(output, graph);
		}
		public virtual T Deserialize<T>(Stream input)
		{
			return (T)this.formatter.Deserialize(input);
		}
	}
}