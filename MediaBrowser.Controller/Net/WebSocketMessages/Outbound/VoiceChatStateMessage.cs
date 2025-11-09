using MediaBrowser.Model.Session;
using MediaBrowser.Model.SyncPlay.VoiceChat;

namespace MediaBrowser.Controller.Net.WebSocketMessages.Outbound
{
    /// <summary>
    /// Voice chat state message.
    /// </summary>
    public class VoiceChatStateMessage : OutboundWebSocketMessage<VoiceChatState>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="VoiceChatStateMessage"/> class.
        /// </summary>
        /// <param name="data">Voice chat state data.</param>
        public VoiceChatStateMessage(VoiceChatState data)
            : base(data)
        {
        }

        /// <inheritdoc />
        public override SessionMessageType MessageType => SessionMessageType.VoiceChatState;
    }
}
