using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;



// loads a scene.  public var used for debugging.
public class EnterSystem : MonoBehaviour {
	public string scene = "Shub1Env_Unity_1";

	public void Login(){
		SceneManager.LoadScene (scene);				// load scene.  Findme todo: turn into lazy load (if possible with server).
	}
		
}
