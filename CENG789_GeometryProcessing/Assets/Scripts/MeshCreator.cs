using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum ModelExtention{off,obj}


public class MeshCreator : MonoBehaviour {
    public string offMeshPath;
    public ModelExtention extention;
    [HideInInspector]
    public Transform VertexParent, TriangleParent, EdgeParent, ShortestPathParent;
    public Model model;
    bool isActive = false;

	private void Awake()
    {
        VertexParent = new GameObject("Vertices").transform;
        VertexParent.transform.parent = transform;
        VertexParent.transform.localPosition = Vector3.zero;
        VertexParent.transform.localRotation = Quaternion.identity;

        TriangleParent = new GameObject("Triangles").transform;
        TriangleParent.transform.parent = transform;
        TriangleParent.transform.localPosition = Vector3.zero;
        TriangleParent.transform.localRotation = Quaternion.identity;

        EdgeParent = new GameObject("Edges").transform;
        EdgeParent.transform.parent = transform;
        EdgeParent.transform.localPosition = Vector3.zero;
        EdgeParent.transform.localRotation = Quaternion.identity;


		ShortestPathParent = new GameObject ("ShortestPathList").transform;
		ShortestPathParent.transform.parent = transform;
		ShortestPathParent.transform.localPosition = Vector3.zero;
		ShortestPathParent.transform.localRotation = Quaternion.identity;

	}
    public void Deactivate(){
        if (model != null){
            model.ClearModel();}
        
        if (VertexParent != null){
            List<Transform> verticies = VertexParent.GetComponentsInChildren<Transform>().ToList();
            verticies.Remove(VertexParent);
            foreach (Transform trans in verticies)
            {
                DestroyImmediate(trans.gameObject);
            }
        }

        if (TriangleParent != null)
        {
            List<Transform> triangles = TriangleParent.GetComponentsInChildren<Transform>().ToList();
            triangles.Remove(TriangleParent);
            foreach (Transform trans in triangles)
            {
                DestroyImmediate(trans.gameObject);
            }
        }

		if (EdgeParent != null) {
			List<Transform> edges = EdgeParent.GetComponentsInChildren<Transform> ().ToList ();
			edges.Remove (EdgeParent);
			foreach (Transform trans in edges) {
				DestroyImmediate (trans.gameObject);
			}
		}


		if (ShortestPathParent != null) {
			List<Transform> shortestPathList = ShortestPathParent.GetComponentsInChildren<Transform> ().ToList ();
			shortestPathList.Remove (ShortestPathParent);
			foreach (Transform trans in shortestPathList) {
				DestroyImmediate (trans.gameObject);
			}
		}

		isActive = false;
    }

    public void Activate(){
        if (!isActive)
        {
            isActive = true;
            model = new Model();

            model.VertexParent = VertexParent;
            model.TriangleParent = TriangleParent;
            model.EdgeParent = EdgeParent;
			model.ShortestPathParent = ShortestPathParent;

			model.ModelPointPrefab = ModelManager.Instance.modelPointPrefab;
            model.TrianglePrefab = ModelManager.Instance.modelTrianglePrefab;
            model.EdgePrefab = ModelManager.Instance.modelEdgePrefab;
			model.PathPrefab = ModelManager.Instance.modelPathPrefab;

			model.maxPathNumber = ModelManager.Instance.maxPathNumber;

			if (ModelReader.Reader.LoadModel(Application.streamingAssetsPath + offMeshPath, extention, ref model))
            {
                Debug.Log("Model Loaded");
                model.CalculateNormalsFromTriangles();
                model.GenerateKDTree();
            }
        }
    }
}
