
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if DOTS
using Unity.Entities;
using Unity.Transforms;
using Unity.Rendering;
using Unity.Mathematics;

using Unity.Collections;
#endif

public class MusicVisualManager : MonoBehaviour
{

    public AudioSource musicPlayer;

    private AudioClip musicClip;

    [Header("Visual Settings")]
    public Mesh visualMesh;
    public Material visualMat;
    public Transform VisualHolder;

    public float Width = 1f;
    public float SampleSpacing = 0.25f;
    public float Scale = 1f;
    public float ChannelSpacing = 3f;

    [Header("Audio Info")]
    public int SampleCount = 5;

#if DOTS
    public BlobAssetReference<MusicDataBlobAsset> SampleData;
#else
    public List<float> SampleData = new List<float>();
#endif


#if DOTS
    private NativeArray<Entity> entityVisualizers;
    //private NativeArray<VisualizerData> VisualizerData;

    private EntityManager entityManager;
#else
    private Transform[] VisualTrans;
#endif

    public static MusicVisualManager instance;

    private int lastSampleCount;

    private bool VisualsActive = false;

    private void Start()
    {
        if(instance == null || instance == this)
        {
            instance = this;
        }
        else
        {
            DestroyImmediate(this);
        }

        StartPlaying();
    }

    void OnDestroy()
    {
        if(VisualsActive)
        {
            ShutdownVisuals();
        }

        if(instance == this)
        {
            instance = null;
        }
    }


    // Start is called before the first frame update
    void StartPlaying()
    {
        musicClip = musicPlayer.clip;
        musicPlayer.Play();

        lastSampleCount = Mathf.ClosestPowerOfTwo((int)Mathf.Pow(SampleCount, 2f));
        lastSampleData = new float[lastSampleCount];
        SetUpVisuals(lastSampleCount * musicClip.channels, lastSampleCount);
    }

    // Update is called once per frame
    void Update()
    {
        if (!musicPlayer.isPlaying)
            return;

        int sampleValue = Mathf.ClosestPowerOfTwo((int)Mathf.Pow(SampleCount, 2f));

        if(sampleValue != lastSampleCount)
        {
            ShutdownVisuals();
            lastSampleCount = sampleValue;
            lastSampleData = new float[lastSampleCount];
            SetUpVisuals(sampleValue * musicClip.channels, sampleValue);
        }

#if DOTS
        // if(!SampleData.IsCreated)
        // {
        //     Debug.Log("SampleData is Null");
        //     return;
        // }

        int arrayLength = SampleData.Value.sampleArray.Length;

        if (arrayLength != lastSampleCount * musicClip.channels)
        {
            //TODO: Resize the Blob array
            arrayLength = lastSampleCount * musicClip.channels;

            //Dispose of the old blob array
            SampleData.Dispose();

            using (BlobBuilder blobBuilder = new BlobBuilder(Allocator.Temp))
            {
                ref MusicDataBlobAsset musicDataBlobAsset = ref blobBuilder.ConstructRoot<MusicDataBlobAsset>();

                BlobBuilderArray<MusicSample> musicDataArray = blobBuilder.Allocate(ref musicDataBlobAsset.sampleArray, arrayLength);

                //Setup array data
                for(int i = 0; i < arrayLength; i++)
                {
                    musicDataArray[i] = new MusicSample { Value = 0 };
                }

                SampleData = blobBuilder.CreateBlobAssetReference<MusicDataBlobAsset>(Allocator.Persistent);

                for(int i = 0; i < entityVisualizers.Length; i++)
                {
                    var viz = entityManager.GetComponentData<VisualizerData>(entityVisualizers[i]);
                    viz.MusicData = SampleData;
                    entityManager.SetComponentData<VisualizerData>(entityVisualizers[i], viz);
                }
            }
        }
#else
        SampleData.Clear();
#endif

        if(lastSampleCount != lastSampleData.Length)
        {
            lastSampleData = new float[lastSampleCount];
        }

        for(int c = 0; c < musicClip.channels; c++)
        {
            UnityEngine.Profiling.Profiler.BeginSample($"{musicClip.name} Output Channel-{c}", this);

            //lastSampleData = new float[lastSampleCount];
            musicPlayer.GetOutputData(lastSampleData, c);
#if DOTS
            int startIndex = c * lastSampleCount;
            for(int i = 0; i < lastSampleCount; i++)
            {
                SampleData.Value.sampleArray[startIndex + i].Value = lastSampleData[i];
            }
#else
            SampleData.AddRange(sample);
#endif

            UnityEngine.Profiling.Profiler.EndSample();
        }

#if !DOTS
        UnityEngine.Profiling.Profiler.BeginSample("Setting Visual Transforms");
        //Update Visuals
        for(int c = 0; c < musicClip.channels; c++)
        {
            float channelHeight = (ChannelSpacing + Scale) * c;
            for(int s = 0; s < lastSampleCount; s++)
            {
                int index = (c * lastSampleCount) + s;
                float value = SampleData[index] * Scale;
                VisualTrans[index].localScale = new Vector3(Width, value, 1f);
                Vector3 pos = new Vector3(s * (Width + SampleSpacing), channelHeight + value / 2f);
                VisualTrans[index].localPosition = pos;
            }
        }
        UnityEngine.Profiling.Profiler.EndSample();
#endif
    }

