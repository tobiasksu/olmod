﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Harmony;
using Overload;
using UnityEngine;

namespace GameMod
{
    static class MPTeams
    {
        public static int NetworkMatchTeamCount;
        public static int MenuManagerTeamCount;
        public static readonly int Min = 2;
        public static readonly int Max = 8;
        private static readonly float[] colors = { 0.08f, 0.16f, 0.32f, 0.51f, 0.62f, 0.71f, 0.91f, 0.002f, 0.6f };
        private static readonly int[] colorIdx = { 4, 0, 2, 3, 5, 6, 7, 8 };
        public static readonly MpTeam[] AllTeams = { MpTeam.TEAM0, MpTeam.TEAM1,
            MpTeam.NUM_TEAMS, MpTeam.NUM_TEAMS + 1, MpTeam.NUM_TEAMS + 2, MpTeam.NUM_TEAMS + 3,
            MpTeam.NUM_TEAMS + 4, MpTeam.NUM_TEAMS + 5 };
        public static readonly MpTeam MPTEAM_NUM = MpTeam.NUM_TEAMS + 6;
        private static readonly int[] teamIndexList = { 0, 1, -1, 2, 3, 4, 5, 6, 7 };

        public static int TeamNum(MpTeam team)
        {
            return teamIndexList[(int)team];
        }

        public static IEnumerable<MpTeam> Teams
        {
            get
            {
                return AllTeams.Take(NetworkMatchTeamCount);
            }
        }

        public static IEnumerable<MpTeam> ActiveTeams
        {
            get
            {
                int[] team_counts = new int[(int)MPTeams.MPTEAM_NUM];
                foreach (var player in Overload.NetworkManager.m_PlayersForScoreboard)
                    if (!player.m_spectator)
                        team_counts[(int)player.m_mp_team]++;
                foreach (var team in AllTeams)
                {
                    var idx = teamIndexList[(int)team];
                    if (team_counts[(int)team] > 0 ||
                        (idx < NetworkMatch.m_team_scores.Length && NetworkMatch.m_team_scores[idx] != 0))
                        yield return team;
                }
            }
        }

        public static MpTeam[] TeamsByScore
        {
            get
            {
                var teams = ActiveTeams.ToArray();
                Array.Sort<MpTeam>(teams, new Comparison<MpTeam>((i1, i2) =>
                {
                    var n = NetworkMatch.m_team_scores[(int)i2].CompareTo(NetworkMatch.m_team_scores[(int)i1]);
                    return n == 0 ? i1.CompareTo(i2) : n;
                }));
                return teams;
            }
        }

        public static MpTeam NextTeam(MpTeam team)
        {
            team = team + 1;
            if (team == MpTeam.ANARCHY)
                team = team + 1;
            return team == MPTEAM_NUM || AllTeams.IndexOf(x => x == team) >= NetworkMatchTeamCount ? MpTeam.TEAM0 : team;
        }

        public static string TeamName(MpTeam team)
        {
            var c = MenuManager.mpc_decal_color;
            MenuManager.mpc_decal_color = colorIdx[TeamNum(team)];
            var ret = MenuManager.GetMpDecalColor();
            MenuManager.mpc_decal_color = c;
            return ret;
        }

        public static int TeamColorIdx(MpTeam team)
        {
            return colorIdx[TeamNum(team)];
        }

        public static Color TeamColor(MpTeam team, int mod)
        {
            int cIdx = colorIdx[TeamNum(team)];
            float sat = cIdx == 8 ? 0.01f : cIdx == 4 && mod == 5 ? 0.6f : 0.95f - mod * 0.05f;
            float bright = mod == 5 ? 0.95f : 0.5f + mod * 0.1f;
            return HSBColor.ConvertToColor(colors[cIdx], sat, bright);
        }

        static void DrawTeamHeader(UIElement uie, Vector2 pos, MpTeam team, float w = 255f)
        {
            Color c = TeamColor(team, 1);
            Color c2 = TeamColor(team, 4);
            c.a = uie.m_alpha;
            UIManager.DrawQuadBarHorizontal(pos, 13f, 13f, w * 2f, c, 7);
            uie.DrawStringSmall(NetworkMatch.GetTeamName(team), pos, 0.6f, StringOffset.CENTER, c2, 1f, -1f);
        }

