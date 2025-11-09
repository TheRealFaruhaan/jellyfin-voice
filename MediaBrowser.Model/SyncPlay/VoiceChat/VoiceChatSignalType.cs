namespace MediaBrowser.Model.SyncPlay.VoiceChat
{
    /// <summary>
    /// Enum VoiceChatSignalType for WebRTC signaling message types.
    /// </summary>
    public enum VoiceChatSignalType
    {
        /// <summary>
        /// WebRTC offer message (SDP).
        /// </summary>
        Offer = 0,

        /// <summary>
        /// WebRTC answer message (SDP).
        /// </summary>
        Answer = 1,

        /// <summary>
        /// ICE candidate message.
        /// </summary>
        IceCandidate = 2,

        /// <summary>
        /// User joined voice chat.
        /// </summary>
        UserJoined = 3,

        /// <summary>
        /// User left voice chat.
        /// </summary>
        UserLeft = 4,

        /// <summary>
        /// User muted their microphone.
        /// </summary>
        UserMuted = 5,

        /// <summary>
        /// User unmuted their microphone.
        /// </summary>
        UserUnmuted = 6
    }
}
