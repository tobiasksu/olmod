using HarmonyLib;
using Overload;
using System.Reflection;
using UnityEngine;

namespace GameMod.Weapons
{
    public class Impulse : IWeapon
    {
        FieldInfo c_right_Field = AccessTools.Field(typeof(PlayerShip), "c_right");
        FieldInfo c_up_Field = AccessTools.Field(typeof(PlayerShip), "c_up");

        public void Fire(Player player, float refire_multiplier)
        {
            Vector3 c_right = (Vector3)c_right_Field.GetValue(player.c_player_ship);
            Vector3 c_up = (Vector3)c_up_Field.GetValue(player.c_player_ship);
            Vector3 vector = player.c_player_ship.c_forward;
            Vector3 vector2 = c_right;
            Vector3 vector3 = c_up;

            player.c_player_ship.FiringVolumeModifier = 0.75f;
            ProjPrefab type = ProjPrefab.proj_impulse;
            if (player.m_weapon_level[(int)player.m_weapon_type] == WeaponUnlock.LEVEL_2A || GameplayManager.IsMultiplayerActive)
            {
                Quaternion localRotation = player.c_player_ship.c_transform.localRotation;
                ProjectileManager.PlayerFire(player, type, player.c_player_ship.m_muzzle_right.position, localRotation, 0f, WeaponUnlock.LEVEL_2A, true, 0);
                ProjectileManager.PlayerFire(player, type, player.c_player_ship.m_muzzle_right.position + vector2 * 0.25f + vector3 * -0.15f + vector * -0.3f, localRotation, 0f, WeaponUnlock.LEVEL_2A, true, 1);
                ProjectileManager.PlayerFire(player, type, player.c_player_ship.m_muzzle_left.position, localRotation, 0f, WeaponUnlock.LEVEL_2A, true, 2);
                ProjectileManager.PlayerFire(player, type, player.c_player_ship.m_muzzle_left.position + vector2 * -0.25f + vector3 * -0.15f + vector * -0.3f, localRotation, 0f, WeaponUnlock.LEVEL_2A, false, 3);
                player.c_player_ship.m_refire_time += 0.28f * refire_multiplier;
                if (Server.IsActive())
                {
                    player.UseEnergy(0.666667f);
                }
                player.PlayCameraShake(CameraShakeType.FIRE_IMPULSE, 1.3f, 1.2f);
            }
            else
            {
                Quaternion localRotation = player.c_player_ship.c_transform.localRotation;
                ProjectileManager.PlayerFire(player, type, player.c_player_ship.m_muzzle_right.position, localRotation, 0f, player.m_weapon_level[(int)player.m_weapon_type], true, 0);
                ProjectileManager.PlayerFire(player, type, player.c_player_ship.m_muzzle_left.position, localRotation, 0f, player.m_weapon_level[(int)player.m_weapon_type], false, 2);
                player.c_player_ship.m_refire_time += ((player.m_weapon_level[(int)player.m_weapon_type] != WeaponUnlock.LEVEL_2B) ? 0.25f : 0.2f) * refire_multiplier;
                if (Server.IsActive())
                {
                    player.UseEnergy((player.m_weapon_level[(int)player.m_weapon_type] != WeaponUnlock.LEVEL_2B) ? 0.4f : 0.33333f);
                }
                player.PlayCameraShake(CameraShakeType.FIRE_IMPULSE, 1f, 1f);
            }
        }
    }
}
