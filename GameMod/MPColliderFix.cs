using HarmonyLib;
using Overload;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Resources;
using System.Text;
using System.Threading;
using UnityEngine;
using UnityEngine.Networking;

namespace GameMod
{
    internal class MPColliderFix
    {
        public static GameObject m_prefab;
        public static GameObject m_go;
    }

    [HarmonyPatch(typeof(PlayerShip), "Start")]
    internal class MPColliderFix_PlayerShip_Start
    {
        static void Postfix(PlayerShip __instance)
        {
            if (!GameplayManager.IsMultiplayer)
                return;

            if (__instance.c_mesh_collider.GetComponent<MeshCollider>() == null)
            {
                if (MPColliderFix.m_prefab == null || MPColliderFix.m_go == null)
                {
                    using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("GameMod.Resources.playershipmeshcollider"))
                    {
                        var ab = AssetBundle.LoadFromStream(stream);
                        MPColliderFix.m_prefab = ab.LoadAsset<GameObject>("PlayershipCollider");
                        MPColliderFix.m_go = UnityEngine.Object.Instantiate(MPColliderFix.m_prefab);
                        ab.Unload(false);
                    }
                }

                UnityEngine.Object.Destroy(__instance.c_mesh_collider);
                var go = UnityEngine.Object.Instantiate(MPColliderFix.m_prefab);
                go.GetComponent<MeshRenderer>().sharedMaterial = UIManager.gm.m_energy_material;
                var coll = go.GetComponent<MeshCollider>();
                __instance.c_mesh_collider = coll;
                __instance.c_mesh_collider_trans = __instance.c_mesh_collider.transform;
            }
        }
    }

    [HarmonyPatch(typeof(Player), "PrepareForMP")]
    internal class MPColliderFix_Player_PrepareForMP
    {
        static bool Prefix(ref Player __instance)
        {
            if (__instance.c_player_ship != null)
            {
                __instance.c_player_ship.c_level_collider.enabled = false;
                __instance.c_player_ship.c_mesh_collider.enabled = false;
            }
            __instance.m_remote_player = true;
            __instance.m_remote_thrusters_active = false;
            __instance.m_server_tick = -1;
            NetworkIdentity component = __instance.GetComponent<NetworkIdentity>();
            if (component != null)
            {
                component.localPlayerAuthority = false;
            }
            if (__instance.c_player_ship.c_mesh_collider.GetType() == typeof(SphereCollider))
            {
                SphereCollider sphereCollider = (SphereCollider)__instance.c_player_ship.c_mesh_collider;
                sphereCollider.radius = 1f;
            }

            return false;
        }
    }

    [HarmonyPatch(typeof(Player), "LerpRemotePlayer")]
    internal static class MPColliderFix_LerpRemotePlayer_ColliderFix
    {
        static void Postfix(Player __instance)
        {
            __instance.c_player_ship.c_mesh_collider_trans.rotation = __instance.c_player_ship.c_transform.rotation;
        }
    }

    [HarmonyPatch(typeof(PlayerShip), "FixedUpdateProcessControlsInternal")]
    internal static class MPColliderFix_PlayerShip_FixedUpdateProcessControlsInternal
    {
        static void Postfix(ref PlayerShip __instance)
        {
            if (GameplayManager.IsMultiplayerActive && __instance.c_mesh_collider_trans != null && __instance.c_transform != null)
            {
                __instance.c_mesh_collider_trans.localRotation = __instance.c_transform.localRotation;
            }
        }
    }

    [HarmonyPatch(typeof(Projectile), "InitData")]
    internal static class MPColliderFix_Projectile_InitData
    {
        static void Postfix(Projectile __instance, ref float ___m_init_speed, ref float ___m_lifetime)
        {
            if (__instance.GetComponent<SphereCollider>() != null)
            {
                var sc = __instance.GetComponent<SphereCollider>();
                var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                sphere.transform.localScale = new Vector3(sc.radius, sc.radius, sc.radius);
                Mesh mesh = UnityEngine.Object.Instantiate(sphere.GetComponent<MeshFilter>().mesh);
                UnityEngine.Object.Destroy(sphere);
                __instance.c_go.AddComponent<MeshFilter>();
                __instance.c_go.AddComponent<MeshRenderer>();
                __instance.c_go.GetComponent<MeshFilter>().mesh = mesh;
                __instance.c_go.GetComponent<MeshFilter>().transform.localScale = new Vector3(sc.radius, sc.radius, sc.radius);
                __instance.c_go.GetComponent<MeshRenderer>().material = UIManager.gm.m_energy_material;
            }

            ___m_init_speed = 5f;
            ___m_lifetime = 100f;
        }
    }
}
