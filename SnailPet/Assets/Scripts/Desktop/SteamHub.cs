using System;
using System.Collections.Generic;
using UnityEngine;
#if !DISABLESTEAMWORKS
using Steamworks;
#endif

namespace SnailPet.Desktop
{
    /// <summary>
    /// 스팀과 닿는 곳을 전부 모아 둔 창구.
    ///
    /// <b>스팀 호출은 이 파일 밖으로 새지 않게 한다.</b> 멀티를 접거나 다른 방식으로 갈아탈 때
    /// 이 파일 하나만 지우면 되고, 나머지 코드는 스팀을 모른 채로 남는다.
    ///
    /// 스팀이 안 떠 있어도 게임은 그대로 돌아가야 한다 — 데스크톱 펫이라 스팀 없이 켜는 것이
    /// 오히려 보통이다. 그래서 초기화 실패는 오류가 아니라 <see cref="Available"/> 가 거짓인
    /// 평범한 상태로 다룬다.
    ///
    /// 앱 ID 는 <c>steam_appid.txt</c> 가 정한다. 지금은 480(Spacewar) — 스팀이 공개해 둔
    /// 테스트용 앱이라 파트너 등록 없이 개발할 수 있다. 대신 <b>로비 목록이 전 세계 테스트와
    /// 섞이므로</b> 방을 만들 때 우리 표시를 박고 그걸로 거른다 (<see cref="GameKey"/>).
    ///
    /// 위치는 맞추지 않기로 했다(비동기). 그래서 P2P 패킷은 쓰지 않고 로비만 쓴다.
    /// </summary>
    public static class SteamHub
    {
        /// <summary>우리 방을 알아보는 표시. 480 을 같이 쓰는 남의 방을 걸러 낸다.</summary>
        private const string GameKey = "game", GameValue = "snailpet";
        private const string NameKey = "name";

        /// <summary>한 방에 들어갈 수 있는 인원. 목업의 max 5 다.</summary>
        public const int MaxMembers = 5;

        public static bool Available { get; private set; }
        public static string LastError { get; private set; }
        public static string MyName { get; private set; } = "";

        /// <summary>목록이나 방 상태가 바뀌었다. 화면을 다시 그리라는 뜻이다.</summary>
        public static event Action Changed;

        /// <summary>무슨 일이 있었는지 한 줄로. 부르는 쪽이 로그에 적는다.</summary>
        public static event Action<string> Note;

        /// <summary>방에 들어왔다. 받는 쪽이 자기 달팽이를 올린다.</summary>
        public static event Action Entered;

#if DISABLESTEAMWORKS
        public static bool InLobby => false;
        public static string LobbyName => "";
        public static string[] Friends() => new string[0];
        public static string[] Lobbies() => new string[0];
        public static string[] Members() => new string[0];

        public static bool Init() { LastError = "이 빌드에는 스팀이 빠져 있습니다"; return false; }
        public static void Pump() { }
        public static void Shutdown() { }
        public static void CreateLobby() { }
        public static void RefreshLobbies() { }
        public static void JoinLobby(int index) { }
        public static void JoinById(string text) { }
        public static void JoinRandom() { }
        public static void Leave() { }
        public static void Invite(int friendIndex) { }
        public static void PublishSnail(string look) { }
        public static (string name, string look, bool me)[] MemberLooks() => new (string, string, bool)[0];
#else
        private static readonly List<CSteamID> _friendIds = new List<CSteamID>();
        private static readonly List<CSteamID> _lobbyIds = new List<CSteamID>();
        private static readonly List<string> _lobbyNames = new List<string>();

        private static CSteamID _lobby;
        private static bool _joinRandomPending;

        private static CallResult<LobbyCreated_t> _onCreated;
        private static CallResult<LobbyMatchList_t> _onList;
        private static CallResult<LobbyEnter_t> _onEnter;
        private static Callback<LobbyChatUpdate_t> _onChat;
        private static Callback<LobbyDataUpdate_t> _onData;
        private static Callback<GameLobbyJoinRequested_t> _onInvited;

        public static bool InLobby => _lobby.IsValid();

        public static string LobbyName =>
            InLobby ? SteamMatchmaking.GetLobbyData(_lobby, NameKey) : "";

