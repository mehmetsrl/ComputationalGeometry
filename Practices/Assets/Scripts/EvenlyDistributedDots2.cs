using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class EvenlyDistributedDots2 : MonoBehaviour
{

	[System.Serializable]
	public struct Point
	{
		public Vector3 V3Pos { get { return new Vector3(position.x, position.y, 0); } }
		public Vector2 position;

		public List<Edge> conectedEdges;

		public Point(Vector2 position)
		{
			this.position = position;
			conectedEdges = new List<Edge>();
		}

		public override string ToString()
		{
			return ("Point at: " + position);
		}

		public List<Point> GetNeigbourPoints()
		{
			List<Point> neigbours = new List<Point>(conectedEdges.Count);

			foreach (var edge in conectedEdges)
			{
				if (edge.StartPoint == this)
				{
					neigbours.Add(edge.EndPoint);
				}
				else
				{
					neigbours.Add(edge.StartPoint);
				}
			}

			return neigbours;
		}

		public static bool operator ==(Point p1, Point p2)
		{
			return p1.Equals(p2);
		}

		public static bool operator !=(Point p1, Point p2)
		{
			return !p1.Equals(p2);
		}
	};


	[System.Serializable]
	public class Edge
	{
		public Point StartPoint { get { return start; } }
		public Point EndPoint { get { return end; } }
		[SerializeField]
		string description;

		[SerializeField]
		Point start, end;

		public override string ToString()
		{
			return description;
		}

		public Edge(Point start, Point end)
		{
			this.start = start;
			this.end = end;

			description = "Edge from "+start.ToString() + " | to " + end.ToString();
		}

	}

	public int numberOfPoints = 500;
	public float gridDist = 5f;

	[SerializeField]
	bool distributePoints = false;
	//[SerializeField]
	//bool randomRayCasting = false;

	[SerializeField]
	List<Point> shapePoints = new List<Point>() {
	new Point(new Vector2(1,-3)),
	new Point(new Vector2(-0.5f,-4)),
	new Point(new Vector2(-2,-3)),
	new Point(new Vector2(-3,-1)),
	new Point(new Vector2(-1.5f,1.5f)),
	new Point(new Vector2(0.5f,2)),
	new Point(new Vector2(2.5f,1)),
	new Point(new Vector2(3,-0.5f)),
	new Point(new Vector2(2.5f,-2.5f)),
	new Point(new Vector2(1,-3f))
	};

	[SerializeField]
	List<Edge> shapeEdges = new List<Edge>();


	public LineRenderer shapeRenderer, baundaryRenderer, rayRenderer;

	public GameObject pointSample;

	GameObject pointHolder;

	// Start is called before the first frame update
	void Start()
	{
		pointHolder = new GameObject("Points");
		DrawShape();
		DrawBoundaries();
	}

	void Update()
	{
		if (distributePoints)
		{
			distributePoints = false;
			DistributePoints();
		}
		////Testing
		//if (randomRayCasting)
		//{
		//	randomRayCasting = false;
		//	RandomRaycasting();
		//}

	}




	private void DrawShape()
	{
		for (int i = 0; i < shapePoints.Count - 1; i++)
		{
			shapeEdges.Add(new Edge(shapePoints[i], shapePoints[i + 1]));
			shapePoints[i].conectedEdges.Add(shapeEdges.Last());
		}

		shapeRenderer.positionCount = shapePoints.Count;

		shapeRenderer.SetPositions(shapePoints.Select(
				(point) => { return point.V3Pos; }
				).ToArray()
				);
	}

	float lBound, rBound, bBound, tBound;
	float w, h;
	private void DrawBoundaries()
	{
		baundaryRenderer.positionCount = shapePoints.Count;

		lBound = shapePoints.Min(p => p.position.x);
		rBound = shapePoints.Max(p => p.position.x);
		bBound = shapePoints.Min(p => p.position.y);
		tBound = shapePoints.Max(p => p.position.y);
		w = rBound - lBound;
		h = tBound - bBound;

		baundaryRenderer.positionCount = 5;
		baundaryRenderer.SetPosition(0, new Vector3(lBound, bBound, 0));
		baundaryRenderer.SetPosition(1, new Vector3(lBound, tBound, 0));
		baundaryRenderer.SetPosition(2, new Vector3(rBound, tBound, 0));
		baundaryRenderer.SetPosition(3, new Vector3(rBound, bBound, 0));
		baundaryRenderer.SetPosition(4, new Vector3(lBound, bBound, 0));
	}

	void DistributePoints()
	{
		int placedPoints = PlacePoints(numberOfPoints);		
		Debug.Log("Placed points for first try: " + placedPoints);
		DrawPoints();

		//Simple area based prediction here
		placedPoints = PlacePoints(Mathf.RoundToInt(Mathf.Pow(numberOfPoints, 2) / placedPoints));
        Debug.Log("Placed points in the end: " + placedPoints);
		DrawPoints();
    }


	void DrawPoints()
    {
        for (int i = 0; i < pointHolder.transform.childCount; i++)
        {
			Destroy(pointHolder.transform.GetChild(i).gameObject);
        }

        foreach (var pointListOnIntersectionInterval in placedPointList)
        {
			if (pointListOnIntersectionInterval.Count > 0)
			{

				foreach (var point in pointListOnIntersectionInterval)
				{
					GameObject p = GameObject.Instantiate(pointSample);
					p.transform.position = point;
					p.transform.parent = pointHolder.transform;
				}
			}
        }
    }

	LinkedList<LinkedList<Vector2>> placedPointList = new LinkedList<LinkedList<Vector2>>();
	LinkedList<GameObject> iterationRayList = new LinkedList<GameObject>();

	int PlacePoints(int pointsToBePlaced)
    {
		int placedPoints=0;
		placedPointList.Clear();
		Queue<Edge> edgeQueue = new Queue<Edge>(shapeEdges.OrderBy((edge) => { return Mathf.Min(edge.StartPoint.position.y, edge.EndPoint.position.y); }));
		LinkedList<Edge> intersectionCheckList = new LinkedList<Edge>();

		//Fixed size data structure needed for add/remove operations. List takes O(n) when we not give fixed size to it. Or linked list may be used.
		LinkedList<Point> intersections = new LinkedList<Point>();

		////For displaying rays for each case
		//iterationRayList.Last?.Value.SetActive(false);
		//iterationRayList.AddLast(new GameObject("raysFor " + pointsToBePlaced + " Points"));

		Vector2 rayStartPoint = new Vector2(lBound, 0);
		Vector2 rayEndPoint = new Vector2(rBound, 0);

		//(w/l)*(h/l) = n
		float l = Mathf.Sqrt(w*h/pointsToBePlaced);

		for (float ly = bBound; ly < tBound; ly += l)
        {
			intersections.Clear();

			rayStartPoint.y = ly;
			rayEndPoint.y = ly;

			////For displaying rays for each ray
			//LineRenderer rayLine = GameObject.Instantiate(rayRenderer);
			//rayLine.transform.parent = iterationRayList.Last.Value.transform;
			//rayLine.positionCount = 2;
			//rayLine.SetPositions(new Vector3[] { rayStartPoint, rayEndPoint });


			//Adding interested edges to intersectionChecklist
			while (edgeQueue.Count>0 && Mathf.Min(edgeQueue.Peek().StartPoint.position.y, edgeQueue.Peek().EndPoint.position.y) <=ly)
            {
				intersectionCheckList.AddLast(edgeQueue.Dequeue());
            }

			LinkedListNode<Edge> intersectionCheckListNode = intersectionCheckList.First;
			//Removing passed edges from intersectionChecklist
			while (intersectionCheckListNode != null)
            {
				Edge edge = intersectionCheckListNode.Value;

				intersectionCheckListNode = intersectionCheckListNode.Next;

				if (Mathf.Max(edge.StartPoint.position.y, edge.EndPoint.position.y) < ly)
                {
					intersectionCheckList.Remove(edge);
				}
			}

			//In the end most probably we have 2 lines to check intersection. It may increase for complex shapes.
			foreach (var edge in intersectionCheckList)
            {
				Point intersection;

				if (checkLineIntersection(edge, rayStartPoint, rayEndPoint, out intersection))
				{
					intersections.AddLast(intersection);
				}
			}

			//If there is no intersection no need to continue.
			if (intersections.Count == 0)
				return placedPoints;

			//Else we will have new point list on intersection interval
			placedPointList.AddLast(new LinkedListNode<LinkedList<Vector2>>(new LinkedList<Vector2>()));
			LinkedList<Vector2> pointListOnIntersectionInterval = placedPointList.Last.Value;

			int pointGoingToBePlaced = 0;

			LinkedListNode<Point> intersectionNode = intersections.First;
			if (intersections.Count > 1)
            {
				do
				{
					float intersectedRaySegmentLength = Mathf.Abs(intersectionNode.Value.position.x - intersectionNode.Next.Value.position.x);

					//Correcting connection points of edges.(In case of finding same point which is on right and left edge at the same time)
					if (intersectedRaySegmentLength == 0f)
					{
						pointGoingToBePlaced = 1;
						pointListOnIntersectionInterval.AddLast(intersectionNode.Value.position);
					}
					else
					{
						Point raySegmentPoint1 = (intersectionNode.Value.position.x < intersectionNode.Next.Value.position.x) ? intersectionNode.Value : intersectionNode.Next.Value;
						Point raySegmentPoint2 = (intersectionNode.Value.position.x > intersectionNode.Next.Value.position.x) ? intersectionNode.Value : intersectionNode.Next.Value;

                        //We had to determine points to place
                        pointGoingToBePlaced = Mathf.CeilToInt(intersectedRaySegmentLength / l);
                        for (int ip = 0; ip < pointGoingToBePlaced; ip++)
                        {
                            Vector2 candidatePoint = new Vector2(raySegmentPoint1.position.x - raySegmentPoint1.position.x % l, raySegmentPoint1.position.y) + Vector2.right * l * ip;
                            if (candidatePoint.x < raySegmentPoint2.position.x &&
                                candidatePoint.x > raySegmentPoint1.position.x)
                            {
                                pointListOnIntersectionInterval.AddLast(new LinkedListNode<Vector2>(candidatePoint));
                            }
                        }
                    }

					intersectionNode = intersectionNode.Next;
					intersectionNode = intersectionNode.Next;
				}
				while (intersectionNode != null && intersectionNode.Next != null);

				pointGoingToBePlaced = pointListOnIntersectionInterval.Count;
			}
			else
            {
				pointGoingToBePlaced = 1;
				pointListOnIntersectionInterval.AddLast(intersectionNode.Value.position);

			}
			placedPoints += pointGoingToBePlaced;
		}
        return placedPoints;
    }

	bool checkLineIntersection(
	Edge e, Vector2 p1, Vector2 p2, out Point intersection)
	{
		float s1_x, s1_y, s2_x, s2_y;
		s1_x = e.EndPoint.position.x - e.StartPoint.position.x;
		s1_y = e.EndPoint.position.y - e.StartPoint.position.y;
		s2_x = p2.x - p1.x;
		s2_y = p2.y - p1.y;

		float s, t;
		s = (-s1_y * (e.StartPoint.position.x - p1.x) + s1_x * (e.StartPoint.position.y - p1.y)) / (-s2_x * s1_y + s1_x * s2_y);
		t = (s2_x * (e.StartPoint.position.y - p1.y) - s2_y * (e.StartPoint.position.x - p1.x)) / (-s2_x * s1_y + s1_x * s2_y);

		if (s >= 0 && s <= 1 && t >= 0 && t <= 1)
		{
			// Collision detected
			intersection = new Point(new Vector2(e.StartPoint.position.x + (t * s1_x), e.StartPoint.position.y + (t * s1_y)));
			return true;
		}
		intersection = new Point(Vector2.positiveInfinity);
		return false; // No collision
	}

	////For testing
	//void RandomRaycasting()
	//{
	//	float yRandom = Random.Range(bBound, tBound);

	//	Vector2 o = new Vector2(lBound, yRandom);
	//	Ray2D r = new Ray2D(o, Vector2.right);

	//	rayRenderer.positionCount = 2;
	//	rayRenderer.SetPositions(new Vector3[]{
	//		new Vector3(r.origin.x,r.origin.y,0),
	//		new Vector3((r.origin+r.direction*w).x,(r.origin+r.direction*w).y,0),
	//	});

	//	foreach (var e in shapeEdges.Where((e) => 
	//								{ return (e.StartPoint.position.y > r.origin.y && e.EndPoint.position.y < r.origin.y) || 
	//										e.StartPoint.position.y < r.origin.y && e.EndPoint.position.y > r.origin.y; }))
	//								{
	//		Point intersectionPoint;
	//		if (checkLineIntersection(e, r.origin, r.origin + r.direction * w, out intersectionPoint))
	//		{
	//			GameObject point = GameObject.Instantiate(pointSample);
	//			point.transform.position = intersectionPoint.V3Pos;
	//		}
	//	}
	//}
}