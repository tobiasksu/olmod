using HarmonyLib;
using Overload;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;

namespace GameMod
{
    class Segnums
    {
        static void DrawSegmentNums()
        {
            Vector3 shipPos = GameManager.m_player_ship.c_transform_position;
            Vector3 forward = GameManager.m_player_ship.c_camera_transform.forward;
            Quaternion shipQuat = GameManager.m_player_ship.c_transform.localRotation;
            MethodInfo VisibilityRaycast = AccessTools.Method(typeof(UIManager), "VisibilityRaycast");
            
            for (int i = 0; i < GameManager.m_level_data.Segments.Length; i++)
            {
                var seg = GameManager.m_level_data.Segments[i];
                Vector3 segCenter = seg.Center;
                Vector3 vector;
                vector.x = segCenter.x - shipPos.x;
                vector.y = segCenter.y - shipPos.y;
                vector.z = segCenter.z - shipPos.z;
                float dist = Mathf.Max(0.1f, vector.magnitude);
                vector.x /= dist;
                vector.y /= dist;
                vector.z /= dist;
                if ((bool)VisibilityRaycast.Invoke(null, new object[] { shipPos, vector, dist }))
                {
                    int quad_index = UIManager.m_quad_index;
                    Vector2 offset = Vector2.zero;
                    offset.y = -80f / dist;
                    UIManager.DrawStringAlignCenter($"{i}", offset, 1f, UIManager.m_col_white2, -1f);
                    WorldText.PreviousQuadsTransformText(segCenter, shipQuat, dist, quad_index);
                }
            }
        }
    }

    class SpawnPoints
    {
        static void DrawSpawnPoints()
        {
            Vector3 shipPos = GameManager.m_player_ship.c_transform_position;
            Vector3 forward = GameManager.m_player_ship.c_camera_transform.forward;
            Quaternion shipQuat = GameManager.m_player_ship.c_transform.localRotation;

            for (int i = 0; i < GameManager.m_level_data.m_player_spawn_points.Length; i++)
            {
                LevelData.SpawnPoint sp = GameManager.m_level_data.m_player_spawn_points[i];
                Vector3 spawnPos = sp.position;
                Vector3 vector;
                vector.x = spawnPos.x - shipPos.x;
                vector.y = spawnPos.y - shipPos.y;
                vector.z = spawnPos.z - shipPos.z;
                float dist = Mathf.Max(0.1f, vector.magnitude);

                int quad_index = UIManager.m_quad_index;
                Vector2 offset = Vector2.zero;
                offset.y = -80f / dist;
                UIManager.DrawStringAlignCenter($"Spawn {i}", offset, 1f, UIManager.m_col_red, -1f);
                WorldText.PreviousQuadsTransformText(spawnPos, shipQuat, dist, quad_index);
            }
        }
    }

    /// <summary>
    /// Riff on UIManager.PreviousQuadsTransformPlayer
    /// </summary>
    class WorldText
    {
        public static void PreviousQuadsTransformText(Vector3 worldPos, Quaternion localOrient, float dist, int old_index)
        {
            float num = 0.0125f * dist;
            for (int i = UIManager.m_quad_index - 1; i >= old_index; i--)
            {
                int num2 = i * 4;
                for (int j = 0; j < 4; j++)
                {
                    Vector3[] vertices = UIManager.m_vertices;
                    int num3 = num2 + j;
                    vertices[num3].x = vertices[num3].x * num;
                    Vector3[] vertices2 = UIManager.m_vertices;
                    int num4 = num2 + j;
                    vertices2[num4].y = vertices2[num4].y * num;
                    Vector3[] vertices3 = UIManager.m_vertices;
                    int num5 = num2 + j;
                    vertices3[num5].z = vertices3[num5].z * num;
                    UIManager.m_vertices[num2 + j] = localOrient * UIManager.m_vertices[num2 + j];
                    UIManager.m_vertices[num2 + j].x = UIManager.m_vertices[num2 + j].x + worldPos.x;
                    UIManager.m_vertices[num2 + j].y = UIManager.m_vertices[num2 + j].y + worldPos.y;
                    UIManager.m_vertices[num2 + j].z = UIManager.m_vertices[num2 + j].z + worldPos.z;
                }
            }
        }
    }

    [HarmonyPatch(typeof(UIManager), "Draw")]
    class Segnums_UIManager_Draw
    {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> codes)
        {
            int state = 0;
            foreach (var code in codes)
            {
                if (code.opcode == OpCodes.Call && code.operand == AccessTools.Method(typeof(UIManager), "DrawMultiplayerNames"))
                    state = 1;

                if (state == 1 && code.opcode == OpCodes.Call && code.operand == AccessTools.Method(typeof(UIManager), "EndDrawing"))
                {
                    state = 2;
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Segnums), "DrawSegmentNums")) { labels = code.labels };
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(SpawnPoints), "DrawSpawnPoints"));
                    code.labels = null;
                }
                yield return code;
            }
        }   
    }

    [HarmonyPatch(typeof(UIElement), "DrawHUD")]
    class Segnums_UIElement_DrawHUD
    {
        static void Prefix(UIElement __instance)
        {
            int curSeg = GameManager.m_player_ship.GetMovingObject().CurrentSegmentIndex;
            __instance.DrawStringSmall($"Cur Seg: {curSeg}", new Vector2(UIManager.UI_LEFT + 5f, UIManager.UI_TOP + 75f), 0.5f, StringOffset.LEFT, UIManager.m_col_white2, 0.5f, -1f);
        }
    }
}
