using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


public class EvenlyDistributedDots : MonoBehaviour
{
    public int numberOfPoints = 500;

    [SerializeField]
    bool distributePoints = false;

    [SerializeField]
    List<Vector2> shapePoints;

    public int[] triangulationIndices;


    [SerializeField]
    List<Triangle> shapeTriangles;
    float totalShapeArea;

    public List<float> pointPerTriangle = new List<float>();

    public LineRenderer shapeRenderer, triangulationRenderer;

    public GameObject pointSample, pointSample2;
    List<Vector2> samplePositions = new List<Vector2>();
    List<Transform> sampleObjects = new List<Transform>();

    BenTools.Mathematics.VoronoiGraph vg;

    // Start is called before the first frame update
    void Start()
    {
        DrawShape();
        TriangulateShape();

        shapeRenderer.gameObject.SetActive(false);
        triangulationRenderer.gameObject.SetActive(false);
    }

    private void OnDrawGizmos()
    {
        if (vg != null)
        {
            Gizmos.color = Color.blue;

            //var vertices = vg.Vertizes.ToArray();
            //for (int i = 0; i < 10; i++)
            //{
            //    if (i < vertices.Length)
            //    {
            //        vertices[i].
            //    }

            //}

            //var edges = vg.Edges.ToArray();
            //for (int i = 0; i < 10; i++)
            //{
            //    if (i < edges.Length)
            //    {
            //        Vector2 p1 = new Vector2((float)edges[i].LeftData[0], (float)edges[i].LeftData[1]);
            //        Vector2 p2 = new Vector2((float)edges[i].RightData[0], (float)edges[i].RightData[1]);
            //        Gizmos.DrawLine(p1, p2);
            //    }

            //}

            foreach (var edge in vg.Edges)
            {
                Vector2 p1 = new Vector2((float)edge.LeftData[0], (float)edge.LeftData[1]);
                Vector2 p2 = new Vector2((float)edge.RightData[0], (float)edge.RightData[1]);

                Gizmos.DrawLine(p1, p2);
            }
        }
    }



    float CalculateTriangleArea(float a, float b, float c)
    {
        float s = (a + b + c) / 2;
        return Mathf.Sqrt(s * (s - a) * (s - b) * (s - c));
    }

    private void TriangulateShape()
    {
        Triangulator tr = new Triangulator(shapePoints.Where((p) => { return p != shapePoints.Last(); }).ToArray());
        triangulationIndices = tr.Triangulate();

        //Little tuning needed... :/
        var indices = triangulationIndices.ToList();
        //indices.Add(shapePoints.Count - 3);
        //indices.Add(shapePoints.Count - 2);
        //indices.Add(shapePoints.Count - 1);
        triangulationIndices = indices.ToArray();


        totalShapeArea = 0;
        for (int i = 0; i < triangulationIndices.Length; i += 3)
        {
            LineRenderer triangle = GameObject.Instantiate(triangulationRenderer);
            triangle.transform.parent = triangulationRenderer.transform;
            triangle.name = "Triangle" + (i + 1);

            shapeTriangles.Add(new Triangle(shapePoints[triangulationIndices[i]], shapePoints[triangulationIndices[i + 1]], shapePoints[triangulationIndices[i + 2]]));

            totalShapeArea += CalculateTriangleArea(
                    (shapeTriangles.Last().P3 - shapeTriangles.Last().P1).magnitude,
                    (shapeTriangles.Last().P1 - shapeTriangles.Last().P2).magnitude,
                    (shapeTriangles.Last().P2 - shapeTriangles.Last().P3).magnitude
                );

            triangle.positionCount = 4;
            triangle.SetPositions(
                new Vector3[] {
                    new Vector3(shapeTriangles.Last().P1.x, shapeTriangles.Last().P1.y, 0),
                    new Vector3(shapeTriangles.Last().P2.x, shapeTriangles.Last().P2.y, 0),
                    new Vector3(shapeTriangles.Last().P3.x, shapeTriangles.Last().P3.y, 0),
                    new Vector3(shapeTriangles.Last().P1.x, shapeTriangles.Last().P1.y, 0)
                });
        }
    }

    void DistributePointsOnTriangles()
    {
        pointPerTriangle.Clear();
        sampleObjects.Clear();
        for (int i = 0; i < shapeTriangles.Count; i++)
        {
            float triangleArea = CalculateTriangleArea(
                    (shapeTriangles[i].P3 - shapeTriangles[i].P1).magnitude,
                    (shapeTriangles[i].P1 - shapeTriangles[i].P2).magnitude,
                    (shapeTriangles[i].P2 - shapeTriangles[i].P3).magnitude
                );
            pointPerTriangle.Add((numberOfPoints * triangleArea / totalShapeArea));


            //width-height of half rect
            Vector2 xVector = (shapeTriangles[i].P3 - shapeTriangles[i].P1);
            Vector2 yVector = (shapeTriangles[i].P2 - shapeTriangles[i].P1);
            float v1 = xVector.magnitude;
            float v2 = yVector.magnitude;
            float pointsOnTriangle = pointPerTriangle.Last() * 2;

            float nx = (
                Mathf.Sqrt(
                        ((v1 / v2) * pointsOnTriangle) +
                        (Mathf.Pow(v1 - v2, 2) / (4 * Mathf.Pow(v2, 2)))
                    ) -
                    (v1 - v2) / 2 * v2);
            float ny = pointsOnTriangle / nx;

            Debug.Log("pointsOnTriangle: " + pointsOnTriangle + "   nx: " + nx + "   ny:" + ny);

            float xUnit = (Mathf.Sign(nx) / nx);
            float yUnit = (Mathf.Sign(ny) / ny);

            for (float ix = xUnit / 2; ix < 1 + xUnit / 2; ix += xUnit)
            {

                for (float iy = yUnit / 2; iy < 1 + yUnit / 2; iy += yUnit)
                {
                    Vector2 x, y;
                    if ((ix + iy) < 1)
                    {
                        x = xVector * ix;
                        y = yVector * iy;

                        GameObject point = GameObject.Instantiate(pointSample);
                        samplePositions.Add(shapeTriangles[i].P1 + x + y);
                        point.transform.position = samplePositions.Last();

                        sampleObjects.Add(point.transform);
                    }
                }
            }

        }

        Debug.Log("placedPoints: " + samplePositions.Count);

        //CorrectPoints();
    }