        public static void DrawLobby(UIElement uie, Vector2 pos)
        {
            float name_offset = -250f;
            float highlight_width = 285f;
            float org_x = pos.x;
            int max_row_count = NetworkMatch.GetMaxPlayersForMatch() + MPTeams.NetworkMatchTeamCount;
            int cur_row_count = NetworkMatch.m_players.Count() + MPTeams.NetworkMatchTeamCount;
            bool split = max_row_count > 10;
            if (split)
            {
                pos.x -= 300f;
                pos.y += 50f + 24f;
            }
            float org_y = pos.y;
            float first_y = org_y;
            int rows_per_col = split ? (cur_row_count + 1) / 2 : cur_row_count;
            int row_num = 0;
            foreach (var team in Teams)
            {
                if (row_num >= rows_per_col)
                {
                    first_y = pos.y;
                    pos.x += 300f * 2;
                    pos.y = org_y;
                    rows_per_col = cur_row_count; // no more split
                    row_num = 0;
                }
                DrawTeamHeader(uie, pos, team, 255f);
                pos.y += 24f;
                row_num++;
                int num = 0;
                foreach (var value in NetworkMatch.m_players.Values)
                {
                    if (value.m_team == team)
                    {
                        uie.DrawPlayerName(pos, value, num % 2 == 0, highlight_width, name_offset, -1f);
                        pos.y += 20f;
                        num++;
                        row_num++;
                    }
                }
                pos.y += 10f;
            }
            pos.y = Mathf.Max(first_y, pos.y) + 10f;
            pos.x = org_x;
            if (MenuManager.m_menu_micro_state != 2 && MenuManager.m_mp_private_match)
            {
                float alpha_mod = (MenuManager.m_mp_cst_timer > 0f) ? 0.2f : 1f;
                uie.DrawStringSmall(ScriptTutorialMessage.ControlString(CCInput.MENU_DELETE) + " - " + Loc.LS("CHANGE TEAMS"), pos, 0.45f, StringOffset.CENTER, UIManager.m_col_ui0, alpha_mod, -1f);
            }
        }

        public static void DrawTeamScoreSmall(UIElement uie, Vector2 pos, MpTeam team, int score, float w = 350f, bool my_team = false)
        {
            Color c = TeamColor(team, my_team ? 2 : 0);
            Color color = TeamColor(team, my_team ? 4 : 2);
            c.a = uie.m_alpha;
            if (my_team)
                UIManager.DrawQuadBarHorizontal(pos, 15f, 15f, w * 2f, c, 7);
            UIManager.DrawQuadBarHorizontal(pos, 12f, 12f, w * 2f, c, 7);
            uie.DrawDigitsVariable(pos + Vector2.right * w, score, 0.55f, StringOffset.RIGHT, color, uie.m_alpha);
            uie.DrawStringSmall(NetworkMatch.GetTeamName(team), pos - Vector2.right * (w + 9f), 0.5f, StringOffset.LEFT, color, 1f, -1f);
        }

        public static void DrawTeamScore(UIElement uie, Vector2 pos, MpTeam team, int score, float w = 350f, bool my_team = false)
        {
            Color c = TeamColor(team, my_team ? 2 : 0);
            Color color = TeamColor(team, my_team ? 4 : 2);
            c.a = uie.m_alpha;
            if (my_team)
                UIManager.DrawQuadBarHorizontal(pos, 18f, 18f, w * 2f, c, 7);
            UIManager.DrawQuadBarHorizontal(pos, 15f, 15f, w * 2f, c, 7);
            uie.DrawDigitsVariable(pos + Vector2.right * w, score, 0.7f, StringOffset.RIGHT, color, uie.m_alpha);
            uie.DrawStringSmall(NetworkMatch.GetTeamName(team), pos - Vector2.right * (w + 9f), 0.6f, StringOffset.LEFT, color, 1f, -1f);
        }

        private static MethodInfo _UIElement_DrawScoreHeader_Method = AccessTools.Method(typeof(UIElement), "DrawScoreHeader");
        public static void DrawScoreHeader(UIElement uie, Vector2 pos, float col1, float col2, float col3, float col4, float col5, bool score = false)
        {
            _UIElement_DrawScoreHeader_Method.Invoke(uie, new object[] { pos, col1, col2, col3, col4, col5, score });
            return;
        }

