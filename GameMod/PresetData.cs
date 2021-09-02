using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using Overload;
using UnityEngine;

namespace GameMod
{

    public static class PresetData
    {
        public static bool ProjDataExists
        {
            get
            {
                return !String.IsNullOrEmpty(MPModPrivateData.CustomProjdata);
            }
        }

        public static void UpdateLobbyStatus()
        {
            if (ProjDataExists)
            {
                MenuManager.AddMpStatus("USING CUSTOM PROJDATA FOR THIS MATCH", 1f, 21);
            }
            else
            {
                // Clear status 21 so it doesn't incorrectly persist between lobbies
                var idx = Array.IndexOf(MenuManager.m_mp_status_id, 21);
                if (idx >= 0)
                {
                    MenuManager.m_mp_status_details[idx] = String.Empty;
                    MenuManager.m_mp_status_flash[idx] = 0f;
                    MenuManager.m_mp_status_id[idx] = -1;
                }
            }
        }

    }

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
            if (PresetData.ProjDataExists)
            {
                return MPModPrivateData.CustomProjdata;
            }
            else if (GameplayManager.IsMultiplayer)
            {
                return MPModPrivateData.DEFAULT_PROJ_DATA;
            }
            else
            {
                // Look for "robotdata.txt" in SP/CM zip files and use if possible
                if (!GameplayManager.IsMultiplayer && GameplayManager.Level.IsAddOn)
                {
                    string text3 = null;
                    string filepath = Path.Combine(Path.GetDirectoryName(GameplayManager.Level.FilePath), "projdata");
                    byte[] array = Mission.LoadAddonData(GameplayManager.Level.ZipPath, filepath, ref text3, new string[]
                    {
                        ".txt"
                    });
                    if (array != null)
                    {
                        return System.Text.Encoding.UTF8.GetString(array);
                    }
                }
                return GetData(ta, "projdata.txt");
            }

        }
        public static string GetRobotData(TextAsset ta)
        {
            // Look for "robotdata.txt" in SP/CM zip files and use if possible
            if (!GameplayManager.IsMultiplayer && GameplayManager.Level.IsAddOn)
            {
                string text3 = null;
                string filepath = Path.Combine(Path.GetDirectoryName(GameplayManager.Level.FilePath), "robotdata");
                byte[] array = Mission.LoadAddonData(GameplayManager.Level.ZipPath, filepath, ref text3, new string[]
                {
                        ".txt"
                });
                if (array != null)
                {
                    return System.Text.Encoding.UTF8.GetString(array);
                }
            }
            return GetData(ta, "robotdata.txt");
        }
    }

    [HarmonyPatch(typeof(ProjectileManager), "ReadProjPresetData")]
    class ReadProjPresetDataPatch
    {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var dataReader_GetProjData_Method = typeof(DataReader).GetMethod("GetProjData");
            foreach (var code in instructions)
            {
                if (code.opcode == OpCodes.Callvirt && ((MethodInfo)code.operand).Name == "get_text")
                {
                    yield return new CodeInstruction(OpCodes.Call, dataReader_GetProjData_Method);
                    continue;
                }

                if (code.opcode == OpCodes.Call && code.operand == AccessTools.Method(typeof(RUtility), "ReadField"))
                {
                    yield return code;
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    yield return new CodeInstruction(OpCodes.Ldloc_S, 6);
                    yield return new CodeInstruction(OpCodes.Ldloc_S, 10);
                    yield return new CodeInstruction(OpCodes.Ldc_I4_0);
                    yield return new CodeInstruction(OpCodes.Ldelem_Ref);
                    yield return new CodeInstruction(OpCodes.Ldloc_S, 10);
                    yield return new CodeInstruction(OpCodes.Ldc_I4_1);
                    yield return new CodeInstruction(OpCodes.Ldelem_Ref);
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(ReadProjPresetDataPatch), "MaybeReadProjectileExt"));
                    continue;
                }
                yield return code;
            }
        }

        static void MaybeReadProjectileExt(GameObject[] prefabs, int arrIdx, string name, string value)
        {
            Debug.Log($"MaybeReadProjectileExt({prefabs.Length}, {arrIdx}, {name}, {value})");
            ProjectileExt component = prefabs[arrIdx].GetComponent<ProjectileExt>();
            if (component)
            {
                if (name.StartsWith("olmod"))
                {
                    Debug.Log($"Processing {name}");
                    RUtility.ReadField(component, name, value);
                }
            }
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

    // Add annoying custom projdata HUD message when playing MP
    [HarmonyPatch(typeof(UIElement), "DrawHUD")]
    class Preset_UIElement_DrawHUD
    {
        static void Postfix(UIElement __instance)
        {
            if (PresetData.ProjDataExists)
            {
                Vector2 vector = default(Vector2);
                vector.x = UIManager.UI_LEFT + 110;
                vector.y = UIManager.UI_TOP + 120f;
                __instance.DrawStringSmall("Using custom projdata", vector, 0.5f, StringOffset.CENTER, UIManager.m_col_damage, 1f);
            }
        }
    }

    // Update lobby status display
    [HarmonyPatch(typeof(MenuManager), "MpMatchSetup")]
    class PresetData_MenuManager_MpMatchSetup
    {
        static void Postfix()
        {
            if (MenuManager.m_menu_sub_state == MenuSubState.ACTIVE)
            {
                if (MenuManager.m_menu_micro_state != 2)
                {
                    PresetData.UpdateLobbyStatus();
                }
            }
        }
    }

    // Update lobby status display
    [HarmonyPatch(typeof(NetworkMatch), "OnAcceptedToLobby")]
    class MPModifiers_NetworkMatch_OnAcceptedToLobby
    {
        static void Postfix()
        {
            PresetData.UpdateLobbyStatus();
        }
    }

    // Update lobby status display
    [HarmonyPatch(typeof(UIElement), "DrawMpPreMatchMenu")]
    class MPModifiers_UIElement_DrawMpPreMatchMenu
    {
        static void Prefix()
        {
            PresetData.UpdateLobbyStatus();
        }
    }

    // Heavy-handed, re-init robot/projdatas on scene loaded
    [HarmonyPatch(typeof(GameplayManager), "OnSceneLoaded")]
    class PresetData_GameplayManager_OnSceneLoaded
    {
        static void LoadCustomPresets()
        {
            ProjectileManager.ReadProjPresetData(ProjectileManager.proj_prefabs);
            RobotManager.ReadPresetData(RobotManager.m_enemy_prefab);
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

    //[HarmonyPatch(typeof(RUtility), "ReadField")]
    //class PresetData_RUtility_ReadField
    //{
    //    static bool Prefix(object obj, string name, string value)
    //    {
    //        try
    //        {
    //            switch (name)
    //            {
    //                case "m_ammo_consumption":
    //                case "m_energy_consumption":
    //                default:
    //                    return true;
    //            }
    //        }
    //        catch (Exception ex)
    //        {
    //            Debug.Log("Custom projdata error: " + ex.Message);
    //        }
    //        return true;
    //    }
    //}

    [HarmonyPatch]
    class PresetData_ProjectileManager_Init
    {
        static MethodBase TargetMethod()
        {
            foreach (var x in typeof(ProjectileManager).GetNestedTypes(BindingFlags.NonPublic))
            {
                if (x.Name.Contains("Init"))
                {
                    return x.GetMethod("MoveNext");
                }
            }
            return null;
        }

        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> codes)
        {
            int state = 0;
            foreach (var code in codes)
            {
                if (code.opcode == OpCodes.Stelem_Ref)
                {
                    state++;
                    if (state == 3)
                    {
                        state = 4;
                        yield return code;
                        yield return new CodeInstruction(OpCodes.Ldloc_1);
                        yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(PresetData_ProjectileManager_Init), "PatchInit"));
                        continue;
                    }
                }
                yield return code;
            }
        }

        static void PatchInit(int arrIdx)
        {
            ProjectileManager.proj_prefabs[arrIdx].AddComponent<ProjectileExt>();
        }
    }

    //[HarmonyPatch(typeof(PlayerShip), "MaybeFireWeapon")]
    //class PresetData_PlayerShip_MaybeFireWeapon
    //{
    //    static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> codes)
    //    {
    //        foreach (var code in codes)
    //        {
    //            if (code.opcode == OpCodes.Ldc_R4 && (float)code.operand == 0.666667f)
    //            {
    //                yield return new CodeInstruction(OpCodes.Ldloc_S, 7);
    //                yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(PresetData_PlayerShip_MaybeFireWeapon), "GetEnergyUsage"));
    //                continue;
    //            }
    //            yield return code;
    //        }
    //    }

    //}

    

    //[HarmonyPatch(typeof(Projectile), "OnCollisionEnter")]
    //class PresetData_Projectile_ProcessCollision
    //{
    //    static void Prefix(Projectile __instance, Collision collision)
    //    {
    //        var collider = collision.gameObject;
    //        Debug.Log($"m_alive = {__instance.m_alive}, layer = {collider.layer}, bounce? = {collider.CompareTag("Bounce")}, shouldplaydamageeffect = {__instance.ShouldPlayDamageEffect(collider.layer)}");
    //    }
    //}
}
