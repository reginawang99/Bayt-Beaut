﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Net;
using System.IO;
using System;
using System.Web;

using System.Net.Http;

public class utilities
{
    static string access_token;
    static string path = "C:/Users/Regina Wang/Config.txt";
    public static GameManager gm;

    public static void startup()
    {
        access_token = System.IO.File.ReadAllLines(@path)[10];
    }

    public static void refreshTokens()
    {
        try
        {
            string[] lines = System.IO.File.ReadAllLines(@path);
            string input = "&grant_type=refresh_token&client_id=" + lines[1] + "&refresh_token=" + lines[4] + "&client_secret=" + lines[7]; ;

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(new Uri("https://www.googleapis.com/oauth2/v4/token"));
            System.Net.ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls;
            request.Method = "POST";
            request.ContentType = "application/x-www-form-urlencoded";
            using (StreamWriter stOut = new StreamWriter(request.GetRequestStream(), System.Text.Encoding.ASCII))
                stOut.Write(input);

            HttpWebResponse hwr = (HttpWebResponse)request.GetResponse();
            using (StreamReader stRead = new StreamReader(hwr.GetResponseStream()))
            {
                string result = stRead.ReadToEnd();
                access_token = result.Substring(21, result.Substring(21).IndexOf('"'));
                lines[10] = access_token;
                System.IO.File.WriteAllLines(@path, lines);

            }
        }
        catch (WebException e)
        {
            if (e.Response != null)
                using (StreamReader sr = new StreamReader((e.Response as HttpWebResponse).GetResponseStream()))
                    Debug.Log(sr.ReadToEnd());
        }
    }

    public static void requestText(string file)
    {
        try {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(new Uri("https://speech.googleapis.com/v1/speech:recognize"));
            System.Net.ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls;

            request.Method = "POST";
            request.Headers["Authorization"] = "Bearer " + access_token;
            request.ContentType = "application/json";

        using (StreamWriter sw = new StreamWriter(request.GetRequestStream()))
        {
                string text = Convert.ToBase64String(File.ReadAllBytes(file));
            string json = 
                    "{" +
                    "\"config\":" +
                    "   {" +
                    "       \"languageCode\" : \"en-US\"," +
                    "       \"speechContexts\" : [{" +
                    "           \"phrases\" : [\"mirror on\" , \"mirror off\" , \"start\"]" +
                    "       }]" +
                    "   }," +
                    "\"audio\":" +
                    "   {" +
                    "       \"content\" : \""  +text+ "\"" +
                    "   }" +
                    "}";
                Debug.Log(json);
            sw.Write(json);
        }
            HttpWebResponse hwr = (HttpWebResponse)request.GetResponse();
            using (StreamReader sr = new StreamReader(hwr.GetResponseStream()))
            {
                string result = sr.ReadToEnd().ToLower();
                if (result.Contains("mirror on"))
                    gm.wordSaid("mirror on");
                else if (result.Contains("mirror off"))
                    gm.wordSaid("mirror off");
                else if (result.Contains("start"))
                    gm.wordSaid("start");
                Debug.Log(result);

            }
        }
        catch (WebException e)
        {
            if (e.Response != null)
                using (StreamReader sr = new StreamReader((e.Response as HttpWebResponse).GetResponseStream()))
                    Debug.Log(sr.ReadToEnd());
            refreshTokens();
        }
    }

    public static void ConvertAndWrite(FileStream fileStream, AudioClip clip)
    {
        var samples = new float[clip.samples];
        clip.GetData(samples, 0);
        Int16[] intData = new Int16[samples.Length];

        //converting in 2 float[] steps to Int16[], //then Int16[] to Byte[]
        Byte[] bytesData = new Byte[samples.Length * 2];

        //bytesData array is twice the size of
        //dataSource array because a float converted in Int16 is 2 bytes.

        int rescaleFactor = 32767; //to convert float to Int16
        for (int i = 0; i < samples.Length; i++)
        {
            intData[i] = (short)(samples[i] * rescaleFactor);
            Byte[] byteArr = new Byte[2];
            byteArr = BitConverter.GetBytes(intData[i]);
            byteArr.CopyTo(bytesData, i * 2);
        }

        fileStream.Write(bytesData, 0, bytesData.Length);
    }

    public static void WriteHeader(FileStream fileStream, AudioClip clip)
    {
        var hz = clip.frequency;
        var channels = clip.channels;
        var samples = clip.samples;

        fileStream.Seek(0, SeekOrigin.Begin);
        Byte[] riff = System.Text.Encoding.UTF8.GetBytes("RIFF");
        fileStream.Write(riff, 0, 4);

        Byte[] chunkSize = BitConverter.GetBytes(fileStream.Length - 8);
        fileStream.Write(chunkSize, 0, 4);

        Byte[] wave = System.Text.Encoding.UTF8.GetBytes("WAVE");
        fileStream.Write(wave, 0, 4);

        Byte[] fmt = System.Text.Encoding.UTF8.GetBytes("fmt ");
        fileStream.Write(fmt, 0, 4);

        Byte[] subChunk1 = BitConverter.GetBytes(16);
        fileStream.Write(subChunk1, 0, 4);

        UInt16 two = 2;
        UInt16 one = 1;

        Byte[] audioFormat = BitConverter.GetBytes(one);
        fileStream.Write(audioFormat, 0, 2);

        Byte[] numChannels = BitConverter.GetBytes(channels);
        fileStream.Write(numChannels, 0, 2);

        Byte[] sampleRate = BitConverter.GetBytes(hz);
        fileStream.Write(sampleRate, 0, 4);

        Byte[] byteRate = BitConverter.GetBytes(hz * channels * 2); // sampleRate * bytesPerSample*number of channels, here 44100*2*2
        fileStream.Write(byteRate, 0, 4);

        UInt16 blockAlign = (ushort)(channels * 2);
        fileStream.Write(BitConverter.GetBytes(blockAlign), 0, 2);

        UInt16 bps = 16;
        Byte[] bitsPerSample = BitConverter.GetBytes(bps);
        fileStream.Write(bitsPerSample, 0, 2);

        Byte[] datastring = System.Text.Encoding.UTF8.GetBytes("data");
        fileStream.Write(datastring, 0, 4);

        Byte[] subChunk2 = BitConverter.GetBytes(samples * channels * 2);
        fileStream.Write(subChunk2, 0, 4);
        //        fileStream.Close();
    }

}
