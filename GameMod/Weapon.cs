using HarmonyLib;
using Overload;
using Rewired;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Player = Overload.Player;

namespace GameMod
{
    internal class Weapon
    {
    }

    static class WeaponDefinitions
    {
        public static Weapons.IWeapon[] weapons =
        {
            new Weapons.Impulse(),
            new Weapons.Cyclone(),
            new Weapons.Reflex(),
            new Weapons.Crusher(),
            new Weapons.Driller(),
            new Weapons.Flak(),
            new Weapons.Thunderbolt(),
            new Weapons.Lancer()
        };
    }

    [HarmonyPatch(typeof(PlayerShip), "MaybeFireWeapon")]
    internal class Weapon_PlayerShip_MaybeFireWeapon
    {
        static bool Prefix(PlayerShip __instance)
        {
            if (!(__instance.m_refire_time <= 0f) || __instance.c_player.m_spectator)
            {
                return false;
            }
            bool flag = false;
            if (!__instance.c_player.CanFireWeaponAmmo())
            {
                if ((float)__instance.c_player.m_energy <= 0f)
                {
                    if ((int)__instance.c_player.m_ammo <= 0)
                    {
                        if (__instance.c_player.WeaponUsesAmmo(__instance.c_player.m_weapon_type))
                        {
                            __instance.c_player.SwitchToEnergyWeapon();
                        }
                        flag = true;
                    }
                    else if (!__instance.c_player.SwitchToAmmoWeapon())
                    {
                        flag = true;
                    }
                }
                else
                {
                    __instance.c_player.SwitchToEnergyWeapon();
                }
                if (!flag)
                {
                    __instance.m_refire_time = 0.5f;
                    return false;
                }
            }
            if (GameplayManager.IsMultiplayerActive && __instance.c_player.m_spawn_invul_active)
            {
                Player player = __instance.c_player;
                player.m_timer_invuln = (float)player.m_timer_invuln - (float)NetworkMatch.m_respawn_shield_seconds;
            }
            __instance.m_alternating_fire = !__instance.m_alternating_fire;
            float refire_multiplier = ((!flag) ? 1f : 3f);
            __instance.FiringVolumeModifier = 1f;
            __instance.FiringPitchModifier = 0f;

            WeaponDefinitions.weapons[(int)__instance.c_player.m_weapon_type].Fire(__instance.c_player, refire_multiplier);

            if (__instance.m_refire_time < 0.01f)
            {
                __instance.m_refire_time = 0.01f;
            }

            return false;
        }
    }
}
