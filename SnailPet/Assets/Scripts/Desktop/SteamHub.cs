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

        /// <summary>
        /// 이 방이 어느 <b>더미 방의 자리</b>인가 (DummyData.RoomNumber). 없으면 보통 방이다.
        ///
        /// 스팀 로비는 사람이 들어가 있는 동안에만 존재한다 — 목록에 늘 떠 있는 방을 만들 수가 없다.
        /// 그래서 목록에는 시트의 더미 방을 <b>자리표시자</b>로 걸어 두고, 누가 처음 들어가면 그때
        /// 이 표시를 박은 진짜 방을 연다. 남이 같은 줄을 누르면 새로 만드는 대신 그 방으로 들어간다.
        /// </summary>
        private const string DummyKey = "dummy";

        /// <summary>
        /// 방 코드. 스팀의 로비 ID 는 17자리 숫자라 사람이 불러 주기 어렵다.
        /// 그래서 짧은 코드를 따로 붙이고, 들어갈 때는 그 코드로 목록을 걸러 찾는다.
        /// 헷갈리는 글자(0/O, 1/I)는 뺀다 — 불러 주고 받아 적는 물건이다.
        /// </summary>
        private const string CodeKey = "code";
        private const string CodeAlphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        private const int CodeLength = 6;

        /// <summary>한 방에 들어갈 수 있는 인원. 목업의 max 5 다.</summary>
        public const int MaxMembers = 5;

        public static bool Available { get; private set; }
        public static string LastError { get; private set; }
        public static string MyName { get; private set; } = "";

        /// <summary>목록이나 방 상태가 바뀌었다. 화면을 다시 그리라는 뜻이다.</summary>
        public static event Action Changed;

        /// <summary>무슨 일이 있었는지 한 줄로. 부르는 쪽이 로그에 적는다.</summary>
        public static event Action<string> Note;

        /// <summary>
        /// 방에 들어왔다. 인자는 <b>내가 만든 방인가</b>다 — 받는 쪽이 문구를 가른다.
        /// 만들었든 들어왔든 자기 달팽이를 올리는 것은 똑같다.
        /// </summary>
        public static event Action<bool> Entered;

        /// <summary>지금 있는 방으로 또 들어가려 했다. 받는 쪽이 안내를 띄운다.</summary>
        public static event Action SameRoom;

        /// <summary>없는 방에 들어가려 했다 (코드가 틀렸거나 그 방이 사라졌다).</summary>
        public static event Action NoSuchRoom;

        /// <summary>
        /// 방을 나왔다. <b>유저가 스스로 나온 때만</b> 온다 —
        /// 다른 방으로 옮기느라 내부에서 먼저 나가는 것은 알릴 일이 아니다.
        /// </summary>
        public static event Action Left;

#if DISABLESTEAMWORKS
        public static bool InLobby => false;
        public static string LobbyName => "";
        public static string LobbyCode => "";
        public static bool IsHost => false;
        public static bool IsCurrent(int index) => false;
        public static void RenameLobby(string name) { }
        public static string[] Friends() => new string[0];
        public static string[] Lobbies() => new string[0];
        public static string[] Members() => new string[0];

        public static bool Init() { LastError = "이 빌드에는 스팀이 빠져 있습니다"; return false; }
        public static void Pump() { }
        public static void Shutdown() { }
        public static void CreateLobby(string name = null, int dummyRoom = 0) { }
        public static int DummyOf(int index) => 0;
        public static int IndexOfDummy(int roomNumber) => -1;
        public static int CurrentDummy => 0;
        public static void RefreshLobbies() { }
        public static void JoinLobby(int index) { }
        public static void JoinById(string text) { }
        public static void JoinRandom() { }
        public static void Leave(bool quiet = false) { }
        public static void Invite(int friendIndex) { }
        public static void PublishSnail(string look) { }
        public static (string name, string look, bool me)[] MemberLooks() => new (string, string, bool)[0];
