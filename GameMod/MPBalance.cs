using HarmonyLib;
using Overload;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using UnityEngine;

namespace GameMod
{
    public class MPBalance
    {

        public static bool ShowCycloneTrails = true;
        public static float CycloneTrailOpacity = 1f;
        public static readonly MenuState msBalanceOptions = (MenuState)101;
        public static readonly UIElementType uiBalanceOptions = (UIElementType)93;
        public static bool UseProjdataCrusherTrail = true;
        public static int ThunderboltSelfDamageSoundEffect = (int)SoundEffect.imp_force_field1;

        public static float GetThunderboltChargeTimeMultiplierFloat()
        {
            if (GameplayManager.IsMultiplayer)
            {
                ProjectileExt component = ProjectileManager.proj_prefabs[27].GetComponent<ProjectileExt>();
                return component.olmod_m_tb_chargetime_multiplier;
            }
            else
            {
                return 1f;
            }
        }

        public static float GetThunderboltSelfDamageMultiplierFloat()
        {
            if (GameplayManager.IsMultiplayer)
            {
                ProjectileExt component = ProjectileManager.proj_prefabs[27].GetComponent<ProjectileExt>();
                return component.olmod_m_tb_overchargedamage_multiplier;
            }
            else
            {
                return 1f;
            }
        }
    }


    [HarmonyPatch(typeof(UIElement), "DrawOptionsMenu")]
    class UIElement_DrawOptionsMenu
    {

        static void DrawModOption(UIElement uie, ref Vector2 position)
        {
            uie.SelectAndDrawItem(Loc.LS("BALANCE MOD OPTIONS"), position, 10, false, 1f, 0.75f);
            position.y += 62f;
        }

        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> codes)
        {
            foreach (var code in codes)
            {
                if (code.opcode == OpCodes.Ldstr && (string)code.operand == "LANGUAGE")
                {
                    yield return new CodeInstruction(OpCodes.Ldloca, 0);
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(UIElement_DrawOptionsMenu), "DrawModOption"));
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                }

