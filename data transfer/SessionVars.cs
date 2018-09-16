using UnityEngine;

public class SessionVars : MonoBehaviour
{
	// switch for local and production servers

	public static string serverPath = "http://88.212.165.213/";
	public static string uploadPhp = "todb.php";
	public static string downloadPhp = "fromdb.php";

	/*
	public static string serverPath = "http://localhost:8888/shub1Env-test/";
	public static string uploadPhp = "decode.php";
	public static string downloadPhp = "encode.php";
	*/


	public string user = "aP";

	void Awake() {
		DontDestroyOnLoad (transform.gameObject);
	}


	public void SetUser(string u) {
		user = u;
	}
}

