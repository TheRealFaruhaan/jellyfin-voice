using System;

namespace MediaBrowser.Model.SyncPlay.VoiceChat
{
    /// <summary>
    /// Represents a voice chat signaling message.
    /// </summary>
    public class VoiceChatSignal
    {
        /// <summary>
        /// Gets or sets the group identifier.
        /// </summary>
        public Guid GroupId { get; set; }

        /// <summary>
        /// Gets or sets the sender session identifier.
        /// </summary>
        public string FromSessionId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the target session identifier (null for broadcast).
        /// </summary>
        public string? ToSessionId { get; set; }

        /// <summary>
        /// Gets or sets the signal type.
        /// </summary>
        public VoiceChatSignalType Type { get; set; }

        /// <summary>
        /// Gets or sets the signal data (SDP or ICE candidate as JSON).
        /// </summary>
        public string? Data { get; set; }

        /// <summary>
        /// Gets or sets the timestamp when signal was created.
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}
