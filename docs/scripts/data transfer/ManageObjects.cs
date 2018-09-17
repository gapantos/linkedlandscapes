using UnityEngine;
using System;
using System.Collections;
using SimpleJSON;

using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Reflection;


// routines for sending comments, cameras and other non terrain objects over the network
[RequireComponent(typeof(MapOffsets))]
[ExecuteInEditMode] 
public class ManageObjects : MonoBehaviour
{


	public WWW transferData;

	// generic class wrapper.  T takes the type of data structure passed to it.
	[System.Serializable]
	public class Row<T>
	{
		public T[] array;
	}

	public string user;
	public string submitURL;
	public string getURL;

	public GameObject commentPrefab;							// container for comment prefab object used when populating data from server

	public Camera[] cameras;
	public int camCount;


	public GameObject[] genObjs;

	// wrap a json string of arrays in [] to make 1 json array
	public string wrapJson(string json) {
		json = "[" + json + "]";
		return json;
	}


	private string serverPath;


	// on enable needed to work both ingame and in editor
	void OnEnable() {
		if (GameObject.Find("SessionVars")) {
			user = GameObject.Find("SessionVars").GetComponent<SessionVars>().user;
		} else {
			user = "aP";
		}

		submitURL = SessionVars.serverPath + SessionVars.uploadPhp;		// the url for sending data from the server
		getURL = SessionVars.serverPath + SessionVars.downloadPhp;		// the url for requesting data from the server

		// get all game objects of cameras in the scene (assuming they are all in this empty)
		camCount = Camera.allCamerasCount;

		cameras = new Camera[camCount];					// initialise empty array hold cameras
		Camera.GetAllCameras(cameras);					// fill array with all active cameras

	}


	void Start () {

	}




	// findme ref: based on https://docs.unity3d.com/ScriptReference/WWWForm.html
	public IEnumerator TransmitData(bool isGet, string schema, string format, bool isEditor = false, bool isSend = false, string data = null, byte[] bdata = null, string table = null, int layer = 0) {
		MapOffsets offset =  this.GetComponent<MapOffsets> ();		// gets the defualt world offset coordinates if no others set.

		string url = submitURL;
		if (isGet) {
			url = getURL;
		}

		// findme todo: add routines for handling different data types
		// create form with named key-val pairs for $_post
		WWWForm form = new WWWForm();
		form.AddField("user", user);								// the user findme todo: add user input.   - create a get_list to get users from the database only and select on login.
		form.AddField("schema", schema);							// the type of data - trees/detail/heightmap - used for insterting into correct part of database
		form.AddField("format", format);							// the format of the data - json/binary

		form.AddField("offset", JsonUtility.ToJson(offset) );		// SW corner offset for terrain data


		if (table != null) {
			form.AddField ("table", table);
		}

		//set data fields
		if (data != null) {
			print ("add json");										// findme debug
			form.AddField("data", data);							// the array to send or receive
		}

		transferData = new WWW(url, form);
		print ("transfering");

		// wait for download to complete and return data
		if (!isEditor) {
			yield return transferData;
		}else{
			while (!transferData.isDone);					// freezes the editor until the transmition is finished.
			// findme todo: replace with system threading to avoid freeze?
		}


		// handle returned values

		if (!string.IsNullOrEmpty(transferData.error)) {				// check for transmit errors
			print(transferData.error);

		}
		else {
			if (isSend) {
				print ( "Data transmitted" );
			} else {													// check if it is a send or get procedure	
				print ( "Data Received" );
				switch (table) {
				case "comments":
					addComments (transferData.text);
					break;
				case "cameras":
					addCameras (transferData.text);
					break;
				case "genObjs":
					addGenObj (transferData.text);
					break;
				}
			}
		}
	}



	///////// comments

	public void UploadComments(bool isEditor) {
		GameObject[] comments =  GameObject.FindGameObjectsWithTag("comment");
		UploadGameObjects (comments, "comments", isEditor);
	}




	public void DownloadComments(bool isEditor){
		// pass info to transfer handler to get comment data. note. no data passed
		StartCoroutine (TransmitData (true, "objects", "json", isEditor : isEditor, table : "comments"));
	}