        public static bool Init()
        {
            if (Available) return true;

            if (!Packsize.Test())
            {
                LastError = "Steamworks.NET 이 이 플랫폼용이 아닙니다 (Packsize)";
                return false;
            }

            try
            {
                // 스팀이 안 떠 있거나 앱 ID 를 못 찾으면 false. 예외가 아니라 평범한 실패다.
                if (!SteamAPI.Init())
                {
                    LastError = "스팀이 실행 중이 아니거나 steam_appid.txt 를 못 읽었습니다";
                    return false;
                }
            }
            catch (DllNotFoundException e)
            {
                LastError = "steam_api64.dll 을 못 찾았습니다: " + e.Message;
                return false;
            }

            Available = true;
            MyName = SteamFriends.GetPersonaName();
            LastError = null;

            // 콜백은 만들어 두고 살려 둬야 한다. 지역 변수로 두면 GC 가 걷어 간다.
            _onCreated = CallResult<LobbyCreated_t>.Create(OnLobbyCreated);
            _onList    = CallResult<LobbyMatchList_t>.Create(OnLobbyList);
            _onEnter   = CallResult<LobbyEnter_t>.Create(OnLobbyEnter);
            _onChat    = Callback<LobbyChatUpdate_t>.Create(_ => Changed?.Invoke());

            // 남이 자기 달팽이를 올리면 이쪽으로 온다
            _onData    = Callback<LobbyDataUpdate_t>.Create(_ => Changed?.Invoke());

            // 스팀 오버레이나 친구 초대로 들어오는 길
            _onInvited = Callback<GameLobbyJoinRequested_t>.Create(e => SteamMatchmaking.JoinLobby(e.m_steamIDLobby));

            return true;
        }

        public static void Pump()
        {
            if (Available) SteamAPI.RunCallbacks();
        }

        public static void Shutdown()
        {
            if (!Available) return;

            Leave();
            SteamAPI.Shutdown();
            Available = false;
            MyName = "";
        }

        // ── 친구 ──

        /// <summary>
        /// 접속해 있는 친구 이름. 오프라인까지 넣으면 수백 줄이 되어 목록이 못 쓰게 된다.
        /// 순서는 <see cref="Invite"/> 의 번호와 같다.
        /// </summary>
        public static string[] Friends()
        {
            _friendIds.Clear();
            if (!Available) return new string[0];

            var names = new List<string>();
            int n = SteamFriends.GetFriendCount(EFriendFlags.k_EFriendFlagImmediate);
            for (int i = 0; i < n; i++)
            {
                var id = SteamFriends.GetFriendByIndex(i, EFriendFlags.k_EFriendFlagImmediate);
                if (SteamFriends.GetFriendPersonaState(id) == EPersonaState.k_EPersonaStateOffline) continue;

                _friendIds.Add(id);
                names.Add(SteamFriends.GetFriendPersonaName(id));
            }
            return names.ToArray();
        }

        /// <summary>친구를 지금 방으로 부른다. 방이 없으면 아무 일도 안 한다.</summary>
        public static void Invite(int friendIndex)
        {
            if (!Available || !InLobby) { Note?.Invoke("초대: 먼저 방을 만들어야 합니다"); return; }
            if (friendIndex < 0 || friendIndex >= _friendIds.Count) return;

            SteamMatchmaking.InviteUserToLobby(_lobby, _friendIds[friendIndex]);
            Note?.Invoke("초대 보냄: " + SteamFriends.GetFriendPersonaName(_friendIds[friendIndex]));
        }

        // ── 방 ──

        public static void CreateLobby()
        {
            if (!Available) return;

            Leave();
            _onCreated.Set(SteamMatchmaking.CreateLobby(ELobbyType.k_ELobbyTypePublic, MaxMembers));
        }

        private static void OnLobbyCreated(LobbyCreated_t e, bool failed)
        {
            if (failed || e.m_eResult != EResult.k_EResultOK)
            {
                Note?.Invoke("방 만들기 실패: " + e.m_eResult);
                return;
            }

            _lobby = new CSteamID(e.m_ulSteamIDLobby);

            // 480 을 같이 쓰는 남의 방과 섞이지 않게 표시를 박는다
            SteamMatchmaking.SetLobbyData(_lobby, GameKey, GameValue);
            SteamMatchmaking.SetLobbyData(_lobby, NameKey, MyName + "의 방");

            Note?.Invoke("방을 만들었습니다: " + LobbyName + " (" + _lobby.m_SteamID + ")");
            Entered?.Invoke();
            Changed?.Invoke();
        }

        /// <summary>공개된 우리 방들을 다시 받아 온다. 결과는 <see cref="Changed"/> 로 온다.</summary>
        public static void RefreshLobbies()
        {
            if (!Available) return;

            SteamMatchmaking.AddRequestLobbyListStringFilter(
                GameKey, GameValue, ELobbyComparison.k_ELobbyComparisonEqual);
            _onList.Set(SteamMatchmaking.RequestLobbyList());
        }

