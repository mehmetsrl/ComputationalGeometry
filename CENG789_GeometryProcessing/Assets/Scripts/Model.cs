using System.Collections.Generic;
using System.Linq;
using KdTree;
using KdTree.Math;
using UnityEngine;
using Accord.Statistics;
using System;

[System.Serializable]
public class Model {

	#region Structs
	[System.Serializable]
	public class Vertex {
		public static int edgeBoundaryVertexCount = 0;
		public static Vector3 Center = Vector3.zero;
		static int count = 0;
		public GameObject vertexRepresentation;
		public MeshRenderer rend;
		public Color initialColor;
		public Vector3 coords, normals; //3d coordinates etc
		public int idx; //who am i; verts[idx]
		public bool edgeBoundary;

		public List<int> VertexList; //adj vvertices;
		public List<int> TriangleList;
		public List<int> EdgeList;

		internal Vertex (int i, Vector3 position, GameObject prefab, Transform parent)
		{
			normals = Vector3.zero;
			VertexList = new List<int> ();
			TriangleList = new List<int> ();
			EdgeList = new List<int> ();
			idx = i;
			coords = position;
			vertexRepresentation = GameObject.Instantiate (prefab, parent);
			vertexRepresentation.layer = 10;
			vertexRepresentation.name = idx.ToString ();
			vertexRepresentation.transform.localPosition = coords;

			Center = Center * count;
			count++;
			Center = (Center + coords) / count;

			rend = vertexRepresentation.GetComponent<MeshRenderer> ();
			initialColor = rend.materials [0].color;
			edgeBoundary = false;
		}

		public void UpdateView ()
		{
			vertexRepresentation.name = idx.ToString ();
			vertexRepresentation.transform.localPosition = coords;
		}

		public void SelectVertex ()
		{
			edgeBoundary = !edgeBoundary;
			edgeBoundaryVertexCount = edgeBoundary ? (edgeBoundaryVertexCount + 1) : (edgeBoundaryVertexCount - 1);
			rend.materials [0].color = edgeBoundary ? Color.yellow : initialColor;
		}

		public override string ToString ()
		{
			return ("Vertex: " + " inx: " + idx);
		}
	};

	[System.Serializable]
	public class Edge {
		Transform parent;
		public LineRenderer edgeRepresentation;
		public int idx; //edges[idx]
		public int v1i, v2i; //endpnts
		public float length;
		internal Edge (int id, int v1, int v2, Vector3 position, GameObject prefab, Transform parent)
		{
			this.parent = parent;
			edgeRepresentation = GameObject.Instantiate (prefab, parent).GetComponent<LineRenderer> ();
			edgeRepresentation.transform.localPosition = position;
			idx = id;
			v1i = v1;
			v2i = v2;
			length = 0;
		}

		internal void UpdateVertexPoints (Vector3 p1, Vector3 p2)
		{
			Vector3 p1Translated = edgeRepresentation.transform.TransformDirection (p1);
			Vector3 p2Translated = edgeRepresentation.transform.TransformDirection (p2);
			length = Vector3.Distance (p1Translated, p2Translated);
			edgeRepresentation.SetPositions (new Vector3 [] { p1Translated + parent.position, p2Translated + parent.position });
		}
	};

	[System.Serializable]
	public class Triangle {
		public LineRenderer normalRepresentation;
		public int idx; //tris[idx]
		public int v1i, v2i, v3i;
		public Vector3 normal;
		internal Triangle (int id, int v1, int v2, int v3, Vector3 position, GameObject prefab, Transform parent)
		{
			normalRepresentation = GameObject.Instantiate (prefab, parent).GetComponent<LineRenderer> ();
			normalRepresentation.transform.localPosition = position;
			normal = Vector3.zero;
			idx = id;
			v1i = v1;
			v2i = v2;
			v3i = v3;
		}
		internal void SetNormal (Vector3 normal)
		{
			this.normal = normalRepresentation.transform.TransformDirection (normal);
			normalRepresentation.SetPositions (new Vector3 [] { normalRepresentation.transform.position, normalRepresentation.transform.position + this.normal * 5 });
		}
	};

