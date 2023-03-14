using HarmonyLib;
using Overload;
using System.Reflection;
using UnityEngine;

namespace GameMod.Weapons
{
    public class Lancer : IWeapon
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
            ProjPrefab type = ProjPrefab.proj_beam;
            Quaternion localRotation = AngleRandomize(player.c_player_ship.c_transform.localRotation, (player.m_weapon_level[(int)player.m_weapon_type] != WeaponUnlock.LEVEL_2B) ? 0f : 0.2f, c_up, c_right);
            if (player.m_weapon_level[(int)player.m_weapon_type] == WeaponUnlock.LEVEL_2B)
            {
                if (player.c_player_ship.m_alternating_fire)
                {
                    ProjectileManager.PlayerFire(player, type, player.c_player_ship.m_muzzle_right.position, localRotation, 0f, player.m_weapon_level[(int)player.m_weapon_type], false, 0);
                }
                else
                {
                    ProjectileManager.PlayerFire(player, type, player.c_player_ship.m_muzzle_left.position, localRotation, 0f, player.m_weapon_level[(int)player.m_weapon_type], false, 1);
                }
                player.c_player_ship.m_alternating_fire = !player.c_player_ship.m_alternating_fire;
                player.c_player_ship.m_refire_time += 0.133333f * refire_multiplier;
                if (Server.IsActive())
                {
                    player.UseEnergy(1f);
                }
                player.PlayCameraShake(CameraShakeType.FIRE_LANCER, 1f, 1f);
                return;
            }
            ProjectileManager.PlayerFire(player, type, player.c_player_ship.m_muzzle_right.position, localRotation, 0f, player.m_weapon_level[(int)player.m_weapon_type], true, 0);
            ProjectileManager.PlayerFire(player, type, player.c_player_ship.m_muzzle_left.position, localRotation, 0f, player.m_weapon_level[(int)player.m_weapon_type], false, 1);
            if (GameplayManager.IsMultiplayerActive)
            {
                if (player.m_overdrive)
                {
                    player.c_player_ship.m_refire_time += 0.29f;
                }
                else
                {
                    player.c_player_ship.m_refire_time += 0.23f * refire_multiplier;
                }
            }
            else if (player.m_overdrive)
            {
                player.c_player_ship.m_refire_time += ((player.m_weapon_level[(int)player.m_weapon_type] != WeaponUnlock.LEVEL_2A) ? 0.28f : 0.2f);
            }
            else
            {
                player.c_player_ship.m_refire_time += ((player.m_weapon_level[(int)player.m_weapon_type] != WeaponUnlock.LEVEL_2A) ? 0.2f : 0.1f) * refire_multiplier;
            }
            if (Server.IsActive())
            {
                if (GameplayManager.IsMultiplayerActive)
                {
                    player.UseEnergy(1f);
                }
                else
                {
                    player.UseEnergy((player.m_weapon_level[(int)player.m_weapon_type] != WeaponUnlock.LEVEL_2A) ? 2f : 1.5f);
                }
            }
            player.PlayCameraShake(CameraShakeType.FIRE_LANCER, 1.3f, 1.5f);
        }
    }
}
