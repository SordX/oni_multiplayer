using MultiplayerMod.Core.Logging;
using MultiplayerMod.Core.Scheduling;
using MultiplayerMod.Core.Unity;
using MultiplayerMod.ModRuntime.StaticCompatibility;
using MultiplayerMod.Multiplayer.Commands;
using MultiplayerMod.Multiplayer.Commands.Player;
using MultiplayerMod.Multiplayer.UI.Overlays;
using MultiplayerMod.Network;
using MultiplayerMod.Platform.Common.Network.Components;
using MultiplayerMod.Platform.Common.Network.Messaging;
using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using UnityEngine;
using WebSocketSharp;

namespace MultiplayerMod.Platform.Lan.Network;

internal class LanClient : IMultiplayerClient {
    public static LanClient? instance = null;

    public IMultiplayerClientId Id => lanMultiplayerClientId;
    public MultiplayerClientState State { get; private set; } = MultiplayerClientState.Disconnected;

    public event Action<MultiplayerClientState>? StateChanged;
    public event Action<IMultiplayerCommand>? CommandReceived;

    internal readonly LanMultiplayerClientId lanMultiplayerClientId = new LanMultiplayerClientId();
    private WebSocket? network;

    private GameObject? gameObject = null!;

    private readonly Core.Logging.Logger log = LoggerFactory.GetLogger<LanClient>();
    private ConcurrentQueue<System.Action> commandQueue = new();

    public LanClient() {
        //log.Level = Core.Logging.LogLevel.Debug;
        instance = this;
    }

    public void Connect(IMultiplayerEndpoint endpoint) {
        log.Debug("Client preparing to connect to server " + LanConfiguration.instance.hostUrl);
        commandQueue = new();
        network = new WebSocket(LanConfiguration.instance.hostUrl+"/oni");
        network.OnOpen += OnOpen;
        network.OnMessage += OnMessage;
        network.OnClose += OnClose;
        network.OnError += OnError;
        SetState(MultiplayerClientState.Connecting);
        network.ConnectAsync();
        gameObject = UnityObject.CreateStaticWithComponent<ClientComponent>();
    }

    public void Disconnect() {
        if (network == null) { return; }
        log.Debug("Client preparing to disconnect from server");
        var oldnetwork = network;
        network = null;
        oldnetwork.CloseAsync();
        if (gameObject != null) {
            UnityObject.Destroy(gameObject);
            gameObject = null!;
        }
    }

    public void Send(IMultiplayerCommand command, MultiplayerCommandOptions options = MultiplayerCommandOptions.None) {
        if (network == null) { return; }
        try {
            var data = new NetworkMessage(command, options).toBytes();

            if (command.GetType() != typeof(UpdatePlayerCursorPosition)) {
                log.Debug("Client sending command " + command.GetType() + ". Client " + Id + ". Len " + data.Length);
            }
            network.Send(data);
        } catch (Exception e) {
            log.Debug("Client failed to send command " + command.GetType() + ". " + e.Message);
        }
    }

    public void Tick() {
        while (commandQueue.TryDequeue(out var action)) {
            action();
        }
    }

    private void SetState(MultiplayerClientState status) {
        State = status;
        StateChanged?.Invoke(status);
    }

    private void OnOpen(object sender, EventArgs e) {
        log.Debug("q Client connected to server");
        commandQueue.Enqueue(() => {
            log.Debug("p Client connected to server");
            SetState(MultiplayerClientState.Connected);
        });
    }

    private void OnClose(object sender, CloseEventArgs e) {
        log.Debug("q Client disconnected from server");
        commandQueue.Enqueue(() => {
            log.Debug("p Client disconnected from server");
            if (State == MultiplayerClientState.Connecting) {
                MultiplayerStatusOverlay.Text = "Failed to connect to server";
                Task _ = this.closeOverlay();
            }
            SetState(MultiplayerClientState.Disconnected);
            Disconnect();
        });
    }

    private async Task closeOverlay() {
        await Task.Delay(2000);
        Dependencies.Get<UnityTaskScheduler>().Run(MultiplayerStatusOverlay.Close);
    }

    private void OnMessage(object sender, MessageEventArgs e) {
        var message = NetworkMessage.from(e.RawData);
        log.Debug("q Client received command " + message.Command.GetType() + ". Client " + Id + ". Len " + e.RawData.Length);
        commandQueue.Enqueue(() => {
            log.Debug("p Client received command " + message.Command.GetType() + ". Client " + Id + ". Len " + e.RawData.Length);
            if (State != MultiplayerClientState.Connected) { return; }
            CommandReceived?.Invoke(message.Command);
        });
    }

    private void OnError(object sender, ErrorEventArgs e) {
        log.Debug("q Client error " + e.Message);
        commandQueue.Enqueue(() => {
            log.Debug("p Client error " + e.Message);
            SetState(MultiplayerClientState.Error);
        });
    }

}
