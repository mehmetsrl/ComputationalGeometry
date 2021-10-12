using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UI2 : MonoBehaviour {
	private Vector3 initialCamPos;
	private Quaternion initialCamRot;

	public float mouseControlSpeed = 20f;
	public Toggle showVertexToggle, showEdgeToggle, showTriangleToggle;
	public Button LoadMeshButton, UnfoldButton,ReturnInitialCamTrans;
	public Dropdown modelListDropdown;
	public Image MouseControlZone,UnfoldingResult;


	private void Start ()
	{
		initialCamPos = Camera.main.transform.position;
		initialCamRot = Camera.main.transform.rotation;

		BindEvents ();
	}

	private void OnDestroy ()
	{
		RemoveEvents ();
	}

	void BindEvents()
	{
		showVertexToggle.onValueChanged.AddListener ((bool enabled) => { ModelManager.Instance.DisplayVerticies (enabled); });
		showEdgeToggle.onValueChanged.AddListener ((bool enabled) => { ModelManager.Instance.DisplayEdges (enabled); });
		showTriangleToggle.onValueChanged.AddListener ((bool enabled) => { ModelManager.Instance.DisplayTriangles (enabled); });

		LoadMeshButton.onClick.AddListener (()=> {
			ModelManager.Instance.SetActiveMesh (modelListDropdown.value);
			showTriangleToggle.gameObject.SetActive (false);
		});
		UnfoldButton.onClick.AddListener (()=> { ModelManager.Instance.createdMeshes[ModelManager.Instance.activeMeshIndex].model.UnfoldModel (DrawUnfoldedModel); });

		ReturnInitialCamTrans.onClick.AddListener (()=> { Camera.main.transform.position = initialCamPos; Camera.main.transform.rotation = initialCamRot; });


		EventTrigger.Entry mouseDragEntry = new EventTrigger.Entry ();
		mouseDragEntry.eventID = EventTriggerType.Drag;
		mouseDragEntry.callback.AddListener ((data) => { OnMouseDragOnZone (); });
		MouseControlZone.gameObject.AddComponent<EventTrigger> ().triggers.Add (mouseDragEntry);
	}

	void RemoveEvents(){
		showVertexToggle.onValueChanged.RemoveAllListeners ();
		showEdgeToggle.onValueChanged.RemoveAllListeners ();
		showTriangleToggle.onValueChanged.RemoveAllListeners ();

		LoadMeshButton.onClick.RemoveAllListeners ();
		UnfoldButton.onClick.RemoveAllListeners ();
		ReturnInitialCamTrans.onClick.RemoveAllListeners ();

		MouseControlZone.gameObject.GetComponent<EventTrigger> ().triggers.RemoveAll (x => x.eventID == EventTriggerType.Drag);
	}


	private void OnMouseDragOnZone ()
	{
		float verticalMovement = -Input.GetAxis ("Mouse X") * Time.deltaTime * mouseControlSpeed;
		float horizontalMovement = -Input.GetAxis ("Mouse Y") * Time.deltaTime * mouseControlSpeed;

		if (Input.GetMouseButton (0)) {
			Camera.main.transform.Translate (verticalMovement, horizontalMovement, 0);
		}
		if (Input.GetMouseButton (1)) {
			Camera.main.transform.Rotate (horizontalMovement, -verticalMovement, 0);
			Camera.main.transform.rotation = Quaternion.Euler (new Vector3 (Camera.main.transform.eulerAngles.x, Camera.main.transform.eulerAngles.y, 0));
		}
	}



	private void DrawUnfoldedModel (List<Vector2> UVMap)
	{
		//showTriangleToggle.gameObject.SetActive(true);
	}

	private void FixedUpdate ()
	{
		float mouseScrollMovement = -Input.mouseScrollDelta.y * Time.deltaTime * mouseControlSpeed;

		Camera.main.transform.Translate (0, 0, mouseScrollMovement);


		if( Input.GetMouseButtonUp(0)){
			RaycastHit hit;
			Ray ray = Camera.main.ScreenPointToRay (Input.mousePosition);
			//Debug.DrawRay (transform.position, ray.direction*100000, Color.yellow);


			if (Physics.Raycast (Camera.main.transform.position, ray.direction,out hit,Mathf.Infinity,~10)) {
				//Debug.DrawRay (transform.position, ray.direction * hit.distance, Color.red);
				//Debug.Log (hit.transform.name);

				int selectedVertex;
				if (Int32.TryParse (hit.transform.name, out selectedVertex))
				    ModelManager.Instance.createdMeshes [ModelManager.Instance.activeMeshIndex].model.SelectVertex (selectedVertex);

			}
		}

	}
}
