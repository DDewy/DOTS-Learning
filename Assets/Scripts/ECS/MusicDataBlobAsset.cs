using Unity.Entities;
using Unity.Mathematics;

public struct MusicSample
{
    public float Value;
}

public struct MusicDataBlobAsset
{
    public BlobArray<MusicSample> sampleArray;
}