    private float[] lastSampleData;


#if DOTS
    void SetUpVisuals(int EntityCount, int SampleCount)
    {
        World defaultWorld = World.DefaultGameObjectInjectionWorld;
        entityManager = defaultWorld.EntityManager;

        if (entityVisualizers != null && entityVisualizers.IsCreated)
            entityVisualizers.Dispose();

        entityVisualizers = new NativeArray<Entity>(EntityCount, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);

        //Try to create a GameObject and then convert that into an entity
        //GameObject cubeObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
        //DestroyImmediate(cubeObject.GetComponent<Collider>());
        //GameObjectConversionSettings settings = GameObjectConversionSettings.FromWorld(defaultWorld, null);
        //Entity entityPrefab = GameObjectConversionUtility.ConvertGameObjectHierarchy(cubeObject, settings);
        //Destroy(cubeObject);

        EntityArchetype archeType = entityManager.CreateArchetype(
            typeof(Translation),
            typeof(Rotation),
            typeof(NonUniformScale),
            typeof(RenderMesh),
            typeof(RenderBounds),
            typeof(LocalToWorld)
        );

        for (int i = 0; i < EntityCount; i++)
        {
            Entity myEntity = entityManager.CreateEntity(archeType);

            entityManager.AddSharedComponentData(myEntity, new RenderMesh
            {
                mesh = visualMesh,
                material = visualMat
            });

            entityManager.AddComponentData(myEntity, new RenderBounds
            {
                Value = new AABB() { Extents = new float3(0.5f, 0.5f, 0.5f) }
            });

            entityManager.AddComponentData(myEntity, new NonUniformScale
            {
                Value = new float3(1f, 1f, 1f)
            });

            //Setup the Visual Data
            entityManager.AddComponentData(myEntity, new VisualizerData
            {
                Channel = (byte)(i / SampleCount),
                index = i % SampleCount,
                SampleValue = 0f
            });

            entityVisualizers[i] = myEntity;
        }

        VisualsActive = true;
    }

    void ShutdownVisuals()
    {
        //I know this should be after destroy entity but the DestroyEntity function is erroring out at the moment
        entityVisualizers.Dispose();

        //This has been erroring out so try and do it last
        // if (entityManager != null)
        //     entityManager.DestroyEntity(entityVisualizers);

        VisualsActive = false;   
    }
#else
    void SetUpVisuals(int visCount, int SampleCount)
    {
        VisualTrans = new Transform[visCount];

        GameObject baseObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Destroy(baseObj.GetComponent<Collider>());

        for (int i = 0; i < visCount; i++)
        {
            GameObject visObj = Instantiate(baseObj, VisualHolder);
            VisualTrans[i] = visObj.transform;
        }

        Destroy(baseObj);

        VisualsActive = true;
    }

    void ShutdownVisuals()
    {
        for (int i = 0; i < VisualTrans.Length; i++)
        {
            if (VisualTrans[i] != null)
            {
                Destroy(VisualTrans[i].gameObject);
            }
        }

        VisualsActive = false;
    }
#endif
#if DOTS
    public void GetSampleData(ref BlobAssetReference<MusicDataBlobAsset> sampleData, out float Scale, out int SampleCount)
    {
        sampleData = SampleData;
#else
    public void GetSampleData(ref List<float> sampleData, out float Scale, out int SampleCount)
    {
        sampleData = SampleData;
#endif
        SampleCount = this.lastSampleCount;
        Scale = this.Scale;
    }
}