        private static MethodInfo _UIElement_DrawMpScoreboardRaw_Method = typeof(UIElement).GetMethod("DrawMpScoreboardRaw", BindingFlags.NonPublic | BindingFlags.Instance);
        public static void DrawPostgame(UIElement uie)
        {
            Vector2 pos = Vector2.zero;
            pos.y = -300f;
            Color c = UIManager.m_col_ui5;
            string s = Loc.LS("MATCH OVER!");
            var teams = TeamsByScore;
            if (teams.Length >= 2 && NetworkMatch.m_team_scores[(int)teams[0]] != NetworkMatch.m_team_scores[(int)teams[1]])
            {
                c = TeamColor(teams[0], 4);
                s = TeamName(teams[0]) + " WINS!";
            }
            float a = uie.m_alpha * uie.m_alpha * uie.m_alpha;
            uie.DrawWideBox(pos, 300f, 29f, c, a, 7);
            uie.DrawWideBox(pos, 300f, 25f, c, a, 11);
            uie.DrawStringSmall(s, pos, 1.35f, StringOffset.CENTER, UIManager.m_col_ub3, uie.m_alpha, -1f);
            pos.y = -200f;
            _UIElement_DrawMpScoreboardRaw_Method.Invoke(uie, new object[] { pos });
            pos.y = -290f;
            pos.x = 610f;
            uie.DrawStringSmall(NetworkMatch.GetModeString(MatchMode.NUM), pos, 0.75f, StringOffset.RIGHT, UIManager.m_col_ui5, 1f, -1f);
            pos.y += 25f;
            uie.DrawStringSmall(GameplayManager.Level.DisplayName, pos, 0.5f, StringOffset.RIGHT, UIManager.m_col_ui1, 1f, -1f);
            if (GameManager.m_player_ship.m_wheel_select_state == WheelSelectState.QUICK_CHAT)
            {
                pos.y = UIManager.UI_TOP + 158f;
                pos.x = -418f;
                uie.DrawQuickChatWheel(pos);
            }
            else
            {
                pos.y = UIManager.UI_TOP + 90f;
                pos.x = UIManager.UI_LEFT + 35f;
                uie.DrawQuickChatMP(pos);
            }
        }

        public static int HighestScore()
        {
            int max_score = 0;
            foreach (var team in Teams)
            {
                int score = NetworkMatch.GetTeamScore(team);
                if (score > max_score)
                    max_score = score;
            }
            return max_score;
        }
    }

    [HarmonyPatch(typeof(UIElement), "MaybeDrawPlayerList")]
    static class MPTeamsDraw
    {
        static bool Prefix(UIElement __instance, Vector2 pos)
        {
            if (!MenuManager.mp_display_player_list ||
                ((!NetworkMatch.IsTeamMode(NetworkMatch.GetMode()) || MPTeams.NetworkMatchTeamCount == 2))) // &&
                //NetworkMatch.GetMaxPlayersForMatch() <= 8))
                return true;
            MPTeams.DrawLobby(__instance, pos);
            return false;
        }
    }

    [HarmonyPatch(typeof(UIElement), "DrawHUDScoreInfo")]
    static class MPTeamsHUDScore
    {
        static bool Prefix(UIElement __instance, ref Vector2 pos)
        {
            if (!GameplayManager.IsMultiplayerActive || NetworkMatch.GetMode() == MatchMode.ANARCHY || MPTeams.NetworkMatchTeamCount == 2)
                return true;

            var uie = __instance;
            pos.x -= 4f;
            pos.y -= 5f;
            Vector2 temp_pos;
            temp_pos.y = pos.y;
            temp_pos.x = pos.x - 100f;
            uie.DrawStringSmall(NetworkMatch.GetModeString(MatchMode.NUM), temp_pos, 0.4f, StringOffset.LEFT, UIManager.m_col_ub0, 1f, 130f);
            temp_pos.x = pos.x + 95f;
            int match_time_remaining = NetworkMatch.m_match_time_remaining;
            int num3 = (int)NetworkMatch.m_match_elapsed_seconds;
            uie.DrawDigitsTime(temp_pos, (float)match_time_remaining, 0.45f,
                (num3 <= 10 || match_time_remaining >= 10) ? UIManager.m_col_ui2 : UIManager.m_col_em5, uie.m_alpha, false);
            temp_pos.x = pos.x - 100f;
            temp_pos.y = temp_pos.y - 20f;
            uie.DrawPing(temp_pos);
            pos.y += 24f;

            MpTeam myTeam = GameManager.m_local_player.m_mp_team;
            foreach (var team in MPTeams.TeamsByScore)
            {
                if (!NetworkManager.m_PlayersForScoreboard.Any(x => x.m_mp_team == team))
                    continue;

                MPTeams.DrawTeamScoreSmall(__instance, pos, team, NetworkMatch.GetTeamScore(team), 98f, team == myTeam);
                pos.y += 28f;
            }
            pos.y += 6f - 28f;

            pos.y += 22f;

            pos.x += 100f;
            uie.DrawRecentKillsMP(pos);
            if (GameManager.m_player_ship.m_wheel_select_state == WheelSelectState.QUICK_CHAT)
            {
                pos.y = UIManager.UI_TOP + 128f;
                pos.x = -448f;
                uie.DrawQuickChatWheel(pos);
            }
            else
            {
                pos.y = UIManager.UI_TOP + 60f;
                pos.x = UIManager.UI_LEFT + 5f;
                uie.DrawQuickChatMP(pos);
            }

            return false;
        }
    }

