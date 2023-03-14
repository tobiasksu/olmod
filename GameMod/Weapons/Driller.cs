using HarmonyLib;
using Overload;
using System.Reflection;
using UnityEngine;

namespace GameMod.Weapons
{
    public class Driller : IWeapon
    {
        FieldInfo c_right_Field = AccessTools.Field(typeof(PlayerShip), "c_right");
        FieldInfo c_up_Field = AccessTools.Field(typeof(PlayerShip), "c_up");

        private Quaternion AngleRandomize(Quaternion rot, float angle, Vector3 c_up, Vector3 c_right)
        {
            Vector2 insideUnitCircle = UnityEngine.Random.insideUnitCircle;
            return AngleSpreadY(AngleSpreadX(rot, angle * insideUnitCircle.x, c_up), angle * insideUnitCircle.y, c_right);
        }

        private Quaternion AngleSpreadX(Quaternion rot, float angle, Vector3 c_up)
        {
            Quaternion quaternion = Quaternion.AngleAxis(angle, c_up);
            return quaternion * rot;
        }

        private Quaternion AngleSpreadY(Quaternion rot, float angle, Vector3 c_right)
        {
            Quaternion quaternion = Quaternion.AngleAxis(angle, c_right);
            return quaternion * rot;
        }

        public void Fire(Player player, float refire_multiplier)
        {
            Vector3 c_right = (Vector3)c_right_Field.GetValue(player.c_player_ship);
            Vector3 c_up = (Vector3)c_up_Field.GetValue(player.c_player_ship);

            player.c_player_ship.FiringVolumeModifier = 0.75f;
            if (player.m_weapon_level[(int)player.m_weapon_type] == WeaponUnlock.LEVEL_2B)
            {
                ProjPrefab type = ProjPrefab.proj_driller_mini;
                Quaternion localRotation = AngleRandomize(player.c_player_ship.c_transform.localRotation, 0.6f, c_up, c_right);
                ProjectileManager.PlayerFire(player, type, player.c_player_ship.m_muzzle_center.position, localRotation, 0f, player.m_weapon_level[(int)player.m_weapon_type], false, 0);
                player.c_player_ship.m_refire_time += 0.11f;
                player.PlayCameraShake(CameraShakeType.FIRE_DRILLER, 0.7f, 0.7f);
                player.UseAmmo(1);
            }
            else
            {
                ProjPrefab type = ProjPrefab.proj_driller;
                ProjectileManager.PlayerFire(rot: (!GameplayManager.IsMultiplayerActive)
                    ? AngleRandomize(player.c_player_ship.c_transform.localRotation, 0.1f, c_up, c_right)
                    : player.c_player_ship.c_transform.localRotation, player: player, type: type, pos: player.c_player_ship.m_muzzle_center.position, strength: 0f, upgrade_lvl: player.m_weapon_level[(int)player.m_weapon_type], no_sound: false, slot: 0);
                player.c_player_ship.m_refire_time += ((player.m_weapon_level[(int)player.m_weapon_type] < WeaponUnlock.LEVEL_1) ? 0.26f : 0.22f);
                player.PlayCameraShake(CameraShakeType.FIRE_DRILLER, 1f, 1f);
                player.UseAmmo(2);
            }
        }
    }
}
