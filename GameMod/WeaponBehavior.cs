using HarmonyLib;
using Overload;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace GameMod
{
    class WeaponBehavior
    {
        public static float GetRefireTime(ProjPrefab prefab, WeaponUnlock level, int flak_fire_count = 0, bool m_alternating_fire = false, float num = 1f, float num2 = 1f)
        {
            var component = ProjectileManager.proj_prefabs[(int)prefab].GetComponent<ProjectileExt>();
            //return level == WeaponUnlock.LEVEL_1 ? component.olmod_m_refire_LEVEL1 : (level == WeaponUnlock.LEVEL_2A ? component.olmod_m_refire_LEVEL2A : component.olmod_m_refire_LEVEL2B);
            switch (prefab)
            {
                case ProjPrefab.proj_impulse:
                    if (GameplayManager.IsMultiplayerActive)
                        return 0.28f * num;
                    switch (level)
                    {
                        case WeaponUnlock.LEVEL_2A:
                            return 0.28f * num;
                        case WeaponUnlock.LEVEL_2B:
                            return 0.2f * num;
                        default:
                            return 0.25f * num;
                    }
                case ProjPrefab.proj_vortex:
                    switch (level)
                    {
                        case WeaponUnlock.LEVEL_2B:
                            return 0.16f * num2 * num;
                        default:
                            return 0.2f * num2 * num;
                    }
                case ProjPrefab.proj_reflex:
                    return (level == WeaponUnlock.LEVEL_2A ? 0.08f : 0.1f) * num;
                case ProjPrefab.proj_shotgun:
                    switch (level)
                    {
                        case WeaponUnlock.LEVEL_2B:
                            return 0.2f;
                        case WeaponUnlock.LEVEL_0:
                            return 0.5f + (GameplayManager.IsMultiplayerActive ? 0.45f : 0f);
                        default:
                            return 0.3f + (GameplayManager.IsMultiplayerActive ? 0.45f : 0f);
                    }
                case ProjPrefab.proj_driller:
                    switch (level)
                    {
                        case WeaponUnlock.LEVEL_2B:
                            return 0.11f;
                        case WeaponUnlock.LEVEL_0:
                            return 0.26f;
                        default:
                            return 0.22f;
                    }
                case ProjPrefab.proj_flak_cannon:
                    switch (level)
                    {
                        case WeaponUnlock.LEVEL_2B:
                            return (flak_fire_count == 0 ? 0.15f : 0.08f + (float)Mathf.Max(0, 4 - flak_fire_count) * 0.01f);
                        default:
                            if (m_alternating_fire)
                            {
                                return (level < WeaponUnlock.LEVEL_1 ? 0.095f : 0.075f);
                            }
                            else
                            {
                                return 0.03f;
                            }
                    }
                case ProjPrefab.proj_thunderbolt:
                    return (GameplayManager.IsMultiplayerActive ? 0.5f * num : ((level != WeaponUnlock.LEVEL_2A) ? 0.45f : 0.5f) * num);
                case ProjPrefab.proj_beam:
                    switch (level)
                    {
                        case WeaponUnlock.LEVEL_2B:
                            return 0.133333f * num;
                        case WeaponUnlock.LEVEL_2A:
                            return 0.1f * num;
                        default:
                            return 0.2f * num;
                    }
                default:
                    return 0f;
            }
        }

        public static float GetFireAngle(ProjPrefab prefab, WeaponUnlock level)
        {
            var component = ProjectileManager.proj_prefabs[(int)prefab].GetComponent<ProjectileExt>();
            return level == WeaponUnlock.LEVEL_1 ? component.olmod_m_angle_LEVEL1 : (level == WeaponUnlock.LEVEL_2A ? component.olmod_m_angle_LEVEL2A : component.olmod_m_angle_LEVEL2B);
        }

        public static float GetEnergyUsage(ProjPrefab prefab, WeaponUnlock level, float m_thunder_power = 0f)
        {
            ProjectileExt component = ProjectileManager.proj_prefabs[(int)prefab].GetComponent<ProjectileExt>();
            //if (false)
            //{
            //    Debug.Log($"GetEnergyUsage for {prefab}: {component.olmod_m_energy_consumption}");
            //    return component.olmod_m_energy_consumption;
            //}
            //else
            //{
            switch (prefab)
            {
                case ProjPrefab.proj_impulse:
                    switch (level)
                    {
                        case WeaponUnlock.LEVEL_2A:
                            return 0.666667f;
                        case WeaponUnlock.LEVEL_2B:
                            return 0.33333f;
                        default:
                            return 0.4f;
                    }
                case ProjPrefab.proj_vortex:
                    switch (level)
                    {
                        case WeaponUnlock.LEVEL_0:
                            return 0.4f;
                        case WeaponUnlock.LEVEL_2A:
                            return 0.33333f;
                        default:
                            return 0.3f;
                    }
                case ProjPrefab.proj_reflex:
                    return 0.3f;
                case ProjPrefab.proj_thunderbolt:
                    return 2f + m_thunder_power * 3f;
                case ProjPrefab.proj_beam:
                    switch (level)
                    {
                        case WeaponUnlock.LEVEL_2B:
                            return 1f;
                        case WeaponUnlock.LEVEL_2A:
                            return 1.5f;
                        default:
                            return 2f;
                    }
                default:
                    return 0f;
            }
            //}
        }

        public static int GetAmmoUsage(ProjPrefab prefab, WeaponUnlock level)
        {
            switch (prefab)
            {
                case ProjPrefab.proj_shotgun:
                    switch (level)
                    {
                        case WeaponUnlock.LEVEL_2B:
                            return 3;
                        default:
                            return 6;
                    }
                case ProjPrefab.proj_driller:
                    return 2;
                case ProjPrefab.proj_flak_cannon:
                    return 1;
                default:
                    return 0;
            }
        }
    }

    class ProjectileExt : MonoBehaviour
    {
        public int olmod_m_ammo_consumption_LEVEL1 = -1;
        public int olmod_m_ammo_consumption_LEVEL2A = -1;
        public int olmod_m_ammo_consumption_LEVEL2B = -1;
        public float olmod_m_energy_consumption_LEVEL1 = -1f;
        public float olmod_m_energy_consumption_LEVEL2A = -1f;
        public float olmod_m_energy_consumption_LEVEL2B = -1f;
        public float olmod_m_refire_LEVEL1 = -1f;
        public float olmod_m_refire_LEVEL2A = -1f;
        public float olmod_m_refire_LEVEL2B = -1f;
        public float olmod_m_muzzle_left_adjust = 0f;
        public float olmod_m_muzzle_right_adjust = 0f;
        public float olmod_m_angle_LEVEL1 = 0f;
        public float olmod_m_angle_LEVEL2A = 0f;
        public float olmod_m_angle_LEVEL2B = 0f;
    }

    [HarmonyPatch(typeof(PlayerShip), "MaybeFireWeapon")]
    class PresetData_PlayerShip_MaybeFireWeapon
    {
        static bool Prefix(PlayerShip __instance, Vector3 ___c_right, Vector3 ___c_up, ref int ___flak_fire_count, ref float ___m_thunder_sound_timer)
        {
            if (!GameplayManager.IsMultiplayer)
                return true;

            MethodInfo _AngleRandomize = AccessTools.Method(typeof(PlayerShip), "AngleRandomize");
            MethodInfo _AngleSpreadX = AccessTools.Method(typeof(PlayerShip), "AngleSpreadX");
            MethodInfo _AngleSpreadY = AccessTools.Method(typeof(PlayerShip), "AngleSpreadY");
            MethodInfo _AngleSpreadZ = AccessTools.Method(typeof(PlayerShip), "AngleSpreadZ");

            if (__instance.m_refire_time <= 0f && !__instance.c_player.m_spectator)
            {
                bool flag = false;
                if (!__instance.c_player.CanFireWeaponAmmo())
                {
                    if (__instance.c_player.m_energy <= 0f)
                    {
                        if (__instance.c_player.m_ammo <= 0)
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
                    player.m_timer_invuln -= (float)NetworkMatch.m_respawn_shield_seconds;
                }
                __instance.m_alternating_fire = !__instance.m_alternating_fire;
                Vector3 a = __instance.c_forward;
                Vector3 a2 = ___c_right;
                Vector3 a3 = ___c_up;
                float num = (!flag) ? 1f : 3f;
                __instance.FiringVolumeModifier = 1f;
                __instance.FiringPitchModifier = 0f;
                WeaponUnlock wl = __instance.c_player.m_weapon_level[(int)__instance.c_player.m_weapon_type];
                Debug.Log($"Firing {__instance.c_player.m_weapon_level[(int)__instance.c_player.m_weapon_type]}");
                switch (__instance.c_player.m_weapon_type)
                {
                    case WeaponType.IMPULSE:
                        {
                            __instance.FiringVolumeModifier = 0.75f;
                            ProjPrefab type = ProjPrefab.proj_impulse;
                            if (wl == WeaponUnlock.LEVEL_2A || (GameplayManager.IsMultiplayerActive && MPModPrivateData.MatchMode != ExtMatchMode.RACE))
                            {
                                Quaternion rot = __instance.c_transform.localRotation;
                                MPSniperPackets.MaybePlayerFire(__instance.c_player, type, __instance.m_muzzle_right.position, rot, 0f, WeaponUnlock.LEVEL_2A, true, 0, -1);
                                MPSniperPackets.MaybePlayerFire(__instance.c_player, type, __instance.m_muzzle_right.position + a2 * 0.25f + a3 * -0.15f + a * -0.3f, rot, 0f, WeaponUnlock.LEVEL_2A, true, 1, -1);
                                MPSniperPackets.MaybePlayerFire(__instance.c_player, type, __instance.m_muzzle_left.position, rot, 0f, WeaponUnlock.LEVEL_2A, true, 2, -1);
                                MPSniperPackets.MaybePlayerFire(__instance.c_player, type, __instance.m_muzzle_left.position + a2 * -0.25f + a3 * -0.15f + a * -0.3f, rot, 0f, WeaponUnlock.LEVEL_2A, false, 3, -1);
                                if (MPSniperPackets.AlwaysUseEnergy())
                                {
                                    __instance.c_player.UseEnergy(WeaponBehavior.GetEnergyUsage(type, wl));
                                }
                                __instance.c_player.PlayCameraShake(CameraShakeType.FIRE_IMPULSE, 1.3f, 1.2f);
                            }
                            else
                            {
                                Quaternion rot = __instance.c_transform.localRotation;
                                MPSniperPackets.MaybePlayerFire(__instance.c_player, type, __instance.m_muzzle_right.position, rot, 0f, __instance.c_player.m_weapon_level[(int)__instance.c_player.m_weapon_type], true, 0, -1);
                                MPSniperPackets.MaybePlayerFire(__instance.c_player, type, __instance.m_muzzle_left.position, rot, 0f, __instance.c_player.m_weapon_level[(int)__instance.c_player.m_weapon_type], false, 2, -1);
                                if (MPSniperPackets.AlwaysUseEnergy())
                                {
                                    __instance.c_player.UseEnergy(WeaponBehavior.GetEnergyUsage(type, wl));
                                }
                                __instance.c_player.PlayCameraShake(CameraShakeType.FIRE_IMPULSE, 1f, 1f);
                            }
                            __instance.m_refire_time += WeaponBehavior.GetRefireTime(type, wl);
                            break;
                        }
                    case WeaponType.CYCLONE:
                        {
                            float num2 = 1f - Mathf.Min((float)___flak_fire_count * 0.05f, (__instance.c_player.m_weapon_level[(int)__instance.c_player.m_weapon_type] != WeaponUnlock.LEVEL_2B) ? 0.4f : 0.25f);
                            __instance.FiringPitchModifier = ((__instance.c_player.m_weapon_level[(int)__instance.c_player.m_weapon_type] != WeaponUnlock.LEVEL_2B) ? (0.6f - num2) : (0.75f - num2)) * 0.25f;
                            __instance.FiringVolumeModifier = 0.75f;
                            ProjPrefab type = ProjPrefab.proj_vortex;
                            ProjectileExt projext = ProjectileManager.proj_prefabs[(int)type].GetComponent<ProjectileExt>();
                            __instance.c_player.PlayCameraShake(CameraShakeType.FIRE_CYCLONE, 1f, 1f);
                            float fire_angle = __instance.m_fire_angle;
                            Quaternion localRotation = __instance.c_transform.localRotation;
                            //float angle = (__instance.c_player.m_weapon_level[(int)__instance.c_player.m_weapon_type] != WeaponUnlock.LEVEL_2B) ? 2.25f : 1.5f;
                            float angle = WeaponBehavior.GetFireAngle(type, __instance.c_player.m_weapon_level[(int)__instance.c_player.m_weapon_type]);
                            Quaternion rot;
                            if (__instance.c_player.m_weapon_level[(int)__instance.c_player.m_weapon_type] == WeaponUnlock.LEVEL_2A)
                            {
                                rot = (Quaternion)_AngleRandomize.Invoke(__instance, new object[] { localRotation, 0.5f });
                                MPSniperPackets.MaybePlayerFire(__instance.c_player, type, __instance.m_muzzle_center.position, rot, 0f, __instance.c_player.m_weapon_level[(int)__instance.c_player.m_weapon_type], true, -1, -1);
                            }
                            if (__instance.IsCockpitVisible)
                            {
                                ParticleManager.psm[2].StartParticle(8, __instance.m_muzzle_center.position, localRotation, __instance.c_transform, null, false);
                            }
                            Vector3 point = ___c_right * 0.1f;
                            Vector3 b = Quaternion.AngleAxis(fire_angle, __instance.c_forward) * point;
                            rot = (Quaternion)_AngleSpreadX.Invoke(__instance, new object[] { localRotation, angle });
                            rot = (Quaternion)_AngleSpreadZ.Invoke(__instance, new object[] { rot, fire_angle });
                            rot = (Quaternion)_AngleRandomize.Invoke(__instance, new object[] { rot, 0.25f });
                            MPSniperPackets.MaybePlayerFire(__instance.c_player, type, __instance.m_muzzle_center.position + b, rot, 0f, __instance.c_player.m_weapon_level[(int)__instance.c_player.m_weapon_type], true, -1, -1);
                            b = Quaternion.AngleAxis(fire_angle + 120f, __instance.c_forward) * point;
                            rot = (Quaternion)_AngleSpreadX.Invoke(__instance, new object[] { localRotation, angle });
                            rot = (Quaternion)_AngleSpreadZ.Invoke(__instance, new object[] { rot, fire_angle + 120f });
                            rot = (Quaternion)_AngleRandomize.Invoke(__instance, new object[] { rot, 0.25f });
                            MPSniperPackets.MaybePlayerFire(__instance.c_player, type, __instance.m_muzzle_center.position + b, rot, 0f, __instance.c_player.m_weapon_level[(int)__instance.c_player.m_weapon_type], true, -1, -1);
                            b = Quaternion.AngleAxis(fire_angle + 240f, __instance.c_forward) * point;
                            rot = (Quaternion)_AngleSpreadX.Invoke(__instance, new object[] { localRotation, angle });
                            rot = (Quaternion)_AngleSpreadZ.Invoke(__instance, new object[] { rot, fire_angle + 240f });
                            rot = (Quaternion)_AngleRandomize.Invoke(__instance, new object[] { rot, 0.25f });
                            MPSniperPackets.MaybePlayerFire(__instance.c_player, type, __instance.m_muzzle_center.position + b, rot, 0f, __instance.c_player.m_weapon_level[(int)__instance.c_player.m_weapon_type], false, -1, -1);
                            if (MPSniperPackets.AlwaysUseEnergy())
                            {
                                __instance.c_player.UseEnergy(WeaponBehavior.GetEnergyUsage(type, wl));
                            }
                            if (__instance.c_player.m_weapon_level[(int)__instance.c_player.m_weapon_type] == WeaponUnlock.LEVEL_2B)
                            {
                                __instance.m_fire_angle = (__instance.m_fire_angle + 350f) % 360f;
                            }
                            else
                            {
                                __instance.m_fire_angle = (__instance.m_fire_angle + 345f) % 360f;
                            }
                            __instance.m_refire_time += WeaponBehavior.GetRefireTime(type, wl, ___flak_fire_count, __instance.m_alternating_fire, num, num2);
                            ___flak_fire_count++;
                            break;
                        }
                    case WeaponType.REFLEX:
                        {
                            __instance.FiringVolumeModifier = 0.75f;
                            ProjPrefab type = ProjPrefab.proj_reflex;
                            if (__instance.m_alternating_fire)
                            {
                                MPSniperPackets.MaybePlayerFire(__instance.c_player, type, __instance.m_muzzle_right.position, __instance.c_transform.localRotation, 0f, __instance.c_player.m_weapon_level[(int)__instance.c_player.m_weapon_type], false, 0, -1);
                            }
                            else
                            {
                                MPSniperPackets.MaybePlayerFire(__instance.c_player, type, __instance.m_muzzle_left.position, __instance.c_transform.localRotation, 0f, __instance.c_player.m_weapon_level[(int)__instance.c_player.m_weapon_type], false, 1, -1);
                            }
                            __instance.m_refire_time += WeaponBehavior.GetRefireTime(type, wl);
                            if (MPSniperPackets.AlwaysUseEnergy())
                            {
                                __instance.c_player.UseEnergy(WeaponBehavior.GetEnergyUsage(type, wl));
                            }
                            __instance.c_player.PlayCameraShake(CameraShakeType.FIRE_REFLEX, 1f, 1f);
                            break;
                        }
                    case WeaponType.CRUSHER:
                        {
                            __instance.FiringVolumeModifier = 0.75f;
                            ProjPrefab type = ProjPrefab.proj_shotgun;
                            Vector3 position = __instance.m_muzzle_left.position;
                            Vector3 position2 = __instance.m_muzzle_right.position;
                            float num4 = 0.15f;
                            Quaternion localRotation = __instance.c_transform.localRotation;
                            if (__instance.c_player.m_weapon_level[(int)__instance.c_player.m_weapon_type] == WeaponUnlock.LEVEL_2B)
                            {
                                float angle = 0.5f;
                                for (int i = 0; i < 7; i++)
                                {
                                    if (__instance.m_alternating_fire)
                                    {
                                        Vector2 vector;
                                        vector.x = UnityEngine.Random.Range(-num4, num4);
                                        vector.y = UnityEngine.Random.Range(-num4, num4);
                                        Vector3 pos;
                                        pos.x = position.x + vector.x * a2.x + vector.y * a3.x;
                                        pos.y = position.y + vector.x * a2.y + vector.y * a3.y;
                                        pos.z = position.z + vector.x * a2.z + vector.y * a3.z;
                                        Quaternion rot = (Quaternion)_AngleSpreadY.Invoke(__instance, new object[] { localRotation, 2f * vector.x / num4 });
                                        rot = (Quaternion)_AngleSpreadX.Invoke(__instance, new object[] { rot, 2f * vector.y / num4 });
                                        rot = (Quaternion)_AngleRandomize.Invoke(__instance, new object[] { rot, angle });
                                        MPSniperPackets.MaybePlayerFire(__instance.c_player, type, pos, rot, 0f, __instance.c_player.m_weapon_level[(int)__instance.c_player.m_weapon_type], i < 6, 0, -1);
                                    }
                                    else
                                    {
                                        Vector2 vector;
                                        vector.x = UnityEngine.Random.Range(-num4, num4);
                                        vector.y = UnityEngine.Random.Range(-num4, num4);
                                        Vector3 pos;
                                        pos.x = position2.x + vector.x * a2.x + vector.y * a3.x;
                                        pos.y = position2.y + vector.x * a2.y + vector.y * a3.y;
                                        pos.z = position2.z + vector.x * a2.z + vector.y * a3.z;
                                        Quaternion rot = (Quaternion)_AngleSpreadY.Invoke(__instance, new object[] { localRotation, 2f * vector.x / num4 });
                                        rot = (Quaternion)_AngleSpreadX.Invoke(__instance, new object[] { rot, 2f * vector.y / num4 });
                                        rot = (Quaternion)_AngleRandomize.Invoke(__instance, new object[] { rot, angle });
                                        MPSniperPackets.MaybePlayerFire(__instance.c_player, type, pos, rot, 0f, __instance.c_player.m_weapon_level[(int)__instance.c_player.m_weapon_type], i < 6, 1, -1);
                                    }
                                }
                                if (__instance.IsCockpitVisible)
                                {
                                    if (__instance.m_alternating_fire)
                                    {
                                        ParticleManager.psm[2].StartParticle(6, position2, localRotation, __instance.c_transform, null, false);
                                    }
                                    else
                                    {
                                        ParticleManager.psm[2].StartParticle(6, position, localRotation, __instance.c_transform, null, false);
                                    }
                                }
                                __instance.c_player.UseAmmo(WeaponBehavior.GetAmmoUsage(type, wl));
                                if (!GameplayManager.IsMultiplayer)
                                {
                                    __instance.c_rigidbody.AddForce(a * (UnityEngine.Random.Range(-100f, -150f) * __instance.c_rigidbody.mass * RUtility.FIXED_FT_INVERTED));
                                    __instance.c_rigidbody.AddTorque(___c_right * (UnityEngine.Random.Range(-300f, -200f) * RUtility.FIXED_FT_INVERTED));
                                }
                                __instance.c_player.PlayCameraShake(CameraShakeType.FIRE_CRUSHER, 1f, 0.8f);
                            }
                            else
                            {
                                float angle = (__instance.c_player.m_weapon_level[(int)__instance.c_player.m_weapon_type] != WeaponUnlock.LEVEL_2A) ? 0.75f : 0.5f;
                                for (int j = 0; j < 7; j++)
                                {
                                    Vector2 vector;
                                    vector.x = UnityEngine.Random.Range(-num4, num4);
                                    vector.y = UnityEngine.Random.Range(-num4, num4);
                                    Vector3 pos;
                                    pos.x = position2.x + vector.x * a2.x + vector.y * a3.x;
                                    pos.y = position2.y + vector.x * a2.y + vector.y * a3.y;
                                    pos.z = position2.z + vector.x * a2.z + vector.y * a3.z;
                                    Quaternion rot = (Quaternion)_AngleSpreadY.Invoke(__instance, new object[] { localRotation, 2.25f * vector.x / num4 });
                                    rot = (Quaternion)_AngleSpreadX.Invoke(__instance, new object[] { rot, 2.25f * vector.y / num4 });
                                    rot = (Quaternion)_AngleRandomize.Invoke(__instance, new object[] { rot, angle });
                                    MPSniperPackets.MaybePlayerFire(__instance.c_player, type, pos, rot, 0f, __instance.c_player.m_weapon_level[(int)__instance.c_player.m_weapon_type], true, 0, -1);
                                    vector.x = UnityEngine.Random.Range(-num4, num4);
                                    vector.y = UnityEngine.Random.Range(-num4, num4);
                                    pos.x = position.x + vector.x * a2.x + vector.y * a3.x;
                                    pos.y = position.y + vector.x * a2.y + vector.y * a3.y;
                                    pos.z = position.z + vector.x * a2.z + vector.y * a3.z;
                                    rot = (Quaternion)_AngleSpreadY.Invoke(__instance, new object[] { localRotation, 2.25f * vector.x / num4 });
                                    rot = (Quaternion)_AngleSpreadX.Invoke(__instance, new object[] { rot, 2.25f * vector.y / num4 });
                                    rot = (Quaternion)_AngleRandomize.Invoke(__instance, new object[] { rot, angle });
                                    MPSniperPackets.MaybePlayerFire(__instance.c_player, type, pos, rot, 0f, __instance.c_player.m_weapon_level[(int)__instance.c_player.m_weapon_type], j < 6, 1, -1);
                                }
                                if (__instance.IsCockpitVisible)
                                {
                                    ParticleManager.psm[2].StartParticle(6, position2, localRotation, __instance.c_transform, null, false);
                                    ParticleManager.psm[2].StartParticle(6, position, localRotation, __instance.c_transform, null, false);
                                }
                                if (__instance.c_player.m_overdrive)
                                {
                                    if (GameplayManager.IsMultiplayerActive)
                                    {
                                        __instance.m_refire_time += 0.55f;
                                    }
                                    else
                                    {
                                        __instance.m_refire_time += ((__instance.c_player.m_weapon_level[(int)__instance.c_player.m_weapon_type] < WeaponUnlock.LEVEL_1) ? 0.5f : 0.35f);
                                    }
                                }
                                __instance.c_player.UseAmmo(WeaponBehavior.GetAmmoUsage(type, wl));
                                if (!GameplayManager.IsMultiplayer)
                                {
                                    __instance.c_rigidbody.AddForce(a * (UnityEngine.Random.Range(-150f, -200f) * __instance.c_rigidbody.mass * RUtility.FIXED_FT_INVERTED));
                                    __instance.c_rigidbody.AddTorque(___c_right * (UnityEngine.Random.Range(-500f, -400f) * RUtility.FIXED_FT_INVERTED));
                                }
                                __instance.c_player.PlayCameraShake(CameraShakeType.FIRE_CRUSHER, 2f, 1f);
                            }
                            __instance.m_refire_time += WeaponBehavior.GetRefireTime(type, wl);
                            break;
                        }
                    case WeaponType.DRILLER:
                        __instance.FiringVolumeModifier = 0.75f;
                        if (__instance.c_player.m_weapon_level[(int)__instance.c_player.m_weapon_type] == WeaponUnlock.LEVEL_2B)
                        {
                            ProjPrefab type = ProjPrefab.proj_driller_mini;
                            Quaternion rot = (Quaternion)_AngleRandomize.Invoke(__instance, new object[] { __instance.c_transform.localRotation, 0.6f });
                            MPSniperPackets.MaybePlayerFire(__instance.c_player, type, __instance.m_muzzle_center.position, rot, 0f, __instance.c_player.m_weapon_level[(int)__instance.c_player.m_weapon_type], false, 0, -1);
                            __instance.c_player.PlayCameraShake(CameraShakeType.FIRE_DRILLER, 0.7f, 0.7f);
                        }
                        else
                        {
                            ProjPrefab type = ProjPrefab.proj_driller;
                            Quaternion rot;
                            if (GameplayManager.IsMultiplayerActive)
                            {
                                rot = __instance.c_transform.localRotation;
                            }
                            else
                            {
                                rot = (Quaternion)_AngleRandomize.Invoke(__instance, new object[] { __instance.c_transform.localRotation, 0.1f });
                            }
                            MPSniperPackets.MaybePlayerFire(__instance.c_player, type, __instance.m_muzzle_center.position, rot, 0f, __instance.c_player.m_weapon_level[(int)__instance.c_player.m_weapon_type], false, 0, -1);
                            __instance.c_player.PlayCameraShake(CameraShakeType.FIRE_DRILLER, 1f, 1f);
                        }
                        ProjPrefab pt = wl == WeaponUnlock.LEVEL_2B ? ProjPrefab.proj_driller_mini : ProjPrefab.proj_driller;
                        __instance.m_refire_time += WeaponBehavior.GetRefireTime(pt, wl);
                        __instance.c_player.UseAmmo(WeaponBehavior.GetAmmoUsage(pt, wl));
                        break;
                    case WeaponType.FLAK:
                        {
                            __instance.FiringVolumeModifier = 0.75f;
                            ProjPrefab type = ProjPrefab.proj_flak_cannon;
                            if (__instance.c_player.m_weapon_level[(int)__instance.c_player.m_weapon_type] == WeaponUnlock.LEVEL_2B)
                            {
                                if (___flak_fire_count == 0)
                                {
                                    GameManager.m_audio.PlayCue2D(337, 0.7f, 0.5f, 0f, true);
                                    GameManager.m_audio.PlayCue2D(338, 0.7f, 0.5f, 0f, true);
                                    ___flak_fire_count++;
                                    __instance.m_refire_time = 0.15f;
                                }
                                else
                                {
                                    float angle = 5f + (float)Mathf.Min(7, ___flak_fire_count) * 0.2f;
                                    Quaternion rot = (Quaternion)_AngleRandomize.Invoke(__instance, new object[] { __instance.c_transform_rotation, angle });
                                    MPSniperPackets.MaybePlayerFire(__instance.c_player, type, __instance.m_muzzle_right.position, rot, 0f, __instance.c_player.m_weapon_level[(int)__instance.c_player.m_weapon_type], true, 0, -1);
                                    rot = (Quaternion)_AngleRandomize.Invoke(__instance, new object[] { __instance.c_transform_rotation, angle });
                                    MPSniperPackets.MaybePlayerFire(__instance.c_player, type, __instance.m_muzzle_left.position, rot, 0f, __instance.c_player.m_weapon_level[(int)__instance.c_player.m_weapon_type], false, 1, -1);
                                    __instance.m_refire_time += WeaponBehavior.GetRefireTime(type, wl, ___flak_fire_count, __instance.m_alternating_fire);
                                    if (!GameplayManager.IsMultiplayer)
                                    {
                                        __instance.c_rigidbody.AddForce(UnityEngine.Random.onUnitSphere * (UnityEngine.Random.Range(20f, 30f) * __instance.c_rigidbody.mass * RUtility.FIXED_FT_INVERTED));
                                        __instance.c_rigidbody.AddTorque(UnityEngine.Random.onUnitSphere * (UnityEngine.Random.Range(2f, 3f) * __instance.c_rigidbody.mass * RUtility.FIXED_FT_INVERTED));
                                    }
                                    __instance.c_player.PlayCameraShake(CameraShakeType.FIRE_FLAK, 0.9f, 1.1f);
                                    ___flak_fire_count++;
                                    __instance.c_player.UseAmmo(WeaponBehavior.GetAmmoUsage(type, wl));
                                }
                            }
                            else
                            {
                                float angle2 = (!GameplayManager.IsMultiplayerActive) ? 8f : 6f;
                                if (__instance.m_alternating_fire)
                                {
                                    Quaternion rot = (Quaternion)_AngleRandomize.Invoke(__instance, new object[] { __instance.c_transform.localRotation, angle2 });
                                    MPSniperPackets.MaybePlayerFire(__instance.c_player, type, __instance.m_muzzle_right.position, rot, 0f, __instance.c_player.m_weapon_level[(int)__instance.c_player.m_weapon_type], true, 0, -1);
                                    __instance.m_refire_time += WeaponBehavior.GetRefireTime(type, wl, ___flak_fire_count, __instance.m_alternating_fire);
                                }
                                else
                                {
                                    Quaternion rot = (Quaternion)_AngleRandomize.Invoke(__instance, new object[] { __instance.c_transform.localRotation, angle2 });
                                    MPSniperPackets.MaybePlayerFire(__instance.c_player, type, __instance.m_muzzle_left.position, rot, 0f, __instance.c_player.m_weapon_level[(int)__instance.c_player.m_weapon_type], false, 1, -1);
                                    __instance.m_refire_time += WeaponBehavior.GetRefireTime(type, wl, ___flak_fire_count, __instance.m_alternating_fire);
                                    if (!GameplayManager.IsMultiplayer)
                                    {
                                        __instance.c_rigidbody.AddForce(UnityEngine.Random.onUnitSphere * (UnityEngine.Random.Range(10f, 20f) * __instance.c_rigidbody.mass * RUtility.FIXED_FT_INVERTED));
                                        __instance.c_rigidbody.AddTorque(UnityEngine.Random.onUnitSphere * (UnityEngine.Random.Range(1f, 2f) * __instance.c_rigidbody.mass * RUtility.FIXED_FT_INVERTED));
                                    }
                                    __instance.c_player.PlayCameraShake(CameraShakeType.FIRE_FLAK, 0.6f, 1f);
                                    __instance.c_player.UseAmmo(WeaponBehavior.GetAmmoUsage(type, wl));
                                }
                            }
                            break;
                        }
                    case WeaponType.THUNDERBOLT:
                        {
                            ProjPrefab type = ProjPrefab.proj_thunderbolt;
                            var projext = ProjectileManager.proj_prefabs[(int)type].GetComponent<ProjectileExt>();
                            Quaternion rot = __instance.c_transform.localRotation;
                            __instance.m_thunder_power = Mathf.Min((wl != WeaponUnlock.LEVEL_2A) ? 1f : 1.15f, __instance.m_thunder_power);
                            MPSniperPackets.MaybePlayerFire(__instance.c_player, type, __instance.m_muzzle_right.position + a2 * projext.olmod_m_muzzle_right_adjust, rot, __instance.m_thunder_power, __instance.c_player.m_weapon_level[(int)__instance.c_player.m_weapon_type], true, 0, -1);
                            MPSniperPackets.MaybePlayerFire(__instance.c_player, type, __instance.m_muzzle_left.position + a2 * projext.olmod_m_muzzle_left_adjust, rot, __instance.m_thunder_power, __instance.c_player.m_weapon_level[(int)__instance.c_player.m_weapon_type], false, 1, -1);
                            __instance.m_refire_time += WeaponBehavior.GetRefireTime(type, wl, ___flak_fire_count, __instance.m_alternating_fire, num);
                            if (!GameplayManager.IsMultiplayer)
                            {
                                __instance.c_rigidbody.AddForce(a * (UnityEngine.Random.Range(-300f, -350f) * (0.5f + __instance.m_thunder_power * 1.2f) * __instance.c_rigidbody.mass * RUtility.FIXED_FT_INVERTED));
                                Vector3 a4 = RUtility.RandomUnitVector();
                                __instance.c_rigidbody.AddTorque((a4 + UnityEngine.Random.onUnitSphere * 0.2f) * (UnityEngine.Random.Range(1000f, 1500f) * (0.5f + __instance.m_thunder_power * 1.2f) * RUtility.FIXED_FT_INVERTED));
                            }
                            __instance.c_player.PlayCameraShake(CameraShakeType.FIRE_THUNDER, 1f + __instance.m_thunder_power * 2f, 1f + __instance.m_thunder_power);
                            if (MPSniperPackets.AlwaysUseEnergy())
                            {
                                __instance.c_player.UseEnergy(WeaponBehavior.GetEnergyUsage(type, wl, __instance.m_thunder_power));
                            }
                            __instance.m_thunder_power = 0f;
                            ___m_thunder_sound_timer = 0f;
                            break;
                        }
                    case WeaponType.LANCER:
                        {
                            __instance.FiringVolumeModifier = 0.75f;
                            ProjPrefab type = ProjPrefab.proj_beam;
                            Quaternion rot = (Quaternion)_AngleRandomize.Invoke(__instance, new object[] { __instance.c_transform.localRotation, (__instance.c_player.m_weapon_level[(int)__instance.c_player.m_weapon_type] != WeaponUnlock.LEVEL_2B) ? 0f : 0.2f });
                            if (__instance.c_player.m_weapon_level[(int)__instance.c_player.m_weapon_type] == WeaponUnlock.LEVEL_2B)
                            {
                                if (__instance.m_alternating_fire)
                                {
                                    MPSniperPackets.MaybePlayerFire(__instance.c_player, type, __instance.m_muzzle_right.position, rot, 0f, __instance.c_player.m_weapon_level[(int)__instance.c_player.m_weapon_type], false, 0, -1);
                                }
                                else
                                {
                                    MPSniperPackets.MaybePlayerFire(__instance.c_player, type, __instance.m_muzzle_left.position, rot, 0f, __instance.c_player.m_weapon_level[(int)__instance.c_player.m_weapon_type], false, 1, -1);
                                }
                                __instance.m_alternating_fire = !__instance.m_alternating_fire;
                                __instance.m_refire_time += WeaponBehavior.GetRefireTime(type, wl, ___flak_fire_count, __instance.m_alternating_fire, num);
                                if (MPSniperPackets.AlwaysUseEnergy())
                                {
                                    __instance.c_player.UseEnergy(WeaponBehavior.GetEnergyUsage(type, wl));
                                }
                                __instance.c_player.PlayCameraShake(CameraShakeType.FIRE_LANCER, 1f, 1f);
                            }
                            else
                            {
                                MPSniperPackets.MaybePlayerFire(__instance.c_player, type, __instance.m_muzzle_right.position, rot, 0f, __instance.c_player.m_weapon_level[(int)__instance.c_player.m_weapon_type], true, 0, -1);
                                MPSniperPackets.MaybePlayerFire(__instance.c_player, type, __instance.m_muzzle_left.position, rot, 0f, __instance.c_player.m_weapon_level[(int)__instance.c_player.m_weapon_type], false, 1, -1);
                                if (GameplayManager.IsMultiplayerActive)
                                {
                                    if (__instance.c_player.m_overdrive)
                                    {
                                        __instance.m_refire_time += 0.29f;
                                    }
                                    else
                                    {
                                        __instance.m_refire_time += 0.23f * num;
                                    }
                                }
                                else if (__instance.c_player.m_overdrive)
                                {
                                    __instance.m_refire_time += ((__instance.c_player.m_weapon_level[(int)__instance.c_player.m_weapon_type] != WeaponUnlock.LEVEL_2A) ? 0.28f : 0.2f);
                                }
                                else
                                {
                                    __instance.m_refire_time += WeaponBehavior.GetRefireTime(type, wl, ___flak_fire_count, __instance.m_alternating_fire, num);
                                }
                                if (MPSniperPackets.AlwaysUseEnergy())
                                {
                                    __instance.c_player.UseEnergy(WeaponBehavior.GetEnergyUsage(type, wl));
                                }
                                __instance.c_player.PlayCameraShake(CameraShakeType.FIRE_LANCER, 1.3f, 1.5f);
                            }
                            break;
                        }
                }
                if (__instance.m_refire_time < 0.01f)
                {
                    __instance.m_refire_time = 0.01f;
                }
            }

            return false;
        }
    }

    [HarmonyPatch(typeof(PlayerShip), "MaybeFireMissile")]
    class WeaponBehavior_PlayerShip_MaybeFireMissile
    {
        static bool Prefix(PlayerShip __instance, Player ___c_player, float ___m_refire_missile_time, Vector3 ___c_right, Vector3 ___c_up)
        {
            if (!GameplayManager.IsMultiplayer)
                return true;

            if (__instance.c_player.m_spectator)
                return false;

            MethodInfo _AngleRandomize = AccessTools.Method(typeof(PlayerShip), "AngleRandomize");
            MethodInfo _AngleSpreadX = AccessTools.Method(typeof(PlayerShip), "AngleSpreadX");

            if (__instance.m_refire_missile_time <= 0f)
            {
                if (!__instance.c_player.CanFireMissileAmmo(MissileType.NUM))
                {
                    __instance.c_player.m_old_missile_type = __instance.c_player.m_missile_type;
                    if (__instance.c_player.m_missile_type_prev != MissileType.NUM)
                    {
                        __instance.c_player.Networkm_missile_type = __instance.c_player.m_missile_type_prev;
                        if (__instance.c_player.m_missile_ammo[(int)__instance.c_player.m_missile_type] <= 0)
                        {
                            __instance.c_player.SwitchToNextMissileWithAmmo(false);
                        }
                        else
                        {
                            __instance.MissileSelectFX();
                        }
                        __instance.c_player.UpdateCurrentMissileName();
                    }
                    else
                    {
                        __instance.c_player.SwitchToNextMissileWithAmmo(false);
                    }
                    __instance.c_player.FindBestPrevMissile(false);
                    __instance.m_refire_missile_time = 0.5f;
                    return false;
                }
                if (GameplayManager.IsMultiplayerActive && __instance.c_player.m_spawn_invul_active)
                {
                    Player player = __instance.c_player;
                    player.m_timer_invuln -= (float)NetworkMatch.m_respawn_shield_seconds;
                }
                Vector3 direction = __instance.c_forward;
                Quaternion localRotation = __instance.c_transform.localRotation;
                Vector2 zero = Vector2.zero;
                if (!GameplayManager.IsMultiplayer && MenuManager.opt_use_tobii_secondaryaim && UIManager.GetEyeTrackingActivePos(ref zero, false))
                {
                    direction = __instance.c_camera.ScreenPointToRay(Tobii.Gaming.TobiiAPI.GetGazePoint().Screen).direction;
                    localRotation.SetLookRotation(direction, ___c_up);
                }
                if (!Player.CheatUnlimited)
                {
                    CodeStage.AntiCheat.ObscuredTypes.ObscuredInt[] missile_ammo = __instance.c_player.m_missile_ammo;
                    MissileType missile_type = __instance.c_player.m_missile_type;
                    missile_ammo[(int)missile_type] = missile_ammo[(int)missile_type] - 1;
                }
                __instance.FiringVolumeModifier = 1f;
                WeaponUnlock wl = __instance.c_player.m_missile_level[(int)__instance.c_player.m_missile_type];
                switch (__instance.c_player.m_missile_type)
                {
                    case MissileType.FALCON:
                        {
                            ProjPrefab type = ProjPrefab.missile_falcon;
                            Quaternion rot = localRotation;
                            if (__instance.m_alternating_missile_fire)
                            {
                                MPSniperPackets.MaybePlayerFire(__instance.c_player, type, __instance.m_muzzle_right2.position, rot, 0f, __instance.c_player.m_missile_level[(int)__instance.c_player.m_missile_type], false, -1, -1);
                            }
                            else
                            {
                                MPSniperPackets.MaybePlayerFire(__instance.c_player, type, __instance.m_muzzle_left2.position, rot, 0f, __instance.c_player.m_missile_level[(int)__instance.c_player.m_missile_type], false, -1, -1);
                            }
                            __instance.m_alternating_missile_fire = !__instance.m_alternating_missile_fire;
                            __instance.m_refire_missile_time += WeaponBehavior.GetRefireTime(type, wl);
                            __instance.c_player.PlayCameraShake(CameraShakeType.FIRE_FALCON, 1f, 1f);
                            break;
                        }
                    case MissileType.MISSILE_POD:
                        {
                            ProjPrefab type = ProjPrefab.missile_pod;
                            Quaternion rot;
                            if (GameplayManager.IsMultiplayerActive)
                            {
                                rot = localRotation;
                            }
                            else
                            {
                                float angle = (__instance.c_player.m_missile_level[(int)__instance.c_player.m_missile_type] != WeaponUnlock.LEVEL_0) ? 3.5f : 2.5f;
                                rot = (Quaternion)_AngleRandomize.Invoke(__instance, new object[] { localRotation, angle });
                            }
                            if (__instance.m_alternating_missile_fire)
                            {
                                MPSniperPackets.MaybePlayerFire(__instance.c_player, type, __instance.m_muzzle_right2.position, rot, 0f, __instance.c_player.m_missile_level[(int)__instance.c_player.m_missile_type], false, -1, -1);
                            }
                            else
                            {
                                MPSniperPackets.MaybePlayerFire(__instance.c_player, type, __instance.m_muzzle_left2.position, rot, 0f, __instance.c_player.m_missile_level[(int)__instance.c_player.m_missile_type], false, -1, -1);
                            }
                            __instance.c_player.PlayCameraShake(CameraShakeType.FIRE_MISSILE_POD, 1f, 1f);
                            __instance.m_refire_missile_time += WeaponBehavior.GetRefireTime(type, wl);
                            break;
                        }
                    case MissileType.HUNTER:
                        {
                            ProjPrefab type = ProjPrefab.missile_hunter;
                            Quaternion rot;
                            if (__instance.c_player.m_missile_level[(int)__instance.c_player.m_missile_type] == WeaponUnlock.LEVEL_2A)
                            {
                                rot = localRotation;
                                ProjectileManager.PlayerFire(__instance.c_player, type, __instance.m_muzzle_center2.position, rot, 0f, __instance.c_player.m_missile_level[(int)__instance.c_player.m_missile_type], true, -1, -1);
                            }
                            rot = (Quaternion)_AngleSpreadX.Invoke(__instance, new object[] { localRotation, 0.5f });
                            MPSniperPackets.MaybePlayerFire(__instance.c_player, type, __instance.m_muzzle_right2.position, rot, 0f, __instance.c_player.m_missile_level[(int)__instance.c_player.m_missile_type], true, -1, -1);
                            rot = (Quaternion)_AngleSpreadX.Invoke(__instance, new object[] { localRotation, -0.5f });
                            MPSniperPackets.MaybePlayerFire(__instance.c_player, type, __instance.m_muzzle_left2.position, rot, 0f, __instance.c_player.m_missile_level[(int)__instance.c_player.m_missile_type], false, -1, -1);
                            __instance.m_refire_missile_time += WeaponBehavior.GetRefireTime(type, wl);
                            __instance.c_player.PlayCameraShake(CameraShakeType.FIRE_HUNTER, 1f, 1f);
                            break;
                        }
                    case MissileType.CREEPER:
                        {
                            ProjPrefab type = ProjPrefab.missile_creeper;
                            Quaternion rot;
                            if (GameplayManager.IsMultiplayerActive)
                            {
                                float angle2 = (!__instance.m_alternating_missile_fire) ? 2f : -2f;
                                rot = (Quaternion)_AngleSpreadX.Invoke(__instance, new object[] { localRotation, angle2 });
                            }
                            else
                            {
                                float angle3 = (!__instance.m_alternating_missile_fire) ? UnityEngine.Random.Range(0.5f, 2.5f) : UnityEngine.Random.Range(-2.5f, -0.5f);
                                rot = (Quaternion)_AngleRandomize.Invoke(__instance, new object[] { localRotation, 6f });
                                rot = (Quaternion)_AngleSpreadX.Invoke(__instance, new object[] { rot, angle3 });
                            }
                            if (__instance.m_alternating_missile_fire)
                            {
                                MPSniperPackets.MaybePlayerFire(__instance.c_player, type, __instance.m_muzzle_right2.position, rot, 0f, __instance.c_player.m_missile_level[(int)__instance.c_player.m_missile_type], false, -1, -1);
                            }
                            else
                            {
                                MPSniperPackets.MaybePlayerFire(__instance.c_player, type, __instance.m_muzzle_left2.position, rot, 0f, __instance.c_player.m_missile_level[(int)__instance.c_player.m_missile_type], false, -1, -1);
                            }
                            __instance.m_alternating_missile_fire = !__instance.m_alternating_missile_fire;
                            __instance.m_refire_missile_time += WeaponBehavior.GetRefireTime(type, wl);
                            __instance.c_player.PlayCameraShake(CameraShakeType.FIRE_CREEPER, 1f, 1f);
                            break;
                        }
                    case MissileType.NOVA:
                        {
                            ProjPrefab type = ProjPrefab.missile_smart;
                            Quaternion rot = localRotation;
                            MPSniperPackets.MaybePlayerFire(__instance.c_player, type, __instance.m_muzzle_center2.position, rot, 0f, __instance.c_player.m_missile_level[(int)__instance.c_player.m_missile_type], false, -1, -1);
                            __instance.m_refire_missile_time += WeaponBehavior.GetRefireTime(type, wl);
                            __instance.c_player.PlayCameraShake(CameraShakeType.FIRE_NOVA, 1f, 1f);
                            break;
                        }
                    case MissileType.DEVASTATOR:
                        {
                            ProjPrefab type = ProjPrefab.missile_devastator;
                            MPSniperPackets.MaybePlayerFire(__instance.c_player, type, __instance.m_muzzle_center2.position, localRotation, 0f, __instance.c_player.m_missile_level[(int)__instance.c_player.m_missile_type], false, -1, -1);
                            __instance.m_refire_missile_time += WeaponBehavior.GetRefireTime(type, wl);
                            __instance.c_player.PlayCameraShake(CameraShakeType.FIRE_DEVASTATOR, 1f, 1f);
                            break;
                        }
                    case MissileType.TIMEBOMB:
                        {
                            ProjPrefab type = ProjPrefab.missile_timebomb;
                            Quaternion rot = (Quaternion)_AngleRandomize.Invoke(__instance, new object[] { localRotation, 0.1f });
                            MPSniperPackets.MaybePlayerFire(__instance.c_player, type, __instance.m_muzzle_center2.position, rot, 0f, __instance.c_player.m_missile_level[(int)__instance.c_player.m_missile_type], false, -1, -1);
                            __instance.m_refire_missile_time += WeaponBehavior.GetRefireTime(type, wl);
                            if (!GameplayManager.IsMultiplayer)
                            {
                                __instance.c_rigidbody.AddForce(direction * (UnityEngine.Random.Range(-200f, -250f) * __instance.c_rigidbody.mass * RUtility.FIXED_FT_INVERTED));
                                __instance.c_rigidbody.AddTorque(___c_right * (UnityEngine.Random.Range(-1500f, -1000f) * RUtility.FIXED_FT_INVERTED));
                            }
                            __instance.c_player.PlayCameraShake(CameraShakeType.FIRE_TIMEBOMB, 1f, 1f);
                            break;
                        }
                    case MissileType.VORTEX:
                        {
                            ProjPrefab type = ProjPrefab.missile_vortex;
                            __instance.m_refire_missile_time += WeaponBehavior.GetRefireTime(type, wl);
                            Quaternion rot = localRotation;
                            MPSniperPackets.MaybePlayerFire(__instance.c_player, type, __instance.m_muzzle_center2.position, rot, 0f, __instance.c_player.m_missile_level[(int)__instance.c_player.m_missile_type], false, -1, -1);
                            __instance.c_player.PlayCameraShake(CameraShakeType.FIRE_VORTEX, 1f, 1f);
                            break;
                        }
                }
                if (__instance.m_refire_missile_time < 1f || __instance.c_player.m_missile_type == MissileType.TIMEBOMB)
                {
                    __instance.c_player.MaybeSwitchToNextMissile();
                }
                if (__instance.m_refire_missile_time < 0.01f)
                {
                    __instance.m_refire_missile_time = 0.01f;
                }

                if (MPSniperPackets.enabled)
                    return false;

                if (!GameplayManager.IsMultiplayerActive ||
                    !Server.IsActive() ||
                    !(___m_refire_missile_time == 1f &&
                    ___c_player.m_old_missile_type != MissileType.NUM &&
                    ___c_player.m_missile_ammo[(int)___c_player.m_old_missile_type] == 0)) // just switched?
                    return false;

                // make sure ammo is also zero on the client
                ___c_player.CallRpcSetMissileAmmo((int)___c_player.m_old_missile_type, 0);

                // workaround for not updating missle name in hud
                ___c_player.CallRpcSetMissileType(___c_player.m_missile_type);
                ___c_player.CallTargetUpdateCurrentMissileName(___c_player.connectionToClient);
            }

            return false;
        }
    }
}
