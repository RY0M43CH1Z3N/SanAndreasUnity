﻿using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Newtonsoft.Json.Linq;
using System.Linq;
using System.IO;

namespace SanAndreasUnity.Editor
{
    [InitializeOnLoad]
    public class DevIdManager
    {
        private static string game_path = "";

        static DevIdManager()
        {
            //game_path = Environment.GetEnvironmentVariable("ProgramFiles");

            string configPath = Utilities.Config.FileName,
                   contents = File.ReadAllText(configPath);
            var obj = contents.JsonDeserialize<JObject>();

            var prop = obj["game_dir"];
            string game_dir = prop != null ? prop.Value<string>() : "";
            bool isSet = true;

            if (prop != null)
                obj.Remove("game_dir");

            var objDev = obj["dev_profiles"];

            if (objDev != null)
            {
                Dictionary<string, string> devs = objDev.Value<Dictionary<string, string>>();
                game_dir = devs.Where(x => x.Key == SystemInfo.deviceUniqueIdentifier).FirstOrDefault().Value;
            }
            else
                isSet = false;

            if (string.IsNullOrEmpty(game_dir))
                game_path = EditorUtility.OpenFolderPanel("Select GTA instalation Path", game_path, "");
            else
                game_path = game_dir;

            if (!isSet)
                obj["dev_profiles"] = JObject.FromObject(new Dictionary<string, string> { { SystemInfo.deviceUniqueIdentifier, game_path } });

            string postContents = obj.JsonSerialize(true);
            if (postContents != contents)
                File.WriteAllText(configPath, postContents);
        }
    }
}