	void addComments (string dataJson) {
		print ("the data = " + dataJson);

		JSONNode arr = JSON.Parse (dataJson);										// parse database results

		GameObject[] oldcomments = GameObject.FindGameObjectsWithTag("comment");	// get all exisitng comments

		// findme add: replace with a specific diffing routine / history sequence
		// destroy all current comments
		for (int i = 0; i < oldcomments.Length; i++) {
			DestroyImmediate (oldcomments [i]);										// findme note:  permantly destroys objects - needed in editor mode
		}

		Vector3 p = new Vector3 ();
		// loop through comments and insantiate new comment objects
		for (int i = 0; i < arr.Count; i++) {
			p.x = arr[i] ["position"] ["x"];
			p.y = arr[i] ["position"] ["y"];
			p.z = arr[i] ["position"] ["z"];


			GameObject obj = Instantiate (commentPrefab, p , Quaternion.identity );

			obj.GetComponent<Comments> ().comment = arr[i] ["comment"];
			obj.GetComponent<Comments> ().timeMade = arr[i] ["timeMade"];
			obj.GetComponent<Comments> ().timeUpdate = arr[i] ["timeUpdate"];
			obj.GetComponent<Comments> ().author = arr[i] ["author"];

		}


	}



	//===== cameras



	public void UploadCameras(bool isEditor) {

		// create array of game objects that belong to cameras and pass to upload routine
		// findme edit:  this is all a bit roundabout, probably better to split into separate upload routines than 1 with a switch.
		GameObject[] upcameras = new GameObject[camCount];
		int i = 0;
		foreach (Camera cam in cameras) {
			upcameras [i] = cam.gameObject;	
			i++;
		}
		UploadGameObjects (upcameras, "cameras", isEditor);
	
	}




	public void DownloadCameras(bool isEditor){
		// pass info to transfer handler to get camera data. note. no data passed
		StartCoroutine (TransmitData (true, "objects", "json", isEditor : isEditor, table : "cameras"));
	}





	void addCameras (string dataJson) {
		print (camCount + " the camera data = " + dataJson);

		JSONNode arr = JSON.Parse (dataJson);										// parse database results
		Vector3 p,r,u = new Vector3 ();


		// loop through cameras and adjusts settings based on name
		for (int i = 0; i < camCount; i++) {
			// construct position vector from pass json
			p.x = arr[i] ["position"] ["x"];
			p.y = arr[i] ["position"] ["y"];
			p.z = arr[i] ["position"] ["z"];

			// construct rotation from json array
			r.x = arr [i] ["rotation"] ["x"];
			r.y = arr [i] ["rotation"] ["y"];
			r.z = arr [i] ["rotation"] ["z"];

			// compare each camera in passed array to each camera in scene list
			// update information if there is a match
			foreach (Camera camera in cameras) {
				// findme todo: add handler for storing user specific camera positions. currently ignore first person camera
				if (camera.gameObject.name == arr [i] ["cameraName"] ) {
					GameObject cameraObj = camera.gameObject;
					if (arr [i] ["tag"] != "StaticCamera") {
						// set transform for roaming camera (one axis on parent, one on camera object)
						u = cameraObj.transform.parent.gameObject.transform.localEulerAngles;
						u.y = r.y;
						cameraObj.transform.parent.gameObject.transform.localEulerAngles = u;

						u = cameraObj.transform.localEulerAngles;
						u.x = r.x;

						cameraObj.transform.localEulerAngles = u;
						cameraObj.transform.parent.gameObject.transform.position = p;
					} else {
						// set transform for static camera
						cameraObj.transform.position = p;
						cameraObj.transform.localEulerAngles = r;
					}
					camera.fieldOfView = arr [i] ["fieldOfView"];
				}
			}

		}
	}



	// ========  everything else. a.k.a. egneric objecs (GenObj)



	// send generic objects
	public void UploadGenObjs(bool isEditor){
		genObjs = GameObject.FindGameObjectsWithTag ("genObj");
		if (genObjs.Length > 0) {
			UploadGameObjects (genObjs, "genObjs", isEditor);
		} else {
			print ("no general objects");
		}
	}
		

	public void DownloadGenObjs(bool isEditor){
		genObjs = GameObject.FindGameObjectsWithTag ("genObj");
		// pass request to http call
		StartCoroutine (TransmitData (true, "objects", "json", isEditor : isEditor, table : "genObjs"));
	}


