using HarmonyLib;
using Overload;
using Rewired;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;
using static YamlDotNet.Samples.DeserializeObjectGraph;

namespace GameMod
{
    internal static class MPBots
    {
        public static Dictionary<NetworkHash128, GameObject> m_registered_prefabs = new Dictionary<NetworkHash128, GameObject>()
        {
            { NetworkHash128.Parse("e2656f"), (GameObject)Resources.Load("entity_enemy_RecoilA") },
            { NetworkHash128.Parse("e2657f"), (GameObject)Resources.Load("entity_enemy_RecoilB") },
            { NetworkHash128.Parse("e2658f"), (GameObject)Resources.Load("entity_enemy_ViperA") }
        };

        /// <summary>
        /// Client-side handler
        /// </summary>
        /// <param name="pos"></param>
        /// <param name="asset_id"></param>
        /// <returns></returns>
        private static GameObject SpawnNetworkRobotHandler(Vector3 pos, NetworkHash128 asset_id)
        {
            Debug.Log($"NetworkSpawnRobotHandler: {asset_id}");
            GameObject prefabFromAssetId = m_registered_prefabs[asset_id];
            if (prefabFromAssetId == null)
            {
                Debug.LogErrorFormat("Error looking up prefab with asset_id {0}", asset_id.ToString());
                return null;
            }
            GameObject gameObject = InstantiateNetworkRobot(prefabFromAssetId, pos, Quaternion.identity);
            if (gameObject == null)
            {
                Debug.LogErrorFormat("Error instantiating robot in SpawnNetworkRobotHandler");
                return null;
            }
            Robot component = gameObject.GetComponent<Robot>();
            if (component == null)
            {
                Debug.LogErrorFormat("Could not find Robot component on instantiated robot prefab in SpawnNetworkRobotHandler");
                return null;
            }
            gameObject.SetActive(true);
            return gameObject;
        }

        /// <summary>
        /// Client-side handler
        /// </summary>
        /// <param name="spawned"></param>
        private static void UnspawnNetworkRobotHandler(GameObject spawned)
        {
            spawned.SetActive(false);
        }

        /// <summary>
        /// Server-side instantiate
        /// </summary>
        /// <param name="robot_prefab"></param>
        /// <param name="pos"></param>
        /// <param name="rot"></param>
        /// <returns></returns>
        private static GameObject InstantiateNetworkRobot(GameObject robot_prefab, Vector3 pos, Quaternion rot)
        {
            GameObject gameObject = UnityEngine.Object.Instantiate(robot_prefab, pos, rot);
            if (gameObject == null)
            {
                Debug.LogErrorFormat("Failed to instantiate {0} in InstantiateNetworkRobot", robot_prefab.name);
                return null;
            }
            return gameObject;
        }

        /// <summary>
        /// Client-side registration
        /// </summary>
        [HarmonyPatch(typeof(Client), "OnConnectMsg")]
        static class MPBots_Client_OnConnectMsg
        {
            public static void Postfix()
            {
                foreach (var prefab in m_registered_prefabs)
                {
                    if (prefab.Value.GetComponent<NetworkIdentity>() == null)
                    {
                        prefab.Value.AddComponent<NetworkIdentity>();
                    }

                    if (prefab.Value.GetComponent<NetworkTransform>() == null)
                    {
                        prefab.Value.AddComponent<NetworkTransform>();
                        NetworkTransform networkTransform = prefab.Value.GetComponent<NetworkTransform>();
                        networkTransform.enabled = true;
                        networkTransform.sendInterval = 0.16666667f;
                        networkTransform.transformSyncMode = NetworkTransform.TransformSyncMode.SyncRigidbody3D;
                        networkTransform.interpolateMovement = 1f;
                        networkTransform.interpolateRotation = 1f;
                        networkTransform.velocityThreshold = 0.01f;
                    }
                    ClientScene.RegisterPrefab(prefab.Value, prefab.Key);
                    ClientScene.RegisterSpawnHandler(prefab.Key, SpawnNetworkRobotHandler, UnspawnNetworkRobotHandler);
                }
            }
        }

        /// <summary>
        /// Server-side spawn robot
        /// </summary>
        /// <param name="prefabName"></param>
        public static void SpawnNetworkBot(NetworkHash128 asset_id, Vector3 position)
        {
            var robot_prefab = m_registered_prefabs[asset_id];

            var go = InstantiateNetworkRobot(robot_prefab, position, Quaternion.identity);
            var robot = go.GetComponent<Robot>();

            robot.MakeRobotVariant();
            go.SetActive(true);
            NetworkServer.Spawn(go, asset_id);
        }
    }

    [HarmonyPatch(typeof(Robot), "Start")]
    class MPBots_Robot_Start
    {
        static bool Prefix()
        {
            if (!Overload.NetworkManager.IsServer())
                return false;

            return false;
        }
    }

    [HarmonyPatch(typeof(Robot), "Update")]
    class MPBots_Robot_Update
    {
        static bool Prefix()
        {
            if (!Overload.NetworkManager.IsServer())
                return false;

            return false;
        }
    }

