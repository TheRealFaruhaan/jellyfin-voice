using System;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Model.SyncPlay.VoiceChat;

namespace MediaBrowser.Controller.SyncPlay
{
    /// <summary>
    /// Interface IVoiceChatManager for managing voice chat in SyncPlay groups.
    /// </summary>
    public interface IVoiceChatManager
    {
        /// <summary>
        /// Joins a user to voice chat in a group.
        /// </summary>
        /// <param name="groupId">The group identifier.</param>
        /// <param name="sessionId">The session identifier.</param>
        /// <param name="userId">The user identifier.</param>
        /// <param name="userName">The user name.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task returning the voice chat state.</returns>
        Task<VoiceChatState> JoinVoiceChatAsync(Guid groupId, string sessionId, Guid userId, string userName, CancellationToken cancellationToken);

        /// <summary>
        /// Leaves voice chat in a group.
        /// </summary>
        /// <param name="groupId">The group identifier.</param>
        /// <param name="sessionId">The session identifier.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        Task LeaveVoiceChatAsync(Guid groupId, string sessionId, CancellationToken cancellationToken);

        /// <summary>
        /// Sends a WebRTC signaling message.
        /// </summary>
        /// <param name="signal">The voice chat signal.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        Task SendSignalAsync(VoiceChatSignal signal, CancellationToken cancellationToken);

        /// <summary>
        /// Updates mute status for a participant.
        /// </summary>
        /// <param name="groupId">The group identifier.</param>
        /// <param name="sessionId">The session identifier.</param>
        /// <param name="isMuted">Whether the user is muted.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        Task UpdateMuteStatusAsync(Guid groupId, string sessionId, bool isMuted, CancellationToken cancellationToken);

        /// <summary>
        /// Gets the voice chat state for a group.
        /// </summary>
        /// <param name="groupId">The group identifier.</param>
        /// <returns>The voice chat state or null if not active.</returns>
        VoiceChatState? GetVoiceChatState(Guid groupId);

        /// <summary>
        /// Gets the voice chat configuration.
        /// </summary>
        /// <returns>The voice chat configuration.</returns>
        VoiceChatConfiguration GetConfiguration();
    }
}