    // shown on death
    [HarmonyPatch(typeof(UIElement), "DrawMpMiniScoreboard")]
    static class MPTeamsMiniScore
    {
        static bool Prefix(UIElement __instance, ref Vector2 pos)
        {
            if (MPModPrivateData.MatchMode == ExtMatchMode.RACE)
            {
                Race.DrawMpMiniScoreboard(ref pos, __instance);
                return false;
            }

            if (NetworkMatch.GetMode() == MatchMode.ANARCHY || MPTeams.NetworkMatchTeamCount == 2)
                return true;

            int match_time_remaining = NetworkMatch.m_match_time_remaining;
            int match_time = (int)NetworkMatch.m_match_elapsed_seconds;
            pos.y -= 15f;
            __instance.DrawDigitsTime(pos + Vector2.right * 95f, (float)match_time_remaining, 0.45f,
                (match_time <= 10 || match_time_remaining >= 10) ? UIManager.m_col_ui2 : UIManager.m_col_em5,
                __instance.m_alpha, false);
            pos.y -= 3f;

            MpTeam myTeam = GameManager.m_local_player.m_mp_team;
            foreach (var team in MPTeams.TeamsByScore)
            {
                if (!NetworkManager.m_PlayersForScoreboard.Any(x => x.m_mp_team == team))
                    continue;

                pos.y += 28f;
                int score = NetworkMatch.GetTeamScore(team);
                MPTeams.DrawTeamScoreSmall(__instance, pos, team, score, 98f, team == myTeam);
            }
            pos.y += 6f;
            return false;
        }
    }

    [HarmonyPatch(typeof(NetworkMatch), "GetTeamName")]
    static class MPTeamsName
    {
        static bool Prefix(MpTeam team, ref string __result)
        {
            __result = MPTeams.TeamName(team);
            return false;
        }
    }

    [HarmonyPatch(typeof(MenuManager), "GetMpTeamName")]
    static class MPTeamsNameMenu
    {
        static bool Prefix(MpTeam team, ref string __result)
        {
            if (team < MpTeam.NUM_TEAMS)
                return true;
            __result = MPTeams.TeamName(team) + " TEAM";
            return false;
        }
    }

    [HarmonyPatch(typeof(NetworkMatch), "Init")]
    static class MPTeamsInit
    {
        static void Prefix()
        {
            NetworkMatch.m_team_scores = new int[(int)MPTeams.MPTEAM_NUM];
        }
    }

    [HarmonyPatch(typeof(NetworkMatch), "StartPlaying")]
    static class MPTeamsStartPlaying
    {
        static void Prefix()
        {
            for (int i = 0, l = NetworkMatch.m_team_scores.Length; i < l; i++)
                NetworkMatch.m_team_scores[i] = 0;
        }
    }

    [HarmonyPatch(typeof(NetworkMatch), "InitBeforeEachMatch")]
    static class MPTeamsInitBeforeEachMatch
    {
        static void Prefix()
        {
            for (int i = 0, l = NetworkMatch.m_team_scores.Length; i < l; i++)
                NetworkMatch.m_team_scores[i] = 0;
        }
    }

