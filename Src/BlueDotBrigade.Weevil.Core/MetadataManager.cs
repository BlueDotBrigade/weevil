using System.Collections.Concurrent;

public class MetadataManager
{
    private readonly ConcurrentDictionary<int, Metadata> _metadataStore;

    public MetadataManager()
    {
        _metadataStore = new ConcurrentDictionary<int, Metadata>();
    }

    public Metadata GetMetadata(int lineNumber)
    {
        return _metadataStore.GetOrAdd(lineNumber, _ => Metadata.Default);
    }

    public void SetMetadata(int lineNumber, Metadata metadata)
    {
        _metadataStore[lineNumber] = metadata;
    }
}