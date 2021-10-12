using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ModelManager : MonoBehaviour {

    private KeyCode[] keyCodes = {
         KeyCode.Alpha0,
         KeyCode.Alpha1,
         KeyCode.Alpha2,
         KeyCode.Alpha3,
         KeyCode.Alpha4,
         KeyCode.Alpha5,
         KeyCode.Alpha6,
         KeyCode.Alpha7,
         KeyCode.Alpha8,
         KeyCode.Alpha9,
    };


    public GameObject modelPointPrefab, modelTrianglePrefab, modelEdgePrefab, modelPathPrefab;

	public int maxPathNumber = 5;

    public List<MeshCreator> createdMeshes;
    [HideInInspector]
    public int activeMeshIndex = -1;
    [SerializeField]
    int meshIndexToActivate = 0;
    public bool showVertices, showTriangles, showEdges;

    static ModelManager instance;
	public static ModelManager Instance
    {
        get
        {
            return instance;
        }
        private set
        {
            if (instance == null)
                instance = value;
        }
    }

    private void Awake()
    {
        Instance = this;
        createdMeshes = FindObjectsOfType<MeshCreator>().ToList();
        //SetActiveMesh(0);
    }

    private void Update()
    {
        if (Input.GetKeyUp(KeyCode.U))
        {
            UpdateModelDisplay();
        }
        else if (Input.GetKeyUp(KeyCode.A))
        {
            SetActiveMesh(meshIndexToActivate);
        }
        else if (Input.GetKeyUp(KeyCode.V))
        {
            DisplayVerticies(!showVertices);
        }
        else if (Input.GetKeyUp(KeyCode.T))
        {
            DisplayTriangles(!showTriangles);
        }
        else if (Input.GetKeyUp(KeyCode.E))
        {
            DisplayEdges(!showEdges);

        }
        else if(Input.GetKeyUp(KeyCode.S)){
            FindRandomShortestPath();
        }
		else if (Input.GetKeyUp (KeyCode.R)) {
			RemeshModel ();
		}
		else
        {
            for (int i = 0; i < keyCodes.Length; i++)
            {
                if (Input.GetKeyDown(keyCodes[i]))
                {
                    int numberPressed = i;
                    if (numberPressed > -1 && numberPressed < createdMeshes.Count)
                        meshIndexToActivate = numberPressed;
                }
            }
        }
    }

	public void SetActiveMesh(int index)
    {
        if (index > -1 && createdMeshes.Count > index && createdMeshes[index] != null)
        {
            for (int i = 0; i < createdMeshes.Count;i++)
            {
                if (index != i)
                {
                    createdMeshes[i].gameObject.SetActive(false);
                    createdMeshes[i].Deactivate();
                }
            }

            createdMeshes[index].gameObject.SetActive(true);
            createdMeshes[index].Activate();
            activeMeshIndex = index;

            UpdateModelDisplay();
        }
    }

    public void UpdateModelDisplay(){
        DisplayVerticies(showVertices);
        DisplayTriangles(showTriangles);
        DisplayEdges(showEdges);
    }

    public void DisplayVerticies(bool show)
    {
        if (activeMeshIndex > -1 && createdMeshes.Count > activeMeshIndex)
            createdMeshes[activeMeshIndex].VertexParent.gameObject.SetActive(show);
		showVertices = show;
    }

    public void DisplayTriangles(bool show)
    {
        if (activeMeshIndex > -1 && createdMeshes.Count > activeMeshIndex)
            createdMeshes[activeMeshIndex].TriangleParent.gameObject.SetActive(show);
		showTriangles = show;
	}
    public void DisplayEdges(bool show)
    {
        if (activeMeshIndex > -1 && createdMeshes.Count > activeMeshIndex)
            createdMeshes[activeMeshIndex].EdgeParent.gameObject.SetActive(show);
		showEdges = show;
	}
	public void FindRandomShortestPath(Model.ShortestPathMethod  method = Model.ShortestPathMethod.aStar)
    {
        if (activeMeshIndex > -1 && createdMeshes.Count > activeMeshIndex)
			createdMeshes[activeMeshIndex].model.GetRandomShortestPath(method);
	}
	public void FindShortestPath (int sourceVertexIndex,int targetVertexIndex, Model.ShortestPathMethod method = Model.ShortestPathMethod.aStar)
	{
		if (activeMeshIndex > -1 && createdMeshes.Count > activeMeshIndex)
			createdMeshes [activeMeshIndex].model.GetShortestPath (sourceVertexIndex, targetVertexIndex, method);
	}
	public void RemeshModel ()
	{
		if (activeMeshIndex > -1 && createdMeshes.Count > activeMeshIndex) {
			createdMeshes [activeMeshIndex].model.RemeshModel ();
		}
	}

	internal void SetMaxPathNumber (int intValue)
	{
		if (activeMeshIndex > -1 && createdMeshes.Count > activeMeshIndex)
			createdMeshes [activeMeshIndex].model.maxPathNumber = intValue;
	}


}
