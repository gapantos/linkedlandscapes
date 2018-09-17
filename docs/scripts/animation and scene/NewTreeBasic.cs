using System.Collections;
using System.Collections.Generic;
using UnityEngine;



// findme note: Small script for adding a new instance of a tree whereever the ray hits the terrain
// add to Player


public class NewTreeBasic : MonoBehaviour {

	private Camera mainCam;			// var for holding main camera - makes coding alter more efficient
	private Ray ray;				// ray used for planting/painting
	private RaycastHit hit;			// 3D point if the ray hits an object
	private Vector3 hitPos;			// position of the hitpoint, used for spawning location
	private bool isHit;				// flag to show if raycast hits something or not

	public int dist = 100;			// maximum distance of raycast if needed. Defualt 100 units (m)
	public GameObject cursor;		// 3D cursor to show spawning point

	public GameObject prefabTree;	// the tree to plant

	public Animator animCursor;		// controller for the cursor fading animation

	public LayerMask layerMask;		// ticky list of layers that can be hit
	// findme ref: from http://answers.unity3d.com/questions/416919/making-raycast-ignore-multiple-layers.html



	// Use this for initialization
	void Start () {
		mainCam = Camera.main;		// set camera var
		Terrain [] terrains = FindObjectsOfType( typeof(Terrain) ) as Terrain [] ;		// get all terrains to reset collisions meshes for raycasts to work.

		//findme shim: fudge to make racycast collisions with terrains work.
		foreach (Terrain terrain in terrains){
			// reload colliders for terrain objects to allow raycast hit
			terrain.GetComponent<TerrainCollider> ().enabled = false;
			terrain.GetComponent<TerrainCollider> ().enabled = true;
		}
	}



	// Update is called once per frame
	void FixedUpdate () {


		// cast a ray along the ray defined above
		ray = new Ray (mainCam.transform.position, mainCam.transform.forward);	// define ray to cast

		if (Physics.Raycast (ray, out hit, dist, layerMask)) {					// cast a ray and check for hits
			hitPos = hit.point;													// set point for spawning tree
			isHit = true;														// enable tree spawning
			animCursor.SetBool("on",true);										// enable animation to play
			print("type" + hit.collider.gameObject.GetType());
			print("name" + hit.collider.gameObject.name);
		} else {
			hitPos = ray.GetPoint (dist);										// get a point along the ray at max distance
			isHit = false;														// disable tree spawning
			animCursor.SetBool("on",false);										// enable animation to play
		}

		cursor.transform.position = hitPos;										// set the location of the 3D cursor
		Debug.DrawLine (ray.origin, hitPos, Color.green);						// draw a green line in editor for debugging
			
		// spawn tree at hitpoint on mouse click
		if ( (Input.GetMouseButtonDown (0)) && (isHit) ) {						// detect left mouse button and check there is a hit
			GameObject tree = (GameObject)Instantiate (prefabTree);				// create a new tree game object from a prefab

			Vector3 euler = tree.transform.eulerAngles;
			euler.y = Random.Range(0f, 360f);									// randomize the y rotation of the tree

			tree.transform.eulerAngles = euler;
			tree.transform.position = hitPos;									// place tree at the end of the ray
			tree.layer = 2;														// put the tree on ignore-raycast layer so the trees don't stack up



		}
	}
}
