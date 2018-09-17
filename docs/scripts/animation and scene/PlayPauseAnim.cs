using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayPauseAnim : MonoBehaviour {

	public Animator anim;
	public float speed = 1.0f;

	void go (){
		anim.speed = speed;
	}

	void stop(){
		anim.speed = 0.0f;
	}

}
