// findme ref: Original script in Javascript by gregorz: https://forum.unity3d.com/threads/writing-audiolistener-getoutputdata-to-wav-problem.119295/
// adapted to c# with additional commenting. aP 2017

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.IO;

public class RecordSound : MonoBehaviour {


	private int bufferSize;
	private int numBuffers;
	private int outputRate = 44100;								// defualt, try and set from system later + set to 44100 in project audio settings!
	public string fileName = "Renders/shub1env_audio-TEMP.wav"; // file path
	private int headerSize = 44; 								//default for uncompressed wav

	private bool recOutput;

	private FileStream fileStream;

	void Awake()
	{
		outputRate = AudioSettings.outputSampleRate;
	}

	void Start()
	{
		// re-initialise the audio settings to make sure that
		// multi channel output is activated after chaning to soundflower
		// findme fix: adding lines here makes script behave oddly. In Awake() it causes all audio to fail
//		AudioConfiguration config = AudioSettings.GetConfiguration ();
//		AudioSettings.Reset (config);									

		AudioSettings.GetDSPBufferSize(out bufferSize, out numBuffers);	// get the audio buffer sizes
	}

	void Update()
	{		
		if(recOutput == false)
		{
			print("rec");
		}
	}


	// start recording when enabled
	void OnEnable () {


		recOutput = true;
		StartWriting(fileName);
	}


	// write header and close file when script disables
	void OnDisable () {
		recOutput = false;
		WriteHeader();     
		print("rec stop");
	}

	// create and prepare the empty file and pass for writing
	void StartWriting(string name)
	{
		fileStream = new FileStream(name, FileMode.Create);
		byte emptyByte = new byte();

		for(int i = 0; i < headerSize; i++) //preparing the header
		{
			fileStream.WriteByte(emptyByte);
		}
	}

	// pass audio to be saved. Called for every chunk of audio data passed through the system
	void OnAudioFilterRead(float[] data, int channels)
	{
		if(recOutput)
		{
			Write(data); //audio data is interlaced
		}
	}

	// write data to tshe file
	void Write(float[] dataSource)
	{
		// create an empty array to fill with floats. 1 float = 4 bits is 4 bits long
		Byte[] bytesData = new Byte[dataSource.Length*4];

		// copy floats to byte array
		Buffer.BlockCopy(dataSource, 0, bytesData, 0, bytesData.Length);

		// save stream of bytes to file
		fileStream.Write(bytesData,0,bytesData.Length);
	}

	// writes the header information for the wav file and closes
	// findme ref: modified with reference to http://evanxmerz.com/?p=212 & https://gist.github.com/darktable/2317063#file-savwav-cs
	// see http://www.lightlink.com/tjweber/StripWav/Canon.html or http://www-mmsp.ece.mcgill.ca/Documents/AudioFormats/WAVE/WAVE.html for more info
	void WriteHeader()
	{
		// reset the write position to start of file
		fileStream.Seek(0,SeekOrigin.Begin);

		Byte[] riff  = System.Text.Encoding.UTF8.GetBytes("RIFF");				// top section of WAV file
		fileStream.Write(riff,0,4);												// riff = data to write, 0 = offset, 4 = length of bytes to write

		Byte[] chunkSize  = BitConverter.GetBytes(fileStream.Length-8);			// size of chunk - the head
		fileStream.Write(chunkSize,0,4);

		Byte[] wave  = System.Text.Encoding.UTF8.GetBytes("WAVE");				// header format tag
		fileStream.Write(wave,0,4);

		Byte[] fmt  = System.Text.Encoding.UTF8.GetBytes("fmt ");				// sample fmt header tag
		fileStream.Write(fmt,0,4);

		Byte[] subChunk1  = BitConverter.GetBytes(16);							// length of the fmt data header section
		fileStream.Write(subChunk1,0,4);

		UInt16 three  = 3;
		UInt16 two  = 2;
		UInt16 one  = 1;

		Byte[] audioFormat = BitConverter.GetBytes(three);						// format code for float data
		fileStream.Write(audioFormat,0,2);

		Byte[] numChannels  = BitConverter.GetBytes(6);							// number of channels
		fileStream.Write(numChannels,0,2);

		Byte[] sampleRate  = BitConverter.GetBytes(outputRate);					// sampling rate (blocks per second)
		fileStream.Write(sampleRate,0,4);

		Byte[] byteRate = BitConverter.GetBytes(outputRate*6*2);				// data rate
		fileStream.Write(byteRate,0,4);

		//		UInt16 four  = 4;
		Byte[] blockAlign = BitConverter.GetBytes(6 * two);						// block align...
		fileStream.Write(blockAlign,0,2);

		UInt16 thirtytwo = 32;
		Byte[] bitsPerSample = BitConverter.GetBytes(thirtytwo);				// bitrate
		fileStream.Write(bitsPerSample,0,2);

		Byte[] dataString  = System.Text.Encoding.UTF8.GetBytes("data");		// header tag for start of the actual data
		fileStream.Write(dataString,0,4);

		Byte[] subChunk2 = BitConverter.GetBytes(fileStream.Length-headerSize);	// length of data section of file
		fileStream.Write(subChunk2,0,4);

		// end header

		fileStream.Close();
	}
}