    Vector2 VectorToVector2(BenTools.Mathematics.Vector v)
    {
        return new Vector2((float)v[0], (float)v[1]);
    }
    /*
    void CorrectPoints()
    {

        vg =
            BenTools.Mathematics.Fortune.FilterVG(
            BenTools.Mathematics.Fortune.ComputeVoronoiGraph(samplePositions.Select((point) => { return new BenTools.Mathematics.Vector(new double[] { point.x, point.y }); })), 0f, 0.5f);

        avgDotDist = 0;
        variance = 0;
        Dictionary<BenTools.Mathematics.Vector, List<BenTools.Mathematics.Vector>> longestEdgePoints = new Dictionary<BenTools.Mathematics.Vector, List<BenTools.Mathematics.Vector>>();

        Debug.Log("vg points: "+ vg.Vertizes.Count+" vg edges: "+vg.Edges.Count);


        foreach (var p in vg.Vertizes)
        {
            GameObject point = GameObject.Instantiate(pointSample2);
            point.transform.position = VectorToVector2(p);
        }

        foreach (var edge in vg.Edges)
        {
            Vector2 p1 = VectorToVector2(edge.LeftData);
            Vector2 p2 = VectorToVector2(edge.RightData);

            //Fill dictionary with fartest point
            //if (longestEdgePoints.ContainsKey(edge.LeftData))
            //{
            //    Vector2 p = new Vector2((float)longestEdgePoints[edge.LeftData][0], (float)longestEdgePoints[edge.LeftData][1]);

            //    if (Vector2.Distance(p, p1) < Vector2.Distance(p2, p1))
            //    {
            //        longestEdgePoints[edge.LeftData] = edge.RightData;
            //    }

            //}
            //else
            //{
            //    longestEdgePoints.Add(edge.LeftData, edge.RightData);
            //}

            if (longestEdgePoints.ContainsKey(edge.LeftData))
            {
                longestEdgePoints[edge.LeftData].Add(edge.RightData);
            }
            else
            {
                longestEdgePoints.Add(edge.LeftData,new List<BenTools.Mathematics.Vector>() { edge.RightData });
            }

            avgDotDist += Vector2.Distance(p1, p2);
        }
        avgDotDist /= vg.Edges.Count;

        foreach (var edge in vg.Edges)
        {
            Vector2 p1 = new Vector2((float)edge.LeftData[0], (float)edge.LeftData[1]);
            Vector2 p2 = new Vector2((float)edge.RightData[0], (float)edge.RightData[1]);
            variance += Mathf.Pow(Vector2.Distance(p1, p2) - avgDotDist, 2);
        }
        variance = Mathf.Sqrt(variance / vg.Edges.Count);

        if (variance > varianceTreshold)
        {
            Debug.Log(longestEdgePoints.Count+"   "+ sampleObjects.Count);
            //Correction iteration
            for (int i = 0; i < longestEdgePoints.Count; i++)
            {
                Vector2 p1 = new Vector2((float)longestEdgePoints.ElementAt(i).Key[0], (float)longestEdgePoints.ElementAt(i).Key[1]);



                Vector2 center=Vector2.zero;
                foreach (var edgePoints in longestEdgePoints.ElementAt(i).Value)
                {
                    center+= new Vector2((float)edgePoints[0], (float)edgePoints[1]);
                }
                center /= longestEdgePoints.ElementAt(i).Value.Count;

                //Mathf.Lerp(Vector2.Distance(p1,p2),avgDotDist,.5f);

                //Vector2 p2 = new Vector2((float)longestEdgePoints.ElementAt(i).Value[0], (float)longestEdgePoints.ElementAt(i).Value[1]);
                //sampleObjects[i].position = p1 + (p2 - p1).normalized * (Vector2.Distance(p1, p2)-avgDotDist)/2;//Mathf.Lerp(Vector2.Distance(p1,p2),avgDotDist,.5f);

                samplePositions[i] = center;
            }

            for (int i = 0; i < longestEdgePoints.Count; i++) {
                sampleObjects[i].position = samplePositions[i];
            }

            //CorrectPoints();
        }

    }
    */
    public bool correctionIteration = false;

    public float variance = 0;
    public float avgDotDist = 0;
    public float varianceTreshold = 0.05f;

    void Update()
    {
        if (distributePoints)
        {
            distributePoints = false;
            DistributePointsOnTriangles();
        }

        if (correctionIteration)
        {
            correctionIteration = false;
            //CorrectPoints();
        }
    }




    private void DrawShape()
    {


        shapeRenderer.positionCount = shapePoints.Count;

        shapeRenderer.SetPositions(shapePoints.Select(
                        (point) => { return new Vector3(point.x, point.y, 0); }
                        ).ToArray()
                        );
    }
}
