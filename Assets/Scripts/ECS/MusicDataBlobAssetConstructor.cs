using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Unity.Entities;
using Unity.Collections;

[UpdateInGroup(typeof(GameObjectConversionGroup))]
public class MusicDataBlobAssetConstructor : GameObjectConversionSystem
{
    protected override void OnUpdate()
    {
        Debug.Log("Music Data BLob constructor called");
        BlobAssetReference<MusicDataBlobAsset> musicDataBlobAssetReference;
        using (BlobBuilder blobBuilder = new BlobBuilder(Allocator.Temp))
        {
            ref MusicDataBlobAsset musicDataBlobAsset = ref blobBuilder.ConstructRoot<MusicDataBlobAsset>();

            int sampleSize = 1024;
            BlobBuilderArray<MusicSample> musicDataArray = blobBuilder.Allocate(ref musicDataBlobAsset.sampleArray, sampleSize);

            //Set array data here
            for(int i = 0; i < sampleSize; i++)
            {
                musicDataArray[i] = new MusicSample { Value = 0 };
            }

            musicDataBlobAssetReference = blobBuilder.CreateBlobAssetReference<MusicDataBlobAsset>(Allocator.Persistent);
        }
        
#if DOTS
        //Save the Blob Reference into the Music Visual Manager
        var visManager = GameObject.FindObjectOfType<MusicVisualManager>();
        if(visManager != null)
        {
            visManager.SampleData = musicDataBlobAssetReference;
        }
#endif

        EntityQuery musicVisQuery = DstEntityManager.CreateEntityQuery(typeof(VisualizerData));
        Entities.ForEach((ref VisualizerData visData) =>
        {
            visData.MusicData = musicDataBlobAssetReference;
        });
    }
}
