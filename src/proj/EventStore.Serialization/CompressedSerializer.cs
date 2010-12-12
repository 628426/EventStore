namespace EventStore.Serialization
{
	using System.IO;
	using System.IO.Compression;

	public class CompressedSerializer : ISerialize
	{
		private readonly BinarySerializer inner;

		public CompressedSerializer(BinarySerializer inner)
		{
			this.inner = inner;
		}

		public virtual void Serialize(Stream output, object graph)
		{
			using (var compress = new DeflateStream(output, CompressionMode.Compress, true))
				this.inner.Serialize(compress, graph);
		}
		public virtual object Deserialize(Stream input)
		{
			using (var decompress = new DeflateStream(input, CompressionMode.Decompress, true))
				return this.inner.Deserialize(decompress);
		}
	}
}