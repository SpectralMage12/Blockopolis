using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;

public struct BuiltChunkData
{
    public NativeList<ChunkVertex> vertices;
    public NativeList<uint> indices;
    public long coord;

    public BuiltChunkData(ref NativeList<ChunkVertex> vertices, ref NativeList<uint> indices, long coord) {
        this.vertices = vertices;
        this.indices = indices;
        this.coord = coord;
    }
}
