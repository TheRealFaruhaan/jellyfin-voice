using MediaBrowser.Model.Session;
using MediaBrowser.Model.SyncPlay.VoiceChat;

namespace MediaBrowser.Controller.Net.WebSocketMessages.Outbound
{
    /// <summary>
    /// Voice chat signal message.
    /// </summary>
    public class VoiceChatSignalMessage : OutboundWebSocketMessage<VoiceChatSignal>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="VoiceChatSignalMessage"/> class.
        /// </summary>
        /// <param name="data">Voice chat signal data.</param>
        public VoiceChatSignalMessage(VoiceChatSignal data)
            : base(data)
        {
        }

        /// <inheritdoc />
        public override SessionMessageType MessageType => SessionMessageType.VoiceChatSignal;
    }
}
