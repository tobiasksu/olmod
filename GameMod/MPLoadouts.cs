using HarmonyLib;
using Overload;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using UnityEngine;
using UnityEngine.Networking;

namespace GameMod
{
    public class MPLoadouts
    {
        public static Dictionary<int, LoadoutDataMessage> NetworkLoadouts = new Dictionary<int, LoadoutDataMessage>();

        public static CustomLoadout[] Loadouts = new CustomLoadout[4]
        {
            new BomberLoadout(WeaponType.IMPULSE, MissileType.FALCON, MissileType.CREEPER),
            new GunnerLoadout(WeaponType.DRILLER, WeaponType.CYCLONE, MissileType.HUNTER),
            new BomberLoadout(WeaponType.THUNDERBOLT, MissileType.FALCON, MissileType.CREEPER),
            new GunnerLoadout(WeaponType.CRUSHER, WeaponType.CYCLONE, MissileType.HUNTER)
        };

        public enum LoadoutType
        {
            BOMBER = 0, // One primary, two secondaries
            GUNNER = 1  // Two primaries, one secondary
        }

        public class CustomLoadout
        {
            public LoadoutType loadoutType;
            public List<WeaponType> weapons;
            public List<MissileType> missiles;

            public CustomLoadout()
            {
                weapons = new List<WeaponType>();
                missiles = new List<MissileType>();
            }
        }

        public class BomberLoadout : CustomLoadout
        {
            public BomberLoadout(WeaponType weapon, MissileType missile1, MissileType missile2)
            {
                loadoutType = LoadoutType.BOMBER;
                weapons = new List<WeaponType>() { weapon };
                missiles = new List<MissileType>() { missile1, missile2 };
            }
        }

        public class GunnerLoadout : CustomLoadout
        {
            public GunnerLoadout(WeaponType weapon1, WeaponType weapon2, MissileType missile)
            {
                loadoutType = LoadoutType.GUNNER;
                weapons = new List<WeaponType>() { weapon1, weapon2 };
                missiles = new List<MissileType>() { missile };
            }
        }

        public class LoadoutDataMessage : MessageBase
        {
            public override void Serialize(NetworkWriter writer)
            {
                writer.WritePackedUInt32((uint)this.lobby_id);
                writer.WritePackedUInt32((uint)loadouts.Count);
                for (int i = 0; i < loadouts.Count; i++)
                {
                    writer.WritePackedUInt32((uint)i);
                    writer.WritePackedUInt32((uint)loadouts[i].loadoutType);
                    writer.WritePackedUInt32((uint)loadouts[i].weapons.Count);
                    for (int j = 0; j < loadouts[i].weapons.Count; j++)
                    {
                        writer.WritePackedUInt32((uint)loadouts[i].weapons[j]);
                    }
                    writer.WritePackedUInt32((uint)loadouts[i].missiles.Count);
                    for (int j = 0; j < loadouts[i].missiles.Count; j++)
                    {
                        writer.WritePackedUInt32((uint)loadouts[i].missiles[j]);
                    }
                }
            }

            public override void Deserialize(NetworkReader reader)
            {
                loadouts = new List<CustomLoadout>();
                this.lobby_id = (int)reader.ReadPackedUInt32();
                uint numLoadouts = reader.ReadPackedUInt32();
                for (int i = 0; i < numLoadouts; i++)
                {
                    uint loadoutIndex = reader.ReadPackedUInt32();
                    CustomLoadout loadout = new CustomLoadout();
                    loadout.loadoutType = (LoadoutType)reader.ReadPackedUInt32();
                    uint weaponCount = reader.ReadPackedUInt32();
                    for (int j = 0; j < weaponCount; j++)
                    {
                        loadout.weapons.Add((WeaponType)reader.ReadPackedUInt32());
                    }
                    uint missileCount = reader.ReadPackedUInt32();
                    for (int j = 0; j < missileCount; j++)
                    {
                        loadout.missiles.Add((MissileType)reader.ReadPackedUInt32());
                    }
                    loadouts.Add(loadout);
                }
            }

            public int lobby_id;
            public List<CustomLoadout> loadouts;
        }

        public static void MpCycleWeapon(int loadoutIndex, int weaponIndex)
        {
            MPLoadouts.Loadouts[loadoutIndex].weapons[weaponIndex] = (WeaponType)((((int)MPLoadouts.Loadouts[loadoutIndex].weapons[weaponIndex]) + 1) % (int)WeaponType.LANCER);

            if (MPLoadouts.Loadouts[loadoutIndex].weapons.Count(x => x == MPLoadouts.Loadouts[loadoutIndex].weapons[weaponIndex]) > 1)
                MPLoadouts.Loadouts[loadoutIndex].weapons[weaponIndex] = (WeaponType)((((int)MPLoadouts.Loadouts[loadoutIndex].weapons[weaponIndex]) + 1) % (int)WeaponType.LANCER);
        }

