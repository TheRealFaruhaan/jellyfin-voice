using System;
using System.Collections.Generic;

namespace MediaBrowser.Model.SyncPlay.VoiceChat
{
    /// <summary>
    /// Represents the state of voice chat for a SyncPlay group.
    /// </summary>
    public class VoiceChatState
    {
        /// <summary>
        /// Gets or sets the group identifier.
        /// </summary>
        public Guid GroupId { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether voice chat is active.
        /// </summary>
        public bool IsActive { get; set; }

        /// <summary>
        /// Gets or sets the session ID of the user receiving this state.
        /// </summary>
        public string? MySessionId { get; set; }

        /// <summary>
        /// Gets or sets the list of participants in the voice chat.
        /// </summary>
        public List<VoiceChatParticipant> Participants { get; set; } = new List<VoiceChatParticipant>();

        /// <summary>
        /// Gets or sets the ICE server configuration.
        /// </summary>
        public List<IceServer> IceServers { get; set; } = new List<IceServer>();
    }
}
