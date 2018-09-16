using UnityEngine;
using System;
using System.Collections;
using SimpleJSON;
// using Unity.IO.Compression;			findme: deprecated debug removed from project

using System.Linq; // used for Sum of array


// needed for changing color array to byte array for saving raw
// findme ref: from http://answers.unity3d.com/questions/190340/how-can-i-send-a-render-texture-over-the-network.html
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Reflection;




// findme ref: adapted from https://docs.unity3d.com/ScriptReference/TerrainData.GetAlphamaps.html

[RequireComponent(typeof(MapOffsets))]
[ExecuteInEditMode]
public class ManageTerrainData : MonoBehaviour
{
	public WWW transferData;
	public Terrain terrain;

	// generic class wrapper.  T takes the type of data structure passed to it.
	[System.Serializable]
	public class Row<T>
	{
		public T[] array;
	}
		
	public string submitURL;
	public string getURL;


	// wrap a json string of arrays in [] to make 1 json array
	public string wrapJson(string json) {
		json = "[" + json + "]";
		return json;
	}

	private byte[] gzbytes;							// container for zipped byte data (outside of loop to provide access to data)

	public string user;
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
	}


	// findme ref: based on https://docs.unity3d.com/ScriptReference/WWWForm.html
	public IEnumerator TransmitData(bool isGet, string schema, string format, bool isEditor = false, bool isSend = false, string data = null, byte[] bdata = null, string table = null, bool isTerrainData = true, int layer = 0) {
		MapOffsets offset =  this.GetComponent<MapOffsets> ();

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
		if (isTerrainData) {										// excludes terrain ID for non terrain-linked objects (like comments)
			form.AddField ("terrainID", terrain.name);				// ID of the terrain to be accessed
			form.AddField ("tilesize", JsonUtility.ToJson (terrain.terrainData.size));

			// get the offsets from the current terrain
			offset = terrain.GetComponent<MapOffsets> ();			// modify offset from defualt to terrain specfic
			print ("offset = " + offset);
		}
		form.AddField("offset", JsonUtility.ToJson(offset) );		// SW corner offset for terrain data


		if (table != null) {
			form.AddField ("table", table);
		}

		print ("sending to  " + url);
		//set data fields
		if (data != null) {
			print ("add json " + schema + "." + table);					// findme debug
			form.AddField("data", data);		// the array to send or receive
		}
		if (bdata != null) {
			print ("add binary " + schema + "." + table);				// findme debug

			// findme todo: add variable filename / mime type depending on content (e.g. png)
			form.AddBinaryData("bdata", bdata, schema + "_" + table, "application/x-gzip");		// binary data transmitter.  findme ref: see https://docs.unity3d.com/ScriptReference/WWWForm.AddBinaryData.html
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

			print (transferData.text);
			if (isSend) {
				print ( "Data transmitted" );
			} else {													// check if it is a send or get procedure	
				print ( "Data Received" );
				// wrap json with string to work with json helper
				if (format == "json") {
					addJsonData (transferData.text, schema, table : table, layer : layer);
				} else {
					addByteData (transferData.bytes, schema, layer);
				}
			}


		}
	}



	// detirmines where to send received data
	void addJsonData (string dataJson, string schema, string table = null, int layer = 0) {
		print (dataJson);
		switch (schema) {
		case "objects":
			if (table == "trees") {
				addTrees (dataJson);
			}
			break;
		case "detailmaps":
			addDetailmaps (dataJson, layer);
			break;
		case "splatmaps":
			addSplatmaps (dataJson, layer);
			break;
		case "heightmaps":
			addHeightmaps (dataJson, layer);
			break;
		}
	}




	// loads stored detail maps into the terrain.
	// findme add: add processing for mismatching detail maps
	void addByteData (byte[] data, string schema, int layer){
		int height = 0;
		int width = 0;

		switch (schema) {
		case "detailmaps":
			height = terrain.terrainData.detailHeight;
			width = terrain.terrainData.detailWidth;

			Texture2D tex = new Texture2D (width, height, TextureFormat.RGBA32, false);
			tex.LoadImage (data);
			print (layer);

			int [,] map = new int[width,height];

			// iterate through the passed data and transfer to new array
			for (int y = height -1 ; y > -1 ; y--) {
				for (int x = 0; x < tex.width; x++) {
					map[y,x] = (int)( 255 * (tex.GetPixel(x,y).grayscale));					// set value to red channel of pixel array and scale from 0-1 to 0-255
				}
			}
			terrain.terrainData.SetDetailLayer (0, 0, layer, map);
			terrain.Flush();
			break;
		case "splatmaps":
			print ("add splat data");
			height = terrain.terrainData.alphamapHeight;
			width = terrain.terrainData.alphamapWidth;
			print (height + "  " + width);

			Texture2D alphamap = new Texture2D (width, height, TextureFormat.RGBA32, false);
			alphamap.LoadImage (data);
			print (layer);

			// iterate through the passed data and transfer to new array
			for (int y = height - 1; y > -1; y--) {
				for (int x = 0; x < alphamap.width; x++) {
					splatMaps [y, x, layer] = (alphamap.GetPixel (x, y).grayscale);
				}
			}



			break;

		case "heightmaps":
			// findme ref: from http://answers.unity3d.com/questions/1084016/how-to-use-a-script-to-import-terrain-raw.html
			height = terrain.terrainData.heightmapHeight;
			width = terrain.terrainData.heightmapWidth;

			float[,] heightmap = new float[width,height];

			int i = 0;
			// iterate through each layer and set the received data
			for (int y = height -1 ; y > -1 ; y--) {
				for (int x = 0; x < width; x++) {
					heightmap[y,x] = (float)data[i];					// set value to red channel of pixel array and scale from 0-1 to 0-255



				}
			}



			terrain.terrainData.SetHeights(0, 0, heightmap);


			terrain.Flush();
			break;
		}
	}






	void addSplatmaps (string dataJson, int layer){
		// findme add: info for splat map layers (smoothness etc)
	}

	void addHeightmaps (string dataJson, int layer){
		// findme add: info for height map
	}



