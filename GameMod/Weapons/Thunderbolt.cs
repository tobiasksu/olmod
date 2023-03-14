using HarmonyLib;
using Overload;
using System.Reflection;
using UnityEngine;


namespace GameMod.Weapons
{
    public class Thunderbolt : IWeapon
    {
        FieldInfo m_thunder_sound_timer_Field = AccessTools.Field(typeof(PlayerShip), "m_thunder_sound_timer");

        public void Fire(Player player, float refire_multiplier)
        {
            ProjPrefab type = ProjPrefab.proj_thunderbolt;
            Quaternion localRotation = player.c_player_ship.c_transform.localRotation;
            player.c_player_ship.m_thunder_power = Mathf.Min((player.m_weapon_level[(int)player.m_weapon_type] != WeaponUnlock.LEVEL_2A) ? 1f : 1.15f, player.c_player_ship.m_thunder_power);
            ProjectileManager.PlayerFire(player, type, player.c_player_ship.m_muzzle_right.position, localRotation, player.c_player_ship.m_thunder_power, player.m_weapon_level[(int)player.m_weapon_type], true, 0);
            ProjectileManager.PlayerFire(player, type, player.c_player_ship.m_muzzle_left.position, localRotation, player.c_player_ship.m_thunder_power, player.m_weapon_level[(int)player.m_weapon_type], false, 1);
            if (GameplayManager.IsMultiplayerActive)
            {
                player.c_player_ship.m_refire_time += 0.5f * refire_multiplier;
            }
            else
            {
                player.c_player_ship.m_refire_time += ((player.m_weapon_level[(int)player.m_weapon_type] != WeaponUnlock.LEVEL_2A) ? 0.45f : 0.5f) * refire_multiplier;
            }
            if (!GameplayManager.IsMultiplayer)
            {
                player.c_player_ship.c_rigidbody.AddForce(player.c_player_ship.c_forward * (UnityEngine.Random.Range(-300f, -350f) * (0.5f + player.c_player_ship.m_thunder_power * 1.2f) * player.c_player_ship.c_rigidbody.mass * RUtility.FIXED_FT_INVERTED));
                Vector3 vector4 = RUtility.RandomUnitVector();
                player.c_player_ship.c_rigidbody.AddTorque((vector4 + UnityEngine.Random.onUnitSphere * 0.2f) * (UnityEngine.Random.Range(1000f, 1500f) * (0.5f + player.c_player_ship.m_thunder_power * 1.2f) * RUtility.FIXED_FT_INVERTED));
            }
            player.PlayCameraShake(CameraShakeType.FIRE_THUNDER, 1f + player.c_player_ship.m_thunder_power * 2f, 1f + player.c_player_ship.m_thunder_power);
            if (Server.IsActive())
            {
                player.UseEnergy(2f + player.c_player_ship.m_thunder_power * 3f);
            }
            player.c_player_ship.m_thunder_power = 0f;
            m_thunder_sound_timer_Field.SetValue(player.c_player_ship, 0f);
        }
    }
}
