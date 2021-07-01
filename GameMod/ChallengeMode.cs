using Harmony;
using Newtonsoft.Json;
using Overload;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace GameMod
{
    class ChallengeMode
    {
    }

    class CMChallengeMenu
    {

        public static string ChallengeId = "No Challenge";

    }

    public class Challenge
    {
        public int ChallengeId { get; set; }
        public string EventName { get; set; }
        public string LevelName { get; set; }
        public string CRC { get; set; }
        public int DifficultyId { get; set; }
        public bool CountdownMode { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string Leader { get; set; }
        public int Score { get; set; }
        public bool HasPassword { get; set; }
        public string TimeLeft
        {
            get
            {
                TimeSpan ts = EndDate.Subtract(DateTime.Now);
                if (ts.Days > 1)
                {
                    return string.Format("{0}d, {1}h", ts.Days, ts.Hours);
                }
                else
                {
                    return string.Format("{0}h{1}m", ts.Hours, ts.Minutes);
                }
            }
        }
    }

    /// <summary>
    /// Author: Tobias
    /// Created: 2019-06-03
    /// Email: tobiasksu@gmail.com
    /// </summary>
    [HarmonyPatch(typeof(UIElement), "DrawChallengeLevelSelectMenu")]
    class CMDrawChallengeLevelSelectMenu
    {

        private static void DrawChallengeLevelSelectMenuOCR(UIElement uie, ref Vector2 position)
        {
            position.x += 252f;
            uie.SelectAndDrawStringOptionItem("Open Challenges", position, 1, CMChallengeMenu.ChallengeId, string.Empty);
        }

        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> codes)
        {
            foreach (var code in codes)
            {
                // Replace Twitch Tournaments button
                if (code.opcode == OpCodes.Ldstr && (string)code.operand == "TWITCH TOURNAMENTS")
                    code.operand = "OCR CHALLENGES";
                
                yield return code;
            }
        }
    }

    class CMMenuManager
    {
        public static List<Challenge> challenges = new List<Challenge>();
        public static int selected_mission_index = 0;
        public static int selected_difficulty_index = 0;
        public static bool selected_countdown = true;
        public static string selected_pw = "";
        public static string selected_title = "";
        public static string selected_startDate = DateTime.Now.ToString("yyyy-MM-dd");
        public static string selected_endDate = DateTime.Now.AddDays(1).ToString("yyyy-MM-dd");
        public static bool selected_public = true;
        public static int selected_challenge = 0;
        public static bool need_challenge_refresh = true;
        public static bool is_data_refreshing = false;

        public static string selected_public_text
        {
            get
            {
                return selected_public ? "PUBLIC" : "PRIVATE";
            }
        }

        public static string selected_mode
        {
            get
            {
                return selected_countdown ? "COUNTDOWN" : "INFINITE";
            }
        }

        public static bool ProcessInputField(ref string s, string title, bool hide)
        {
            string inputString = Input.inputString;
            foreach (char c in inputString)
            {
                switch (c)
                {
                    case '\b':
                        if (s.Length != 0)
                        {
                            s = s.Substring(0, s.Length - 1);
                            SFXCueManager.PlayRawSoundEffect2D(SoundEffect.hud_notify_bot_died, 0.4f, UnityEngine.Random.Range(-0.15f, -0.05f));
                        }
                        break;
                    case '\n':
                    case '\r':
                        AccessTools.Field(typeof(MenuManager), "m_menu_state_timer").SetValue(typeof(float), 0f);
                        s = s.Trim();
                        MenuManager.SetDefaultSelection((MenuManager.m_menu_micro_state != 1) ? 1 : 0);
                        MenuManager.PlayCycleSound();
                        return true;
                    default:
                        if (MenuManager.IsPrintableChar(c))
                        {
                            s += c;
                            SFXCueManager.PlayRawSoundEffect2D(SoundEffect.hud_notify_bot_died, 0.5f, UnityEngine.Random.Range(0.1f, 0.2f));
                        }
                        break;
                }
            }
            return false;
        }

        public static IEnumerator PostEvent()
        {

            string savedPasswordHash = "";
            if (!String.IsNullOrEmpty(CMMenuManager.selected_pw))
            {
                byte[] salt;
                new RNGCryptoServiceProvider().GetBytes(salt = new byte[16]);
                var pbkdf2 = new Rfc2898DeriveBytes(CMMenuManager.selected_pw, salt, 10000);
                byte[] hash = pbkdf2.GetBytes(20);
                byte[] hashBytes = new byte[36];
                Array.Copy(salt, 0, hashBytes, 0, 16);
                Array.Copy(hash, 0, hashBytes, 16, 20);
                savedPasswordHash = Convert.ToBase64String(hashBytes);
            }

            var eventData = new PostEvent
            {
                EventName = CMMenuManager.selected_title,
                LevelName = GameManager.ChallengeMission.GetLevelDisplayName(CMMenuManager.selected_mission_index),
                LevelCrc = GameManager.ChallengeMission.GetAddOnLevelIdStringHash(CMMenuManager.selected_mission_index),
                DifficultyId = CMMenuManager.selected_difficulty_index,
                IsCountdown = CMMenuManager.selected_countdown,
                StartDate = DateTime.Parse(CMMenuManager.selected_startDate),
                EndDate = DateTime.Parse(CMMenuManager.selected_endDate),
                Password = savedPasswordHash
            };

            uConsole.Log(eventData.ToString());

            //using (UnityWebRequest www = UnityWebRequest.Put("http://localhost:61323/uploadchallenge", JsonConvert.SerializeObject(eventData)))
            using (UnityWebRequest www = UnityWebRequest.Put("http://ocr.tobiasksu.net/uploadchallenge", JsonConvert.SerializeObject(eventData)))
            {
                uConsole.Log(JsonConvert.SerializeObject(eventData));
                www.SetRequestHeader("Content-Type", "application/json");
                yield return www.SendWebRequest();

                if (www.isHttpError)
                {
                    uConsole.Log(www.error);
                }
                else
                {
                    uConsole.Log(www.downloadHandler.text);
                    CMMenuManager.need_challenge_refresh = true;
                }
            }
        }
        public static IEnumerator GetOcrChallenges()
        {

            //uConsole.Log("Downloading olmod CM scores for " + levelName + ", Difficulty: " + difficultyLevelId.ToString() + ", Countdown: " + isCountdown.ToString());
            //string url = "http://localhost:61323/challengelist";
            string url = "http://ocr.tobiasksu.net/challengelist";

            CMMenuManager.need_challenge_refresh = false;
            CMMenuManager.is_data_refreshing = true;

            UnityWebRequest www = UnityWebRequest.Get(url);
            yield return www.SendWebRequest();

            if (www.isNetworkError || www.isHttpError)
            {
                uConsole.Log(www.error);
            }
            else
            {
                // Show results as text
                List<Challenge> results = JsonConvert.DeserializeObject<List<Challenge>>(www.downloadHandler.text);

                CMMenuManager.challenges = results.Select(x => new Challenge
                {
                    ChallengeId = x.ChallengeId,
                    EventName = x.EventName,
                    LevelName = x.LevelName,
                    CRC = x.CRC,
                    CountdownMode = x.CountdownMode,
                    DifficultyId = x.DifficultyId,
                    StartDate = x.StartDate,
                    EndDate = x.EndDate,
                    HasPassword = x.HasPassword,
                    Leader = x.Leader,
                    Score = x.Score
                }).ToList();

                uConsole.Log(www.downloadHandler.text);
                CMMenuManager.is_data_refreshing = false;
            }
        }
    }

    /// <summary>
    /// Author: Tobias
    /// Created: 2019-06-03
    /// Email: tobiasksu@gmail.com
    /// </summary>
    [HarmonyPatch(typeof(MenuManager), "GameOnUpdate")]
    class GameOnUpdate
    {

        public static bool Prefix()
        {
            UIManager.MouseSelectUpdate();
            float m_menu_state_timer = (float)AccessTools.Field(typeof(MenuManager), "m_menu_state_timer").GetValue(null);
            if (CMMenuManager.need_challenge_refresh && !CMMenuManager.is_data_refreshing)
                GameManager.m_gm.StartCoroutine(CMMenuManager.GetOcrChallenges());

            switch (MenuManager.m_menu_sub_state)
            {
                case MenuSubState.INIT:
                    if (m_menu_state_timer > 0.25f)
                    {
                        UIManager.CreateUIElement(UIManager.SCREEN_CENTER, 7000, UIElementType.GAMEON_MENU);
                        GameManager.m_gm.StartCoroutine(CMMenuManager.GetOcrChallenges());
                        MenuManager.m_menu_sub_state = MenuSubState.ACTIVE;
                        MenuManager.SetDefaultSelection(0);
                        MenuManager.m_menu_micro_state = 1;
                    }
                    break;
                case MenuSubState.ACTIVE:
                    UIManager.ControllerMenu();

                    switch (UIManager.m_menu_selection) // Process text inputs
                    {
                        case 211:
                            Controls.m_disable_menu_letter_keys = true;
                            CMMenuManager.ProcessInputField(ref CMMenuManager.selected_title, "CHALLENGE TITLE", hide: false);
                            break;
                        case 212:
                            Controls.m_disable_menu_letter_keys = true;
                            CMMenuManager.ProcessInputField(ref CMMenuManager.selected_startDate, "START DATE", hide: false);
                            break;
                        case 213:
                            Controls.m_disable_menu_letter_keys = true;
                            CMMenuManager.ProcessInputField(ref CMMenuManager.selected_endDate, "END DATE", hide: false);
                            break;
                        case 205:
                            Controls.m_disable_menu_letter_keys = true;
                            CMMenuManager.ProcessInputField(ref CMMenuManager.selected_pw, "PASSWORD", hide: false);
                            break;
                    }

                    if (!UIManager.PushedSelect(100))
                    {
                        break;
                    }

                    // Handle "play" clicks
                    if (UIManager.m_menu_selection > 10000)
                    {
                        int ChallengeId = UIManager.m_menu_selection - 10000;
                        Challenge challenge = CMMenuManager.challenges.FirstOrDefault(x => x.ChallengeId == ChallengeId);
                        ChallengeManager.CountdownMode = challenge.CountdownMode;
                        ChallengeManager.m_player_has_selected_weapons = true;
                        GameManager.difficulty_level = challenge.DifficultyId;
                        GameplayManager.DifficultyLevel = challenge.DifficultyId;
                        CMMenuManager.selected_challenge = challenge.ChallengeId;

                        uConsole.Log("Play challenge " + challenge.ChallengeId.ToString());
                        //List<LevelInfo> __m_mission_list = (List<LevelInfo>)AccessTools.Field(typeof(Mission), "Levels").GetValue(null);
                        int _levelNum = 0;
                        for (var i = 0; i < GameManager.ChallengeMission.NumLevels; i++)
                        {
                            if (GameManager.ChallengeMission.GetLevelDisplayName(i) == challenge.LevelName)
                            {
                                _levelNum = i;
                                break;
                            }
                        }
                        UIManager.DestroyAll();
                        GameplayManager.CreateNewGame(MenuManager.ChallengeMission, _levelNum);
                        MenuManager.ChangeMenuState(MenuState.CHALLENGE_BRIEFING);
                        break;
                    }

                    Controls.m_disable_menu_letter_keys = false;

                    switch (UIManager.m_menu_selection)
                    {
                        case 2:
                            if (m_menu_state_timer > 0.25f)
                            {
                                MenuManager.PlaySelectSound();
                                MenuManager.m_menu_micro_state = 2;
                            }
                            break;
                        case 5:
                            if (m_menu_state_timer > 0.25f)
                            {
                                MenuManager.PlaySelectSound();
                                CMMenuManager.selected_public = !CMMenuManager.selected_public;
                            }
                            break;
                        case 102: // Main back
                            if (m_menu_state_timer > 0.25f)
                            {
                                MenuManager.PlaySelectSound();
                                UIManager.DestroyAll();
                                MenuManager.m_menu_sub_state = MenuSubState.BACK;
                            }
                            break;
                        case 202: // Level select
                            if (m_menu_state_timer > 0.25f)
                            {
                                MenuManager.PlaySelectSound();
                                CMMenuManager.selected_mission_index = (CMMenuManager.selected_mission_index + GameManager.ChallengeMission.NumLevels + UIManager.m_select_dir) % GameManager.ChallengeMission.NumLevels;
                            }
                            break;
                        case 203: // Difficulty Level
                            if (m_menu_state_timer > 0.25f)
                            {
                                MenuManager.PlaySelectSound();
                                CMMenuManager.selected_difficulty_index = (CMMenuManager.selected_difficulty_index + 6 + UIManager.m_select_dir) % 6;
                            }
                            break;
                        case 204: // Countdown/Infinite
                            if (m_menu_state_timer > 0.25f)
                            {
                                MenuManager.PlaySelectSound();
                                CMMenuManager.selected_countdown = !CMMenuManager.selected_countdown;
                            }
                            break;
                        case 209:
                            if (m_menu_state_timer > 0.25f)
                            {

                                GameManager.m_gm.StartCoroutine(CMMenuManager.PostEvent());
                                MenuManager.PlaySelectSound();
                                UIManager.m_menu_selection = 1;
                                MenuManager.m_menu_micro_state = 1;
                            }
                            break;
                        case 210: // Create challenge back
                            if (m_menu_state_timer > 0.25f)
                            {
                                uConsole.Log("case 210 menu back");
                                MenuManager.PlaySelectSound();
                                UIManager.m_menu_selection = 1;
                                MenuManager.m_menu_micro_state = 1;
                            }
                            break;
                    }
                    break;
                case MenuSubState.BACK:
                    if (m_menu_state_timer > 0.25f)
                    {
                        MenuManager.PlaySelectSound();
                        MenuManager.ChangeMenuState(MenuState.CHALLENGE_SELECT);
                    }
                    break;
                case MenuSubState.START:
                    if (m_menu_state_timer > 0.25f)
                    {
                        MenuManager.PlaySelectSound();
                        MenuManager.ChangeMenuState(MenuState.CHALLENGE_BRIEFING);
                    }
                    break;
            }
            return false;
        }
    }

    // Fix physics bug when switching between CM and MP
    [HarmonyPatch(typeof(GameManager), "Update")]
    class GameManagerUpdate
    {

        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> codes)
        {
            int state = 0;
            foreach (var code in codes)
            {
                if (state == 0 && code.opcode == OpCodes.Ldc_R4 && (float)code.operand == 0.0166666675f)
                {
                    state = 1;
                }
                else if (state == 1 && code.opcode == OpCodes.Stsfld && code.operand == AccessTools.Field(typeof(GameManager), "m_fixed_frametime_low_hz"))
                {
                    state = 2;
                }
                else if (state == 2)
                {
                    state = 3;
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    yield return new CodeInstruction(OpCodes.Ldc_R4, 60f);
                    yield return new CodeInstruction(OpCodes.Stfld, AccessTools.Field(typeof(GameManager), "m_last_fixed_fps"));
                }
                yield return code;
            }
        }
    }
    
    public class PostEvent
    {
        public string EventName { get; set; }
        public string LevelName { get; set; }
        public string LevelCrc { get; set; }
        public int DifficultyId { get; set; }
        public bool IsCountdown { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string Password { get; set; }
    }

    /// <summary>
    /// Author: Tobias
    /// Created: 2019-06-03
    /// Email: tobiasksu@gmail.com
    /// </summary>
    [HarmonyPatch(typeof(Overload.Platform), "Init")]
    class CMLeaderboard
    {

        [HarmonyPostfix]
        static void Postfix()
        {
            AccessTools.Field(typeof(Platform), "CloudProvider").SetValue(null, 4);
        }

    }

    [HarmonyPatch(typeof(Platform))]
    [HarmonyPatch("UserName", PropertyMethod.Getter)]
    public static class CMUserName
    {
        [HarmonyPostfix]
        public static void UserName(ref string __result)
        {
            __result = PilotManager.PilotName;
        }
    }

    [HarmonyPatch(typeof(Platform))]
    [HarmonyPatch("PlatformName", PropertyMethod.Getter)]
    public static class CMPlatformName
    {
        [HarmonyPostfix]
        public static void PlatformName(ref string __result)
        {
            __result = "OLMOD";
        }
    }

    [HarmonyPatch(typeof(Platform))]
    [HarmonyPatch("StatsAvailable", PropertyMethod.Getter)]
    public static class CMPlatformStatsAvailable
    {
        [HarmonyPostfix]
        public static void StatsAvailable(ref bool __result)
        {
            __result = true;
        }
    }

    [HarmonyPatch(typeof(Platform), "GetLeaderboardData")]
    public static class CMGetLeaderboardData
    {
        [HarmonyPostfix]
        public static void GetLeaderboardData(ref LeaderboardEntry[] __result, out int leaderboard_length, out int user_index, out Platform.LeaderboardDataState result)
        {
            user_index = -1;
            leaderboard_length = 0;

            if (m_download_state == DownloadState.NoLeaderboard)
            {
                result = Platform.LeaderboardDataState.NoLeaderboard;
                __result = null;
                return;
            }
            if (m_download_state == DownloadState.RetryFromStart)
            {
                m_request_start = 1;
                try
                {
                    Platform.RequestChallengeLeaderboardData(MenuManager.ChallengeMission.DisplayName, MenuManager.m_leaderboard_challenge_countdown, MenuManager.m_leaderboard_difficulty, 1, m_request_num_entries - 1, false);
                    m_download_state = DownloadState.WaitingForData;
                    result = Platform.LeaderboardDataState.Waiting;
                }
                catch (Exception ex)
                {
                    Debug.Log("Error requesting olmod leaderboard entries: " + ex.Message);
                    m_download_state = DownloadState.NoLeaderboard;
                    result = Platform.LeaderboardDataState.NoLeaderboard;
                }
                __result = null;
                return;
            }
            if (m_download_state != DownloadState.HaveData)
            {
                result = Platform.LeaderboardDataState.Waiting;
                __result = null;
                return;
            }

            LeaderboardEntry[] array = new LeaderboardEntry[m_entries.Length];
            for (int i = 0; i < m_entries.Length; i++)
            {
                if (m_entries[i].m_name == null)
                {
                    m_entries[i].m_name = "m_name here";
                    m_entries[i].m_rank = i + 1;
                }
                array[i] = m_entries[i];
            }
            leaderboard_length = m_entries.Length;
            result = Platform.LeaderboardDataState.HaveData;
            //uConsole.Log("GetLeaderboardData.Length: " + array.Length.ToString());
            __result = array;
        }

        public static DownloadState m_download_state = DownloadState.RetryFromStart;
        public static int m_request_start;
        public static int m_request_num_entries = 0;
        public static int m_leaderboard_length = 0;
        public static LeaderboardEntry[] m_entries;

        public enum DownloadState
        {
            WaitingForData,
            NoLeaderboard,
            RetryFromStart,
            HaveData
        }
    }

    [HarmonyPatch(typeof(Platform), "RequestChallengeLeaderboardData")]
    public static class CMRequestChallengeLeaderboardData
    {
        [HarmonyPostfix]
        public static CloudDataYield RequestChallengeLeaderboardData(CloudDataYield __result, string level_name, bool submode, int difficulty_level, int range_start, int num_entries, bool friends)
        {
            //uConsole.Log("Custom RequestChallengeLeaderboardData");
            string challengeLeaderboardName = "challenge:" + ChallengeManager.GetChallengeSubModeName(submode, dont_localize: true) + ":" + difficulty_level.ToString() + MenuManager.GetDifficultyLevelName(difficulty_level, dont_localize: true).ToLower() + ":" + level_name;
            CMGetLeaderboardData.m_download_state = CMGetLeaderboardData.DownloadState.WaitingForData;
            GameManager.m_gm.StartCoroutine(DownloadLeaderboard(level_name, submode, difficulty_level));
            CloudDataYield cdy = new CloudDataYield(() => CMGetLeaderboardData.m_download_state != CMGetLeaderboardData.DownloadState.HaveData && CMGetLeaderboardData.m_download_state != CMGetLeaderboardData.DownloadState.NoLeaderboard);

            __result = cdy;
            return null;
        }

        static IEnumerator DownloadLeaderboard(string levelName, bool isCountdown, int difficultyLevelId)
        {
            //uConsole.Log("Downloading olmod CM scores for " + levelName + ", Difficulty: " + difficultyLevelId.ToString() + ", Countdown: " + isCountdown.ToString());
            //string url = "http://localhost:61323/getleaderboardresults?levelname=" + levelName + "&difficultylevelid=" + difficultyLevelId.ToString() + "&iscountdown=" + isCountdown.ToString();
            string url = "http://ocr.tobiasksu.net/getleaderboardresults?levelname=" + levelName + "&difficultylevelid=" + difficultyLevelId.ToString() + "&iscountdown=" + isCountdown.ToString();

            UnityWebRequest www = UnityWebRequest.Get(url);
            yield return www.SendWebRequest();

            if (www.isNetworkError || www.isHttpError)
            {
                uConsole.Log(www.error);
            }
            else
            {
                // Show results as text
                List<OcrResult> results = JsonConvert.DeserializeObject<List<OcrResult>>(www.downloadHandler.text);
                //uConsole.Log("RequestChallengeLeaderboardData() found " + results.Count.ToString() + " results");

                CMGetLeaderboardData.m_entries = results.Select(x => new LeaderboardEntry
                {
                    m_data_is_valid = true,
                    m_favorite_weapon = x.FavoriteWeapon,
                    m_game_time = (int)Math.Round(x.AliveTime),
                    m_kills = x.RobotsDestroyed,
                    m_name = x.PlayerName,
                    m_rank = 1,
                    m_score = x.Score,
                    m_time_stamp = DateTime.Now
                }).ToArray();
                CMGetLeaderboardData.m_download_state = CMGetLeaderboardData.DownloadState.HaveData;
                uConsole.Log(www.downloadHandler.text);
            }
        }
    }

    /// <summary>
    /// Author: Tobias
    /// Created: 2019-06-03
    /// Email: tobiasksu@gmail.com
    /// </summary>
    [HarmonyPatch(typeof(UIElement), "DrawGameOnMenu")]
    class DrawGameOnMenu
    {

        public static bool Prefix(UIElement __instance)
        {
            UIElement uie = __instance;
            float m_menu_state_timer = (float)AccessTools.Field(typeof(MenuManager), "m_menu_state_timer").GetValue(null);
            UIManager.ui_bg_dark = true;
            Vector2 position = uie.m_position;
            uie.DrawMenuBG();
            switch (MenuManager.m_menu_micro_state)
            {
                case 1: // Leaderboard
                    uie.DrawHeaderMedium(Vector2.up * (UIManager.UI_TOP + 20f), Loc.LS("OCR CHALLENGES"));
                    position.y = UIManager.UI_TOP + 84f;
                    uie.SelectAndDrawStringOptionItem("EVENT LISTING", position, 5, CMMenuManager.selected_public_text);
                    position.y += 64f;
                    float d = -600f; // Title
                    float d2 = -250f; // Level
                    float d3 = -30f; // Difficulty
                    float d4 = 120f; // Time Left
                    float d5 = 250f; // Mode
                    float d6 = 410f; // Leader
                    float d7 = 520f; // Score
                    float d8 = 580f; // Play
                    uie.DrawStringSmall(Loc.LS("TITLE"), position + Vector2.right * d, 0.6f, StringOffset.LEFT, UIManager.m_col_hi2, 1f);
                    uie.DrawStringSmall(Loc.LS("LEVEL"), position + Vector2.right * d2, 0.6f, StringOffset.CENTER, UIManager.m_col_hi2, 1f);
                    uie.DrawStringSmall(Loc.LS("DIFFICULTY"), position + Vector2.right * d3, 0.6f, StringOffset.CENTER, UIManager.m_col_hi2, 1f);
                    uie.DrawStringSmall(Loc.LS("TIME LEFT"), position + Vector2.right * d4, 0.6f, StringOffset.CENTER, UIManager.m_col_hi2, 1f);
                    uie.DrawStringSmall(Loc.LS("MODE"), position + Vector2.right * d5, 0.6f, StringOffset.CENTER, UIManager.m_col_hi2, 1f);
                    uie.DrawStringSmall("LEADER", position + Vector2.right * d6, 0.6f, StringOffset.CENTER, UIManager.m_col_hi2, 1f);
                    uie.DrawStringSmall("SCORE", position + Vector2.right * d7, 0.6f, StringOffset.CENTER, UIManager.m_col_hi2, 1f);
                    position.y += 30f;
                    uie.DrawVariableSeparator(position, 500f);
                    var events = CMMenuManager.challenges.Where(x => x.HasPassword != CMMenuManager.selected_public).ToList();
                    for (var i = 0; i < events.Count; i++)
                    {
                        position.y += 30f;
                        if (i % 2 == 0)
                        {
                            UIManager.DrawQuadUI(position, 600f, 10f, UIManager.m_col_ub0, uie.m_alpha * 0.1f, 13);
                        }
                        Color col_ui = UIManager.m_col_ui3;
                        uie.DrawStringSmall(events[i].EventName, position + Vector2.right * d, 0.6f, StringOffset.LEFT, col_ui, 1f);
                        uie.DrawStringSmall(events[i].LevelName, position + Vector2.right * d2, 0.6f, StringOffset.CENTER, col_ui, 1f);
                        uie.DrawStringSmall(MenuManager.DifficultyLevelName[events[i].DifficultyId], position + Vector2.right * d3, 0.6f, StringOffset.CENTER, col_ui, 1f);
                        UIManager.m_fixed_width_digits = true;
                        uie.DrawStringSmall(events[i].TimeLeft, position + Vector2.right * d4, 0.6f, StringOffset.CENTER, col_ui, 1f);
                        uie.DrawStringSmall(ChallengeManager.GetChallengeSubModeName(events[i].CountdownMode, true), position + Vector2.right * d5, 0.6f, StringOffset.CENTER, col_ui, 1f);
                        uie.DrawStringSmall(events[i].Leader, position + Vector2.right * d6, 0.6f, StringOffset.CENTER, col_ui, 1f);
                        uie.DrawStringSmall(String.Format("{0:n0}", events[i].Score), position + Vector2.right * d7, 0.6f, StringOffset.CENTER, col_ui, 1f);
                        uie.SelectAndDrawTextOnlyItem("PLAY", position + Vector2.right * d8, 10000 + events[i].ChallengeId, 0.6f, StringOffset.LEFT, fade: false);
                        UIManager.m_fixed_width_digits = false;
                    }
                    position.y = UIManager.UI_BOTTOM - 94f;
                    uie.SelectAndDrawItem(Loc.LS("CREATE CHALLENGE"), position, 2, fade: false);
                    position.y = UIManager.UI_BOTTOM - 30f;
                    uie.SelectAndDrawItem(Loc.LS("RETURN TO CM MENU"), position, 102, fade: false);
                    break;
                case 2: // Create Challenge
                    uie.DrawHeaderMedium(Vector2.up * (UIManager.UI_TOP + 20f), Loc.LS("CHALLENGE OPTIONS"));
                    position.y = UIManager.UI_TOP + 84f;
                    uie.SelectAndDrawStringOptionItem("LEVEL", position, 202, GameManager.ChallengeMission.GetLevelDisplayName(CMMenuManager.selected_mission_index), string.Empty, 1.5f, false);
                    position.y += 55f;
                    uie.SelectAndDrawStringOptionItem("DIFFICULTY", position, 203, MenuManager.DifficultyLevelName[CMMenuManager.selected_difficulty_index], string.Empty, 1.5f, false);
                    position.y += 55f;
                    uie.SelectAndDrawStringOptionItem("MODE", position, 204, CMMenuManager.selected_mode, string.Empty, 1.5f, false);
                    position.y += 70f;
                    uie.DrawTextEntryLabeled(position, 211, "CHALLENGE TITLE", CMMenuManager.selected_title, false);
                    position.y += 55f;
                    uie.DrawTextEntryLabeled(position, 212, "START DATE", CMMenuManager.selected_startDate, false);
                    position.y += 55f;
                    uie.DrawTextEntryLabeled(position, 213, "END DATE", CMMenuManager.selected_endDate, false);
                    position.y += 55f;
                    uie.DrawTextEntryLabeled(position, 205, "PASSWORD", CMMenuManager.selected_pw, true);
                    position.y = UIManager.UI_BOTTOM - 280f;
                    uie.SelectAndDrawItem(Loc.LS("PRIMARY LOADOUTS"), position, 206, fade: true);
                    position.y += 55f;
                    uie.SelectAndDrawItem(Loc.LS("SECONDARY LOADOUTS"), position, 207, fade: true);
                    position.y += 55f;
                    uie.SelectAndDrawItem(Loc.LS("ADVANCED OPTIONS"), position, 208, fade: true);
                    position.y += 70f;
                    uie.SelectAndDrawItem(Loc.LS("CREATE"), position, 209, fade: false);
                    position.y += 55f;
                    uie.SelectAndDrawItem(Loc.LS("BACK"), position, 210, fade: false);
                    break;
                case 3: // Primary Loadouts
                    break;
                case 4: // Secondary Loadouts
                    break;
                case 5: // Advanced Options
                    break;
                case 102:
                    MenuManager.PlaySelectSound();
                    m_menu_state_timer = 0f;
                    UIManager.DestroyAll();
                    MenuManager.m_menu_sub_state = MenuSubState.BACK;
                    break;
            }
            return false;
        }
    }

    /// <summary>
    /// Author: Tobias
    /// Created: 2019-05-30
    /// Email: tobiasksu@gmail.com
    /// </summary>
    [HarmonyPatch(typeof(Overload.GameplayManager), "DoneLevel")]
    class CMUpload : MonoBehaviour
    {

        // Update local score stats if using olmod
        static void UpdateChallengeScore()
        {
            if (GameplayManager.m_i_am_a_cheater || GameManager.m_cheating_detected)
            {
                if (GameplayManager.IsChallengeMode && GameplayManager.m_level_info.Mission.FileName != "_EDITOR" && (int)ChallengeManager.ChallengeRobotsDestroyed > 0)
                {
                    Scores.UpdateChallengeScore(GameplayManager.m_level_info.LevelNum, GameplayManager.DifficultyLevel, ChallengeManager.CountdownMode, PilotManager.PilotName, ChallengeManager.ChallengeScore, ChallengeManager.ChallengeRobotsDestroyed, GameplayManager.MostDamagingWeapon(), GameplayManager.AliveTime);
                }
            }
        }

        static void Postfix(GameplayManager.DoneReason reason)
        {
            if (!GameplayManager.m_i_am_a_cheater)
            {
                if (GameplayManager.IsChallengeMode && GameplayManager.m_level_info.Mission.FileName != "_EDITOR" && (int)ChallengeManager.ChallengeRobotsDestroyed > 0)
                {
                    GameManager.m_gm.StartCoroutine(PostScore());
                }
            }
        }

        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> codes)
        {
            foreach (var code in codes)
            {
                if (code.opcode == OpCodes.Call && code.operand == AccessTools.Method(typeof(Platform), "StoreStats"))
                {
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(CMUpload), "UpdateChallengeScore")) { labels = code.labels };
                    code.labels = null;
                }
                yield return code;
            }
        }

        static IEnumerator PostScore()
        {

            var cmdata = new OcrResult
            {
                ChallengeId = CMMenuManager.selected_challenge,
                PlayerName = Platform.UserName,
                RobotsDestroyed = (int)ChallengeManager.ChallengeRobotsDestroyed,
                AliveTime = GameplayManager.AliveTime,
                Score = (int)ChallengeManager.ChallengeScore,
                LevelName = GameplayManager.Level.DisplayName,
                Crc = GameplayManager.Level.GetHashCode().ToString("X"),
                CountdownMode = ChallengeManager.CountdownMode,
                FavoriteWeapon = GameplayManager.MostDamagingWeapon(),
                DifficultyLevel = (int)GameplayManager.DifficultyLevel,
                KillerId = GameplayManager.m_stats_player_killer,
                SmashDamage = GameplayManager.m_robot_cumulative_damage_by_other_type[0],
                SmashKills = GameplayManager.m_other_killer[0],
                AutoOpDamage = GameplayManager.m_robot_cumulative_damage_by_other_type[1],
                AutoOpKills = GameplayManager.m_other_killer[1],
                SelfDamage = GameplayManager.m_stats_player_self_damage,
                RobotStats = new List<OcrResultRobotStats>(),
                PrimaryStats = new List<OcrResultPrimaryStats>(),
                SecondaryStats = new List<OcrResultSecondaryStats>()
            };

            string[] enemyTypes = Enum.GetNames(typeof(EnemyType));
            for (int i = 0; i < GameplayManager.m_player_cumulative_damage_by_robot_type.Length; i++)
            {
                if (GameplayManager.m_player_cumulative_damage_by_robot_type[i] != 0)
                {
                    cmdata.RobotStats.Add(new OcrResultRobotStats
                    {
                        EnemyTypeId = i,
                        IsSuper = false,
                        DamageReceived = GameplayManager.m_player_cumulative_damage_by_robot_type[i],
                        DamageDealt = 0,
                        NumKilled = GameplayManager.m_robots_killed.Count(x => x.robot_type == (EnemyType)i)
                    });
                }

                // Add supers
                if (GameplayManager.m_player_cumulative_damage_by_super_robot_type[i] != 0)
                {
                    cmdata.RobotStats.Add(new OcrResultRobotStats
                    {
                        EnemyTypeId = i,
                        IsSuper = true,
                        DamageReceived = GameplayManager.m_player_cumulative_damage_by_super_robot_type[i],
                        DamageDealt = 0,
                        NumKilled = GameplayManager.m_super_robots_killed.Count(x => x.robot_type == (EnemyType)i)
                    });
                }
            }


            for (int i = 0; i < 8; i++)
            {
                if (GameplayManager.m_robot_cumulative_damage_by_weapon_type[i] != 0 || GameplayManager.m_weapon_killer[i] != 0)
                {
                    cmdata.PrimaryStats.Add(new OcrResultPrimaryStats
                    {
                        PrimaryTypeId = i,
                        DamageDealt = GameplayManager.m_robot_cumulative_damage_by_weapon_type[i],
                        NumKilled = GameplayManager.m_weapon_killer[i]
                    });
                }

            }

            for (int i = 0; i < 8; i++)
            {
                if (GameplayManager.m_robot_cumulative_damage_by_missile_type[i] != 0 || GameplayManager.m_missile_killer[i] != 0)
                {
                    cmdata.SecondaryStats.Add(new OcrResultSecondaryStats
                    {
                        SecondaryTypeId = i,
                        DamageDealt = GameplayManager.m_robot_cumulative_damage_by_missile_type[i],
                        NumKilled = GameplayManager.m_missile_killer[i]
                    });
                }
            }

            uConsole.Log("Posting olmod CM score");
            uConsole.Log(JsonConvert.SerializeObject(cmdata));

            //using (UnityWebRequest www = UnityWebRequest.Put("http://localhost:61323/uploadchallengeresults", JsonConvert.SerializeObject(cmdata)))
            using (UnityWebRequest www = UnityWebRequest.Put("http://ocr.tobiasksu.net/uploadchallengeresults", JsonConvert.SerializeObject(cmdata)))
            {
                uConsole.Log(JsonConvert.SerializeObject(cmdata));
                www.SetRequestHeader("Content-Type", "application/json");
                yield return www.SendWebRequest();

                if (www.isHttpError)
                {
                    uConsole.Log(www.error);
                }
                else
                {
                    CMMenuManager.need_challenge_refresh = true;
                    uConsole.Log(www.downloadHandler.text);
                }
            }
        }

    }

    public class OcrResult
    {

        public string PlayerName { get; set; }
        public string LevelName { get; set; }
        public string Crc { get; set; }
        public float AliveTime { get; set; }
        public int RobotsDestroyed { get; set; }
        public int Score { get; set; }
        public bool CountdownMode { get; set; }
        public int FavoriteWeapon { get; set; }
        public int DifficultyLevel { get; set; }
        public int KillerId { get; set; }
        public float SmashDamage { get; set; }
        public int SmashKills { get; set; }
        public float AutoOpDamage { get; set; }
        public int AutoOpKills { get; set; }
        public float SelfDamage { get; set; }
        public int? ChallengeId { get; set; }
        public List<OcrResultRobotStats> RobotStats { get; set; }
        public List<OcrResultPrimaryStats> PrimaryStats { get; set; }
        public List<OcrResultSecondaryStats> SecondaryStats { get; set; }
    }

    public class OcrResultRobotStats
    {
        public int EnemyTypeId { get; set; }
        public bool IsSuper { get; set; }
        public float DamageReceived { get; set; }
        public float DamageDealt { get; set; }
        public int NumKilled { get; set; }
    }

    public class OcrResultPrimaryStats
    {
        public int PrimaryTypeId { get; set; }
        public float DamageDealt { get; set; }
        public int NumKilled { get; set; }
    }

    public class OcrResultSecondaryStats
    {
        public int SecondaryTypeId { get; set; }
        public float DamageDealt { get; set; }
        public int NumKilled { get; set; }
    }

    public class OcrChallenge
    {
        public int ChallengeId { get; set; }
        public string EventName { get; set; }
        public string LevelName { get; set; }
        public string CRC { get; set; }
        public int DifficultyId { get; set; }
        public bool CountdownMode { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public bool HasPassword { get; set; }
        public string Leader { get; set; }
        public int Score { get; set; }
        public string TimeLeft
        {
            get
            {
                var dateDiff = EndDate.Subtract(StartDate);
                return string.Format("{0} days", (int)(dateDiff.TotalDays % 365) / 30);
            }
        }
    }

    // CM scores don't save locally to pilot.xscores because of cheat detection, disable
    [HarmonyPatch(typeof(PilotManager), "Save")]
    class ChallengeMode_PilotManager_Save
    {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> codes)
        {
            foreach (var code in codes)
            {
                if (code.opcode == OpCodes.Ldsfld && code.operand == AccessTools.Field(typeof(GameManager), "m_cheating_detected"))
                {
                    yield return new CodeInstruction(OpCodes.Ldc_I4_1);
                    continue;
                }
                yield return code;
            }
        }
    }
}
