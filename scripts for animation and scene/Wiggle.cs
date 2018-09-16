using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// randomly wiggles a vlue for variable volume for mosquitos
// findme ref: adapted from http://answers.unity3d.com/questions/806893/how-can-i-script-a-wiggle.html
public class Wiggle : MonoBehaviour {
	public float speed = 1,					// how vast the values lerp from one value to the next
				 minVolume = 0,				
				 maxVolume = 1,
				 minPan = -1,
				 maxPan = 1;
	public bool isPan = true;				// sets whether the pan should wiggle or just volume

	public int octaves = 4;					// how frequently the value changes


	float volume, pan = 0;
	int currentTime = 0;
	AudioSource audioSource;

	void Start () {
		audioSource = gameObject.GetComponent<AudioSource> ();
	}

	void FixedUpdate ()
	{
		// if number of frames played since last change of direction > octaves create a new destination
		if (currentTime > octaves)
		{
			currentTime = 0;
			volume = generateRandomVector (minVolume, maxVolume);
			pan = generateRandomVector(minPan, maxPan);
		}

		// smoothly change volume and pan
		audioSource.volume = Mathf.Lerp (audioSource.volume, volume, Time.deltaTime);
		if (isPan) {
			audioSource.panStereo = Mathf.Lerp (audioSource.panStereo, pan, Time.deltaTime * speed);
		}
		currentTime++;
	}


	// generates a random vector based on a single amplitude for x y and z
	float generateRandomVector(float min, float max)
	{
		float x = Random.Range(min, max);
	
		return x;
	}
}