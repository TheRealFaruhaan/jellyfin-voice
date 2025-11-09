using System.Collections.Generic;

namespace MediaBrowser.Model.SyncPlay.VoiceChat
{
    /// <summary>
    /// Configuration for voice chat feature.
    /// </summary>
    public class VoiceChatConfiguration
    {
        /// <summary>
        /// Gets or sets a value indicating whether voice chat is enabled.
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Gets or sets the list of ICE servers (STUN/TURN) for WebRTC.
        /// </summary>
        public List<IceServer> IceServers { get; set; } = new List<IceServer>
        {
            // Default public STUN servers
            new IceServer
            {
                Urls = new List<string> { "stun:stun.l.google.com:19302" }
            },
            new IceServer
            {
                Urls = new List<string> { "stun:stun1.l.google.com:19302" }
            }
        };

        /// <summary>
        /// Gets or sets the maximum number of voice participants per group.
        /// </summary>
        public int MaxParticipantsPerGroup { get; set; } = 10;

        /// <summary>
        /// Gets or sets the signaling timeout in seconds.
        /// </summary>
        public int SignalingTimeoutSeconds { get; set; } = 30;
    }
}
