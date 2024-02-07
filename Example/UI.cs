using Godot;
using SteamMultiplayerPeer.Example;
using Steamworks.Data;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
public partial class UI : Control
{
    private Button _hostButton = null!;
    private Button _joinButton = null!;
    private Button _quitButton = null!;
    private TextEdit _lobbyIdLabel = null!;
    private TextEdit _textEdit = null!;
    private VBoxContainer _memberVbox = null!;
    private Node3D _playerSpawnerNode = null!;
    private PackedScene _playerScene = ResourceLoader.Load<PackedScene>("res://Example/Player.tscn");
    private bool _inLobby = false;
    public override void _Ready()
    {
        _hostButton = GetNode<Button>("VBoxContainer/HostGame");
        _joinButton = GetNode<Button>("VBoxContainer/JoinGame");
        _quitButton = GetNode<Button>("VBoxContainer/QuitGame");
        _lobbyIdLabel = GetNode<TextEdit>("LobbyVbox/HBoxContainer/LobbyIdLabel");
        _textEdit = GetNode<TextEdit>("VBoxContainer/TextEdit");
        _memberVbox = GetNode<VBoxContainer>("LobbyVbox/MemberVbox");
        _playerSpawnerNode = GetParent().GetNode<Node3D>("PlayerHolder");

        _lobbyIdLabel.Editable = false;

        _hostButton.Pressed += async () => await OnHostButtonPressed();
        _joinButton.Pressed += async () => await OnJoinButtonPressed();
        _quitButton.Pressed += OnQuitButtonPressed;

        this.SteamManager().OnLobbyRefreshCompleted += (List<Lobby> lobbies) => // you would pretty obviously not want to do this exact implementation in a real game, but for the sake of the example
        {
            Lobby? lobby = lobbies.FirstOrDefault(x => x.Id.Value == ulong.Parse(_textEdit.Text));

            if (lobby is not null)
            {
                lobby.Value.Join();

                Timer timer = new Timer(); // shit implementation but you need to load the owner id here and it takes a sec, make the joining lobby awaitable
                AddChild(timer);
                timer.OneShot = true;
                timer.WaitTime = 1.0d;
                timer.Timeout += () =>
                {
                    _inLobby = true;
                    Steam.SteamMultiplayerPeer peer = new Steam.SteamMultiplayerPeer();
                    peer.CreateClient(this.SteamManager().PlayerSteamID, lobby.Value.Owner.Id);
                    Multiplayer.MultiplayerPeer = peer;
                    Rpc(nameof(RequestOtherUsers));
                };
                timer.Start();

            }

        };

    }

    public override void _Process(double delta)
    {
        _hostButton.Disabled = _inLobby;
        _joinButton.Disabled = string.IsNullOrEmpty(_textEdit.Text) || _inLobby;
    }


    private async Task OnHostButtonPressed()
    {

        this.SteamManager().OnLobbySuccessfullyCreated += (lobbyId) =>
        {
            _lobbyIdLabel.Text = lobbyId.Id.ToString();
            Steam.SteamMultiplayerPeer steamMultiplayerPeer = new Steam.SteamMultiplayerPeer();
            steamMultiplayerPeer.CreateHost(25565);
            Multiplayer.MultiplayerPeer = steamMultiplayerPeer;

            AddPlayer(1);
            RefreshPlayerList();
        };

        Multiplayer.PeerConnected += (peer) =>
        {
            AddPlayer((ulong)peer);
            _memberVbox.AddChild(new Label() { Text = peer.ToString(), Name = peer.ToString() });
        };
        Multiplayer.PeerDisconnected += (peer) =>
        {
            _memberVbox.RemoveChild(_memberVbox.GetNode(peer.ToString()));
        };

        _inLobby = true;
        await this.SteamManager().CreateLobby();

    }
    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
    public void RequestOtherUsers()
    {
        Rpc(nameof(RefreshPlayerList));
    }
    [Rpc]
    public void RefreshPlayerList()
    {
        if (Multiplayer.IsServer())
        {
            _memberVbox.GetChildren().ToList().ForEach(x => x.QueueFree());
            foreach (var player in _playerSpawnerNode.GetChildren().Cast<Player>())
            {
                _memberVbox.AddChild(new Label() { Text = player.ToString(), Name = player.ToString() });
            }
        }
    }

    private async Task OnJoinButtonPressed()
    {
        await this.SteamManager().GetMultiplayerLobbies();
    }

    private void OnQuitButtonPressed()
    {
        GetTree().Quit();
    }

    private void AddPlayer(ulong id)
    {
        Player playerRef = _playerScene.Instantiate<Player>();
        playerRef.Name = id.ToString();
        _playerSpawnerNode.AddChild(playerRef, true);
    }
}
