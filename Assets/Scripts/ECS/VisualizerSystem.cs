using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Unity.Jobs;
using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using Unity.Collections;


public class VisualizerSystem : JobComponentSystem
{
    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
#if DOTS
        float Width = 1f;
        float SampleSpacing = 0.25f;
        float ChannelSpacing = 0f;

        JobHandle job = new JobHandle();

        MusicVisualManager musicVis = MusicVisualManager.instance;

        bool musicValid = musicVis != null;
        if (musicValid)
        {
            BlobAssetReference<MusicDataBlobAsset> SampleData = new BlobAssetReference<MusicDataBlobAsset>();

            musicVis.GetSampleData(ref SampleData, out float sampleScale, out int SampleCount);

            job = Entities.ForEach((ref Translation trans, ref NonUniformScale scale, ref VisualizerData visData) =>
            {
                // if(visData.MusicData.IsCreated)
                // {
                //     #if true
                //     visData.SampleValue = SampleData.Value.sampleArray[(visData.Channel * SampleCount) + visData.index].Value * sampleScale;
                //     #else
                //     visData.SampleValue = visData.MusicData.Value.sampleArray[(visData.Channel * SampleCount) + visData.index].Value * sampleScale;
                //     #endif
                // }
                // else
                // {
                //     visData.SampleValue = 0f;
                // }
                visData.SampleValue = SampleData.Value.sampleArray[(visData.Channel * SampleCount) + visData.index].Value * sampleScale;

                trans.Value.x = visData.index * (Width + SampleSpacing);
                trans.Value.y = (ChannelSpacing * visData.Channel) + visData.SampleValue / 2f;

                scale.Value.x = Width;
                scale.Value.y = visData.SampleValue;
            }).Schedule(inputDeps);
        }
        else
        {
            job = Entities.ForEach((ref Translation trans, ref NonUniformScale scale, ref VisualizerData visData) =>
            {
                trans.Value.x = visData.index * (Width + SampleSpacing);
                trans.Value.y = (ChannelSpacing * visData.Channel) + visData.SampleValue / 2f;

                scale.Value.x = Width;
                scale.Value.y = visData.SampleValue;
            }).Schedule(inputDeps);
        }

        return job;
#else
        return default;
#endif
    }
}