        public static void MpCycleMissile(int loadoutIndex, int missileIndex)
        {
            MPLoadouts.Loadouts[loadoutIndex].missiles[missileIndex] = (MissileType)((((int)MPLoadouts.Loadouts[loadoutIndex].missiles[missileIndex]) + 1) % (int)MissileType.NOVA);

            if (MPLoadouts.Loadouts[loadoutIndex].missiles.Count(x => x == MPLoadouts.Loadouts[loadoutIndex].missiles[missileIndex]) > 1)
                MPLoadouts.Loadouts[loadoutIndex].missiles[missileIndex] = (MissileType)((((int)MPLoadouts.Loadouts[loadoutIndex].missiles[missileIndex]) + 1) % (int)MissileType.NOVA);
        }

    //    public static void SendPlayerLoadoutToServer()
    //    {
    //        if (Client.GetClient() == null)
    //        {
    //            Debug.LogErrorFormat("Null client in MPLoadouts.SendServerPlayerLoadout for player", new object[0]);
    //            return;
    //        }

    //        Debug.Log($"SendPlayerLoadoutToServer called for: {Player.Mp_loadout1}, {Player.Mp_loadout2}");

    //        LoadoutDataMessage loadoutDataMessage = new LoadoutDataMessage();
    //        loadoutDataMessage.lobby_id = NetworkMatch.m_my_lobby_id;
    //        loadoutDataMessage.loadouts = new List<CustomLoadout> { MPLoadouts.Loadouts[Player.Mp_loadout1], MPLoadouts.Loadouts[Player.Mp_loadout2] };
    //        Client.GetClient().Send(MessageTypes.MsgCustomLoadouts, loadoutDataMessage);
    //    }
    //}

    //[HarmonyPatch(typeof(Client), "SendPlayerLoadoutToServer")]
    //internal class MPLoadouts_Client_SendPlayerLoadoutToServer
    //{
    //    static void Postfix()
    //    {
    //        MPLoadouts.SendPlayerLoadoutToServer();
    //    }
    //}

    //[HarmonyPatch(typeof(Server), "SendLoadoutDataToClients")]
    //internal class MPLoadouts_Server_SendLoadoutDataToClients
    //{
    //    static void Postfix()
    //    {
    //        foreach (var kvp in MPLoadouts.NetworkLoadouts)
    //        {
    //            NetworkServer.SendToAll(MessageTypes.MsgCustomLoadouts, kvp.Value);
    //        }
    //    }
    //}


    //[HarmonyPatch(typeof(Server), "RegisterHandlers")]
    //internal class MPLoadouts_Server_RegisterHandlers
    //{
    //    static void Postfix()
    //    {
    //        NetworkServer.RegisterHandler(MessageTypes.MsgCustomLoadouts, OnCustomLoadoutDataMessage);
    //    }

    //    private static void OnCustomLoadoutDataMessage(NetworkMessage rawMsg)
    //    {
    //        var msg = rawMsg.ReadMessage<MPLoadouts.LoadoutDataMessage>();
    //        if (!MPLoadouts.NetworkLoadouts.ContainsKey(msg.lobby_id))
    //        {
    //            MPLoadouts.NetworkLoadouts.Add(msg.lobby_id, msg);
    //        }
    //        else
    //        {
    //            MPLoadouts.NetworkLoadouts[msg.lobby_id] = msg;
    //        }
    //    }
    //}

    //[HarmonyPatch(typeof(Client), "RegisterHandlers")]
    //internal class MPLoadouts_Client_RegisterHandlers
    //{
    //    static void Postfix()
    //    {
    //        if (Client.GetClient() == null)
    //            return;

    //        Client.GetClient().RegisterHandler(MessageTypes.MsgCustomLoadouts, OnCustomLoadoutDataMessage);
    //    }

    //    private static void OnCustomLoadoutDataMessage(NetworkMessage rawMsg)
    //    {
    //        var msg = rawMsg.ReadMessage<MPLoadouts.LoadoutDataMessage>();
    //        if (!MPLoadouts.NetworkLoadouts.ContainsKey(msg.lobby_id))
    //        {
    //            MPLoadouts.NetworkLoadouts.Add(msg.lobby_id, msg);
    //        }
    //        else
    //        {
    //            MPLoadouts.NetworkLoadouts[msg.lobby_id] = msg;
    //        }
    //    }
    //}

    //[HarmonyPatch(typeof(Client), "OnRespawnMsg")]
    //internal class MPLoadouts_Client_OnRespawnMsg
    //{
    //    static void SetMultiplayerLoadout(int lobby_id)
    //    {
    //        Debug.Log($"SetMultiplayerLoadout for {lobby_id}");
    //    }

    //    static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> codes)
    //    {
    //        foreach (var code in codes)
    //        {
    //            if (code.opcode == OpCodes.Ldloc_2)
    //            {
    //                yield return new CodeInstruction(OpCodes.Ldloc_3); // int lobby_id
    //                yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(MPLoadouts_Client_OnRespawnMsg), "SetMultiplayerLoadout"));
    //            }
    //            yield return code;
    //        }
    //    }
    }
}
