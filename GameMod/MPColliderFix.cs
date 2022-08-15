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
        public static Vector3 m_hitbox = new Vector3(1f, 1f, 3f);
        public static float m_hitbox_angle = 10f;
    }

    [HarmonyPatch(typeof(PlayerShip), "Awake")]
    internal class MPColliderFix_PlayerShip_Awake
    {
        public static void Postfix(ref PlayerShip __instance)
        {
            if (__instance.c_mesh_collider.GetComponent<BoxCollider>() == null)
            {
                var mat_no_friction = __instance.c_mesh_collider.sharedMaterial;
                UnityEngine.Object.Destroy(__instance.c_mesh_collider);
                __instance.c_mesh_collider = null;
                __instance.c_mesh_collider_trans = null;

                GameObject go = new GameObject("_ship_mesh_collider");
                var newGO = UnityEngine.Object.Instantiate(go, Vector3.zero, Quaternion.identity);
                newGO.layer = 16;
                var coll = newGO.AddComponent<BoxCollider>();
                coll.sharedMaterial = mat_no_friction;
                coll.size = MPColliderFix.m_hitbox;
                coll.transform.parent = null;
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
                var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                cube.name = "hitboxviz";
                cube.layer = 16;
                cube.transform.localScale = MPColliderFix.m_hitbox;
                MeshRenderer mr = cube.GetComponent<MeshRenderer>();
                mr.material = UIManager.gm.m_energy_material;
                MeshFilter mf = cube.GetComponent<MeshFilter>();
                mf.transform.parent = __instance.c_mesh_collider_trans;
                cube.transform.parent = __instance.c_mesh_collider_trans;
                cube.transform.localPosition = __instance.c_mesh_collider_trans.localPosition;
                cube.transform.localRotation = __instance.c_mesh_collider_trans.localRotation;
                UnityEngine.Object.Destroy(cube.GetComponent<Collider>());
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

}
