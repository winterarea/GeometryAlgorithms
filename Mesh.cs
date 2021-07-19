public void Extrude(int faceIndex, System.Numerics.Vector2 dir)
{
    for(int i = 0; i < faces[faceIndex].loopEdges.Length; i++)
    {
        LoopEdge newLoop = new LoopEdge();
        LoopEdge srcLoop = loopEdges[faces[faceIndex].loopEdges[i]];
        newLoop.vertexIndex = new int[srcLoop.vertexIndex.Length];
        for (int j=0;j< srcLoop.vertexIndex.Length; j++)
        {
            newLoop.vertexIndex[j] = vertices.Count;
            vertices.Add(new Vertex() { x = vertices[srcLoop.vertexIndex[j]].x, y = vertices[srcLoop.vertexIndex[j]].y, z = 1 });
        }
    }
}
