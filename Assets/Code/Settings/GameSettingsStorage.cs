using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

public class GameSettingsStorage
{
    private static string settingsFileName = "settings.txt";

    public static void Save() {
        using (StreamWriter writer = new StreamWriter(File.Create(settingsFileName))) {
            writer.WriteLine("viewDistance: " + GameSettings.ViewDistance);
            writer.WriteLine("chunksPerSecond: " + GameSettings.ChunksPerSecond);
            writer.WriteLine("featuresPerSecond: " + GameSettings.FeaturesPerSecond);
            writer.WriteLine("maxFramerate: " + GameSettings.MaxFramerate);
            writer.WriteLine("enableVSync: " + GameSettings.EnableVSync);
            writer.WriteLine("fullscreen: " + GameSettings.Fullscreen);
        }
    }

    public static void Load() {
        if(!File.Exists(settingsFileName)) {
            Debug.Log("Settings file does not exist, can't load settings.");
            return;
        }

        string line;
    
        using (StreamReader reader = new StreamReader(settingsFileName)) while((line = reader.ReadLine()) != null) {
            string[] splitString = line.Split(": ");
            string splitStringValue = splitString[1];

            if(line.Contains("viewDistance")) GameSettings.ViewDistance = int.Parse(splitStringValue);
            if(line.Contains("chunksPerSecond")) GameSettings.ChunksPerSecond = int.Parse(splitStringValue);
            if(line.Contains("featuresPerSecond")) GameSettings.FeaturesPerSecond = int.Parse(splitStringValue);
            if(line.Contains("maxFramerate")) GameSettings.MaxFramerate = int.Parse(splitStringValue);
            if(line.Contains("enableVSync")) GameSettings.EnableVSync = bool.Parse(splitStringValue);
            if(line.Contains("fullscreen")) GameSettings.Fullscreen = bool.Parse(splitStringValue);
        }
    }
}
