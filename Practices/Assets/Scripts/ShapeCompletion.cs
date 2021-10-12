using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


[System.Serializable]
public class Edge {
	public Vector2 StartPoint { get { return start; } }
	public Vector2 EndPoint { get { return end; } }
	[SerializeField]
	string description;
	
	[SerializeField]
	Vector2 start, end;

	public override string ToString ()
	{
		return description;
	}
	
	public Edge(Vector2 start, Vector2 end)
	{
		this.start = start;
		this.end = end;

		description = start.ToString () + " | " + end.ToString ();
	}

}

public class ShapeCompletion : MonoBehaviour
{

	public uint completionDotNumber=10;
	[SerializeField]
	bool completeShape = false;

	[SerializeField]
	List<Vector2> shapePoints = new List<Vector2>() {
		new Vector2(1,-3),
		new Vector2(-0.5f,-4),
		new Vector2(-2,-3),
		new Vector2(-3,-1),
		new Vector2(-1.5f,1.5f),
		new Vector2(0.5f,2),
		new Vector2(2.5f,1),
		new Vector2(3,-0.5f)
	};

	Vector2 center;
	[SerializeField]
	public List<Edge> shapeEdges;
	
	
	float angleCount=0;

	public LineRenderer shapeRenderer;


	private void Start ()
	{
		DrawShape ();
	}

	private void Update ()
	{
		if (completeShape) {
			completeShape = false;
			CompleteShape ();
		}
	}


	private void DrawShape ()
	{
		center = Vector2.zero;
		for (int i = 0; i < shapePoints.Count; i++) {
			center += shapePoints [i];
		}
		center /= shapePoints.Count;

		shapeEdges.Clear ();
		angleCount=0;
		
		for (int i = 0; i < shapePoints.Count-1; i++) {
			angleCount += Vector2.SignedAngle (
					shapePoints[i]-center,
					shapePoints[i+1]-center
					);
			shapeEdges.Add (new Edge (shapePoints [i], shapePoints [i + 1]));
		}

		shapeRenderer.positionCount = shapePoints.Count;
		
		shapeRenderer.SetPositions (shapePoints.Select (
						(point)=> { return new Vector3 (point.x, point.y, 0); }
						).ToArray()
						);
	}

	private void CompleteShape ()
	{
		float angleIncrement = (360-Mathf.Abs(angleCount))/(completionDotNumber+1)
					*Mathf.Sign(angleCount)*(Mathf.PI/180);
					
		Vector2 firstPoint = shapePoints [0];
		Vector2 lastPoint = shapePoints.Last();
		
		float angleDif = Vector2.SignedAngle (Vector2.right, (lastPoint - center)) * (Mathf.PI / 180);

		for (int i = 0; i < completionDotNumber; i++) {
			float radius = Mathf.Lerp (
				Vector2.Distance(center,lastPoint),
				Vector2.Distance(center,firstPoint),
				(float)(i+1)/(float)(completionDotNumber+1)
			);
			float angle = (i + 1) * angleIncrement + angleDif;

			shapePoints.Add (
				center+
				new Vector2(
					radius*Mathf.Cos(angle),
					radius*Mathf.Sin(angle)
					)
			);
		}

		shapePoints.Add (firstPoint);
		DrawShape ();
	}
}