#else
        private static readonly List<CSteamID> _friendIds = new List<CSteamID>();
        private static readonly List<CSteamID> _lobbyIds = new List<CSteamID>();
        private static readonly List<string> _lobbyNames = new List<string>();

        /// <summary>줄마다의 더미 방 번호. 보통 방은 0 이다. _lobbyIds 와 길이가 같다.</summary>
        private static readonly List<int> _lobbyDummies = new List<int>();

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

        /// <summary>지금 방의 코드. 방이 없으면 빈 문자열.</summary>
        public static string LobbyCode =>
            InLobby ? SteamMatchmaking.GetLobbyData(_lobby, CodeKey) : "";

        /// <summary>
        /// 내가 방장인가. <b>방 데이터는 방장만 고칠 수 있다</b> —
        /// 남이 고치려 하면 스팀이 조용히 무시하므로, 부르는 쪽이 미리 가려야 한다.
        /// </summary>
        public static bool IsHost =>
            InLobby && SteamMatchmaking.GetLobbyOwner(_lobby) == SteamUser.GetSteamID();

        /// <summary>
        /// 방 안에서 사람이 드나들었다.
        ///
        /// <b>내가 빠진 경우</b>도 여기로 온다 — 연결이 끊기거나 내보내진 때다. 그때 화면을
        /// 그대로 두면 방에 있는 것처럼 보이므로 방을 놓아 준다.
        /// (방장이 나가는 것은 로비가 죽는 것이 아니다. 스팀이 남은 사람 중 하나를 방장으로
        ///  올리고, 그러면 새 방장에게 이름 변경이 열린다 — 아래 Changed 가 그것도 반영한다.)
        /// </summary>
        private static void OnLobbyChat(LobbyChatUpdate_t e)
        {
            const uint OutMask = (uint)EChatMemberStateChange.k_EChatMemberStateChangeLeft
                               | (uint)EChatMemberStateChange.k_EChatMemberStateChangeDisconnected
                               | (uint)EChatMemberStateChange.k_EChatMemberStateChangeKicked
                               | (uint)EChatMemberStateChange.k_EChatMemberStateChangeBanned;

            if (InLobby
                && e.m_ulSteamIDLobby == _lobby.m_SteamID
                && e.m_ulSteamIDUserChanged == SteamUser.GetSteamID().m_SteamID
                && (e.m_rgfChatMemberStateChange & OutMask) != 0)
            {
                _lobby = CSteamID.Nil;
                Note?.Invoke("방에서 나가졌습니다 (끊김 또는 강퇴)");
                Left?.Invoke();
            }

            Changed?.Invoke();
        }

        /// <summary>목록의 그 줄이 <b>지금 있는 방</b>인가. 같은 방에 또 들어가지 않게 가릴 때 쓴다.</summary>
        public static bool IsCurrent(int index) =>
            InLobby && index >= 0 && index < _lobbyIds.Count && _lobbyIds[index] == _lobby;

        /// <summary>방 이름을 고친다. 방장이 아니면 아무 일도 안 한다.</summary>
        public static void RenameLobby(string name)
        {
            if (!IsHost) return;

            string want = (name ?? "").Trim();
            if (want.Length == 0) return;

            SteamMatchmaking.SetLobbyData(_lobby, NameKey, want);
            Note?.Invoke("방 이름을 바꿨습니다: " + want);
            Changed?.Invoke();
        }

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
            _onChat    = Callback<LobbyChatUpdate_t>.Create(OnLobbyChat);

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

            Leave(quiet: true);
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

        /// <summary>
        /// 방을 만든다.
        /// <paramref name="name"/> 을 주면 그 이름으로 열고, 없으면 내 이름을 붙인다.
        /// <paramref name="dummyRoom"/> 이 0 이 아니면 <b>그 더미 방의 자리</b>라는 표시를 박는다 —
        /// 남이 목록에서 같은 줄을 눌렀을 때 새로 만드는 대신 이 방으로 들어오게 하는 표식이다.
        /// </summary>
        public static void CreateLobby(string name = null, int dummyRoom = 0)
        {
            if (!Available) return;

            // 만들어지는 것은 콜백이라 한 박자 뒤다. 그때 쓸 값을 들고 있는다.
            _pendingName = name;
            _pendingDummy = dummyRoom;

            Leave(quiet: true);
            _onCreated.Set(SteamMatchmaking.CreateLobby(ELobbyType.k_ELobbyTypePublic, MaxMembers));
        }

        private static string _pendingName;
        private static int _pendingDummy;

        /// <summary>그 줄의 방이 어느 더미 방 자리인가. 0 이면 보통 방이다.</summary>
        public static int DummyOf(int index) =>
            index >= 0 && index < _lobbyDummies.Count ? _lobbyDummies[index] : 0;

        /// <summary>그 더미 방으로 이미 열려 있는 방의 줄 번호. 없으면 -1.</summary>
        public static int IndexOfDummy(int roomNumber)
        {
            if (roomNumber == 0) return -1;

            for (int i = 0; i < _lobbyDummies.Count; i++)
                if (_lobbyDummies[i] == roomNumber) return i;

            return -1;
        }

        /// <summary>지금 들어가 있는 방이 어느 더미 방 자리인가. 0 이면 보통 방이거나 방 밖이다.</summary>
        public static int CurrentDummy =>
            InLobby && int.TryParse(SteamMatchmaking.GetLobbyData(_lobby, DummyKey), out int n) ? n : 0;

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
            SteamMatchmaking.SetLobbyData(_lobby, NameKey,
                string.IsNullOrEmpty(_pendingName) ? MyName + "의 방" : _pendingName);
            SteamMatchmaking.SetLobbyData(_lobby, CodeKey, MakeCode());

            // 더미 방 자리로 연 것이면 그 번호를 박아 둔다. 남이 같은 줄을 누르면 이리로 온다.
            bool asDummy = _pendingDummy != 0;
            if (asDummy)
                SteamMatchmaking.SetLobbyData(_lobby, DummyKey, _pendingDummy.ToString());

            _pendingName = null;
            _pendingDummy = 0;

            Note?.Invoke((asDummy ? "더미 방 자리를 열었습니다: " : "방을 만들었습니다: ")
                         + LobbyName + " (" + _lobby.m_SteamID + ")");

            // 「만들었다」는 <b>유저가 방 만들기를 눌렀을 때</b>만이다. 더미 방은 목록에 이미 있던
            // 방으로 들어간 것이라, 그 자리를 여느라 로비가 새로 생긴 것은 속사정일 뿐이다.
            Entered?.Invoke(!asDummy);
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
            _lobbyDummies.Clear();

            if (!failed)
            {
                for (int i = 0; i < e.m_nLobbiesMatching; i++)
                {
                    var id = SteamMatchmaking.GetLobbyByIndex(i);
                    string name = SteamMatchmaking.GetLobbyData(id, NameKey);

                    _lobbyIds.Add(id);
                    _lobbyNames.Add(string.IsNullOrEmpty(name) ? id.m_SteamID.ToString() : name);
                    _lobbyDummies.Add(int.TryParse(SteamMatchmaking.GetLobbyData(id, DummyKey), out int d) ? d : 0);
                }
            }

            // 코드로 찾던 중이면 그 목록은 「그 코드짜리 방」뿐이다. 화면의 목록으로 쓰지 않는다.
            if (!string.IsNullOrEmpty(_joinCode))
            {
                string code = _joinCode;
                _joinCode = null;

                if (_lobbyIds.Count > 0)
                {
                    // 지금 있는 방의 코드를 넣었을 수 있다. 그건 들어갈 것이 아니라 알릴 일이다.
                    if (IsCurrent(0)) SameRoom?.Invoke();
                    else
                    {
                        Note?.Invoke("코드로 찾았습니다: " + code);
                        JoinLobby(0);
                    }
                }
                else
                {
                    Note?.Invoke("그런 방이 없습니다: " + code);
                    NoSuchRoom?.Invoke();
                }

                _lobbyIds.Clear();
                _lobbyNames.Clear();
                return;
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

            Leave(quiet: true);
            _onEnter.Set(SteamMatchmaking.JoinLobby(_lobbyIds[index]));
        }

        private static string MakeCode()
        {
            var sb = new System.Text.StringBuilder(CodeLength);
            for (int i = 0; i < CodeLength; i++)
                sb.Append(CodeAlphabet[UnityEngine.Random.Range(0, CodeAlphabet.Length)]);

            return sb.ToString();
        }

        /// <summary>
        /// 받아 적은 것으로 들어간다. <b>방 코드와 로비 ID 를 둘 다 받는다</b> —
        /// 유저에게 보여 주는 것은 짧은 코드지만, 예전처럼 17자리 ID 를 넣어도 통해야 한다.
        /// 코드는 목록을 그 코드로 걸러서 찾는다.
        /// </summary>
        public static void JoinById(string text)
        {
            if (!Available) return;

            string want = (text ?? "").Trim();
            if (want.Length == 0) return;

            // 17자리쯤 되는 숫자는 로비 ID 로 본다. 코드는 그보다 훨씬 짧다.
            if (want.Length > CodeLength && ulong.TryParse(want, out ulong id))
            {
                Leave(quiet: true);
                _onEnter.Set(SteamMatchmaking.JoinLobby(new CSteamID(id)));
                return;
            }

            _joinCode = want.ToUpperInvariant();
            SteamMatchmaking.AddRequestLobbyListStringFilter(
                GameKey, GameValue, ELobbyComparison.k_ELobbyComparisonEqual);
            SteamMatchmaking.AddRequestLobbyListStringFilter(
                CodeKey, _joinCode, ELobbyComparison.k_ELobbyComparisonEqual);

            _onList.Set(SteamMatchmaking.RequestLobbyList());
        }

        /// <summary>코드로 찾는 중이면 그 코드. 목록이 오면 여기에 걸린 방으로 들어간다.</summary>
        private static string _joinCode;

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
                // 사라진 방·틀린 ID 로 들어가려 한 경우가 대부분이다. 유저에게는 하나로 보인다.
                Note?.Invoke("방 진입 실패: " + e.m_EChatRoomEnterResponse);
                NoSuchRoom?.Invoke();
                Changed?.Invoke();
                return;
            }

            _lobby = new CSteamID(e.m_ulSteamIDLobby);
            Note?.Invoke("방에 들어왔습니다: " + LobbyName + " (" + _lobby.m_SteamID + ")");
            Entered?.Invoke(false);
            Changed?.Invoke();
        }

        /// <param name="quiet">
        /// 참이면 <see cref="Left"/> 를 내지 않는다. 다른 방으로 옮기려고 먼저 나가는 것은
        /// 유저가 「나간」 것이 아니므로 알리면 안 된다 — 방을 만들 때마다 「방을 나왔습니다」가
        /// 먼저 떠 버린다.
        /// </param>
        public static void Leave(bool quiet = false)
        {
            if (!Available || !InLobby) return;

            SteamMatchmaking.LeaveLobby(_lobby);
            _lobby = CSteamID.Nil;

            if (!quiet) Left?.Invoke();
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