//trees  ============================

	// add trees from database to terrain
	void addTrees (string dataJson){
		
		JSONNode arr = JSON.Parse (dataJson);
		TreeInstance[] trees = new TreeInstance[arr.Count];			// create array of length

		// instantiate new variables (outside for for efficiency)
		Vector3 p = new Vector3 ();
		Vector3 t = new Vector3 ();

		// loop through tree instances and convert to unity units
		for (int i = 0; i < arr.Count; i++) {
			
			// add non-arrayed data
			trees[i].widthScale = arr [i] ["widthScale"];
			trees[i].heightScale = arr [i] ["heightScale"];
			trees[i].prototypeIndex = arr [i] ["prototypeIndex"];
			trees[i].rotation = arr [i] ["rotation"];


			// rescale to relative positioning of terrain.
			p.x = arr [i] ["position"] ["x"];										// set x (lat) of tree
			p.z = arr [i] ["position"] ["z"];										// set z (long) of tree

			print( "input " + arr [i] ["position"] ["x"]);
			print( "input " + arr [i] ["position"] ["z"]);
			print( "input " + arr [i] ["position"] ["y"]);


			t = terrain.terrainData.size;											// get terrain size for scaling

			p.y = terrain.SampleHeight (p) - terrain.GetPosition().y;				// get height of tree as offset form base in world units										

			// set tree position and scale to pos range of 0-1.
			trees[i].position = new Vector3( p.x/t.x, p.y/t.y, p.z/t.z );			// set tree position

			print ("tree pos   " + trees[i].position);

			// create temporary color to code color data
			Color c = new Color (arr [i] ["color"]["r"].AsFloat, arr [i] ["color"]["g"].AsFloat, arr [i] ["color"]["b"].AsFloat, arr [i] ["color"]["a"].AsFloat);
			trees[i].color = c;

			// create temporary color to code color data
			Color lc = new Color (arr [i] ["lightmapColor"]["r"].AsFloat, arr [i] ["lightmapColor"]["g"].AsFloat, arr [i] ["lightmapColor"]["b"].AsFloat, arr [i] ["lightmapColor"]["a"].AsFloat);
			trees[i].lightmapColor = lc;

		}


		print (trees.Length);
		terrain.terrainData.treeInstances = trees;													// set trees from database
//		terrain.Flush();																			// refresh terrain settings
	}




	// get trees from server
	public void DownloadTrees(bool isEditor)
	{
		
		// pass info to transfer handler to get tree data. note. no data passed
		StartCoroutine (TransmitData (true, "objects", "json", isEditor : isEditor, table : "trees"));

	}



	// upload tree data
	public void uploadTrees(bool isEditor)
	{
		

		TreeInstance[] trees = terrain.terrainData.treeInstances;									// get the tree list for the current terrain
		string jsontrees = "";																		// initialise new var jsontrees

		// loop through each tree and compile into json string
		for (int i=0; i < trees.Length; i++)
		{
			trees[i].position = Vector3.Scale(trees[i].position, terrain.terrainData.size);			// convert the tree position to world units
			
			string jsontree = JsonUtility.ToJson (trees[i]);										// encode as json
			jsontrees = jsontrees + jsontree;														// build json string
			if (i < (trees.Length - 1)) {															// add seperating comma
				jsontrees = jsontrees + ",";														// add a comma after any entry that isn't the last entry
			}
		}

		// create form object with data to upload. 
		StartCoroutine(TransmitData(false, "objects","json", isEditor : isEditor, isSend : true, data : wrapJson(jsontrees), table: "trees"));
	}




	// texture (splat) maps   ============================


	// send texture alpha maps to server
	public void UploadSplatMap(bool isEditor) {

		// load the alphamap data into array

		int height = terrain.terrainData.alphamapHeight;
		int width = terrain.terrainData.alphamapWidth;
		float[,,] amaps = terrain.terrainData.GetAlphamaps (0, 0, width, height);


		// loop through all alphamap layers and throw them at the server.
		// note - might be quite slow.
		for ( int i = 0; i < terrain.terrainData.alphamapLayers; i++) {


			// create a 2D array to take content of each splat layer
			float [,] amap = new float[width, height];

			// extract single layer of splat map for sending
			for (int y = 0; y < height; y++) {
				for (int x = 0; x < width; x++) {
					amap [x, y] = amaps [x, y, i];
				}
			}

//findme todo: standardise this method of ntable name production
			string table = terrain.name + "_" + i;			
			print (table);

			SplatPrototype tex = terrain.terrainData.splatPrototypes [i];

			string info = "{\"table\" : \"" + table + "\" , \"splatPrototype\" : \"" + tex.texture.name + "\", ";
			info += "\"width\" : " + width + ", \"height\" : " + height + "}";
			//findme todo: add more texture info like tile size?


			// findme note: for alternative conversion types see "UploadDetail()";

			// make png
			byte[] pngBytes = ArrayToPNG( amap );

			// transfer png
			StartCoroutine (TransmitData (false, "splatmaps", "png", isEditor : true, table:table, isSend : true, data : info, bdata : pngBytes));

		}
	}

	private float[,,] splatMaps;						// 3D array for holding texture map (splat map) data

	// get texturemaps from server
	// findme note:  only loads data into prepared slots. Doesn't add textures slots if textures have been deleted.
	//findme ref: from https://alastaira.wordpress.com/2013/11/14/procedural-terrain-splatmapping/
	//findme note: Sum of all textures weights must add to 1, so calculate normalization factor from sum of weights

	public void DownloadSplatMap(bool isEditor){
		

		int width = terrain.terrainData.alphamapWidth;
		int height = terrain.terrainData.alphamapHeight;
		int layers = terrain.terrainData.alphamapLayers;
	

		splatMaps = new float [width, height, layers];		// initialise the global var to correct size for current terrain

		// call a request to the server for each alpha map
		for (int i = 0 ; i < terrain.terrainData.alphamapLayers ; i ++) {

			// uid  = terrain id + layer id.  
			string table = terrain.name + "_" + i;

			//findme delete: don't think width and height ar needed
			// package the table info and wdith/height required for iamge to be exported by GDAL
			string info = "{\"width\" : " + width + ", \"height\" : " + height + "}";

			// send post request (no binary data sent, only flags to return correct image)
			// get the image data
			StartCoroutine (TransmitData (true, "splatmaps", "png", isEditor : true, data : info, table: table, layer : i, isTerrainData:false ));
			// get the layer settings
			StartCoroutine (TransmitData (true, "splatmaps", "json", isEditor : true, data : info, table: table, layer : i, isTerrainData:false ));
		}


		// alphamap values must be rescaled to avoid blow-out (they are blend weights and should add to 1.
		// this means that if  you have 2 texutres and only 1 is showing, one map = 1 the other is = 0.
		// this is useful when adding maps from QGIS - but best results are acheived by careful mixing of the source files.
		// findme ref: adapted from https://alastaira.wordpress.com/2013/11/14/procedural-terrain-splatmapping/
		// Sum of all textures weights must add to 1 (for each pixel), so calculate normalization factor from sum of weights

		for (int y = height - 1; y > -1; y--) {
			for (int x = 0; x < width; x++) {

				float[] splatWeights = new float[layers];
				// loop through each layer and get splatmap weight
				for (int i = 0; i < layers; i++) {
					splatWeights [i] = splatMaps [x, y, i];
				}

				float z = splatWeights.Sum();

				for (int i = 0; i < layers; i++) {
					splatMaps [x, y, i] = splatWeights[i] / z;
				}
			}
		}



			
		terrain.terrainData.SetAlphamaps (0, 0, splatMaps);
		terrain.Flush();

	}



	// heightmaps   ============================


	// send heightmaps to server
	public void UploadHeightMap(bool isEditor) {

		int height = terrain.terrainData.heightmapHeight;
		int width = terrain.terrainData.heightmapWidth;

		// load heightmap into array of floats (32bit data)
		float[,] map = terrain.terrainData.GetHeights(0,0,width,height);

		// uid  = terrain name. 
		string table = terrain.name;
		print (table);


		string info = "{\"heightmapResolution\" : " + terrain.terrainData.heightmapResolution + ", ";
		info += "\"heightmapScalex\" : " + terrain.terrainData.heightmapScale.x + ", \"heightmapScaley\" : " + terrain.terrainData.heightmapScale.y + ", \"heightmapScalez\" : " + terrain.terrainData.heightmapScale.z + ", ";
		info += "\"width\" : " + width + ", \"height\" : " + height + "}";
	

		// make png
//		byte[] pngBytes = ArrayToPNG( map );

		// transfer png
//		StartCoroutine (TransmitData (false, "heightmaps", "png", isEditor : isEditor, table:table, isSend : true, data : info, bdata : pngBytes)); 

		// raw is a better choice becuase it 8-bit PNG heavily compresses the height of the shallow terrain.
		// detail will be important in the immediate vicinity of the scene.

		// make raw
		byte[] byteDump = ArrayToRaw ( map );
		// compress raw
//		byte[] zbytes = ZipData (byteDump, info);

//		System.IO.File.WriteAllBytes ("/Users/hairyfreak/Desktop/splatDump.bin", byteDump);		//findme debug
//		System.IO.File.WriteAllBytes ("/Users/hairyfreak/Desktop/splatDump.gz", zbytes);		//findme debug

		// transfer raw data
		StartCoroutine (TransmitData (false, "heightmaps", "raw", isEditor : isEditor, table:table, isSend : true, data : info, bdata : byteDump)); 
		// transfer zipped data
//		StartCoroutine (TransmitData (false, "heightmaps", "zipRaw", isEditor : isEditor, table:table, isSend : true, data : info, bdata : zbytes)); 

	}

	// get heightmap from server
	// findme fix:  not currently working - retrieved values are incorrect.
	public void DownloadHeightMap(bool isEditor) {
		int width = terrain.terrainData.heightmapWidth;
		int height = terrain.terrainData.heightmapHeight;

		// send post request (no binary data sent, only flags to return correct image)
		StartCoroutine (TransmitData (true, "heightmaps", "json", isEditor : true, table: terrain.name, isTerrainData:false ));
		StartCoroutine (TransmitData (true, "heightmaps", "raw", isEditor : true, table: terrain.name, isTerrainData:false ));
	}



	// Detail maps   ============================

	public void UploadDetail(bool isEditor)
	{
		int width = terrain.terrainData.detailHeight;
		int height = terrain.terrainData.detailWidth;
		int i = 0;											// layer index for the prototype

		// loop through all detail types and throw them at the server.
		// note - might be quite slow.
		foreach (DetailPrototype detail in terrain.terrainData.detailPrototypes) {
			string prototypeTextureName = "";
			string detailPrototypeName = "";

			// try... catch... requires using.system.   also needed as not every detail layer will have a mesh (e.g. grass)
			try {
				prototypeTextureName = terrain.terrainData.detailPrototypes [i].prototypeTexture.name;
				print ("texture detail = " + prototypeTextureName);	//findme debug
			} catch (Exception e) {
				print ("no detail texture" + e);
			}

			try {
				detailPrototypeName = terrain.terrainData.detailPrototypes [i].prototype.name;
				print ("texture mesh = " + detailPrototypeName);
			} catch (Exception e) {
				print ("no detail mesh" + e);
			}


			// table/uid  = terrain id + layer id.  
			string table = terrain.name + "_" + i;
			print (table);


			string info = "{\"prototypeIndex\" : " + i + ", \"prototypeTextureName\" : \"" + prototypeTextureName + "\", \"detailPrototypeName\" : \"" + detailPrototypeName + "\", ";
			info += " \"maxWidth\" : " + detail.maxWidth + ", \"maxHeight\" : " + detail.maxHeight + ", ";
			info += " \"minWidth\" : " + detail.minWidth + ", \"minHeight\" : " + detail.minHeight + ", ";
			info += " \"noiseSpread\" : " + detail.noiseSpread + ", \"healthyColor\" : \"" + ColorUtility.ToHtmlStringRGBA (detail.healthyColor) + "\", \"dryColor\" : \"" + ColorUtility.ToHtmlStringRGBA (detail.dryColor) + " \", \"bendFactor\" : " + detail.bendFactor + ",";
			info += " \"width\" : " + width + ", \"height\" : " + height + "}";



			// make png
			byte[] pngBytes = ArrayToPNG( IntToFloat (terrain.terrainData.GetDetailLayer (0, 0, width,height, i),true) );

			// make raw
//			byte[] byteDump = ArrayToRaw (IntToFloat (terrain.terrainData.GetDetailLayer (0, 0, terrain.terrainData.detailWidth, terrain.terrainData.detailHeight, i), false));
			// compress raw
//			byte[] zbytes = ZipData (byteDump, info);

			// make json
//			string mapJSON = ArrayToJsonInt( ( terrain.terrainData.GetDetailLayer(0, 0, terrain.terrainData.detailWidth, terrain.terrainData.detailHeight, 0 ) ) );
//			System.IO.File.WriteAllText ("/Users/hairyfreak/Desktop/splatDump.txt", mapJSON);										//findme debug

			// transfer png
			StartCoroutine (TransmitData (false, "detailmaps", "png", isEditor : true, isSend : true, table : table, data : info, bdata : pngBytes));

			// transfer zipped data
//			StartCoroutine (TransmitData (false, "detailmap", "zipRaw", isEditor : true, isSend : true, data : info, bdata : zbytes)); 

			i++;	// increment counter
		}
	}



	// Color converter
	// findme ref: from http://wiki.unity3d.com/index.php?title=HexConverter
	Color HexToColor(string hex)
	{
		byte r = byte.Parse(hex.Substring(0,2), System.Globalization.NumberStyles.HexNumber);
		byte g = byte.Parse(hex.Substring(2,2), System.Globalization.NumberStyles.HexNumber);
		byte b = byte.Parse(hex.Substring(4,2), System.Globalization.NumberStyles.HexNumber);
		return new Color32(r,g,b, 255);
	}



	// sets the settings of detail layers
	//findme ref: used https://gamedev.stackexchange.com/questions/43929/unity-how-to-apply-programmatical-changes-to-the-terrain-splatprototype

	// findme note:  Requires SimpleJSON to parse data. 
	// Built in JSONUtility is fast but can't cope with JSON arrays or jagged arrays (as created by itself when parsing Vector 3!)

	// findme fix:  object properties (height etc) not updating. don't know why.
	void addDetailmaps (string dataJson, int layer)
	{
		
		DetailPrototype newDetail = terrain.terrainData.detailPrototypes[layer];
		JSONNode arr = JSON.Parse (dataJson);

		newDetail.dryColor =  HexToColor(arr[0]["drycolor"]);
		newDetail.healthyColor = HexToColor(arr[0]["healthycolor"]);

		newDetail.maxHeight = (float)arr[0]["maxheight"];
		newDetail.minHeight = (float)arr[0]["minheight"];
		newDetail.minWidth = (float)arr[0]["minwidth"];
		newDetail.maxWidth = (float)arr[0]["maxwidth"];
		newDetail.noiseSpread = (float)arr[0]["noisespread"];
		newDetail.bendFactor = (float)arr[0]["bendfactor"];

	
		if (arr [0] ["prototypetexturename"] != "") {
			newDetail.prototypeTexture.name = arr[0]["prototypetexturename"];	
		}
		if (arr [0] ["detailprototypename"] != "") {
			newDetail.prototype.name = arr[0]["detailprototypename"];
		}
			

		terrain.terrainData.detailPrototypes[layer] = newDetail;
		terrain.Flush ();
	}




	// loads detail map (rocks and trees) from server
	public void DownloadDetail(bool isEditor)
	{
		
		int width = terrain.terrainData.detailHeight;
		int height = terrain.terrainData.detailWidth;

		int i = 0;	// counter to track detail layer being processed

		// call a request tot he server for each detail map
		foreach (DetailPrototype detail in terrain.terrainData.detailPrototypes) {

			// uid  = terrain id + layer id.  
			string table = terrain.name + "_" + i;

			// package the table info and wdith/height required for iamge to be exported by GDAL
			string info = "{\"width\" : " + width + ", \"height\" : " + height + "}";


			// send post request (no binary data sent, only flags to return correct image)
			StartCoroutine (TransmitData (true, "detailmaps", "png", isEditor : true, data : info, table: table, layer : i, isTerrainData:false ));
			StartCoroutine (TransmitData (true, "detailmaps", "json", isEditor : true, data : info, table: table, layer : i, isTerrainData:false ));

			i++;
		}

	}










	///////// converters and compressors


	// convert a 2D int array to 2D float array for saving to image file

	float[,] IntToFloat (int[,] map, bool is8bit = false) {
		int height = map.GetLength (1);
		int width =  map.GetLength (0);
		float[,] fmap = new float[width, height];
		// check type of array is valid and convert any intger array to float to work with PNG color encoding
		if ( map.GetType() == typeof(int[,]) ){							// check type is integer
			for (int i = 0 ; i < height ; i++ )				// loop rows
			{
				for (int j = 0 ; j < width ; j++ )			// loop columns
				{
					float val = (float)map [i, j];						// convert integer into a float and scale to max

					if (is8bit) {										// scale & truncate if going into an 8-bit PNG
						val = (float)map [i, j] / 255.0f;				// ensure output value is in float - otherwise data lossed
						if (val > 1.0f) {								// truncate anything out of gamut
							val = 1.0f;
						}
					}
					fmap [i, j] = val;				
				}
			}
			return fmap;												// return the converted array
		}else{
			print("wrong array format for conversion");					// output an error message
			return null;												// return nothing
		}
	}




	// converts an array (e.g. from heightmap, detail map) to PNG and sends.
	// should pass in something like terrain.terrainData.HeightmapHeight
	// or map = terrain.terrainData.GetDetailLayer(0, 0, terrain.terrainData.detailWidth, terrain.terrainData.detailHeight, 0);
	// findme ref: made with reference to https://forum.unity3d.com/threads/export-terrain-heightmap-2-png.55020/

	// Note.  need to convert any int[,] into float[,]for saving to texture as color.

	byte[] ArrayToPNG (float[,] map) {
		int height = map.GetLength (1);
		int width =  map.GetLength (0);
		Texture2D tex = new Texture2D (width, height, TextureFormat.RGBA32, false);								// 8bit per channel 2D texture to store array data for convesion to png
		for (int y = 0; y < height; y++) {																		// iterate across rows detail map to the max dimensions of the detail map
			for (int x = 0; x < width; x++) {																	// iterate across columns to max dimensions of detail map
				float val = map [y, x];
				Color color = new Color(val,val,val,1.0f);														// make a color for adding to png
				tex.SetPixel( x, y, color);																		// set pixel to color
			}
		}
				
		tex.Apply();																							// applies setpixel changes to texture on GPU. 
		//findme note: not sure if needed when not rendering the texture?
		byte[] byteDump = tex.EncodeToPNG();																	// encode texture to PNG for sending


		return byteDump;
	}



	// convert a 2D array into a binary raw file (32bit float) and passes to zip for sending
	// Note.  need to convert any int[,] into float[,] (important for detailmaps if being sent.

	// findme ref: used https://alastaira.wordpress.com/2013/11/12/importing-dem-terrain-heightmaps-for-unity-using-gdal/ for identifying rotation
	// http://wiki.unity3d.com/index.php?title=TerrainImporter
	byte[] ArrayToRaw ( float[,] map ) {
		int height = map.GetLength (1);
		int width =  map.GetLength (0);


		float[] map1D = new float[width * height];																	// 1D float for writing to binary file
		int i = 0;
		for (int y = height; y > 0; y--) {																			// iterate across rows (starting from top - unity reads bottom up)
			for (int x = 0; x < width; x++) {																		// iterate across columns to max dimensions of detail map 
				map1D [i] =  map [y-1,x];																			// xy flipped for deal with bottom up. y-1 because array index starts a 0
				i++;
			}
		}

		byte[] byteDump = new byte[map.Length * 4];																	// initiate byte array for holding floats as bytes
		Buffer.BlockCopy(map1D, 0, byteDump, 0, byteDump.Length);													// shift floats across
		print ("buffereing");																						// 


		// the output

	  return byteDump;
	}


	/* // findme deprecated: supporting libraries removed frmo porject

	// compress byte data in a zipfile and return bytes
	byte[] ZipData (byte[] bytes, string data = "") {

		// findme ref: help on explaining 'using'
		// + zipstream to memory stream https://stackoverflow.com/questions/3722192/how-do-i-use-gzipstream-with-system-io-memorystream
		// and for no copyto https://stackoverflow.com/questions/230128/how-do-i-copy-the-contents-of-one-stream-to-another


		print ("begin zip");

		using (System.IO.MemoryStream zipStream = new System.IO.MemoryStream ()) {									// create memory stream to take comrpessed data
			using (GZipStream compressed = new GZipStream (zipStream, CompressionMode.Compress)){					// create compression stream and pass it to memory stream
				compressed.Write (bytes, 0, bytes.Length);															// pass bytes to gzip stream
			}

			// findme note: IMPORTANT to have passing of memory stream outside call to pass data to compression to ensure process has completed otherwise gets corrupted
			// // http://www.nathanfox.net/blog/71/NET-GZipStream-must-be-closed-or-it-produces-corrupt-data
			byte[] zbytes = zipStream.ToArray ();
			// send zipped data to transmit. findme Note: uses named paramter

			return zbytes;
		}
	}
	*/








	///// serialize 2D array into JSON for sending over network.
	//findme obsolete as injection to DB too slow


	[System.Serializable]
	public class RowN
	{
		public int[] array;

	}


	// convert an array to JSON string for sending over network
	string ArrayToJsonInt (int[,] map) {

		//string[] rows = new string[10]; 	// make new array of strings as long as the map is tall

		RowN[] rows = new RowN[terrain.terrainData.detailHeight];													// initialise new array of serializable (to work with json serializing) row objects
		for (int y = 0; y < terrain.terrainData.detailHeight; y++) {													// iterate across rows detail map to the max dimensions of the detail map
			RowN row = new RowN();																					// create a new row (of type row with internal paramter of type T) T =  paramter type which is passed through
			row.array = new int[terrain.terrainData.detailWidth];															// make new array of type T that is as long as the map is wide (one created for every row)

			for (int x = 0; x < terrain.terrainData.detailWidth; x++) {													// iterate across columns to max dimensions of detail map
				row.array[x] = map[x,y];																					// add value to array
			}

			rows[y] = row;																								// add row to array of rows before moving to next row.

		}


		// generate final JSON string for all the rows using JsonHelper.

		string mapJSON = JsonHelper.ToJson(rows);
		//		print ("TABLE    " + mapJSON);

		//		print (JsonUtility.ToJson(rows));

		return mapJSON;													// return the json string

	}




	// convert an array to JSON string for sending over network
	// generic method (allows int[,] or float[,]
	string ArrayToJson <T>(T[,] map) {
		int height = map.GetLength (1);
		int width =  map.GetLength (0);



		Row<T>[] rows = new Row<T>[height];													// initialise new array of serializable (to work with json serializing) row objects

		string[] srows = new string[height];												// array of strings


		for (int y = 0; y < height; y++) {													// iterate across rows detail map to the max dimensions of the detail map
			Row<T> row = new Row<T>();																	// create a new row (of type row with internal paramter of type T) T =  paramter type which is passed through
			row.array = new T[width];														// make new array of type T that is as long as the map is wide (one created for every row)
			for (int x = 0; x < width; x++) {												// iterate across columns to max dimensions of detail map
				row.array[x] = map[x,y];																// add value to array
			}

			rows[y] = row;																	// add row to array of rows before moving to next row.
		}

		print (JsonUtility.ToJson(srows));

		Row<Row<T>> maptable = new Row<Row<T>> ();
		//		Row<string> smaptable = new Row<string> ();

		maptable.array = rows;
		//		smaptable.array = srows;


		// generate final JSON string for all the rows using JsonHelper.
		string mapJSON = JsonHelper.ToJson <Row<T>>(rows);
		//		string smapJSON = JsonUtility.ToJson (smaptable);
		print (JsonUtility.ToJson(rows));


		return mapJSON;													// return the json string
	}



}