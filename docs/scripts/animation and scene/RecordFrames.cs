using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// findme: ref. 
// from https://docs.unity3d.com/ScriptReference/Time-captureFramerate.html
 //& https://docs.unity3d.com/ScriptReference/Application.CaptureScreenshot.html
// && http://answers.unity3d.com/questions/22954/how-to-save-a-picture-take-screenshot-from-a-camer.html
public class RecordFrames : MonoBehaviour {

	public string filename = "Renders/shub1env_video-TEMP/shub1env_video-TEMP-f";
	public int lastframe = 20;	// frame to stop recording at
	public int scaleFactor = 1;	// amount to increase render size by

	int frame = 0;
	float deltaTime = 0.0f;

	// Use this for initialization
	void Start () {
		Time.captureFramerate = 30;			// the desired frame rate

	}

	void Awake() {
		
	}


	// Update is called once per frame
	void Update () {
		if (frame < lastframe){
			// capture the screen frame with multiplier value for extra size
			Application.CaptureScreenshot (filename + frame + ".png", scaleFactor);
		}

		frame ++;
		deltaTime += (Time.deltaTime - deltaTime) * 0.1f;

	}

	// write frame rate info to screen
	// findme ref: from http://wiki.unity3d.com/index.php?title=FramesPerSecond
	/*
	void OnGUI()
	{
		int w = Screen.width, h = Screen.height;

		GUIStyle style = new GUIStyle();

		Rect rect = new Rect(0, 0, w, h * 2 / 100);
		style.alignment = TextAnchor.UpperLeft;
		style.fontSize = h * 2 / 100;
		style.normal.textColor = new Color (0.0f, 0.0f, 0.5f, 1.0f);
		float msec = deltaTime * 1000.0f;
		float fps = 1.0f / deltaTime;
		string text = string.Format("{0:0.0} ms ({1:0.} fps)", msec, fps);
		GUI.Label(rect, text, style);
	}
*/
}
