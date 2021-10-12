using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UI : MonoBehaviour {
	private Vector3 initialCamPos;
	private Quaternion initialCamRot;

	public float mouseControlSpeed = 20f;
	public Toggle showVertexToggle, showEdgeToggle, showTriangleToggle;
	public Button LoadMeshButton, RemeshButton, PathSearchButton,ReturnInitialCamTrans,PickBtnSource,PickBtnDest;
	public Dropdown modelListDropdown, searchAlgorithmsDropdown;
	public InputField maxNumberOfPath,sourceInputField,destInputField;
	public Image MouseControlZone;


	private void Start ()
	{
		initialCamPos = Camera.main.transform.position;
		initialCamRot = Camera.main.transform.rotation;

		sourceInputField = PickBtnSource.transform.parent.GetComponent<InputField> ();
		destInputField = PickBtnDest.transform.parent.GetComponent<InputField> ();

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

		LoadMeshButton.onClick.AddListener (()=> { ModelManager.Instance.SetActiveMesh (modelListDropdown.value); });
		RemeshButton.onClick.AddListener (()=> { ModelManager.Instance.RemeshModel (); });

		PathSearchButton.onClick.AddListener (() => {
			int sourceVertex, destVertex; 
			if(Int32.TryParse(sourceInputField.text,out sourceVertex)&& Int32.TryParse (destInputField.text, out destVertex)) 
				ModelManager.Instance.FindShortestPath (sourceVertex,destVertex,(Model.ShortestPathMethod)searchAlgorithmsDropdown.value); 
			else
				ModelManager.Instance.FindRandomShortestPath ((Model.ShortestPathMethod)searchAlgorithmsDropdown.value);
		});

		ReturnInitialCamTrans.onClick.AddListener (()=> { Camera.main.transform.position = initialCamPos; Camera.main.transform.rotation = initialCamRot; });

		PickBtnSource.onClick.AddListener (() => { raycasTargetInputField = sourceInputField; });
		PickBtnDest.onClick.AddListener (() => { raycasTargetInputField = destInputField; });

		maxNumberOfPath.onValueChanged.AddListener ((string value) => { int intValue; if(Int32.TryParse (value, out intValue)) ModelManager.Instance.SetMaxPathNumber (intValue); });


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
		RemeshButton.onClick.RemoveAllListeners ();
		PathSearchButton.onClick.RemoveAllListeners ();
		ReturnInitialCamTrans.onClick.RemoveAllListeners ();

		PickBtnSource.onClick.RemoveAllListeners ();
		PickBtnDest.onClick.RemoveAllListeners ();

		maxNumberOfPath.onValueChanged.RemoveAllListeners ();

		MouseControlZone.gameObject.GetComponent<EventTrigger> ().triggers.RemoveAll (x => x.eventID == EventTriggerType.Drag);
	}

	void OnToggleChanged(){

	}
	InputField raycasTargetInputField=null;
	void RaycastToVertices(InputField targetField){
		raycasTargetInputField = targetField;
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
	private void FixedUpdate ()
	{
		float mouseScrollMovement = -Input.mouseScrollDelta.y * Time.deltaTime * mouseControlSpeed;

		Camera.main.transform.Translate (0, 0, mouseScrollMovement);


		if(raycasTargetInputField!=null && Input.GetMouseButtonDown(0)){
			RaycastHit hit;
			Ray ray = Camera.main.ScreenPointToRay (Input.mousePosition);
			//Debug.DrawRay (transform.position, ray.direction*100000, Color.yellow);


			if (Physics.Raycast (Camera.main.transform.position, ray.direction,out hit,Mathf.Infinity,~10)) {
				//Debug.DrawRay (transform.position, ray.direction * hit.distance, Color.red);
				//Debug.Log (hit.transform.name);
				raycasTargetInputField.text = hit.transform.name;
				raycasTargetInputField = null;
			}
		}

	}
}
