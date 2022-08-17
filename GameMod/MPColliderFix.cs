using HarmonyLib;
using Overload;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace GameMod
{
    internal class MPColliderFix
    {
        public static Vector3 m_hitbox = new Vector3(0.57f, 0.64f, 0.71f);
        public static float m_hitbox_angle = -90f;
    }

    [HarmonyPatch(typeof(PlayerShip), "Awake")]
    internal class MPColliderFix_PlayerShip_Awake
    {
        public static void Postfix(ref PlayerShip __instance)
        {
            if (__instance.c_mesh_collider.GetComponent<MeshCollider>() == null)
            {
                var mat_no_friction = __instance.c_mesh_collider.sharedMaterial;
                UnityEngine.Object.Destroy(__instance.c_mesh_collider);
                __instance.c_mesh_collider = null;
                __instance.c_mesh_collider_trans = null;

                GameObject go = new GameObject("_ship_mesh_collider");
                var newGO = UnityEngine.Object.Instantiate(go, Vector3.zero, Quaternion.identity);
                newGO.layer = 16;
                var coll = newGO.AddComponent<MeshCollider>();
                coll.sharedMaterial = mat_no_friction;
                coll.sharedMesh = __instance.c_automap_go.GetComponentInChildren<MeshFilter>().mesh;
                coll.transform.parent = null;
                coll.transform.localScale = MPColliderFix.m_hitbox;
                PlayerMeshCollider pmc = newGO.AddComponent<PlayerMeshCollider>();
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
                GameObject go = new GameObject("_ship_mesh_collider");
                var newMesh = UnityEngine.Object.Instantiate(go, Vector3.zero, Quaternion.identity);
                MeshRenderer mr = newMesh.AddComponent<MeshRenderer>();
                mr.material = UIManager.gm.m_energy_material;
                MeshFilter mf = newMesh.AddComponent<MeshFilter>();
                mf.sharedMesh = __instance.c_automap_go.GetComponentInChildren<MeshFilter>().mesh;
                mf.transform.localScale = MPColliderFix.m_hitbox;
                mf.transform.parent = __instance.c_mesh_collider_trans;
                newMesh.transform.parent = __instance.c_mesh_collider_trans;
                newMesh.transform.localPosition = __instance.c_mesh_collider_trans.localPosition;
                newMesh.transform.localRotation = __instance.c_mesh_collider_trans.localRotation;
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
            __instance.c_player_ship.c_mesh_collider_trans.rotation = Quaternion.AngleAxis(MPColliderFix.m_hitbox_angle, __instance.c_player_ship.c_transform.right) * __instance.c_player_ship.c_transform.rotation;
        }
    }

    [HarmonyPatch(typeof(PlayerShip), "FixedUpdateProcessControlsInternal")]
    internal static class MPColliderFix_PlayerShip_FixedUpdateProcessControlsInternal
    {
        static void Postfix(ref PlayerShip __instance)
        {
            if (GameplayManager.IsMultiplayerActive && __instance.c_mesh_collider_trans != null && __instance.c_transform != null)
            {
                __instance.c_mesh_collider_trans.localRotation = Quaternion.AngleAxis(MPColliderFix.m_hitbox_angle, __instance.c_transform.right) * __instance.c_transform.localRotation;
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
