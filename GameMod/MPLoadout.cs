using Harmony;
using Overload;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using UnityEngine;

namespace GameMod
{
    class MPLoadout
    {
    }

    enum ExtCCInput
    {
        CYCLE_LOADOUT_PRIMARY = 60
    }

    [HarmonyPatch(typeof(Controls), "GetInputName")]
    class MPLoadout_Controls_GetInputName
    {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> codes)
        {
            foreach (var code in codes)
            {
                if (code.opcode == OpCodes.Ldstr && (string)code.operand == "Unknown")
                {
                    yield return new CodeInstruction(OpCodes.Ldarg_0) { labels = code.labels };
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(MPLoadout_Controls_GetInputName), "ExtendedInputName"));
                    continue;

                }
                yield return code;
            }
        }

        static string ExtendedInputName(CCInput cc)
        {
            Debug.Log("Calling ExtendedInputName for " + cc.ToString());
            if ((int)cc == (int)ExtCCInput.CYCLE_LOADOUT_PRIMARY)
            {
                return "CYCLE LOADOUT PRIMARY";
            }
            else
            {
                return "Unknown";
            }
        }
    }
    
    [HarmonyPatch(typeof(Controls), "InitControl")]
    class MPLoadout_Controls_InitControl
    {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> codes)
        {
            foreach (var code in codes)
            {
                if (code.opcode == OpCodes.Call && code.operand == AccessTools.Method(typeof(Controls), "ClearInputs"))
                {
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(MPLoadout_Controls_InitControl), "AddOptions"));
                }
                yield return code;
            }
        }

        static void AddOptions()
        {
            Array.Resize(ref Controls.m_input_count, 61);
            Controls.m_input_kc = ResizeArray(Controls.m_input_kc, 2, 61);
            Controls.m_input_joy = ResizeArray(Controls.m_input_joy, 2, 61);
            Debug.Log(Controls.m_input_kc[0, 59].ToString());
            Debug.Log(Controls.m_input_kc[1, 59].ToString());
        }

        static T[,] ResizeArray<T>(T[,] original, int x, int y)
        {
            T[,] newArray = new T[x, y];
            int minX = Math.Min(original.GetLength(0), newArray.GetLength(0));
            int minY = Math.Min(original.GetLength(1), newArray.GetLength(1));

            for (int i = 0; i < minY; ++i)
                Array.Copy(original, i * original.GetLength(0), newArray, i * newArray.GetLength(0), minX);

            return newArray;
        }
    }

    [HarmonyPatch(typeof(Controls), "ClearJoystickSlotsMenu")]
    class MPLoadout_Controls_ClearJoystickSlotsMenu
    {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> codes)
        {
            foreach (var code in codes)
            {
                if (code.opcode == OpCodes.Ldc_I4_S && (sbyte)code.operand == (sbyte)59)
                    code.operand = 61;

                yield return code;
            }
        }
    }

    [HarmonyPatch(typeof(Controls), "ClearControlsForController")]
    class MPLoadout_Controls_ClearControlsForController
    {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> codes)
        {
            foreach (var code in codes)
            {
                if (code.opcode == OpCodes.Ldc_I4_S && (sbyte)code.operand == (sbyte)59)
                    code.operand = 61;

                yield return code;
            }
        }
    }

    [HarmonyPatch(typeof(Controls), "ClearKBMouse")]
    class MPLoadout_Controls_ClearKBMouse
    {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> codes)
        {
            foreach (var code in codes)
            {
                if (code.opcode == OpCodes.Ldc_I4_S && (sbyte)code.operand == (sbyte)59)
                    code.operand = 61;

                yield return code;
            }
        }
    }

    [HarmonyPatch(typeof(Controls), "UpdateDevice")]
    class MPLoadout_Controls_UpdateDevice
    {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> codes)
        {
            foreach (var code in codes)
            {
                if (code.opcode == OpCodes.Ldc_I4_S && (sbyte)code.operand == (sbyte)59)
                    code.operand = 61;

                yield return code;
            }
        }
    }

    //[HarmonyPatch(typeof(Controls), "ReadControlDataFromStream")]
    //class MPLoadout_Controls_ReadControlDataFromStream
    //{
    //    static void Postfix()
    //    {
    //        for (int i = 0; i < Controls.m_controllers.Count; i++)
    //        {
    //            Controls.m_input_joy[0, 59].SetButtonTemplate(i, 59);
    //            Controls.m_input_joy[1, 59].SetButtonTemplate(i, 59);
    //            Controls.m_input_joy[0, 59].SetButtonTemplate(i, 60);
    //            Controls.m_input_joy[1, 59].SetButtonTemplate(i, 60);
    //        }

    //    }
    //}

    [HarmonyPatch(typeof(Controls), "ReadControlDataFromStream")]
    class MPLoadout_Controls_ReadControlDataFromStream
    {
        static void PatchCatchHandler(ref StreamReader sr, string text, int num, int num3)
        {
            Debug.Log("Running PatchCatchHandler with arguments: sr, " + text + ", " + num.ToString() + ", " + num3.ToString());
            try
            {
                ExtCCInput ccinput = (ExtCCInput)Enum.Parse(typeof(ExtCCInput), text);
                Debug.Log("Reading ExtCCInput: " + ccinput.ToString());
                //Controls.m_input_joy[0, (int)ccinput].Read(sr, num);
                //Controls.m_input_joy[1, (int)ccinput].Read(sr, num);
                Controls.m_input_kc[0, (int)ccinput] = (KeyCode)int.Parse(sr.ReadLine());
                Controls.m_input_kc[1, (int)ccinput] = (KeyCode)int.Parse(sr.ReadLine());
                //if (Controls.m_input_joy[0, (int)ccinput].m_controller_num >= num3)
                //{
                //    Controls.m_input_joy[0, (int)ccinput].m_controller_num = num3 - 1;
                //}
                //if (Controls.m_input_joy[1, (int)ccinput].m_controller_num >= num3)
                //{
                //    Controls.m_input_joy[1, (int)ccinput].m_controller_num = num3 - 1;
                //}
            }
            catch (Exception ex)
            {
                Debug.Log("Hit PatchCatchHandler fail exception");
                Debug.Log(ex.ToString());
                for (int l = 0; l < 10; l++)
                {
                    sr.ReadLine();
                }
            }
        }

        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> codes)
        {
            int state = 0;
            foreach (var code in codes)
            {
                if (code.opcode == OpCodes.Leave)
                    state++;

                if (state == 1 && code.opcode == OpCodes.Ldc_I4_0)
                {
                    state = 2;
                    Debug.Log("Patching in PatchCatchHandler");
                    // PatchCatchHandler(ref StreamReader sr, string text, int num, int num3)
                    yield return new CodeInstruction(OpCodes.Ldloca, 1);
                    yield return new CodeInstruction(OpCodes.Ldloc_0);
                    yield return new CodeInstruction(OpCodes.Ldloc, 2);
                    yield return new CodeInstruction(OpCodes.Ldloc, 6);
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(MPLoadout_Controls_ReadControlDataFromStream), "PatchCatchHandler"));
                }

                if (state == 2 && code.opcode == OpCodes.Leave)
                    state = 3;

                if (state == 2)
                {
                    continue;
                }
                yield return code;
            }
        }
    }

    [HarmonyPatch(typeof(UIElement), "DrawControlsMenu")]
    class MPLoadout_UIElement_DrawControlsMenu
    {
        static void DrawControls(ref Vector2 pos, UIElement uie)
        {
            uie.SelectAndDrawControlOption(Controls.GetInputName((CCInput)(int)ExtCCInput.CYCLE_LOADOUT_PRIMARY), pos, (int)ExtCCInput.CYCLE_LOADOUT_PRIMARY, false);
            pos.y += 48f;
        }
        
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> codes)
        {
            int state = 0;
            foreach (var code in codes)
            {
                if (code.opcode == OpCodes.Ldsfld && code.operand == AccessTools.Field(typeof(MenuManager), "control_remap_page2"))
                    state++;

                if ((state == 1 || state == 3) && code.opcode == OpCodes.Ldc_R4 && (float)code.operand == 300f)
                {
                    if (state == 3)
                        state = 4;
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(MPLoadout_UIElement_DrawControlsMenu), "DrawControls"));
                    yield return new CodeInstruction(OpCodes.Ldloca_S, 0);

                }
                yield return code;
            }
        }
    }

    [HarmonyPatch(typeof(MenuManager), "ControlsOptionsUpdate")]
    class MPLoadout_MenuManager_ControlOptionsUpdate
    {
        static void PatchRemap()
        {
            if (UIManager.m_menu_selection >= 59)
            {
                MenuManager.control_remap_index = UIManager.m_menu_selection;
                MenuManager.control_remap_alt = false;
            }
        }

        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> codes)
        {
            foreach (var code in codes)
            {
                if (code.opcode == OpCodes.Stsfld && code.operand == AccessTools.Field(typeof(MenuManager), "control_remap_index"))
                {
                    yield return code;
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(MPLoadout_MenuManager_ControlOptionsUpdate), "PatchRemap"));
                    continue;
                }
                yield return code;
            }
        }
    }

    [HarmonyPatch(typeof(Controls), "WriteControlDataToStream")]
    class MPLoadout_Controls_WriteControlDataToStream
    {
        static void Postfix(StreamWriter w)
        {
            foreach (var k in new List<int>() { 60 })
            {
                ExtCCInput ccinput = (ExtCCInput)k;
                w.WriteLine(ccinput.ToString());
                Controls.m_input_joy[0, k].Write(w);
                Controls.m_input_joy[1, k].Write(w);
                int num = (int)Controls.m_input_kc[0, k];
                w.WriteLine(num.ToString());
                int num2 = (int)Controls.m_input_kc[1, k];
                w.WriteLine(num2.ToString());
            }
            
        }
    }
}
