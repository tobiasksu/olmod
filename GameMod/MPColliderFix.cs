using HarmonyLib;
using Overload;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace GameMod
{
    internal class MPColliderFix
    {
        public static GameObject m_prefab;
    }

    [HarmonyPatch(typeof(PlayerShip), "Awake")]
    internal class MPColliderFix_PlayerShip_Awake
    {
        public static void Postfix(ref PlayerShip __instance)
        {
            if (__instance.c_mesh_collider.GetComponent<MeshCollider>() == null)
            {
                // Restructure this to not reload the AB for every ship, OK for testing
                var ab = AssetBundle.LoadFromFile(Path.Combine(GameMod.Config.OLModDir, @"olmod_assets\playershipmeshcollider"));
                if (ab == null)
                {
                    Debug.Log($"Failed to load PlayershipMeshCollider AssetBundle!");
                }

                MPColliderFix.m_prefab = ab.LoadAsset<GameObject>("PlayershipCollider");
                GameObject go = UnityEngine.Object.Instantiate(MPColliderFix.m_prefab);
                ab.Unload(false);

                var mat_no_friction = __instance.c_mesh_collider.sharedMaterial;
                UnityEngine.Object.Destroy(__instance.c_mesh_collider);
                __instance.c_mesh_collider = null;
                __instance.c_mesh_collider_trans = null;

                go.layer = 16;
                var coll = go.AddComponent<MeshCollider>();
                coll.sharedMaterial = mat_no_friction;
                coll.sharedMesh = go.GetComponentInChildren<MeshFilter>().mesh;
                coll.transform.parent = null;
                PlayerMeshCollider pmc = go.AddComponent<PlayerMeshCollider>();
                pmc.c_player = __instance.c_player;

                __instance.c_mesh_collider = coll;
                __instance.c_mesh_collider_trans = coll.transform;
            }
        }
    }

    [HarmonyPatch(typeof(PlayerShip), "Start")]
    internal class MPColliderFix_PlayerShip_Start
    {
        static void Postfix(PlayerShip __instance)
        {
            if (!__instance.c_player.m_spectator && __instance.netId != GameManager.m_local_player.c_player_ship.netId)
            {
                GameObject go = UnityEngine.Object.Instantiate(MPColliderFix.m_prefab);
                go.transform.parent = __instance.c_mesh_collider_trans;
                go.transform.localPosition = __instance.c_mesh_collider_trans.localPosition;
                go.transform.localRotation = __instance.c_mesh_collider_trans.localRotation;
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