    [HarmonyPatch(typeof(Robot), "FixedUpdate")]
    class MPBots_Robot_FixedUpdate
    {
        static bool Prefix(Robot __instance, int ___fire_pos_index)
        {
            if (!Overload.NetworkManager.IsServer())
                return false;

            // mucking around, shoot a bunch of projectiles for one second, every five seconds
            if ((int)NetworkMatch.m_match_elapsed_seconds % 5 == 0)
            {
                //__instance.MaybeFire();
                Quaternion rot = __instance.c_transform.rotation;

                ProjectileManager.FireProjectileRobot(__instance, __instance.m_fire_projectile, __instance.fire_pos[___fire_pos_index].position, rot, __instance.c_go, 0f, ProjTeam.ENEMY, __instance.m_fire_proj_level, true);
            }

            return false;
        }
    }

    [HarmonyPatch(typeof(PlayerShip), "CallRpcFireFlare")]
    class MPBots_PlayerShip_CallRpcFireFlare
    {
        static void Postfix(PlayerShip __instance)
        {
            // Spawn valkyrie on player flare
            MPBots.SpawnNetworkBot(MPBots.m_registered_prefabs.ElementAt(2).Key, __instance.c_transform.position);
        }
    }

    [HarmonyPatch(typeof(ProjectileManager), "FireProjectileRobot")]
    class MPBots_ProjectileManager_FireProjectileRobot
    {
        static void Postfix(Robot robot, ProjPrefab type, Vector3 pos, Quaternion rot, GameObject owner, float strength, ProjTeam proj_team, WeaponUnlock upgrade_lvl, bool save_pos)
        {
            if (!Overload.NetworkManager.IsServer())
                return;

            NetworkServer.SendToAll(MessageTypes.MsgBotSniperPacket, new BotSniperPacketMessage
            {
                m_robot_id = robot.netId,
                m_type = type,
                m_pos = pos,
                m_rot = rot,
                m_strength = strength,
                m_upgrade_lvl = upgrade_lvl,
                m_save_pos = save_pos
            });

        }
    }

    /// <summary>
    /// Add client handler for MsgBotSniperPacket
    /// </summary>
    [HarmonyPatch(typeof(Client), "RegisterHandlers")]
    class MPBots_Client_RegisterHandlers
    {
        static void Postfix()
        {
            if (Client.GetClient() == null)
                return;
            Client.GetClient().RegisterHandler(MessageTypes.MsgBotSniperPacket, OnBotSniperPacket);
        }

        private static void OnBotSniperPacket(NetworkMessage rawMsg)
        {
            var msg = rawMsg.ReadMessage<BotSniperPacketMessage>();

            GameObject owner = ClientScene.FindLocalObject(msg.m_robot_id);
            ParticleElement pe = ProjectileManager.FireProjectile(msg.m_type, msg.m_pos, msg.m_rot, owner, msg.m_strength, ProjTeam.ENEMY, msg.m_upgrade_lvl, msg.m_save_pos);
            Robot robot = owner.GetComponent<Robot>();
            robot.AddMuzzleFlash(pe);
        }
    }

    /// <summary>
    /// Enable player projectile -> robot mesh collisions
    /// </summary>
    [HarmonyPatch(typeof(GameplayManager), "StartLevel")]
    class MPBots_GameplayManager_StartLevel
    {
        static void Postfix()
        {
            Physics.IgnoreLayerCollision(13, 11, false);
        }
    }

    /// <summary>
    /// This message allows for communication of sniper packets between the client and the server.
    /// </summary>
    public class BotSniperPacketMessage : MessageBase
    {
        public override void Serialize(NetworkWriter writer)
        {
            writer.Write(m_robot_id);
            writer.Write((byte)m_type);
            writer.Write(m_pos.x);
            writer.Write(m_pos.y);
            writer.Write(m_pos.z);
            writer.Write(m_rot.w);
            writer.Write(m_rot.x);
            writer.Write(m_rot.y);
            writer.Write(m_rot.z);
            writer.Write(m_strength);
            writer.Write((byte)m_upgrade_lvl);
            writer.Write(m_save_pos);
        }
        public override void Deserialize(NetworkReader reader)
        {
            m_robot_id = reader.ReadNetworkId();
            m_type = (ProjPrefab)reader.ReadByte();
            m_pos = new Vector3();
            m_pos.x = reader.ReadSingle();
            m_pos.y = reader.ReadSingle();
            m_pos.z = reader.ReadSingle();
            m_rot = new Quaternion();
            m_rot.w = reader.ReadSingle();
            m_rot.x = reader.ReadSingle();
            m_rot.y = reader.ReadSingle();
            m_rot.z = reader.ReadSingle();
            m_strength = reader.ReadSingle();
            m_upgrade_lvl = (WeaponUnlock)reader.ReadByte();
            m_save_pos = reader.ReadBoolean();
        }

        public NetworkInstanceId m_robot_id;
        public ProjPrefab m_type;
        public Vector3 m_pos;
        public Quaternion m_rot;
        public float m_strength;
        public WeaponUnlock m_upgrade_lvl;
        public bool m_save_pos;
    }
}