    // team balancing for new player
    [HarmonyPatch(typeof(NetworkMatch), "NetSystemGetTeamForPlayer")]
    static class MPTeamsForPlayer
    {
        static bool Prefix(ref MpTeam __result, int connection_id)
        {
            if (!NetworkMatch.IsTeamMode(NetworkMatch.GetMode()))
            {
                __result = MpTeam.ANARCHY;
                return false;
            }
            if (NetworkMatch.GetMode() == MatchMode.ANARCHY || (MPTeams.NetworkMatchTeamCount == 2 &&
                !MPJoinInProgress.NetworkMatchEnabled)) // use this simple balancing method for JIP to hopefully solve JIP team imbalances
                return true;
            if (NetworkMatch.m_players.TryGetValue(connection_id, out var connPlayer)) // keep team if player already exists (when called from OnUpdateGameSession)
            {
                __result = connPlayer.m_team;
                return false;
            }
            int[] team_counts = new int[(int)MPTeams.MPTEAM_NUM];
            foreach (var player in NetworkMatch.m_players.Values)
                team_counts[(int)player.m_team]++;
            MpTeam min_team = MpTeam.TEAM0;
            foreach (var team in MPTeams.Teams)
                if (team_counts[(int)team] < team_counts[(int)min_team] ||
                    (team_counts[(int)team] == team_counts[(int)min_team] &&
                        NetworkMatch.m_team_scores[(int)team] < NetworkMatch.m_team_scores[(int)min_team]))
                    min_team = team;
            __result = min_team;
            Debug.LogFormat("GetTeamForPlayer: result {0}, conn {1}, counts {2}, scores {3}", (int)min_team, connection_id,
                team_counts.Join(), NetworkMatch.m_team_scores.Join());
            return false;
        }
    }

    [HarmonyPatch(typeof(NetworkMatch), "GetHighestScoreTeamAnarchy")]
    static class MPTeamsHighest
    {
        static bool Prefix(ref int __result)
        {
            if (NetworkMatch.GetMode() == MatchMode.ANARCHY || MPTeams.NetworkMatchTeamCount == 2)
                return true;
            __result = MPTeams.HighestScore();
            return false;
        }
    }

    [HarmonyPatch(typeof(NetworkMatch), "GetHighestScorePowercore")]
    static class MPTeamsHighestMB
    {
        static bool Prefix(ref int __result)
        {
            if (NetworkMatch.GetMode() == MatchMode.ANARCHY || MPTeams.NetworkMatchTeamCount == 2)
                return true;
            __result = MPTeams.HighestScore();
            return false;
        }
    }


