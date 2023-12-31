using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Profiling;

[RequireComponent(typeof(EndlessTerrain))]
[RequireComponent(typeof(WorldFeatures))]
public class ChunkBuilder : MonoBehaviour
{
    public static ChunkBuilder Instance { get; private set; }

    private readonly ProfilerMarker buildChunkNoiseMapMarker = new ProfilerMarker("ChunkBuilder.BuildChunkNoiseMap3D");
    private readonly ProfilerMarker buildChunkVoxelMapMarker = new ProfilerMarker("ChunkBuilder.BuildChunkVoxelMap");
    private readonly ProfilerMarker buildChunkMeshMarker = new ProfilerMarker("ChunkBuilder.BuildChunkMesh");
    private readonly ProfilerMarker placeChunkFeaturesMarker = new ProfilerMarker("ChunkBuilder.PlaceChunkFeatures");

    private WorldEventSystem worldEventSystem;
    private EndlessTerrain endlessTerrain;
    private WorldFeatures worldFeatures;

    private void Awake() {
        if(Instance != null && Instance != this) Destroy(this);
        else Instance = this;
    }

    private void Start() {
        worldEventSystem = WorldEventSystem.Instance;
        endlessTerrain = EndlessTerrain.Instance;
        worldFeatures = WorldFeatures.Instance;
    }

    public void BuildChunk(object sender, long chunkCoord) {
        NativeArray<long> chunkPos = new NativeArray<long>(1, Allocator.Persistent);

        NativeList<ChunkVertex> vertices = new NativeList<ChunkVertex>(Allocator.Persistent);
        NativeList<uint> indices = new NativeList<uint>(Allocator.Persistent);

        NativeArray<ushort> voxelMap = GetVoxelMap(chunkCoord);
        NativeList<EncodedVoxelMapEntry> encodedVoxelMap = new NativeList<EncodedVoxelMapEntry>(Allocator.Persistent);

        NativeArray<float> noiseOffset = new NativeArray<float>(2, Allocator.Persistent);
        NativeArray<float> noiseMap3D = CreateNewNoiseMap();

        NativeList<float> nativeFrequencies = endlessTerrain.NativeFrequencies;
        NativeList<float> nativeAmplitudes = endlessTerrain.NativeAmplitudes;

        ChunkBuildData chunkBuildData = new ChunkBuildData(
            ref chunkPos, ref vertices,
            ref indices, ref voxelMap, ref encodedVoxelMap,
            ref nativeFrequencies, ref nativeAmplitudes, 
            ref noiseOffset, ref noiseMap3D
        );

        NativeList<FeaturePlacement> featurePlacements = worldFeatures.GetPlacements();
        NativeParallelHashMap<FeaturePlacement, ushort> featureData = FeatureRegistry.FeatureData;
        NativeParallelHashMap<ushort, FeatureSettings> featureSettings = FeatureRegistry.FeatureSettings;

        Vector2 terrainNoiseOffset = endlessTerrain.NoiseOffset;
        chunkBuildData.chunkPos[0] = chunkCoord;

        chunkBuildData.noiseOffset[0] = terrainNoiseOffset.x;
        chunkBuildData.noiseOffset[1] = terrainNoiseOffset.y;

        if(!WorldStorage.DoesChunkExist(chunkCoord)) {
            buildChunkVoxelMapMarker.Begin();

            var chunkVoxelBuilderJob = new ChunkVoxelBuilderJob() {
                voxelMap = chunkBuildData.voxelMap,
                coord = chunkBuildData.chunkPos,

                frequencies = chunkBuildData.frequencies,
                amplitudes = chunkBuildData.amplitudes,

                noiseOffset = chunkBuildData.noiseOffset
            };

            JobHandle chunkVoxelJobHandle = chunkVoxelBuilderJob.Schedule();
            chunkVoxelJobHandle.Complete();

            buildChunkVoxelMapMarker.End();
            placeChunkFeaturesMarker.Begin();

            var chunkPlaceFeaturesJob = new ChunkPlaceFeaturesJob() {
                voxelMap = chunkBuildData.voxelMap,
                coord = chunkBuildData.chunkPos,

                featurePlacements = featurePlacements,
                featureData = featureData,
                featureSettings = featureSettings
            };

            JobHandle placeFeaturesJobHandle = chunkPlaceFeaturesJob.Schedule();
            placeFeaturesJobHandle.Complete();

            placeChunkFeaturesMarker.End();
        }

        BuildChunkMesh(chunkBuildData);
        worldEventSystem.InvokeChunkObjectBuild(new BuiltChunkData(ref vertices, ref indices, chunkPos[0]));

        SaveChunkVoxelMap(chunkCoord, voxelMap);
        chunkBuildData.Dispose();
    }

    private void BuildChunkNoiseMap3D(ChunkBuildData chunkBuildData) {
        buildChunkNoiseMapMarker.Begin();

        var noiseMapBuildJob = new ChunkNoiseMapBuildJob() {
            noiseOffset = chunkBuildData.noiseOffset,
            noiseMap = chunkBuildData.noiseMap3D
        };

        JobHandle noiseMapBuildJobHandle = noiseMapBuildJob.Schedule();
        noiseMapBuildJobHandle.Complete();

        buildChunkNoiseMapMarker.End();
    }

    private void BuildChunkMesh(ChunkBuildData chunkBuildData) {
        buildChunkMeshMarker.Begin();

        var chunkMeshJob = new ChunkMeshBuilderJob() {
            voxelMap = chunkBuildData.voxelMap,
            blockTypes = BlockRegistry.BlockTypeDictionary,

            vertices = chunkBuildData.vertices,
            indices = chunkBuildData.indices
        };

        JobHandle chunkMeshJobHandle = chunkMeshJob.Schedule();
        chunkMeshJobHandle.Complete();

        buildChunkMeshMarker.End();
    }

    public NativeArray<ushort> GetVoxelMap(long chunkCoord) {
        if(WorldStorage.DoesChunkExist(chunkCoord)) return WorldStorage.GetChunk(chunkCoord);
        else return CreateNewVoxelMap();
    }

    public void SaveChunkVoxelMap(long chunkCoord, NativeArray<ushort> voxelMap) {
        if(WorldStorage.DoesChunkExist(chunkCoord)) WorldStorage.SetChunk(chunkCoord, voxelMap);
        WorldStorage.AddChunk(chunkCoord, voxelMap);
    }

    public NativeArray<ushort> CreateNewVoxelMap() {
        return new NativeArray<ushort>((VoxelProperties.chunkWidth + 2) * VoxelProperties.chunkHeight * (VoxelProperties.chunkWidth + 2), Allocator.Persistent);
    }

    public NativeArray<float> CreateNewNoiseMap() {
        return new NativeArray<float>(((VoxelProperties.chunkWidth / 2) + 2) * (VoxelProperties.chunkHeight / 2) * ((VoxelProperties.chunkWidth / 2) + 2), Allocator.Persistent);
    }
}