	#endregion

	#region Variables
	public List<Vertex> Vertexs;
	public List<Triangle> Trianges;
	public List<Edge> Edges;
	public Queue<GameObject> Paths;

	[HideInInspector]
	public Transform VertexParent, TriangleParent, EdgeParent, ShortestPathParent;
	[HideInInspector]
	public List<Transform> ShortestPathGroups = new List<Transform> ();
	[HideInInspector]
	public GameObject ModelPointPrefab, TrianglePrefab, EdgePrefab, PathPrefab;
	[HideInInspector]
	public int maxPathNumber = 2;

	KdTree<float, int> kdTree;
	List<VertexGroup> vertexNeigbourNodeGroups;

	#endregion

	#region BasicMeshStructOperations
	public void AddTriangle (int vID1, int vID2, int vID3)
	{
		int triID = Trianges.Count;
		Vector3 center = (Vertexs [vID1].coords + Vertexs [vID2].coords + Vertexs [vID3].coords) / 3;
		Trianges.Add (new Triangle (triID, vID1, vID2, vID3, center, TrianglePrefab, TriangleParent));

		//set up structure

		Vertexs [vID1].TriangleList.Add (triID);
		Vertexs [vID2].TriangleList.Add (triID);
		Vertexs [vID3].TriangleList.Add (triID);

		if (MakeVertsNeighbor (vID1, vID2))
			AddEdge (vID1, vID2);

		if (MakeVertsNeighbor (vID1, vID3))
			AddEdge (vID1, vID3);

		if (MakeVertsNeighbor (vID2, vID3))
			AddEdge (vID2, vID3);
	}

	public void AddVertex (float x, float y, float z)
	{
		int idx = Vertexs.Count;
		Vector3 point = new Vector3 (x, y, z);
		Vertexs.Add (new Vertex (idx, point, ModelPointPrefab, VertexParent));
	}
	bool MakeVertsNeighbor (int v1i, int v2i)
	{
		//returns false if v1i already neighbor w/ v2i; true o/w
		for (int i = 0; i < Vertexs [v1i].VertexList.Count; i++)
			if (Vertexs [v1i].VertexList [i] == v2i)
				return false;

		Vertexs [v1i].VertexList.Add (v2i);
		Vertexs [v2i].VertexList.Add (v1i);
		return true;
	}
	void AddEdge (int v1, int v2)
	{
		int idx = Edges.Count;
		Vector3 center = (Vertexs [v1].coords + Vertexs [v2].coords) / 2;
		Edge e = new Edge (idx, v1, v2, center, EdgePrefab, EdgeParent);
		e.UpdateVertexPoints (Vertexs [v1].coords, Vertexs [v2].coords);
		Edges.Add (e);

		Vertexs [v1].EdgeList.Add (idx);
		Vertexs [v2].EdgeList.Add (idx);
	}
	#endregion

	#region MeshOperations
	public void CalculateNormalsFromTriangles ()
	{
		foreach (Triangle t in Trianges) {
			Vector3 normal = Vector3.Cross ((Vertexs [t.v2i].coords - Vertexs [t.v1i].coords), (Vertexs [t.v3i].coords - Vertexs [t.v1i].coords)).normalized;
			t.SetNormal (normal);
		}
	}
	#region Remeshing
	public void GenerateKDTree ()
	{
		kdTree = new KdTree<float, int> (3, new FloatMath ());

		foreach (Vertex v in Vertexs) {
			kdTree.Add (new float [] { v.coords.x, v.coords.y, v.coords.z }, v.idx);
		}

	}

	KdTreeNode<float, int> GetLeftMostNode (KdTreeNode<float, int> root)
	{
		if (root.LeftChild != null)
			return GetLeftMostNode (root.LeftChild);

		return root;
	}