    [HarmonyPatch]
    static class MPTeamsCanStartNow
    {
        static MethodBase TargetMethod()
        {
            return typeof(NetworkMatch).GetNestedType("HostActiveMatchInfo", BindingFlags.NonPublic | BindingFlags.Instance)
                .GetMethod("CanStartNow", BindingFlags.Public | BindingFlags.Instance);
        }
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> cs)
        {
            foreach (var c in cs)
            {
                if (c.opcode == OpCodes.Ldsfld && ((FieldInfo)c.operand).Name == "m_match_mode")
                {
                    var c2 = new CodeInstruction(OpCodes.Ldc_I4_1) { labels = c.labels };
                    yield return c2;
                    yield return new CodeInstruction(OpCodes.Ret);
                    c.labels = null;
                }
                yield return c;
            }
        }
    }

    [HarmonyPatch(typeof(UIElement), "DrawMpPreMatchMenu")]
    class MPTeamsLobbyPos
    {
        static bool DrawLast(UIElement uie)
        {
            if (MenuManager.m_menu_micro_state != 0)
                return MenuManager.m_menu_micro_state == 1 ? DrawLastQuit(uie) : true;
            Vector2 position;
            position.x = 0f;
            position.y = 170f + 62f * 2;
            //uie.DrawMenuSeparator(position - Vector2.up * 40f);
            bool flag = NetworkMatch.m_last_lobby_status != null && NetworkMatch.m_last_lobby_status.m_can_start_now && MPModifiers.PlayerModifiersValid(Player.Mp_modifier1, Player.Mp_modifier2);
            uie.SelectAndDrawCheckboxItem(Loc.LS("START MATCH NOW"), position - Vector2.right * 250f, 0, MenuManager.m_mp_ready_to_start && flag,
                !flag || MenuManager.m_mp_ready_vote_timer > 0f, 0.75f, -1);
            //position.y += 62f;
            uie.SelectAndDrawItem(Loc.LS("CUSTOMIZE"), position + Vector2.right * 250f, 1, false, 0.75f, 0.75f);
            position.y += 62f;
            uie.SelectAndDrawItem(Loc.LS("OPTIONS"), position - Vector2.right * 250f, 2, false, 0.75f, 0.75f);
            //position.y += 62f;
            uie.SelectAndDrawItem(Loc.LS("MULTIPLAYER MENU"), position + Vector2.right * 250f, 100, false, 0.75f, 0.75f);
            return false;
        }

        static bool DrawLastQuit(UIElement uie)
        {
            Vector2 position;
            position.x = 0f;
            position.y = 170f + 62f * 2;
            uie.SelectAndDrawItem(Loc.LS("QUIT"), position, 0, false, 1f, 0.75f);
            position.y += 62f;
            uie.SelectAndDrawItem(Loc.LS("CANCEL"), position, 100, false, 1f, 0.75f);
            return false;
        }

        static void DrawDigitsLikeOne(UIElement uie, Vector2 pos, int value, float scl, Color c, float a)
        {
            uie.DrawStringSmall(value.ToString(), pos + Vector2.right * 15f, scl, StringOffset.RIGHT, c, a);
        }

        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var mpTeamsLobbyPos_DrawLast_Method = AccessTools.Method(typeof(MPTeamsLobbyPos), "DrawLast");
            var mpTeamsLobbyPos_DrawDigitsLikeOne_Method = AccessTools.Method(typeof(MPTeamsLobbyPos), "DrawDigitsLikeOne");

            int state = 0; // 0 = before switch, 1 = after switch
            int oneSixtyCount = 0;
            for (var codes = instructions.GetEnumerator(); codes.MoveNext();)
            {
                var code = codes.Current;
                // add call before switch m_menu_micro_state
                if (state == 0 && code.opcode == OpCodes.Ldsfld && ((FieldInfo)code.operand).Name == "m_menu_micro_state")
                {
                    yield return code;
                    codes.MoveNext();
                    code = codes.Current;
                    yield return code;
                    if (code.opcode != OpCodes.Stloc_S)
                        continue;
                    var buf = new List<CodeInstruction>();
                    // find br to end of switch just after switch instruction
                    while (codes.MoveNext() && (code = codes.Current).opcode != OpCodes.Br)
                        buf.Add(code);
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    yield return new CodeInstruction(OpCodes.Call, mpTeamsLobbyPos_DrawLast_Method);
                    yield return new CodeInstruction(OpCodes.Brfalse, code.operand); // returns false? skip to end of switch
                    // preserve switch jump
                    foreach (var bcode in buf)
                        yield return bcode;
                    state = 1;
                }
                if (code.opcode == OpCodes.Call && ((MethodInfo)code.operand).Name == "DrawDigitsOne") // allow >9 with same positioning
                    code.operand = mpTeamsLobbyPos_DrawDigitsLikeOne_Method;
                if (code.opcode == OpCodes.Ldc_R4 && (float)code.operand == 160f)
                {
                    oneSixtyCount++;
                    if (oneSixtyCount == 3)
                    {
                        code.operand = 300f;
                    }
                }
                yield return code;
            }
        }
    }

    [HarmonyPatch(typeof(MenuManager), "MpPreMatchMenuUpdate")]
    class MPTeamsSwitch
    {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var mpTeams_NextTeam_Method = AccessTools.Method(typeof(MPTeams), "NextTeam");

            for (var codes = instructions.GetEnumerator(); codes.MoveNext();)
            {
                var code = codes.Current;
                if (code.opcode == OpCodes.Ldc_R4 && (float)code.operand == 3f) // reduce team switch wait time
                    code.operand = 0.2f;
                if (code.opcode == OpCodes.Ldfld && ((FieldInfo)code.operand).Name == "m_team")
                {
                    yield return code;
                    yield return new CodeInstruction(OpCodes.Call, mpTeams_NextTeam_Method);
                    // skip until RequestSwitchTeam call
                    while (codes.MoveNext() && codes.Current.opcode != OpCodes.Call)
                        ;
                    code = codes.Current;
                }
                yield return code;
            }
        }
    }

    [HarmonyPatch(typeof(UIElement), "DrawHUDArmor")]
    class MPTeamsHUDArmor
    {
        public static IEnumerable<CodeInstruction> ChangeTeamColorLoad(IEnumerator<CodeInstruction> codes, OpCode mod)
        {
            // current team already loaded
            yield return new CodeInstruction(mod);
            yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(MPTeams), "TeamColor"));
            // skip until store
            var labels = new List<Label>();
            while (codes.MoveNext() && codes.Current.opcode != OpCodes.Stloc_S && codes.Current.opcode != OpCodes.Stloc_0)
                if (codes.Current.labels.Count() != 0)
                {
                    //var ncode = new CodeInstruction(OpCodes.Nop);
                    labels.AddRange(codes.Current.labels);
                    //yield return ncode;
                }
            // do store color
            codes.Current.labels.AddRange(labels);
            yield return codes.Current;
        }

        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var codes = instructions.GetEnumerator();
            while (codes.MoveNext())
            {
                var code = codes.Current;
                if (code.opcode == OpCodes.Ldfld && ((FieldInfo)code.operand).Name == "m_mp_team")
                {
                    yield return code;
                    for (int i = 0; i < 2; i++)
                    {
                        // pass on until color init
                        while (codes.MoveNext() &&
                            (codes.Current.opcode != OpCodes.Ldfld || ((FieldInfo)codes.Current.operand).Name != "m_mp_team"))
                            yield return codes.Current;
                        yield return codes.Current;
                        foreach (var c in MPTeamsHUDArmor.ChangeTeamColorLoad(codes, i == 0 ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_3))
                            yield return c;
                    }
                    break;
                }
                yield return code;
            }
            while (codes.MoveNext())
                yield return codes.Current;
        }
    }

    [HarmonyPatch(typeof(UIElement), "DrawMPWeaponOutline")]
    class MPTeamsWeaponOutline
    {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var codes = instructions.GetEnumerator();
            int cnt = 0;
            while (codes.MoveNext())
            {
                var code = codes.Current;
                yield return code;
                if (code.opcode == OpCodes.Ldfld && ((FieldInfo)code.operand).Name == "m_mp_team" && ++cnt == 1)
                    foreach (var c in MPTeamsHUDArmor.ChangeTeamColorLoad(codes, OpCodes.Ldc_I4_1))
                        yield return c;
            }
        }
    }

    [HarmonyPatch(typeof(Overload.UIElement), "DrawMpPostgame")]
    class MPTeamsPostgamePatch
    {
        static bool Prefix(UIElement __instance)
        {
            if (!NetworkMatch.IsTeamMode(NetworkMatch.GetMode()) || (MPTeams.NetworkMatchTeamCount == 2 && NetworkMatch.m_players.Count <= 8))
                return true;
            if (GameManager.m_local_player.m_hitpoints >= 0f)
                MPTeams.DrawPostgame(__instance);
            return false;
        }
    }

    [HarmonyPatch(typeof(Overload.UIElement), "DrawMpPostgameOverlay")]
    class MPTeamsPostgameOverlayPatch
    {
        static bool Prefix(UIElement __instance)
        {
            if (!NetworkMatch.IsTeamMode(NetworkMatch.GetMode()) || (MPTeams.NetworkMatchTeamCount == 2 && NetworkMatch.m_players.Count <= 8))
                return true;
            if (GameManager.m_local_player.m_hitpoints < 0f)
                MPTeams.DrawPostgame(__instance);
            return false;
        }
    }

    [HarmonyPatch(typeof(Overload.UIElement), "DrawHUD")]
    class MPTeamsHUDPatch
    {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> cs)
        {
            foreach (var c in cs)
            {
                if (c.opcode == OpCodes.Ldc_R4 && (float)c.operand == -240f)
                    c.operand = -350f;
                yield return c;
            }
        }
    }

    [HarmonyPatch(typeof(Overload.UIManager), "ChooseMpColor")]
    class MPTeamsMpColor
    {
        static bool Prefix(MpTeam team, ref Color __result)
        {
            if (team < MpTeam.NUM_TEAMS)
                return true;
            __result = MPTeams.TeamColor(team, 2);
            return false;
        }
    }

    [HarmonyPatch(typeof(UIElement), "DrawMpMatchSetup")]
    class MPTeamsMenuDraw
    {
        static void Postfix(UIElement __instance)
        {
            if (MenuManager.m_menu_micro_state != 2 || !NetworkMatch.IsTeamMode(MenuManager.mms_mode))
                return;
            Vector2 position = Vector2.zero;
            position.y = -279f + 62f * 6;
            __instance.SelectAndDrawStringOptionItem("TEAM COUNT", position, 8, MPTeams.MenuManagerTeamCount.ToString(), string.Empty, 1.5f,
                MenuManager.mms_mode == MatchMode.ANARCHY || !MenuManager.m_mp_lan_match);
        }
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> cs)
        {
            var vector2_y_Field = AccessTools.Field(typeof(Vector2), "y");

            int lastAdv = 0;
            foreach (var c in cs)
            {
                if (lastAdv == 0 && c.opcode == OpCodes.Ldstr && (string)c.operand == "ADVANCED SETTINGS")
                {
                    yield return new CodeInstruction(OpCodes.Ldloca_S, 0);
                    yield return new CodeInstruction(OpCodes.Dup);
                    yield return new CodeInstruction(OpCodes.Ldfld, vector2_y_Field);
                    yield return new CodeInstruction(OpCodes.Ldc_R4, 62f);
                    yield return new CodeInstruction(OpCodes.Add);
                    yield return new CodeInstruction(OpCodes.Stfld, vector2_y_Field);
                    lastAdv = 1;
                }
                else if ((lastAdv == 1 || lastAdv == 2) && c.opcode == OpCodes.Call)
                {
                    lastAdv++;
                }
                else if (lastAdv == 3)
                {
                    if (c.opcode != OpCodes.Ldloca_S)
                        continue;
                    lastAdv = 4;
                    yield return new CodeInstruction(OpCodes.Ldloca_S, 0);
                    yield return new CodeInstruction(OpCodes.Dup);
                    yield return new CodeInstruction(OpCodes.Ldfld, vector2_y_Field);
                    yield return new CodeInstruction(OpCodes.Ldc_R4, 62f - 93f);
                    yield return new CodeInstruction(OpCodes.Add);
                    yield return new CodeInstruction(OpCodes.Stfld, vector2_y_Field);
                }
                yield return c;
            }
        }

    }

    [HarmonyPatch(typeof(MenuManager), "MpMatchSetup")]
    class MPTeamsMenuHandle
    {
        static void HandleTeamCount()
        {
            if (MenuManager.m_menu_sub_state == MenuSubState.ACTIVE &&
                MenuManager.m_menu_micro_state == 2 &&
                UIManager.m_menu_selection == 8)
            {
                MPTeams.MenuManagerTeamCount = MPTeams.Min +
                    (MPTeams.MenuManagerTeamCount - MPTeams.Min + (MPTeams.Max - MPTeams.Min + 1) + UIManager.m_select_dir) %
                    (MPTeams.Max - MPTeams.Min + 1);
                MenuManager.PlayCycleSound(1f, (float)UIManager.m_select_dir);
            }
        }

        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> codes)
        {
            var mpTeamsMenuHandle_HandleTeamCount_Method = AccessTools.Method(typeof(MPTeamsMenuHandle), "HandleTeamCount");

            foreach (var code in codes)
            {
                if (code.opcode == OpCodes.Call && ((MethodInfo)code.operand).Name == "MaybeReverseOption")
                {
                    yield return code;
                    yield return new CodeInstruction(OpCodes.Call, mpTeamsMenuHandle_HandleTeamCount_Method);
                    continue;
                }

                yield return code;
            }
        }
    }

    [HarmonyPatch(typeof(PlayerShip), "UpdateShipColors")]
    class MPTeamsShipColors
    {
        static void Prefix(ref MpTeam team, ref int glow_color, ref int decal_color)
        {
            if (team == MpTeam.ANARCHY)
                return;
            glow_color = decal_color = MPTeams.TeamColorIdx(team);
            team = MpTeam.ANARCHY; // prevent original team color assignment
        }
    }

    /// <summary>
    // If ScaleRespawnTime is set, automatically set respawn timer = player's team count
    /// </summary>
    [HarmonyPatch(typeof(PlayerShip), "DyingUpdate")]
    class MPTeams_PlayerShip_DyingUpdate
    {
        static void MaybeUpdateDeadTimer(PlayerShip playerShip)
        {
            if (MPModPrivateData.ScaleRespawnTime)
            {
                playerShip.m_dead_timer = Overload.NetworkManager.m_Players.Count(x => x.m_mp_team == playerShip.c_player.m_mp_team && !x.m_spectator);
            }
        }

        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> codes)
        {
            var playerShip_m_dead_timer_Field = AccessTools.Field(typeof(PlayerShip), "m_dead_timer");
            var mpTeams_PlayerShip_DyingUpdate_Method = AccessTools.Method(typeof(MPTeams_PlayerShip_DyingUpdate), "MaybeUpdateDeadTimer");

            int state = 0;
            foreach (var code in codes)
            {
                if (state == 0 && code.opcode == OpCodes.Stfld && code.operand == playerShip_m_dead_timer_Field)
                {
                    state = 1;
                    yield return code;
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    yield return new CodeInstruction(OpCodes.Call, mpTeams_PlayerShip_DyingUpdate_Method);
                    continue;
                }

                yield return code;
            }
        }
    }

    // still missing: chat colors...
}
