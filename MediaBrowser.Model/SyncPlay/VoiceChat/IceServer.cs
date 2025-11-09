using System.Collections.Generic;

namespace MediaBrowser.Model.SyncPlay.VoiceChat
{
    /// <summary>
    /// Represents an ICE server configuration for WebRTC (STUN/TURN).
    /// </summary>
    public class IceServer
    {
        /// <summary>
        /// Gets or sets the URLs for the ICE server.
        /// </summary>
        public List<string> Urls { get; set; } = new List<string>();

        /// <summary>
        /// Gets or sets the username for TURN server authentication.
        /// </summary>
        public string? Username { get; set; }

        /// <summary>
        /// Gets or sets the credential for TURN server authentication.
        /// </summary>
        public string? Credential { get; set; }

        /// <summary>
        /// Gets or sets the credential type (default is "password").
        /// </summary>
        public string CredentialType { get; set; } = "password";
    }
}
