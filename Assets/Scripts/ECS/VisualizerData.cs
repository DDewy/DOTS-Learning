using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Unity.Entities;

[GenerateAuthoringComponent]
public struct VisualizerData : IComponentData
{
    public byte Channel;
    public int index;
    public float SampleValue;
    public BlobAssetReference<MusicDataBlobAsset> MusicData;
}
