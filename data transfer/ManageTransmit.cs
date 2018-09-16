using System.Collections;
using System.Collections.Generic;
using UnityEngine;




// Handler for calling upload/download of data in game
public class ManageTransmit : MonoBehaviour {

	public Animator animButtonTransmitUp;			// transmit button
	public Animator animButtonTransmitDown;			// transmit button
	public bool isLock;								// flag if the database is locked or not
	public GameObject alert;

	private ManageObjects manageObjects;			// object manager component used for running download on button press
	private UseComments useComments;				// component used for running download on start
	private ManageTerrainData[] manageTerrainDatas;	// terrain data manager called during button and start

	private WWW transferData;

	// path identify if access is locked or not
	private string lockUrl;
	private string updateUrl;

	// Use this for initialization
	void Start () {
		manageObjects = GameObject.Find ("Managers").GetComponent<ManageObjects> ();
		useComments = GameObject.Find ("Managers").GetComponent<UseComments> ();
		// get an array of terrain data objects
		manageTerrainDatas = GameObject.Find ("Managers").GetComponents<ManageTerrainData> ();

		//load the current serverside set of objects
		manageObjects.DownloadComments (true);
		manageObjects.DownloadCameras (true);

		updateUrl = lockUrl = SessionVars.serverPath + "update.php";
		lockUrl = SessionVars.serverPath + "lock.php";		// the url for sending data from the server

		print ("start coroutines");
		StartCoroutine ( TryData() );								// see if new terrain data is available
//		StartCoroutine ( TryLock() );								// set database lock listener running

	}

	// gets all terrain data - very slow, so only check for push flag first
	void getTerrainData (){
		// download all terrain components (except heightmaps - as this isn't working.
		foreach (ManageTerrainData mdata in manageTerrainDatas) {
			mdata.DownloadDetail (false);
			mdata.DownloadTrees (false);
			mdata.DownloadSplatMap (false);
			// heightmap data findme note: not working
		}
	}
		

	// check for get large terrain data assets
	public IEnumerator TryData(string tryData = "" ) {
		alert.SetActive (true);

		WWWForm form = new WWWForm();							// set form data
		transferData = new WWW(updateUrl, form);					// prepare www transmition

		yield return transferData;								// speak to server and return value

		if (transferData.text == "update") {
			getTerrainData ();
		}

		alert.SetActive (false);								// turn alert message off at the end of the function
	}



	// periodically tap the server to make sure it's not in edit mode
	public IEnumerator TryLock(string tryLock = "" ) {
		print ("try");
		WWWForm form = new WWWForm();							// set form data
		form.AddField("isLock", tryLock);							
		transferData = new WWW(lockUrl, form);					// prepare www transmition

		// add a delay if the lock is not being set or unset, otherwise just go on and pass the info
		if (tryLock == "") {
			yield return new WaitForSeconds (20);
			// alert if update has been made by someone
			// findme todo: needs to be user specific!
			//if (transferData.text == "updated") {
				//			animButtonTransmitDown.SetBool ("isAlert", true);		// change button state
			//}
		}


		yield return transferData;								// speak to server and return value
		print("lock  = " + lockUrl);
		print("transfer data = " + transferData.text);

		if (transferData.text == "locked") {
			animButtonTransmitDown.SetBool ("isAlert", true);	// change button state (reset once download button called
		}
		StartCoroutine (TryLock ());							// call itself to run again
	}



	// detect key to switch camera adjust display
	void Update () {
		if (!useComments.isShowing) {
			// upload data (i = 'insert to database')
			if (Input.GetKeyDown ("i")) {							// detect transfer call
				manageObjects.UploadComments (true);
				manageObjects.UploadCameras (true);
				animButtonTransmitUp.SetBool ("isAlert", false);	// change button state

			}


			// download data (l = 'load from database')
			if (Input.GetKeyDown ("l")) {							// detect transfer call
				manageObjects.DownloadComments (true);
				manageObjects.DownloadCameras (true);
				animButtonTransmitDown.SetBool ("isAlert", false);	// change button state
			}
		}
	}



}
