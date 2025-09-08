using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public static class JsonUtilities
{
    public static string SAVE_DATA_PATH = Application.streamingAssetsPath + "/SaveData/";

    public static void SaveData<T>(T data, string fileName, bool isBackup = false)
    {
        string json = JsonConvert.SerializeObject(data, Formatting.Indented);
        string basePath = SAVE_DATA_PATH;
        if (isBackup) basePath += "Backups/";
        string path = basePath + fileName + ".json";
        File.WriteAllText(path, json);
        Debug.Log("Successfully saved " + fileName + " data:\n\n" + json);
    }

    public static T LoadData<T>(string fileName)
    {
        string jsonFilePath = SAVE_DATA_PATH + fileName + ".json";
        T data = default;

        using (StreamReader r = new StreamReader(jsonFilePath))
        {
            string json = r.ReadToEnd();
            data = JsonConvert.DeserializeObject<T>(json);
        }

        if (data == null) throw new System.Exception("Didn't find the save file " + jsonFilePath);
        return data;
    }
}
