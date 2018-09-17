using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioPlay : MonoBehaviour {

	private AudioSource audioSource;
	// Use this for initialization
	void Start () {
	}

	void OnEnable(){
		gameObject.GetComponent<AudioSource> ().Play ();
	}


	// Update is called once per frame
	void Update () {
		
	}
}