	void addGenObj(string dataJson) {
		JSONNode arr = JSON.Parse (dataJson);										// parse database results
		Vector3 p,r,s = new Vector3 ();


		// loop through all currently existing objects
		for (int i = 0; i < genObjs.Length; i++) {
			// construct position vector from pass json
			p.x = arr[i] ["position"] ["x"];
			p.y = arr[i] ["position"] ["y"];
			p.z = arr[i] ["position"] ["z"];

			// construct rotation from json array
			r.x = arr [i] ["rotation"] ["x"];
			r.y = arr [i] ["rotation"] ["y"];
			r.z = arr [i] ["rotation"] ["z"];

			// construct scale from json array
			s.x = arr [i] ["scale"] ["x"];
			s.y = arr [i] ["scale"] ["y"];
			s.z = arr [i] ["scale"] ["z"];

			// compare each camera in passed array to each camera in scene list
			// update information if there is a match
			foreach (GameObject obj in genObjs) {
				print (arr [i] ["name"]);
				// findme todo: add handler for storing user specific camera positions. currently ignore first person camera
				if (obj.name == arr [i] ["name"] ) {
					// set transform for static camera
					obj.transform.position = p;
					obj.transform.localEulerAngles = r;
					obj.transform.localScale = s;
				}
			}

		}

	}




	// package data for upload 
	public void UploadGameObjects(GameObject[] object_r, string table, bool isEditor)
	{

		string jsonobjects = "";																		// initialise new var jsontrees


		// loop through each tree and compile into json string
		// json helper and other wrapper classes not working. not sure why
		for (int i=0; i < object_r.Length; i++)
		{
			GameObject obj = object_r [i];
			string jsonobject = "";



			switch (table) {
			case "comments":
				JsonComment jsonComment = new JsonComment ();
				Comments comment = obj.GetComponent<Comments> ();


				print ("comment = " + comment.comment);
				jsonComment.position = obj.transform.position;
				jsonComment.scale = obj.transform.localScale;
				jsonComment.comment = comment.comment;
				jsonComment.timeMade = comment.timeMade;
				jsonComment.timeUpdate = System.DateTime.Now.ToString();

				jsonobject = JsonUtility.ToJson ( jsonComment );									// encode as json

				break;
			case "cameras":
				JsonCamera jsonCamera = new JsonCamera ();
				Camera camera = obj.GetComponent<Camera> ();
				jsonCamera.fieldOfView = camera.fieldOfView;
				jsonCamera.cameraName = obj.name;
				jsonCamera.tag = obj.tag;

				// check if camera is static or nested in a character
				if (obj.tag != "StaticCamera") {
					// get parent transform (fps character)
					jsonCamera.position = obj.transform.parent.gameObject.transform.position;
					jsonCamera.rotation = obj.transform.parent.gameObject.transform.localEulerAngles;
					jsonCamera.rotation.x = obj.transform.localEulerAngles.x;
				} else {
					// get camera object transform
					jsonCamera.position = obj.transform.position;
					jsonCamera.rotation = obj.transform.localEulerAngles;
				}
				jsonobject = JsonUtility.ToJson ( jsonCamera );										// encode as json

				break;
			case "genObjs":
				JsonGenObj jsonGenObj = new JsonGenObj ();

				// set the serilizable object with bare essentials for objects
				jsonGenObj.position = obj.transform.position;
				jsonGenObj.rotation = obj.transform.rotation.eulerAngles;
				jsonGenObj.scale = obj.transform.localScale;
				jsonGenObj.tag = obj.transform.tag;
				jsonGenObj.name = obj.transform.name;

				jsonobject = JsonUtility.ToJson ( jsonGenObj );										// encode as json

				break;
			}

			jsonobjects = jsonobjects + jsonobject;													// build json string
			if (i < (object_r.Length - 1)) {														// add seperating comma
				jsonobjects = jsonobjects + ",";													// add a comma after any entry that isn't the last entry
			}
		}


		print("json an object " + wrapJson(jsonobjects) );


		// create form object with data to upload.
		StartCoroutine(TransmitData(false, "objects","json", isEditor : isEditor, isSend : true, data : wrapJson(jsonobjects), table: table));
	}


}