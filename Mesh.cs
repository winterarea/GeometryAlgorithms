using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharpFont
{
    public class Vertex
    {
        public float x, y, z;
        public override string ToString()
        {
            return string.Format("v {0} {1} {2}", x, y, z);
        }
    }
    public class LoopEdge
    {
        public bool isHole;
        public bool isReverse;
        //public LoopEdge parentEdge;//如果是洞，则需要记录这个洞所属的外边
        public int[] vertexIndex;
        public Vertex Normal;
    }
    public class Face
    {
        public Vertex Normal;
        public int[] loopEdges;
    }
    public class Mesh
    {
        List<Vertex> vertices;
        List<LoopEdge> loopEdges;
        List<Face> faces;
        public List<Vertex> Vertices {
            get {
                return vertices;
            }
        }
        public List<LoopEdge> LoopEdges {
            get {
                return loopEdges;
            }
        }
        public List<Face> Faces {
            get {
                return faces;
            }
        }
        public Mesh()
        {
            vertices = new List<Vertex>();
            loopEdges = new List<LoopEdge>();
        }
        public Mesh SetVertices(PointF[] points)
        {
            for (int i = 0; i < points.Length; i++)
            {
                Vertex point = new Vertex() { x = points[i].P.X / 100f, y = points[i].P.Y / 100f, z = 0 };
                vertices.Add(point);
                //UnityEngine.Debug.Log(point);
            }
            return this;
        } 
        public Mesh AddLoopEdge(PointF[] points,int firstIndex, int lastIndex)
        {
            LoopEdge face = new LoopEdge();
            int[] vertexIndex = new int[lastIndex+1 - firstIndex];

            System.Numerics.Vector2[] directions = new System.Numerics.Vector2[lastIndex + 1 - firstIndex];
            for (int i = firstIndex; i < lastIndex+1; i++)
            {
                vertexIndex[i-firstIndex] = i;
            }

            PointF[] tmp = new PointF[lastIndex + 1 - firstIndex];
            double sum = 0;
            //https://www.cnblogs.com/helper/p/1911222.html
            for (int i = firstIndex; i < lastIndex; i++)
            {
                tmp[i - firstIndex] = points[i];
                sum += points[i].P.X * points[i + 1].P.Y - points[i + 1].P.X * points[i].P.Y;
            }
            tmp[lastIndex - firstIndex] = points[lastIndex];
            sum += points[lastIndex].P.X * points[firstIndex].P.Y - points[firstIndex].P.X * points[lastIndex].P.Y;
            LoopEdge loopEdge = new LoopEdge() { vertexIndex = vertexIndex };
            Vertex Normal = CalNormal(tmp);
            if (sum > 0) {//posCount > negCount
                loopEdge.isHole = true;
                if (Normal.z < 0) {
                    //vertices.Reverse(firstIndex, lastIndex + 1 - firstIndex);
                    /*
                    int length = loopEdge.vertexIndex.Length;
                    for (int j = 0; j < length / 2; j++) {
                        int tmpVertex = loopEdge.vertexIndex[j];
                        loopEdge.vertexIndex[j] = loopEdge.vertexIndex[length - 1 - j];
                        loopEdge.vertexIndex[length - 1 - j] = tmpVertex;
                    }
                    */
                    loopEdge.isReverse = true;
                }
            }
            else {
                loopEdge.isHole = false;
                if (Normal.z > 0) {
                    //vertices.Reverse(firstIndex, lastIndex + 1 - firstIndex);
                    loopEdge.isReverse = true;
                }
            }
            Console.WriteLine("sum,{0}", sum);
            UnityEngine.Debug.Log("sun:" + sum);
            UnityEngine.Debug.Log("Normal:" + CalNormal(points));
            loopEdges.Add(loopEdge);

            return this;
        }
        public void GenFaces(Mesh mesh)
        {
            faces = new List<Face>();
            for (int i = 0; i < mesh.loopEdges.Count; i++)
            {
                if (mesh.loopEdges[i].isHole == false)
                {
                    List<int> holes = new List<int>();

                    List<System.Numerics.Vector2> vectors = new List<System.Numerics.Vector2>();
                    for (int j = 0; j < mesh.loopEdges[i].vertexIndex.Length; j++)
                    {
                        vectors.Add(new System.Numerics.Vector2(mesh.vertices[mesh.loopEdges[i].vertexIndex[j]].x, mesh.vertices[mesh.loopEdges[i].vertexIndex[j]].y));
                    }

                    for (int k = 0; k < mesh.loopEdges.Count; k++)
                    {
                        if (k == i || mesh.loopEdges[k].isHole == false) continue;
                        int first = mesh.loopEdges[k].vertexIndex[0];
                        if (InPolygon(vectors.Count, vectors, new System.Numerics.Vector2(mesh.vertices[first].x, mesh.vertices[first].y)))
                        {
                            holes.Add(k);
                            Console.WriteLine("Poly " + k + "in Poly " + i);
                        }
                    }
                    Face face = new Face();
                    face.loopEdges = new int[holes.Count + 1];
                    face.loopEdges[0] = i;
                    for (int j = 0; j < holes.Count; j++)
                        face.loopEdges[j + 1] = holes[j];
                    faces.Add(face);
                    CalNormal(face);
                    /*
                    if (face.Normal.z > 0) {
                        int length = loopEdges[face.loopEdges[0]].vertexIndex.Length;
                        for (int j = 0; j < length / 2; j++) {
                            int tmp = loopEdges[face.loopEdges[0]].vertexIndex[j];
                            loopEdges[face.loopEdges[0]].vertexIndex[j] = loopEdges[face.loopEdges[0]].vertexIndex[length - 1 - j];
                            loopEdges[face.loopEdges[0]].vertexIndex[length - 1 - j] = tmp;
                        }
                        CalNormal(face);
                    }
                    */
                }
            }
        }
        public void RevertNormal()
        {
            float maxX=0, maxY = 0;
            for (int i = 0; i < loopEdges.Count; i++)
            {
                for(int j=0;j< loopEdges[i].vertexIndex.Length; j++)
                {
                    if (vertices[loopEdges[i].vertexIndex[j]].x > maxX) maxX = vertices[loopEdges[i].vertexIndex[j]].x;
                    if (vertices[loopEdges[i].vertexIndex[j]].y > maxY) maxY = vertices[loopEdges[i].vertexIndex[j]].y;
                    loopEdges[i].vertexIndex[j] += 4;
                }
                loopEdges[i].vertexIndex.Reverse();
                loopEdges[i].isHole = !loopEdges[i].isHole;
            }
            vertices.Insert(0, new Vertex() { x = 0, y = maxY });
            vertices.Insert(0, new Vertex() { x = 0, y = 0 });
            vertices.Insert(0, new Vertex() { x = maxX, y = 0 });
            vertices.Insert(0, new Vertex() { x = maxX, y = maxY });
            loopEdges.Insert(0, new LoopEdge() { isHole=false,vertexIndex=new int[] { 0,1,2,3} });
        }
        public void GenWavefontObj(Mesh mesh)
        {
            FileInfo fileInfo = new FileInfo("test-" + 0 + ".obj");
            if (!fileInfo.Exists)
            {

            }
            FileStream fileStream = fileInfo.Create();
            StreamWriter streamWriter = new StreamWriter(fileStream);
            for (int i = 0; i < mesh.vertices.Count; i++)
            {
                streamWriter.WriteLine(vertices[i]);
            }
            for (int i = 0; i < mesh.loopEdges.Count; i++)
            {
                if (mesh.loopEdges[i].isHole)
                    streamWriter.Write("-");
                streamWriter.Write("f");
                for (int j = 0; j < mesh.loopEdges[i].vertexIndex.Length;j++)
                {
                    streamWriter.Write(' ');
                    streamWriter.Write(mesh.loopEdges[i].vertexIndex[j] + 1);
                }
                streamWriter.WriteLine();
            }
            streamWriter.Flush();
            streamWriter.Close();
            fileStream.Close();



            //GenTrianguatedObj(mesh);
            //RevertNormal();
            GenFaces(mesh);
            int FaceCount = faces.Count;
            for (int i = 0; i < FaceCount; i++) {
                Extrude(i, new System.Numerics.Vector3(0, 0, .5f));
            }
            GenTrianguatedObj();
        }
        public void Extrude(int faceIndex, System.Numerics.Vector3 dir) {
            int EdgeCount = EdgeCheck(loopEdges[faces[faceIndex].loopEdges[0]].vertexIndex[0]);
            List<LoopEdge> newFaceLoops = new List<LoopEdge>();
            for (int i = 0; i < faces[faceIndex].loopEdges.Length; i++) {
                LoopEdge srcLoop = loopEdges[faces[faceIndex].loopEdges[i]];
                LoopEdge newLoop = new LoopEdge();
                newLoop.vertexIndex = new int[srcLoop.vertexIndex.Length];
                newLoop.isHole = srcLoop.isHole;
                newLoop.isReverse = srcLoop.isReverse;
                //挤压后新表面的所有顶点
                for (int j = 0; j < srcLoop.vertexIndex.Length; j++) {
                    newLoop.vertexIndex[j] = vertices.Count;
                    vertices.Add(new Vertex() { x = vertices[srcLoop.vertexIndex[j]].x + dir.X, y = vertices[srcLoop.vertexIndex[j]].y + dir.Y, z = dir.Z });
                }
                //连接原表面与新表面间的侧面
                Bridge(srcLoop, newLoop);
                //记录新表面的所有Loop
                newFaceLoops.Add(newLoop);
            }
            if (EdgeCount < 0) {
                //连接着其他面，所以原表面替换为新表面，挤压
                for (int i = 0; i < faces[faceIndex].loopEdges.Length; i++) {
                    //if(loopEdges[faces[faceIndex].loopEdges[i]].isHole)
                    //   loopEdges.RemoveAt(faces[faceIndex].loopEdges[i]);
                }
                faces.RemoveAt(faceIndex);

                Face newFace = new Face();
                newFace.loopEdges = new int[newFaceLoops.Count];
                for (int i = 0; i < newFaceLoops.Count; i++) {
                    loopEdges.Add(newFaceLoops[i]);
                    newFace.loopEdges[i] = loopEdges.Count - 1;
                }
                faces.Add(newFace);
                CalNormal(newFace);
            }
            else {
                //孤立面，需要挤压，原位置生成新面，原loop反向法线
                //UnityEngine.Debug.Log("Before;" + faceIndex + "," + faces[faceIndex].Normal);
                int length = loopEdges[faces[faceIndex].loopEdges[0]].vertexIndex.Length;
                for (int j = 0; j < length / 2; j++)
                {
                    int tmp = loopEdges[faces[faceIndex].loopEdges[0]].vertexIndex[j];
                    loopEdges[faces[faceIndex].loopEdges[0]].vertexIndex[j] = loopEdges[faces[faceIndex].loopEdges[0]].vertexIndex[length - 1 - j];
                    loopEdges[faces[faceIndex].loopEdges[0]].vertexIndex[length - 1 - j] = tmp;
                    //UnityEngine.Debug.Log("Exchange;" + j + "," + (length - 1 - j));
                }
                CalNormal(faces[faceIndex]);
                //UnityEngine.Debug.Log("After;"+faceIndex + "," + faces[faceIndex].Normal);

                Face newFace = new Face();
                newFace.loopEdges = new int[newFaceLoops.Count];
                for (int i = 0; i < newFaceLoops.Count; i++) {
                    loopEdges.Add(newFaceLoops[i]);
                    newFace.loopEdges[i] = loopEdges.Count - 1;
                }
                faces.Add(newFace);
                CalNormal(newFace);
            }
        }

        public void Bridge(LoopEdge loop1, LoopEdge loop2) {
            if(loop1.isHole==false)
                UnityEngine.Debug.Log("LoopNormal:" + CalNormal(loop1) + "," + CalNormal(loop2));
            for (int i = 0; i < loop1.vertexIndex.Length; i++) {
                LoopEdge newLoop = new LoopEdge();
                newLoop.vertexIndex = new int[4] { loop1.vertexIndex[(i + 1) % loop1.vertexIndex.Length], loop1.vertexIndex[i] ,loop2.vertexIndex[i], loop2.vertexIndex[(i + 1) % loop2.vertexIndex.Length], };
                newLoop.isHole = false;
                loopEdges.Add(newLoop);
                Face newFace = new Face();
                newFace.loopEdges = new int[] { loopEdges.Count - 1 };
                faces.Add(newFace);
                CalNormal(newFace);
            }
        }
        public int EdgeCheck(int vertexIndex) {
            int Count = 0;
            for (int i = 0; i < loopEdges.Count; i++) {
                if (Array.BinarySearch(loopEdges[i].vertexIndex, vertexIndex) > 0) {
                    Count++;
                }
            }
            return Count;
        }
        public Vertex CalNormal(Face face) {
            Vertex Normal = new Vertex();
            int[] vert = loopEdges[face.loopEdges[0]].vertexIndex;
            for (int i = 0; i < vert.Length; i++) {
                int m = (i + 1) % vert.Length;
                int n = (i + 2) % vert.Length;
                Vertex v1 = new Vertex() { x = vertices[vert[i]].x - vertices[vert[m]].x, y = vertices[vert[i]].y - vertices[vert[m]].y, z = vertices[vert[i]].z - vertices[vert[m]].z };
                Vertex v2 = new Vertex() { x = vertices[vert[m]].x - vertices[vert[n]].x, y = vertices[vert[m]].y - vertices[vert[n]].y, z = vertices[vert[m]].z - vertices[vert[n]].z };
                Normal.x += v1.y * v2.z - v2.y * v1.z;
                Normal.y += v1.x * v2.z - v2.x * v1.z;
                Normal.z += v1.x * v2.y - v2.x * v1.y;
            }
            face.Normal = new Vertex() { x = Normal.x / vert.Length, y = Normal.y / vert.Length, z = Normal.z / vert.Length };
            return face.Normal;
        }
        public Vertex CalNormal(PointF[] vectors) {
            Vertex Normal = new Vertex();
            for (int i = 0; i < vectors.Length; i++) {
                int m = (i + 1) % vectors.Length;
                int n = (i + 2) % vectors.Length;
                Vertex v1 = new Vertex() { x = vectors[i].P.X - vectors[m].P.X, y = vectors[i].P.Y - vectors[m].P.Y, z = 0 };
                Vertex v2 = new Vertex() { x = vectors[m].P.X - vectors[n].P.X, y = vectors[m].P.Y - vectors[n].P.Y, z = 0 };
                Normal.x += v1.y * v2.z - v2.y * v1.z;
                Normal.y += v1.x * v2.z - v2.x * v1.z;
                Normal.z += v1.x * v2.y - v2.x * v1.y;
            }
            Normal = new Vertex() { x = Normal.x / vectors.Length, y = Normal.y / vectors.Length, z = Normal.z / vectors.Length };
            return Normal;
        }
        public Vertex CalNormal(LoopEdge loop) {
            Vertex Normal = new Vertex();
            for (int i = 0; i < loop.vertexIndex.Length; i++) {
                int m = (i + 1) % loop.vertexIndex.Length;
                int n = (i + 2) % loop.vertexIndex.Length;
                Vertex v1 = new Vertex() { x = vertices[loop.vertexIndex[i]].x - vertices[loop.vertexIndex[m]].x, y = vertices[loop.vertexIndex[i]].y - vertices[loop.vertexIndex[m]].y, z = vertices[loop.vertexIndex[i]].z - vertices[loop.vertexIndex[m]].z };
                Vertex v2 = new Vertex() { x = vertices[loop.vertexIndex[m]].x - vertices[loop.vertexIndex[n]].x, y = vertices[loop.vertexIndex[m]].y - vertices[loop.vertexIndex[n]].y, z = vertices[loop.vertexIndex[m]].z - vertices[loop.vertexIndex[n]].z };
                Normal.x += v1.y * v2.z - v2.y * v1.z;
                Normal.y += v1.x * v2.z - v2.x * v1.z;
                Normal.z += v1.x * v2.y - v2.x * v1.y;
            }
            Normal = new Vertex() { x = Normal.x / loop.vertexIndex.Length, y = Normal.y / loop.vertexIndex.Length, z = Normal.z / loop.vertexIndex.Length };
            return Normal;
        }
        public void GenTrianguatedObj()
        {
            FileInfo fileInfo = new FileInfo("test-" + 1 + ".obj");
            FileStream fileStream = fileInfo.Create();
            StreamWriter streamWriter = new StreamWriter(fileStream);
            for (int i = 0; i < vertices.Count; i++)
            {
                streamWriter.WriteLine(vertices[i]);
            }

            for (int i = 0; i < faces.Count; i++)
            {
                List<double> earCutData = new List<double>();
                List<int> holeIndices = new List<int>();
                List<int> earVertexMap = new List<int>();//earcut中的点下表与vertices中的点下标映射
                bool needReverse = false;
                for (int j = 0; j < faces[i].loopEdges.Length; j++)
                {
                    if (loopEdges[faces[i].loopEdges[j]].isHole)
                    {
                        holeIndices.Add(earVertexMap.Count);
                    }
                    for (int k = 0; k < loopEdges[faces[i].loopEdges[j]].vertexIndex.Length; k++)
                    {
                        if (faces[i].Normal.z != 0) {
                            if (Faces[i].Normal.z > 0)
                                needReverse = true;
                            earCutData.Add(vertices[loopEdges[faces[i].loopEdges[j]].vertexIndex[k]].x);
                            earCutData.Add(vertices[loopEdges[faces[i].loopEdges[j]].vertexIndex[k]].y);
                        }
                        else if (faces[i].Normal.y != 0) {
                            if (Faces[i].Normal.y < 0)
                                needReverse = true;
                            earCutData.Add(vertices[loopEdges[faces[i].loopEdges[j]].vertexIndex[k]].x);
                            earCutData.Add(vertices[loopEdges[faces[i].loopEdges[j]].vertexIndex[k]].z);
                        }
                        else {
                            if (Faces[i].Normal.x < 0)
                                needReverse = true;
                            earCutData.Add(vertices[loopEdges[faces[i].loopEdges[j]].vertexIndex[k]].y);
                            earCutData.Add(vertices[loopEdges[faces[i].loopEdges[j]].vertexIndex[k]].z);
                        }
                        earVertexMap.Add(loopEdges[faces[i].loopEdges[j]].vertexIndex[k]);
                    }
                }
                List<int> earCutResult = EarcutNet.Earcut.Tessellate(earCutData, holeIndices);
                if (needReverse)
                    earCutResult.Reverse();
                if (LoopEdges[Faces[i].loopEdges[0]].isReverse)
                    earCutResult.Reverse();
                for (int j = 0; j < earCutResult.Count; j += 3)
                {
                    streamWriter.Write("f ");
                    streamWriter.Write(earVertexMap[earCutResult[j]] + 1);
                    streamWriter.Write(' ');
                    streamWriter.Write(earVertexMap[earCutResult[j+1]] + 1);
                    streamWriter.Write(' ');
                    streamWriter.Write(earVertexMap[earCutResult[j+2]] + 1);
                    streamWriter.WriteLine();
                    //Console.WriteLine((earVertexMap[earCutResult[j]] + 1)+","+(earVertexMap[earCutResult[j + 1]] + 1) +","+(earVertexMap[earCutResult[j + 2]] + 1));
                }
            }

            streamWriter.Flush();
            streamWriter.Close();
            fileStream.Close();
        }
        public void GenTrianguatedObj(Mesh mesh)
        {
            List<double> earCutData = new List<double>();
            List<int> holeIndices = new List<int>();
            for (int i = 0; i < mesh.loopEdges.Count; i++)
            {
                for(int j = 0; j < mesh.loopEdges[i].vertexIndex.Length; j++)
                {
                    earCutData.Add(mesh.vertices[mesh.loopEdges[i].vertexIndex[j]].x);
                    earCutData.Add(mesh.vertices[mesh.loopEdges[i].vertexIndex[j]].y);
                }
            }
            FileInfo fileInfo = new FileInfo("test-" + 1 + ".obj");
            if (!fileInfo.Exists)
            {

            }
            FileStream fileStream = fileInfo.Create();
            StreamWriter streamWriter = new StreamWriter(fileStream);
            


            int StartIndex = 0;
            List<int> loopEdgeIndex = new List<int>();
            for (int i = 0; i < mesh.loopEdges.Count; i++)
            {
                earCutData.Clear();
                holeIndices.Clear();
                loopEdgeIndex.Clear();
                loopEdgeIndex.Add(i);
                if (mesh.loopEdges[i].isHole == false)
                {
                    List<System.Numerics.Vector2> vectors = new List<System.Numerics.Vector2>();
                    for (int j = 0; j < mesh.loopEdges[i].vertexIndex.Length; j++)
                    {
                        earCutData.Add(mesh.vertices[mesh.loopEdges[i].vertexIndex[j]].x);
                        earCutData.Add(mesh.vertices[mesh.loopEdges[i].vertexIndex[j]].y);
                        vectors.Add(new System.Numerics.Vector2(mesh.vertices[mesh.loopEdges[i].vertexIndex[j]].x, mesh.vertices[mesh.loopEdges[i].vertexIndex[j]].y));
                    }
                    for(int k = 0; k < mesh.loopEdges.Count; k++)
                    {
                        if (k == i||mesh.loopEdges[k].isHole==false) continue;
                        int first = mesh.loopEdges[k].vertexIndex[0];
                        if (InPolygon(vectors.Count, vectors, new System.Numerics.Vector2(mesh.vertices[first].x, mesh.vertices[first].y)))
                        {
                            loopEdgeIndex.Add(k);
                            //在内部
                            holeIndices.Add(earCutData.Count / 2);
                            for (int j = 0; j < mesh.loopEdges[k].vertexIndex.Length; j++)
                            {
                                earCutData.Add(mesh.vertices[mesh.loopEdges[k].vertexIndex[j]].x);
                                earCutData.Add(mesh.vertices[mesh.loopEdges[k].vertexIndex[j]].y);
                            }
                            Console.WriteLine("Poly " + k + "in Poly " + i);
                        }
                    }

                    for (int k = 0; k < earCutData.Count; k += 2)
                    {
                        streamWriter.Write("v ");
                        streamWriter.Write(earCutData[k]);
                        streamWriter.Write(' ');
                        streamWriter.Write(earCutData[k + 1]);
                        streamWriter.Write(" 0");
                        streamWriter.WriteLine();
                    }
                    //最后一个，或者下一个Loop不为孔时，三角化当前多边形
                    List<int> earCutResult = EarcutNet.Earcut.Tessellate(earCutData, holeIndices);
                    for (int j = 0; j < earCutResult.Count; j += 3)
                    {
                        streamWriter.Write("f ");
                        streamWriter.Write(StartIndex + earCutResult[j] + 1);
                        streamWriter.Write(' ');
                        streamWriter.Write(StartIndex + earCutResult[j + 1] + 1);
                        streamWriter.Write(' ');
                        streamWriter.Write(StartIndex + earCutResult[j + 2] + 1);
                        streamWriter.WriteLine();
                    }
                    StartIndex += earCutData.Count / 2;
                }
            }

            
            
            streamWriter.Flush();
            streamWriter.Close();
            fileStream.Close();
        }
        /// <summary>
        /// 
        /// https://www.cnblogs.com/anningwang/p/7581545.html
        /// </summary>
        /// <param name="count"></param>
        /// <param name="vectors"></param>
        /// <param name="vert"></param>
        /// <returns></returns>
        public bool InPolygon(int count,List<System.Numerics.Vector2> vectors, System.Numerics.Vector2 vert)
        {
            int i, j;
            float minX = vectors[0].X, minY = vectors[0].Y, maxX = vectors[0].X, maxY = vectors[0].Y;
            for(i = 0; i < vectors.Count; i++)
            {
                if (vectors[i].X < minX) minX = vectors[i].X;
                if (vectors[i].Y < minY) minY = vectors[i].Y;
                if (vectors[i].X > maxX) maxX = vectors[i].X;
                if (vectors[i].Y > maxY) maxY = vectors[i].Y;
            }
            if (vert.X < minX || vert.X > maxX || vert.Y < minY || vert.Y > maxY)
            {
                return false;
            }
            bool c = false;
            for (i = 0, j = count - 1; i < count; j = i++)
            {
                if (((vectors[i].Y > vert.Y) != (vectors[j].Y > vert.Y)) &&
                 (vert.X < (vectors[j].X - vectors[i].X) * (vert.Y - vectors[i].Y) / (vectors[j].Y - vectors[i].Y) + vectors[i].X))
                    c = !c;
            }
            return c;
        }
    }
}
