using HarmonyLib;
using Overload;
using System.Reflection;
using UnityEngine;
using UnityEngine.Networking;

namespace GameMod
{
    /// <summary>
    /// Does a better job of initializing playership state at spawn, resetting the flak/cyclone fire counter, the thunderbolt power level, and clearing the boost overheat.
    /// </summary>
    [HarmonyPatch(typeof(Player), "RestorePlayerShipDataAfterRespawn")]
	class MPSpawnInitialization_Player_RestorePlayerShipDataAfterRespawn
	{
		private static FieldInfo _PlayerShip_flak_fire_count_Field = typeof(PlayerShip).GetField("flak_fire_count", BindingFlags.NonPublic | BindingFlags.Instance);

		static void Prefix(Player __instance)
		{
			_PlayerShip_flak_fire_count_Field.SetValue(__instance.c_player_ship, 0);
			__instance.c_player_ship.m_thunder_power = 0;
			__instance.c_player_ship.m_boost_heat = 0;
			__instance.c_player_ship.m_boost_overheat_timer = 0f;
		}
	}

    class MPSpawnInitialization
    {
        public static WeaponUnlock GetMpDefaultWeaponUnlock(WeaponType wt)
        {
            switch (wt)
            {
                case WeaponType.IMPULSE:
                    return MPClassic.matchEnabled ? WeaponUnlock.LEVEL_1 : WeaponUnlock.LEVEL_2A;
                default:
                    return WeaponUnlock.LEVEL_1;
            }
        }

        public static WeaponUnlock GetMpDefaultMissileUnlock(MissileType mt)
        {
            return WeaponUnlock.LEVEL_1;
        }
    }


    /// <summary>
    /// In vanilla Overload, player's upgrade level of impulse is LEVEL_1 while it is overridden to shoot LEVEL_2A projectiles.
    /// Correct this so proper upgrade level is shown on both client/server.
    /// </summary>
    [HarmonyPatch(typeof(NetworkSpawnPlayer), "SetMultiplayerLoadout")]
    class MPSpawnInitialization_NetworkSpawnPlayer_SetMultiplayerLoadout
    {
        static bool Prefix(Player player, LoadoutDataMessage loadout_data, bool use_loadout1)
        {
            for (int i = 0; i < 8; i++)
            {
                player.m_weapon_level[i] = WeaponUnlock.LOCKED;
            }
            for (int j = 0; j < 8; j++)
            {
                player.m_missile_level[j] = WeaponUnlock.LOCKED;
                player.m_missile_ammo[j] = 0;
            }
            for (int k = 0; k < 10; k++)
            {
                player.m_upgrade_level[k] = 0;
            }
            player.m_ammo = 0;
            player.m_weapon_level[0] = WeaponUnlock.LOCKED;
            player.m_missile_level[0] = WeaponUnlock.LOCKED;
            player.m_upgrade_level[3] = 0;
            int mp_mod = loadout_data.m_mp_modifier1;
            if (NetworkMatch.m_force_modifier1 != 4)
            {
                mp_mod = NetworkMatch.m_force_modifier1;
            }
            player.m_mp_mod1 = mp_mod;
            int num = NetworkMatch.m_turn_speed_limit;
            switch (mp_mod)
            {
                case 0:
                    num++;
                    break;
                case 1:
                    player.m_unlock_boost_speed = true;
                    player.m_unlock_boost_heatsink = true;
                    break;
                case 2:
                    player.m_upgrade_level[3] = 1;
                    break;
                case 3:
                    player.m_upgrade_level[0] = 3;
                    break;
            }
            int mp_mod2 = loadout_data.m_mp_modifier2;
            if (NetworkMatch.m_force_modifier2 != 4)
            {
                mp_mod2 = NetworkMatch.m_force_modifier2;
            }
            player.m_mp_mod2 = mp_mod2;
            switch (mp_mod2)
            {
                case 0:
                    num++;
                    break;
                case 1:
                    player.m_unlock_blast_damage = true;
                    break;
                case 2:
                    player.m_upgrade_level[2] = 1;
                    player.m_ammo += 100;
                    player.m_upgrade_level[1] = 3;
                    break;
                case 3:
                    player.m_unlock_fast_forward = true;
                    break;
            }
            player.c_player_ship.m_turn_speed_mp = num;
            int num2 = 0;
            if (NetworkMatch.m_force_loadout == 1)
            {
                player.m_weapon_level[(int)NetworkMatch.m_force_w1] = MPSpawnInitialization.GetMpDefaultWeaponUnlock(NetworkMatch.m_force_w1);
                if (Player.WeaponUsesAmmo2(NetworkMatch.m_force_w1))
                {
                    num2++;
                }
                if (NetworkMatch.m_force_w2 != WeaponType.NUM)
                {
                    player.m_weapon_level[(int)NetworkMatch.m_force_w2] = MPSpawnInitialization.GetMpDefaultWeaponUnlock(NetworkMatch.m_force_w2);
                    if (Player.WeaponUsesAmmo2(NetworkMatch.m_force_w2))
                    {
                        num2++;
                    }
                }
                if (NetworkMatch.m_force_m1 != MissileType.NUM)
                {
                    player.m_missile_level[(int)NetworkMatch.m_force_m1] = MPSpawnInitialization.GetMpDefaultMissileUnlock(NetworkMatch.m_force_m1);
                    player.m_missile_ammo[(int)NetworkMatch.m_force_m1] = Player.MP_DEFAULT_MISSILE_AMMO[(int)NetworkMatch.m_force_m1];
                }
                if (NetworkMatch.m_force_m2 != MissileType.NUM)
                {
                    player.m_missile_level[(int)NetworkMatch.m_force_m2] = MPSpawnInitialization.GetMpDefaultMissileUnlock(NetworkMatch.m_force_m2);
                    player.m_missile_ammo[(int)NetworkMatch.m_force_m2] = Player.MP_DEFAULT_MISSILE_AMMO[(int)NetworkMatch.m_force_m2];
                }
                player.Networkm_weapon_type = NetworkMatch.m_force_w1;
                player.Networkm_missile_type = ((NetworkMatch.m_force_m1 != MissileType.NUM) ? NetworkMatch.m_force_m1 : NetworkMatch.m_force_m2);
            }
            else
            {
                int idx = (!use_loadout1) ? loadout_data.m_mp_loadout2 : loadout_data.m_mp_loadout1;
                player.m_weapon_level[(int)loadout_data.GetMpLoadoutWeapon1(idx)] = MPSpawnInitialization.GetMpDefaultWeaponUnlock(loadout_data.GetMpLoadoutWeapon1(idx));
                if (loadout_data.GetMpLoadoutWeapon2(idx) != WeaponType.NUM)
                {
                    player.m_weapon_level[(int)loadout_data.GetMpLoadoutWeapon2(idx)] = MPSpawnInitialization.GetMpDefaultWeaponUnlock(loadout_data.GetMpLoadoutWeapon2(idx));
                }
                if (Player.WeaponUsesAmmo2(loadout_data.GetMpLoadoutWeapon1(idx)))
                {
                    num2++;
                }
                if (loadout_data.GetMpLoadoutWeapon2(idx) != WeaponType.NUM && Player.WeaponUsesAmmo2(loadout_data.GetMpLoadoutWeapon2(idx)))
                {
                    num2++;
                }
                player.m_missile_level[(int)loadout_data.GetMpLoadoutMissile1(idx)] = MPSpawnInitialization.GetMpDefaultMissileUnlock(loadout_data.GetMpLoadoutMissile1(idx));
                player.m_missile_ammo[(int)loadout_data.GetMpLoadoutMissile1(idx)] = Player.MP_DEFAULT_MISSILE_AMMO[(int)loadout_data.GetMpLoadoutMissile1(idx)];
                if (loadout_data.GetMpLoadoutMissile2(idx) != MissileType.NUM)
                {
                    player.m_missile_level[(int)loadout_data.GetMpLoadoutMissile2(idx)] = MPSpawnInitialization.GetMpDefaultMissileUnlock(loadout_data.GetMpLoadoutMissile2(idx));
                    player.m_missile_ammo[(int)loadout_data.GetMpLoadoutMissile2(idx)] = Player.MP_DEFAULT_MISSILE_AMMO[(int)loadout_data.GetMpLoadoutMissile2(idx)];
                }
                player.Networkm_weapon_type = loadout_data.GetMpLoadoutWeapon1(idx);
                player.Networkm_missile_type = loadout_data.GetMpLoadoutMissile1(idx);
            }
            if (player.isLocalPlayer)
            {
                player.CallCmdSetCurrentWeapon(player.m_weapon_type);
                player.c_player_ship.SwitchVisibleWeapon(false, player.m_weapon_type);
            }
            player.m_ammo += ((num2 <= 1) ? ((num2 <= 0) ? 0 : 200) : 300);
            player.UpdateCurrentWeaponName();
            if (player.isLocalPlayer)
            {
                player.CallCmdSetCurrentMissile(player.m_missile_type);
            }
            player.UpdateCurrentMissileName();

            return false;
        }
    }