	void GroupVertexes ()
	{
		List<Vertex> SearchVertexs = new List<Vertex> (Vertexs);

		vertexNeigbourNodeGroups = new List<VertexGroup> ();

		//  O(n/8) cost
		while (SearchVertexs.Count > 0) {
			Vertex vertex = SearchVertexs [0];
			KdTreeNode<float, int> [] neigbours = kdTree.GetNearestNeighbours (new float [] { vertex.coords.x, vertex.coords.y, vertex.coords.z }, 8);

			foreach (KdTreeNode<float, int> vNode in neigbours) {
				SearchVertexs.Remove (SearchVertexs.Find (x => x.idx == vNode.Value));
				//Vertexs.Find(x => x.idx == vNode.Value).SetMaterial(groupMaterial);
			}
			vertexNeigbourNodeGroups.Add (new VertexGroup (neigbours));

		}
		/*
		//Colorize edges
		foreach (Edge e in Edges)
		{
		    Material edgeMaterial = new Material(Shader.Find("Unlit/Color"));
		    edgeMaterial.color = Color.Lerp(Vertexs[e.v1i].rend.material.color, Vertexs[e.v2i].rend.material.color, 0.5f);
		    e.edgeRepresentation.material = edgeMaterial;
		}*/

	}

	public void RemeshModel ()
	{
		GroupVertexes ();

		//Clear existing triangles and edges
		foreach (var item in Trianges) {
			GameObject.Destroy (item.normalRepresentation.gameObject);
		}
		Trianges.Clear ();

		foreach (var item in Edges) {
			GameObject.Destroy (item.edgeRepresentation.gameObject);
		}
		Edges.Clear ();


		foreach (VertexGroup neigbourGroup in vertexNeigbourNodeGroups) {

			neigbourGroup.CalculatePlane ();

			neigbourGroup.ProjectPoints ();

			List<int []> regeneratedTriangles = neigbourGroup.Triangulate ();

			foreach (var vID in regeneratedTriangles) {
				AddTriangle (vID [0], vID [1], vID [2]);

				//Note: there are 2 possible normal for each triangle. We need to select normal which is close to whole plane normal.
				Vector3 firstNormal = Vector3.Cross ((Vertexs [vID [1]].coords - Vertexs [vID [0]].coords), (Vertexs [vID [2]].coords - Vertexs [vID [0]].coords)).normalized;
				Vector3 secondNormal = Vector3.Cross ((Vertexs [vID [2]].coords - Vertexs [vID [0]].coords), (Vertexs [vID [1]].coords - Vertexs [vID [0]].coords)).normalized;

				if (Vector3.Angle (firstNormal, neigbourGroup.Dimension2DNormal) < Vector3.Angle (secondNormal, neigbourGroup.Dimension2DNormal))
					Trianges.Last ().SetNormal (firstNormal);
				else
					Trianges.Last ().SetNormal (secondNormal);

				//Or ve can siply use plane normal
				//Trianges.Last ().SetNormal (neigbourGroup.Dimension2DNormal);
			}

		}
	}
	#endregion
	#region Unfolding
	public void UnfoldModel (Action<List<Vector2>> callback)
	{
		List<Vector2> Points2D = new List<Vector2> (Vertexs.Count);

		double [,] MeshPoints = new double [Vertexs.Count, 3];

		for (int i = 0; i < Vertexs.Count; i++) {
			MeshPoints [i, 0] = Vertexs [i].coords.x;
			MeshPoints [i, 1] = Vertexs [i].coords.y;
			MeshPoints [i, 2] = Vertexs [i].coords.z;
		}

		//-----Calculate Plane
		double [,] meshCoveriance;

		meshCoveriance = MeshPoints.Covariance ();

		List<Vector3> CovarianceV3 = new List<Vector3> (3);

		for (int i = 0; i < 3; i++) {
			CovarianceV3.Add (new Vector3 ((float)meshCoveriance [i, 0], (float)meshCoveriance [i, 1], (float)meshCoveriance [i, 2]));
		}
		CovarianceV3 = new List<Vector3> (CovarianceV3.OrderByDescending (v => Mathf.Max (v.x, v.y, v.z)));

		Vector3 Dimension2DNormal = Vector3.Cross (CovarianceV3 [0].normalized, CovarianceV3 [1].normalized).normalized;
		Vector3 dimension1 = CovarianceV3 [0].normalized;
		Vector3 dimension2 = Vector3.Cross (dimension1, Dimension2DNormal);
		//------

		//-----Project on Plane

		for (int i = 0; i < Vertexs.Count; i++) {
			Vector3 relativePosition = Vertexs [i].coords - Vertex.Center;
			float d1Amount = Vector3.Dot (dimension1, relativePosition);
			float d2Amount = Vector3.Dot (dimension2, relativePosition);
			Points2D.Add (new Vector2 (d1Amount, d2Amount));
		}


		//Or we can simply delete z values
		//for (int i = 0; i < Vertexs.Count; i++) {
		//	Points2D.Add( new Vector2 (Vertexs.Find (x => x.idx == i).coords.x, Vertexs.Find (x => x.idx == i).coords.y));
		//}
		//------------


		//Get Unfolded 2d Coords
		UniformUnfold (ref Points2D);
		//callback (Points2D);


		//Update View
		for (int i = 0; i < Vertexs.Count; i++) {
			Vertexs.Find (x => x.idx == i).coords = new Vector3 (Points2D [i].x, Points2D [i].y, 0);
			Vertexs.Find (x => x.idx == i).UpdateView ();
		}
		foreach (var edge in Edges) {
			edge.UpdateVertexPoints (Vertexs.Find (x => x.idx == edge.v1i).coords, Vertexs.Find (x => x.idx == edge.v2i).coords);
		}
	}

