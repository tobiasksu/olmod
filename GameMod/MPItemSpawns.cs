using HarmonyLib;
using Overload;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using UnityEngine;
using UnityEngine.Networking;

namespace GameMod
{

    public class MPFixedItemSpawn
    {
        public ItemType type;
        public bool respawns;
        public float respawn_length;
        public float respawn_timer;
        public bool active;
        public NetworkInstanceId netId;
        public Vector3 position;
        public bool super;

        public void Update()
        {
            respawn_timer += RUtility.FRAMETIME_GAME;

            if (respawn_timer >= respawn_length && !active && netId == default)
            {
                GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(PrefabManager.item_prefabs[(int)MPFixedItemSpawns.GetPrefabFromType(type)], position, Quaternion.identity);
                NetworkSpawnItem.Spawn(gameObject);
                Item item = gameObject.GetComponent<Item>();
                respawn_timer = 0f;
                active = true;
                super = item.m_super;
                netId = item.netId;
            }
        }
    }

    public static class MPFixedItemSpawns
    {
        private static List<MPFixedItemSpawn> spawns = new List<MPFixedItemSpawn>();

        public static void AddSpawn(MPFixedItemSpawn spawn)
        {
            spawns.Add(spawn);
        }

        public static List<MPFixedItemSpawn> GetSpawns()
        {
            return spawns;
        }

        public static MPFixedItemSpawn GetSpawn(NetworkInstanceId netId)
        {
            return spawns.FirstOrDefault(x => x.netId == netId);
        }

        public static void ClearSpawns()
        {
            spawns.Clear();
        }

        public static ItemPrefab GetPrefabFromType(ItemType itemType)
        {
            switch (itemType)
            {
                case ItemType.KEY_SECURITY:
                    return ItemPrefab.entity_item_security_key;
                case ItemType.LOG_ENTRY:
                    return ItemPrefab.entity_item_log_entry;
                case ItemType.POWERUP_SHIELD:
                    return ItemPrefab.entity_item_shields;
                case ItemType.POWERUP_AMMO:
                    return ItemPrefab.entity_item_ammo;
                case ItemType.POWERUP_ALIEN_ORB:
                    return ItemPrefab.entity_item_alien_orb;
                case ItemType.TEMP_CLOAK:
                    return ItemPrefab.entity_item_cloak;
                case ItemType.TEMP_RAPID:
                    return ItemPrefab.entity_item_rapid;
                case ItemType.POWERUP_ENERGY:
                    return ItemPrefab.entity_item_energy;
                case ItemType.WEAPON_DRILLER:
                    return ItemPrefab.entity_item_driller;
                case ItemType.WEAPON_CYCLONE:
                    return ItemPrefab.entity_item_cyclone;
                case ItemType.WEAPON_FLAK:
                    return ItemPrefab.entity_item_flak;
                case ItemType.WEAPON_LANCER:
                    return ItemPrefab.entity_item_lancer;
                case ItemType.WEAPON_REFLEX:
                    return ItemPrefab.entity_item_reflex;
                case ItemType.WEAPON_SHOTGUN:
                    return ItemPrefab.entity_item_crusher;
                case ItemType.WEAPON_IMPULSE:
                    return ItemPrefab.entity_item_impulse;
                case ItemType.WEAPON_THUNDERBOLT:
                    return ItemPrefab.entity_item_thunderbolt;
                case ItemType.MISSILE_CREEPER:
                    return ItemPrefab.entity_item_creeper;
                case ItemType.MISSILE_DEVASTATOR:
                    return ItemPrefab.entity_item_devastator;
                case ItemType.MISSILE_FALCON:
                    return ItemPrefab.entity_item_falcon4pack;
                case ItemType.MISSILE_HUNTER:
                    return ItemPrefab.entity_item_hunter4pack;
                case ItemType.MISSILE_POD:
                    return ItemPrefab.entity_item_missile_pod;
                case ItemType.MISSILE_SMART:
                    return ItemPrefab.entity_item_nova;
                case ItemType.MISSILE_TIMEBOMB:
                    return ItemPrefab.entity_item_timebomb;
                case ItemType.MISSILE_VORTEX:
                    return ItemPrefab.entity_item_vortex;
                default:
                    return ItemPrefab.entity_item_shields;
            }
        }
    }

    [HarmonyPatch(typeof(GameplayManager), "StartLevel")]
    class MPItemSpawns_GameplayManager_StartLevel
    {
        static void Prefix()
        {
            MPFixedItemSpawns.ClearSpawns();

            int count = 0;
            foreach (var item in GameObject.FindObjectsOfType<Item>())
            {
                if (item.netId.Value <= 0)
                {
                    UnityEngine.Object.Destroy(item.c_go);
                    if (GameplayManager.IsDedicatedServer())
                    {
                        count++;
                        MPFixedItemSpawns.AddSpawn(new MPFixedItemSpawn
                        {
                            netId = default,
                            type = item.m_type,
                            respawns = item.m_respawning,
                            respawn_length = item.m_index,
                            respawn_timer = item.m_respawning ? 0f : item.m_index, // If not set to respawn after pickup, make immediately available
                            active = false,
                            position = item.gameObject.transform.position,
                            super = item.is_super
                        });
                    }
                }
            }

            if (count > 0)
                Debug.Log($"Server: converted {count} item spawns to fixed respawn points");
        }
    }

    [HarmonyPatch(typeof(GameplayManager), "Update")]
    class MPItemSpawns_GameplayManager_Update
    {
        static void Postfix()
        {
            if (!Overload.NetworkManager.IsServer())
                return;

            foreach (var spawn in MPFixedItemSpawns.GetSpawns())
            {
                spawn.Update();
            }
        }
    }

    [HarmonyPatch(typeof(Item), "OnTriggerEnter")]
    class MPItemSpawns_Item_OnTriggerEnter
    {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> codes) {
            foreach (var code in codes)
            {
                if (code.opcode == OpCodes.Call && code.operand == AccessTools.Method(typeof(RobotManager), "RemoveItemFromList"))
                {
                    yield return code;
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(MPItemSpawns_Item_OnTriggerEnter), "UpdateFixedItemSpawn"));
                    continue;
                }
                yield return code;
            }
        }

        static void UpdateFixedItemSpawn(Item item)
        {
            if (GameplayManager.IsMultiplayerActive && GameplayManager.IsDedicatedServer())
            {
                var fixedItemSpawn = MPFixedItemSpawns.GetSpawn(item.netId);
                if (fixedItemSpawn != null)
                {
                    fixedItemSpawn.netId = default;
                    fixedItemSpawn.respawn_timer = 0f;
                    fixedItemSpawn.active = false;
                }

            }
        }
    }
}