                yield return code;
            }
        }
    }

    [HarmonyPatch(typeof(MenuManager), "OptionsUpdate")]
    class MenuManager_OptionsUpdate
    {
        static void Postfix()
        {
            if (MenuManager.m_menu_sub_state == MenuSubState.ACTIVE && (UIManager.PushedSelect(100) || (MenuManager.option_dir && UIManager.PushedDir())))
            {
                if (UIManager.m_menu_selection == 10)
                {
                    MenuManager.ChangeMenuState(MPBalance.msBalanceOptions, false);
                    UIManager.DestroyAll(false);
                    MenuManager.PlaySelectSound(1f);
                }
            }
        }
    }

    [HarmonyPatch(typeof(UIElement), "Draw")]
    class UIElement_Draw
    {
        static void Postfix()
        {
            if (MenuManager.m_menu_state == MPBalance.msBalanceOptions)
                DrawModOptionsWindow();
        }

        static void DrawModOptionsWindow()
        {
            UIManager.ui_bg_dark = true;
            UIElement uie = UIManager.m_ui_element[(int)MPBalance.uiBalanceOptions];
            uie.m_alpha = 1f;
            Vector2 position = uie.m_position;
            position.y = UIManager.UI_TOP + 64f;
            uie.DrawMenuBG();
            int menu_micro_state = MenuManager.m_menu_micro_state;

            switch (menu_micro_state)
            {
                default:
                    uie.DrawHeaderMedium(Vector2.up * (UIManager.UI_TOP + 30f), Loc.LS("MOD OPTIONS - SEPT 26 2021 06:04PM BUILD"), 265f);
                    position.y += 20f;
                    uie.DrawMenuSeparator(position);
                    position.y += 64f;
                    uie.SelectAndDrawStringOptionItem("CYCLONE TRAILS", position, 1, MPBalance.ShowCycloneTrails ? "ON" : "OFF", "DISPLAY CYCLONE TRAILS IN-GAME", 1.5f);
                    position.y += 64f;
                    uie.SelectAndDrawSliderItem("CYCLONE TRAIL OPACITY", position, 2, MPBalance.CycloneTrailOpacity);
                    position.y += 64f;
                    uie.SelectAndDrawCheckboxItem("USE SERVER CRUSHER TRAIL SETTING", position, 3, MPBalance.UseProjdataCrusherTrail);
                    position.y += 64f;
                    uie.SelectAndDrawStringOptionItem("TB SELF-DAMAGE SOUND", position, 4, ((SoundEffect)MPBalance.ThunderboltSelfDamageSoundEffect).ToString(), "", 1.5f);
                    position.y += 64f;

                    position.y = UIManager.UI_BOTTOM - 100f;
                    uie.DrawMenuSeparator(position);
                    position.y += 64f;
                    uie.SelectAndDrawItem(Loc.LS("RETURN TO OPTIONS"), position, 100, fade: false);
                    break;
            }
        }
    }


    [HarmonyPatch(typeof(MenuManager), "Update")]
    class MenuManager_Update
    {
        static void Postfix()
        {
            if (MenuManager.m_menu_state == MPBalance.msBalanceOptions)
            {
                ModOptionsUpdate();
            }
        }

        public static void ModOptionsUpdate()
        {
            MenuManager.UpdateMPStatus();
            UIManager.MouseSelectUpdate();
            MenuSubState menu_sub_state = MenuManager.m_menu_sub_state;
            if (menu_sub_state != MenuSubState.INIT)
            {
                if (menu_sub_state == MenuSubState.ACTIVE)
                {
                    UIManager.ControllerMenu();
                    if (UIManager.PushedSelect(100) || (MenuManager.option_dir && UIManager.PushedDir()))
                    {
                        MenuManager.MaybeReverseOption();
                        int menu_selection = UIManager.m_menu_selection;
                        switch (menu_selection)
                        {
                            case 1:
                                MPBalance.ShowCycloneTrails = !MPBalance.ShowCycloneTrails;
                                MenuManager.PlayCycleSound(1f, (float)UIManager.m_select_dir);
                                break;
                            case 2:
                                MPBalance.CycloneTrailOpacity = Math.Max(0.01f, UIElement.SliderPos);
                                if (Input.GetMouseButtonDown(0))
                                {
                                    MenuManager.PlayCycleSound(1f, UIElement.SliderPos * 5f - 3f);
                                }
                                break;
                            case 3:
                                MPBalance.UseProjdataCrusherTrail = !MPBalance.UseProjdataCrusherTrail;
                                MenuManager.PlayCycleSound(1f, (float)UIManager.m_select_dir);
                                break;
                            case 4:
                                MPBalance.ThunderboltSelfDamageSoundEffect = (MPBalance.ThunderboltSelfDamageSoundEffect + 486 + UIManager.m_select_dir) % 486;
                                MenuManager.PlayCycleSound(1f, (float)UIManager.m_select_dir);
                                break;
                            default:
                                if (menu_selection == 100)
                                {
                                    AccessTools.Method(typeof(MenuManager), "GoBack").Invoke(null, new object[] { });
                                    UIManager.DestroyAll(false);
                                    MenuManager.PlaySelectSound(1f);
                                }
                                break;
                        }
                        MenuManager.UnReverseOption();
                    }
                }
            }
            else if ((float)AccessTools.Field(typeof(MenuManager), "m_menu_state_timer").GetValue(null) > 0.25f)
            {
                UIManager.CreateUIElement(UIManager.SCREEN_CENTER, 7000, MPBalance.uiBalanceOptions);
                MenuManager.m_menu_sub_state = MenuSubState.ACTIVE;
                MenuManager.SetDefaultSelection(0);
            }
        }
    }

    [HarmonyPatch(typeof(Projectile), "InitData")]
    class MPBalance_Projectile_InitData
    {
        static void Postfix(Projectile __instance)
        {
            if (!GameplayManager.IsMultiplayerActive)
                return;

            if (__instance.m_type == ProjPrefab.proj_vortex)
            {
                if (!MPBalance.ShowCycloneTrails)
                    __instance.m_trail_particle = FXWeaponEffect.none;
            }
        }
    }

    [HarmonyPatch(typeof(ParticleElement), "Play")]
    class MPBalance_ParticleElement_Play
    {
        static void Postfix(ParticleElement __instance, GameObject ___c_go, ParticleSystem[] ___c_ps)
        {
            if (!GameplayManager.IsMultiplayerActive)
                return;

            for (int i = 0; i < ___c_ps.Length; i++)
            {
                if (___c_ps[i].name == "trail_cyclone(Clone)")
                {
                    ___c_ps[i].startColor = new Color(___c_ps[i].startColor.r, ___c_ps[i].startColor.g, ___c_ps[i].startColor.b, MPBalance.CycloneTrailOpacity);
                    foreach (var x in ___c_ps[i].GetComponentsInChildren<ParticleSystemRenderer>())
                    {
                        if (x.sharedMaterials != null)
                        {
                            //Debug.Log($"# shared mats: {x.sharedMaterials.Length}");
                            foreach (var mat in x.sharedMaterials)
                            {
                                if (mat != null)
                                {
                                    //Debug.Log($"Mat: {mat.name}");
                                    //Debug.Log($"Color: {mat.color}");
                                    //if (mat.shader != null)
                                    //    Debug.Log($"Shader: {mat.shader.name}");

                                    if (mat.name == "_glow_superbright1_yellow")
                                    {
                                        mat.SetFloat("_OpacityScale", MPBalance.CycloneTrailOpacity);
                                        mat.SetFloat("_alpha_power", MPBalance.CycloneTrailOpacity);
                                        foreach (var s in new string[] { "_BaseColor", "_Color", "_CoreColor", "_EmissionColor", "_TintColor", "_glowColor", "_hiColor" })
                                        {
                                            Color c = mat.GetColor(s);
                                            c.a = MPBalance.CycloneTrailOpacity;
                                            mat.SetColor(s, c);
                                        }
                                    }

                                    if (mat.name == "_ring_soft1")
                                    {
                                        foreach (var s in new string[] { "_Color", "_CoreColor", "_TintColor" })
                                        {
                                            Color c = mat.GetColor(s);
                                            c.a = MPBalance.CycloneTrailOpacity;
                                            mat.SetColor(s, c);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
    }

    // Self damage and charge time
    [HarmonyPatch(typeof(PlayerShip), "ThunderCharge")]
    class MPBalance_PlayerShip_ThunderCharge
    {
        static float chargeStart;
        public static int m_charge_loop_index = -1;

        static void Prefix(PlayerShip __instance)
        {
            if (__instance.m_refire_time <= 0f && __instance.m_thunder_power == 0f)
            {
                chargeStart = NetworkMatch.m_match_elapsed_seconds;
                m_charge_loop_index = -1;
            }
        }

        // Audio cue once charged
        static void Postfix(PlayerShip __instance)
        {
            if (__instance.isLocalPlayer && __instance.m_thunder_power >= 2f && m_charge_loop_index == -1)
            {
                m_charge_loop_index = GameManager.m_audio.PlayCue2DLoop(MPBalance.ThunderboltSelfDamageSoundEffect, 1f, 0f, 0f, true);
            }
        }

        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> codes)
        {
            int state = 0;
            int damageState = 0;
            foreach (var code in codes)
            {
                if (state < 2 && code.opcode == OpCodes.Add)
                {
                    state++;
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(MPBalance), "GetThunderboltChargeTimeMultiplierFloat"));
                    yield return new CodeInstruction(OpCodes.Mul);
                }

                if (code.opcode == OpCodes.Stfld && code.operand == AccessTools.Field(typeof(DamageInfo), "damage"))
                {
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(MPBalance), "GetThunderboltSelfDamageMultiplierFloat"));
                    yield return new CodeInstruction(OpCodes.Mul);
                }

                if (code.opcode == OpCodes.Stloc_1)
                    damageState++;

                if (damageState == 1 && code.opcode == OpCodes.Ldfld && code.operand == AccessTools.Field(typeof(PlayerShip), "m_thunder_power"))
                {
                    damageState++;
                    yield return code;
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(MPBalance), "GetThunderboltChargeTimeMultiplierFloat"));
                    yield return new CodeInstruction(OpCodes.Div);
                    continue;
                }

                yield return code;
            }
        }
    }

    [HarmonyPatch(typeof(PlayerShip), "MaybeFireWeapon")]
    class MPBalance_PlayerShip_MaybeFireWeapon
    {

        private static Vector3 AdjustLeftPos(Vector3 muzzle_pos, Vector3 c_right)
        {
            ProjectileExt component = ProjectileManager.proj_prefabs[27].GetComponent<ProjectileExt>();
            var adjusted = muzzle_pos + c_right * component.olmod_m_muzzle_left_adjust;
            return adjusted;
        }

        private static Vector3 AdjustRightPos(Vector3 muzzle_pos, Vector3 c_right)
        {
            ProjectileExt component = ProjectileManager.proj_prefabs[27].GetComponent<ProjectileExt>();
            var adjusted = muzzle_pos + c_right * component.olmod_m_muzzle_right_adjust;
            return adjusted;
        }

        private static void UpdateThunderCharge()
        {
            GameManager.m_audio.StopSound(MPBalance_PlayerShip_ThunderCharge.m_charge_loop_index);
            MPBalance_PlayerShip_ThunderCharge.m_charge_loop_index = -1;
        }

        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> codes)
        {
            int state = 0;
            foreach (var code in codes)
            {
                if (state == 0 && code.opcode == OpCodes.Ldc_I4_S && (sbyte)code.operand == 27)
                    state++;

                if (state == 1 && code.opcode == OpCodes.Callvirt && code.operand == AccessTools.Method(typeof(Transform), "get_position"))
                {
                    state++;
                    yield return code;
                    yield return new CodeInstruction(OpCodes.Ldloc_2);
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(MPBalance_PlayerShip_MaybeFireWeapon), "AdjustRightPos"));
                    continue;
                }

                if (state == 2 && code.opcode == OpCodes.Callvirt && code.operand == AccessTools.Method(typeof(Transform), "get_position"))
                {
                    state++;
                    yield return code;
                    yield return new CodeInstruction(OpCodes.Ldloc_2);
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(MPBalance_PlayerShip_MaybeFireWeapon), "AdjustLeftPos"));
                    continue;
                }

                if (state == 3 && code.opcode == OpCodes.Ldsfld && code.operand == AccessTools.Field(typeof(GameplayManager), "IsMultiplayerActive"))
                    state++;

                if (code.opcode == OpCodes.Stfld && code.operand == AccessTools.Field(typeof(PlayerShip), "m_thunder_sound_timer"))
                {
                    yield return code;
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(MPBalance_PlayerShip_MaybeFireWeapon), "UpdateThunderCharge"));
                    continue;
                }

                yield return code;
            }
        }
    }

    [HarmonyPatch(typeof(ProjectileManager), "ReadProjPresetData")]
    class MPBalance_ProjectileManager_ReadProjPresetData
    {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            foreach (var code in instructions)
            {
                if (code.opcode == OpCodes.Call && code.operand == AccessTools.Method(typeof(RUtility), "ReadField"))
                {
                    yield return code;
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    yield return new CodeInstruction(OpCodes.Ldloc_S, 6);
                    yield return new CodeInstruction(OpCodes.Ldloc_S, 10);
                    yield return new CodeInstruction(OpCodes.Ldc_I4_0);
                    yield return new CodeInstruction(OpCodes.Ldelem_Ref);
                    yield return new CodeInstruction(OpCodes.Ldloc_S, 10);
                    yield return new CodeInstruction(OpCodes.Ldc_I4_1);
                    yield return new CodeInstruction(OpCodes.Ldelem_Ref);
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(MPBalance_ProjectileManager_ReadProjPresetData), "MaybeReadProjectileExt"));
                    continue;
                }
                yield return code;
            }
        }

        static void MaybeReadProjectileExt(GameObject[] prefabs, int arrIdx, string name, string value)
        {
            ProjectileExt component = prefabs[arrIdx].GetComponent<ProjectileExt>();
            if (component)
            {
                if (name.StartsWith("olmod"))
                {
                    try
                    {
                        RUtility.ReadField(component, name, value);
                    }
                    catch (Exception ex)
                    {

                    }
                }
            }
        }
    }

    [HarmonyPatch]
    class PresetData_ProjectileManager_Init
    {
        static MethodBase TargetMethod()
        {
            foreach (var x in typeof(ProjectileManager).GetNestedTypes(BindingFlags.NonPublic))
            {
                if (x.Name.Contains("Init"))
                {
                    return x.GetMethod("MoveNext");
                }
            }
            return null;
        }

        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> codes)
        {
            int state = 0;
            foreach (var code in codes)
            {
                if (code.opcode == OpCodes.Stelem_Ref)
                {
                    state++;
                    if (state == 3)
                    {
                        state = 4;
                        yield return code;
                        yield return new CodeInstruction(OpCodes.Ldloc_1);
                        yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(PresetData_ProjectileManager_Init), "PatchInit"));
                        continue;
                    }
                }
                yield return code;
            }
        }

        static void PatchInit(int arrIdx)
        {
            ProjectileManager.proj_prefabs[arrIdx].AddComponent<ProjectileExt>();
        }
    }

    [HarmonyPatch(typeof(Projectile), "Fire")]
    class MPBalance_Projectile_Fire
    {

        private static void ModifyCrusherTrail(Projectile proj)
        {
            if (!MPBalance.UseProjdataCrusherTrail)
            {
                proj.m_trail_renderer = FXTrailRenderer.trail_renderer_crusher + Projectile.CrusherNextTrailIndex;
                Projectile.CrusherNextTrailIndex = (Projectile.CrusherNextTrailIndex + 1) % 3;
            }
        }

        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> codes)
        {
            int state = 0;
            foreach (var code in codes)
            {
                if (code.opcode == OpCodes.Ldc_I4_6)
                    state++;

                if (state == 1)
                {
                    if (code.opcode == OpCodes.Stsfld && code.operand == AccessTools.Field(typeof(Projectile), "CrusherNextTrailIndex"))
                    {
                        state++;
                        yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(MPBalance_Projectile_Fire), "ModifyCrusherTrail"));
                        continue;
                    }
                    else
                    {
                        continue;
                    }
                }
                yield return code;
            }
        }
    }

    class ProjectileExt : MonoBehaviour
    {
        public float olmod_m_muzzle_left_adjust = 0f;
        public float olmod_m_muzzle_right_adjust = 0f;
        public float olmod_m_tb_chargetime_multiplier = 1f;
        public float olmod_m_tb_overchargedamage_multiplier = 1f;
    }
}