	float [,] laplacianMatrix;
	float UVRadius = 25;
	void UniformUnfold (ref List<Vector2> projected2DPoints)
	{

		if (laplacianMatrix == null) {
			laplacianMatrix = new float [Vertexs.Count, Vertexs.Count];

			for (int i = 0; i < Vertexs.Count; i++) {
				if (Vertexs.Find (x => x.idx == i).edgeBoundary) { laplacianMatrix [i, i] = 1; } else {

					int neigbourCount = 0;
					for (int j = 0; j < Vertexs.Count; j++) {
						{
							if (Vertexs.Find (x => x.idx == i).VertexList.Contains (j)) {
								laplacianMatrix [i, j] = 1;
								neigbourCount++;
							} else { laplacianMatrix [i, j] = 0; }
						}
					}
					if (!Vertexs.Find (x => x.idx == i).edgeBoundary) laplacianMatrix [i, i] = neigbourCount * -1;
				}
			}
		}

		float [] bX = new float [projected2DPoints.Count];
		float [] bY = new float [projected2DPoints.Count];

		float angleDif = Mathf.PI * 2 / (Vertex.edgeBoundaryVertexCount);
		float angle = 0;

		List<Vertex> edgeBounds = Vertexs.FindAll (x => x.edgeBoundary);
		
		if(edgeBounds==null) {
			Debug.LogError ("First select boundary edges");
			return;
		}

		int index = edgeBounds [0].idx;
		while (edgeBounds.Count > 0) {
			projected2DPoints [index] = new Vector2 (Mathf.Cos (angle), Mathf.Sin (angle)) * UVRadius;
			angle += angleDif;

			Vertex v = edgeBounds.Find (x => x.idx == index);
			edgeBounds.Remove (v);

			if (edgeBounds.Count > 0)
				foreach (var eb in edgeBounds) {
					if (v.VertexList.Contains (eb.idx)) {
						index = eb.idx;
						break;
					}
				}
		}

		for (int i = 0; i < projected2DPoints.Count; i++) {

			if (Vertexs.Find (x => x.idx == i).edgeBoundary) {
				bX [i] = projected2DPoints [i].x;
				bY [i] = projected2DPoints [i].y;
			} else {
				bX [i] = 0f; bY [i] = 0f;
			}

		}

		float [,] laplacianMatrixT = Accord.Math.Matrix.Inverse (laplacianMatrix);


		float [] restultX = new float [projected2DPoints.Count];
		float [] restultY = new float [projected2DPoints.Count];

		restultX = Accord.Math.Matrix.Dot (laplacianMatrixT, bX);
		restultY = Accord.Math.Matrix.Dot (laplacianMatrixT, bY);

		for (int i = 0; i < projected2DPoints.Count; i++) {
			//if (!Vertexs.Find (x => x.idx == i).edgeBoundary)
			projected2DPoints [i] = new Vector2 (restultX [i], restultY [i]);
		}
	}

