using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using Harmony;
using Overload;
using UnityEngine;

namespace GameMod
{
    static class DataReader
    {
        public static string GetData(TextAsset ta, string filename)
        {
            string dir = Environment.GetEnvironmentVariable("OLMODDIR");
            try
            {
                return File.ReadAllText(dir + Path.DirectorySeparatorChar + filename);
            }
            catch (FileNotFoundException)
            {
            }
            return ta.text;
        }
        public static string GetProjData(TextAsset ta)
        {
            if (GameplayManager.LevelIsLoaded && GameplayManager.Level.IsAddOn)
            {
                Debug.Log("GetProjData 1");
                string text3 = null;
                string filepath = Path.Combine(Path.GetDirectoryName(GameplayManager.Level.FilePath), "projdata_" + GameplayManager.Level.Mission.FileName);
                Debug.Log("Attempting to load custom projdata " + filepath);
                byte[] array = Mission.LoadAddonData(GameplayManager.Level.ZipPath, filepath, ref text3, new string[]
                {
                ".txt"
                });
                if (array != null)
                {
                    return System.Text.Encoding.UTF8.GetString(array);
                }
                Debug.Log("Error loading custom projdata, reverting to stock");
                return GetData(ta, "projdata.txt");
            }
            else
            {
                Debug.Log("GetProjData 2");
                return GetData(ta, "projdata.txt");
            }
        }
        public static string GetRobotData(TextAsset ta)
        {
            if (GameplayManager.LevelIsLoaded && GameplayManager.Level.IsAddOn)
            {
                string text3 = null;
                string filepath = Path.Combine(Path.GetDirectoryName(GameplayManager.Level.FilePath), "robotdata_" + GameplayManager.Level.Mission.FileName);
                byte[] array = Mission.LoadAddonData(GameplayManager.Level.ZipPath, filepath, ref text3, new string[]
                {
                ".txt"
                });
                if (array != null)
                {
                    return System.Text.Encoding.UTF8.GetString(array);
                }
                Debug.Log("Error loading custom robotdata, reverting to stock");
                return GetData(ta, "robotdata.txt");
            }
            else
            {
                return GetData(ta, "robotdata.txt");
            }
        }
    }

    [HarmonyPatch(typeof(ProjectileManager), "ReadProjPresetData")]
    class ReadProjPresetDataPatch
    {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var dataReader_GetProjData_Method = typeof(DataReader).GetMethod("GetProjData");
            foreach (var code in instructions)
                if (code.opcode == OpCodes.Callvirt && ((MethodInfo)code.operand).Name == "get_text")
                    yield return new CodeInstruction(OpCodes.Call, dataReader_GetProjData_Method);
                else
                    yield return code;
        }
    }

    [HarmonyPatch(typeof(RobotManager), "ReadPresetData")]
    class ReadRobotPresetDataPatch
    {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var dataReader_GetRobotData_Method = typeof(DataReader).GetMethod("GetRobotData");
            foreach (var code in instructions)
                if (code.opcode == OpCodes.Callvirt && ((MethodInfo)code.operand).Name == "get_text")
                    yield return new CodeInstruction(OpCodes.Call, dataReader_GetRobotData_Method);
                else
                    yield return code;
        }
    }

    [HarmonyPatch(typeof(GameplayManager), "OnSceneLoaded")]
    class PresetData_GameplayManager_OnSceneLoaded
    {
        static void LoadCustomPresets()
        {
            RobotManager.Initialize();
            ProjectileManager.ReadProjPresetData(ProjectileManager.proj_prefabs);
        }

        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> codes)
        {
            foreach (var code in codes)
            {
                if (code.opcode == OpCodes.Call && code.operand == AccessTools.Method(typeof(GameplayManager), "StartLevel"))
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(PresetData_GameplayManager_OnSceneLoaded), "LoadCustomPresets"));
                yield return code;
            }
        }
    }
}