        private static void OnLobbyList(LobbyMatchList_t e, bool failed)
        {
            _lobbyIds.Clear();
            _lobbyNames.Clear();

            if (!failed)
            {
                for (int i = 0; i < e.m_nLobbiesMatching; i++)
                {
                    var id = SteamMatchmaking.GetLobbyByIndex(i);
                    string name = SteamMatchmaking.GetLobbyData(id, NameKey);

                    _lobbyIds.Add(id);
                    _lobbyNames.Add(string.IsNullOrEmpty(name) ? id.m_SteamID.ToString() : name);
                }
            }

            Note?.Invoke("로비 목록: " + _lobbyIds.Count + "개");

            // 랜덤 진입을 기다리고 있었으면 여기서 아무 방이나 골라 들어간다
            if (_joinRandomPending)
            {
                _joinRandomPending = false;
                if (_lobbyIds.Count > 0) JoinLobby(UnityEngine.Random.Range(0, _lobbyIds.Count));
                else Note?.Invoke("랜덤 진입: 들어갈 방이 없습니다");
            }

            Changed?.Invoke();
        }

        public static string[] Lobbies() => _lobbyNames.ToArray();

        public static void JoinLobby(int index)
        {
            if (!Available || index < 0 || index >= _lobbyIds.Count) return;

            Leave();
            _onEnter.Set(SteamMatchmaking.JoinLobby(_lobbyIds[index]));
        }

        /// <summary>로비 ID 를 직접 받아 들어간다. 숫자가 아니면 아무 일도 안 한다.</summary>
        public static void JoinById(string text)
        {
            if (!Available) return;
            if (!ulong.TryParse((text ?? "").Trim(), out ulong id))
            {
                Note?.Invoke("로비ID: 숫자가 아닙니다 — " + text);
                return;
            }

            Leave();
            _onEnter.Set(SteamMatchmaking.JoinLobby(new CSteamID(id)));
        }

        /// <summary>목록을 새로 받아 그중 아무 방으로 들어간다.</summary>
        public static void JoinRandom()
        {
            if (!Available) return;

            _joinRandomPending = true;
            RefreshLobbies();
        }

        private static void OnLobbyEnter(LobbyEnter_t e, bool failed)
        {
            if (failed || e.m_EChatRoomEnterResponse != (uint)EChatRoomEnterResponse.k_EChatRoomEnterResponseSuccess)
            {
                Note?.Invoke("방 진입 실패: " + e.m_EChatRoomEnterResponse);
                Changed?.Invoke();
                return;
            }

            _lobby = new CSteamID(e.m_ulSteamIDLobby);
            Note?.Invoke("방에 들어왔습니다: " + LobbyName + " (" + _lobby.m_SteamID + ")");
            Entered?.Invoke();
            Changed?.Invoke();
        }

        public static void Leave()
        {
            if (!Available || !InLobby) return;

            SteamMatchmaking.LeaveLobby(_lobby);
            _lobby = CSteamID.Nil;
            Changed?.Invoke();
        }

        /// <summary>지금 방에 있는 사람들의 이름. 방이 없으면 빈 배열.</summary>
        public static string[] Members()
        {
            if (!Available || !InLobby) return new string[0];

            int n = SteamMatchmaking.GetNumLobbyMembers(_lobby);
            var names = new string[n];
            for (int i = 0; i < n; i++)
                names[i] = SteamFriends.GetFriendPersonaName(SteamMatchmaking.GetLobbyMemberByIndex(_lobby, i));

            return names;
        }

        /// <summary>
        /// 내 달팽이가 어떻게 생겼는지 방에 올린다. 들어갈 때와 모습이 바뀔 때 부르면 된다.
        /// 위치는 안 맞추기로 했으므로 오가는 것은 이 문자열 하나뿐이다.
        /// </summary>
        public static void PublishSnail(string look)
        {
            if (!Available || !InLobby) return;
            SteamMatchmaking.SetLobbyMemberData(_lobby, SnailPet.Snail.SnailShare.Key, look ?? "");
        }

        /// <summary>방에 있는 사람들의 (이름, 달팽이 글자, 나인가). 아직 안 올린 사람은 글자가 빈다.</summary>
        public static (string name, string look, bool me)[] MemberLooks()
        {
            if (!Available || !InLobby) return new (string, string, bool)[0];

            var me = SteamUser.GetSteamID();
            int n = SteamMatchmaking.GetNumLobbyMembers(_lobby);
            var rows = new (string, string, bool)[n];

            for (int i = 0; i < n; i++)
            {
                var id = SteamMatchmaking.GetLobbyMemberByIndex(_lobby, i);
                rows[i] = (SteamFriends.GetFriendPersonaName(id),
                           SteamMatchmaking.GetLobbyMemberData(_lobby, id, SnailPet.Snail.SnailShare.Key),
                           id == me);
            }
            return rows;
        }
#endif
    }
}