	public void SelectVertex (int selectedVertex)
	{
		Vertexs.Find (x => x.idx == selectedVertex).SelectVertex ();
	}
	#endregion

	#endregion

	#region SortingMethods

	public void GetRandomShortestPath (Model.ShortestPathMethod method = Model.ShortestPathMethod.aStar)
	{
		FindShortestPath (Vertexs [UnityEngine.Random.Range (0, Vertexs.Count)], Vertexs [UnityEngine.Random.Range (0, Vertexs.Count)], method);
	}

	public void GetShortestPath (int startIndex, int endIndex, Model.ShortestPathMethod method = Model.ShortestPathMethod.aStar)
	{
		FindShortestPath (Vertexs [startIndex], Vertexs [endIndex], method);
	}

	public enum ShortestPathMethod { aStar, dijkstra }
	private void FindShortestPath (Vertex sourceVertex, Vertex destVertex, ShortestPathMethod mehod)
	{


		IHasNeighbours<Model.Vertex> dataStructure = new VertexDataStruct (ref Vertexs, ref Edges);
		Path<Vertex> path = null;

		if (mehod == ShortestPathMethod.aStar) {
			path = AStar.FindPath (dataStructure, sourceVertex, destVertex, Distance, Estimate);

			//for (int i = 0; i < (path.Count () - 1); i++) 
			//Debug.Log ("AStar path point: " + path.ElementAt (i).idx);

		} else if (mehod == ShortestPathMethod.dijkstra) {

			path = Dijkstra.FindPath (dataStructure, sourceVertex, destVertex, Distance);

			//for (int i = 0; i < (path.Count () - 1); i++)
			//Debug.Log ("Dijkstra path point: " + path.ElementAt (i).idx);
		}

		if (path != null) {

			Color pathColor = UnityEngine.Random.ColorHSV (0f, 1f, 1f, 1f, 0.5f, 1f);
			Material pathMaterial = new Material (Shader.Find ("Unlit/Color"));
			pathMaterial.color = pathColor;

			while (Paths.Count >= maxPathNumber) {
				GameObject pathParent = Paths.Dequeue ();
				foreach (var lineRenderer in pathParent.GetComponentsInChildren<LineRenderer> ()) {
					GameObject.Destroy (lineRenderer.gameObject);
				}
				GameObject.Destroy (pathParent);
			}

			GameObject pathRepresentetive = new GameObject ("Path");
			pathRepresentetive.transform.parent = ShortestPathParent;
			pathRepresentetive.transform.localPosition = Vector3.zero;
			pathRepresentetive.transform.localRotation = Quaternion.identity;

			for (int i = 0; i < (path.Count () - 2); i++) {
				LineRenderer pathPoint = GameObject.Instantiate (PathPrefab, pathRepresentetive.transform).GetComponent<LineRenderer> ();

				pathPoint.material = pathMaterial;

				pathPoint.transform.localPosition = (path.ElementAt (i).coords + path.ElementAt (i + 1).coords) / 2;
				pathPoint.SetPositions (new Vector3 [] { pathPoint.transform.TransformDirection (path.ElementAt (i).coords) + ShortestPathParent.transform.position, pathPoint.transform.TransformDirection (path.ElementAt (i + 1).coords) + ShortestPathParent.transform.position });
			}
			Paths.Enqueue (pathRepresentetive);
		}
	}