    [HarmonyPatch(typeof(Player), "UnlockWeapon")]
    class MPSpawnInitialization_Player_UnlockWeapon
    {
        static bool Prefix(Player __instance, WeaponType wt, bool silent, bool picked_up, ref bool __result)
        {
            if (!GameplayManager.IsMultiplayerActive)
                return true;

            if (!NetworkServer.active)
            {
                Debug.LogWarning("[Server] function 'System.Boolean Overload.Player::UnlockWeapon(Overload.WeaponType,System.Boolean,System.Boolean)' called on client");
                __result = false;
            }
            if (__instance.m_weapon_level[(int)wt] == WeaponUnlock.LOCKED)
            {
                __instance.m_weapon_picked_up[(int)wt] = picked_up;
                if (__instance.WeaponUsesAmmo(wt))
                {
                    __instance.AddAmmo(200, true, false, true);
                }
                else
                {
                    __instance.AddEnergy(10f, true, false);
                }
                __instance.m_weapon_level[(int)wt] = MPSpawnInitialization.GetMpDefaultWeaponUnlock(wt);
                __instance.CallRpcUnlockWeaponClient(wt, silent);
                __result = true;
            }
            if (__instance.WeaponUsesAmmo(wt))
            {
                __result = __instance.AddAmmo(100, false, false, true);
            }
            __result = __instance.AddEnergy(20f, false, true);

            return false;
        }
    }

 //   [HarmonyPatch(typeof(ProjectileManager), "PlayerFire")]
	//class MPSpawnInitialization_ProjectileManager_PlayerFire
 //   {
	//	static void Prefix(Player player, ProjPrefab type, WeaponUnlock upgrade_lvl)
 //       {
	//		Debug.Log($"PlayerFire: {player.m_mp_name}, {type}, {upgrade_lvl}");
 //       }
 //   }
}