	//Heuristic
	private double Estimate (Vertex vertex, Vertex destinationVertex)
	{
		//Debug.Log("Estimation of "+vertex.idx+"  "+vertex.coords);
		return Vector3.Distance (vertex.coords, destinationVertex.coords);
	}

	private double Distance (Vertex v1, Vertex v2)
	{
		//Debug.Log("Distance of " + v1.idx + "  " + v1.coords+"  -   "+ v2.idx + "  " + v2.coords);
		return Vector3.Distance (v1.coords, v2.coords);
	}
	#endregion


	public Model ()
	{
		Vertexs = new List<Vertex> ();
		Trianges = new List<Triangle> ();
		Edges = new List<Edge> ();
		Paths = new Queue<GameObject> ();
	}

	public void ClearModel ()
	{
		foreach (var item in Vertexs) {
			GameObject.Destroy (item.vertexRepresentation);
		}
		Vertexs.Clear ();

		foreach (var item in Trianges) {
			GameObject.Destroy (item.normalRepresentation.gameObject);
		}
		Trianges.Clear ();

		foreach (var item in Edges) {
			GameObject.Destroy (item.edgeRepresentation.gameObject);
		}
		Edges.Clear ();

		while (Paths.Count > 0) {
			GameObject pathParent = Paths.Dequeue ();
			foreach (var lineRenderer in pathParent.GetComponentsInChildren<LineRenderer> ()) {
				GameObject.Destroy (lineRenderer.gameObject);
			}
			GameObject.Destroy (pathParent);
		}
	}


	#region DataStructures
	public class VertexDataStruct : IHasNeighbours<Model.Vertex> {
		public Dictionary<Vertex, float> Distance = new Dictionary<Vertex, float> ();
		public List<Vertex> verticies;
		public List<Edge> edges;
		public VertexDataStruct (ref List<Vertex> verticies, ref List<Edge> edges)
		{
			this.verticies = verticies;
			this.edges = edges;
		}

		public IEnumerable<Vertex> Neighbours (Vertex node)
		{

			return (System.Collections.Generic.IEnumerable<Model.Vertex>)node.VertexList;

			List<Edge> linkedEdges = edges.FindAll ((x => x.v1i == node.idx || x.v2i == node.idx));
			List<Model.Vertex> linkedVertices = new List<Model.Vertex> ();
			foreach (Edge e in linkedEdges) {

				if (node.idx == e.v1i)
					linkedVertices.Add (verticies [e.v2i]);
				else
					linkedVertices.Add (verticies [e.v1i]);
			}
			return linkedVertices.AsEnumerable ();
		}
	}

	public class VertexGroup {
		internal static int Count;
		internal int groupID;

		internal KdTreeNode<float, int> [] neigbours;
		internal List<Vector3> Points;
		internal List<Vector2> Points2D;

		internal Dictionary<int, int> groupIDvertexID_HashTable;

		internal Vector3 CenterPoint;
		internal double [,] NeigbourGroupMatrix;
		internal double [,] CovarianceMatrix;
		internal List<Vector3> CovarianceV3;
		internal Vector3 Dimension2DNormal;

		public Material GroupMaterial {
			get {
				if (groupMaterial != null)
					return groupMaterial;
				return ColorizeGroup ();
			}
		}
		Material groupMaterial;

		Vector3 dimension1, dimension2;
		public Vector3 [] Dimention2D {
			get { return new Vector3 [] { dimension1, dimension2 }; }
		}

		public VertexGroup (KdTreeNode<float, int> [] neigbours)
		{
			this.neigbours = neigbours;

			Points = new List<Vector3> (neigbours.Length);
			groupIDvertexID_HashTable = new Dictionary<int, int> (neigbours.Length);

			NeigbourGroupMatrix = new double [neigbours.Length, 3];
			CenterPoint = Vector3.zero;

			for (int i = 0; i < neigbours.Length; i++) {
				NeigbourGroupMatrix [i, 0] = neigbours [i].Point [0];
				NeigbourGroupMatrix [i, 1] = neigbours [i].Point [1];
				NeigbourGroupMatrix [i, 2] = neigbours [i].Point [2];

				Points.Add (new Vector3 ((float)NeigbourGroupMatrix [i, 0], (float)NeigbourGroupMatrix [i, 1], (float)NeigbourGroupMatrix [i, 2]));

				//Here value is idx of my own Vertex struct
				groupIDvertexID_HashTable.Add (i, neigbours [i].Value);

				CenterPoint += Points.Last ();
			}
			CenterPoint /= neigbours.Length;

			groupID = Count;
			Count++;
		}

		internal Material ColorizeGroup ()
		{
			groupMaterial = new Material (Shader.Find ("Unlit/Color"));
			groupMaterial.color = UnityEngine.Random.ColorHSV (0f, 1f, 1f, 1f, 0.5f, 1f);
			return groupMaterial;
		}

		internal void CalculatePlane ()
		{
			CovarianceMatrix = NeigbourGroupMatrix.Covariance ();
			CovarianceV3 = new List<Vector3> (3);

			for (int i = 0; i < 3; i++) {
				CovarianceV3.Add (new Vector3 ((float)CovarianceMatrix [i, 0], (float)CovarianceMatrix [i, 1], (float)CovarianceMatrix [i, 2]));
			}
			CovarianceV3 = new List<Vector3> (CovarianceV3.OrderByDescending (v => Mathf.Max (v.x, v.y, v.z)));

			Dimension2DNormal = Vector3.Cross (CovarianceV3 [0], CovarianceV3 [1]).normalized;
			dimension1 = CovarianceV3 [0].normalized;
			dimension2 = Vector3.Cross (dimension1, Dimension2DNormal);

		}

		internal void ProjectPoints ()
		{
			Points2D = new List<Vector2> (neigbours.Length);
			for (int i = 0; i < neigbours.Length; i++) {
				Vector3 relativePosition = Points [i] - CenterPoint;
				//float distanceToPlane = Vector3.Dot (Dimension2DNormal, relativePosition);
				float d1Amount = Vector3.Dot (dimension1, relativePosition);
				float d2Amount = Vector3.Dot (dimension2, relativePosition);

				//TriangulatorVertexList.Add (new Triangulator.Vertex(new Vector2 (d1Amount, d2Amount), neigbours[i].Value));
				Points2D.Add (new Vector2 (d1Amount, d2Amount));
			}
		}

		internal List<int []> Triangulate ()
		{
			List<int []> recalculatedTriangles = new List<int []> ();
			Vector2 [] outVertices;
			int [] outIndices;
			Triangulator.Triangulator.Triangulate (Points2D.ToArray (), Triangulator.WindingOrder.CounterClockwise, out outVertices, out outIndices);

			for (int i = 0; i < outIndices.Length; i += 3) {
				//Debug.Log ("triangle " + (i / 3) + ": [" + groupIDvertexID_HashTable [outIndices [i]] + "," + groupIDvertexID_HashTable [outIndices [i + 1]] + "," + groupIDvertexID_HashTable [outIndices [i + 2]] + "]");
				recalculatedTriangles.Add (new int [3] { groupIDvertexID_HashTable [outIndices [i]], groupIDvertexID_HashTable [outIndices [i + 1]], groupIDvertexID_HashTable [outIndices [i + 2]] });
			}

			return recalculatedTriangles;
		}
	}
	#endregion

}
