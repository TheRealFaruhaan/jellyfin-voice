using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Session;
using MediaBrowser.Controller.SyncPlay;
using MediaBrowser.Model.SyncPlay.VoiceChat;
using Microsoft.Extensions.Logging;

namespace Emby.Server.Implementations.SyncPlay
{
    /// <summary>
    /// Class VoiceChatManager for managing voice chat sessions in SyncPlay groups.
    /// </summary>
    public class VoiceChatManager : IVoiceChatManager
    {
        private static readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = true };

        private readonly ILogger<VoiceChatManager> _logger;
        private readonly ISessionManager _sessionManager;
        private readonly IApplicationPaths _appPaths;
        private readonly ConcurrentDictionary<Guid, VoiceChatState> _voiceChatStates;
        private readonly string _configFilePath;
        private VoiceChatConfiguration _configuration = null!;

        /// <summary>
        /// Initializes a new instance of the <see cref="VoiceChatManager"/> class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="sessionManager">The session manager.</param>
        /// <param name="appPaths">The application paths.</param>
        public VoiceChatManager(
            ILogger<VoiceChatManager> logger,
            ISessionManager sessionManager,
            IApplicationPaths appPaths)
        {
            _logger = logger;
            _sessionManager = sessionManager;
            _appPaths = appPaths;
            _voiceChatStates = new ConcurrentDictionary<Guid, VoiceChatState>();
            _configFilePath = Path.Combine(_appPaths.ConfigurationDirectoryPath, "voicechat.json");
            LoadConfiguration();
        }

        /// <summary>
        /// Loads voice chat configuration from file or creates default.
        /// </summary>
        private void LoadConfiguration()
        {
            try
            {
                if (File.Exists(_configFilePath))
                {
                    var json = File.ReadAllText(_configFilePath);
                    _configuration = JsonSerializer.Deserialize<VoiceChatConfiguration>(json) ?? new VoiceChatConfiguration();
                    _logger.LogInformation("Loaded voice chat configuration from {ConfigPath}", _configFilePath);
                }
                else
                {
                    _configuration = new VoiceChatConfiguration();
                    SaveConfiguration();
                    _logger.LogInformation("Created default voice chat configuration at {ConfigPath}", _configFilePath);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading voice chat configuration, using defaults");
                _configuration = new VoiceChatConfiguration();
            }
        }

        /// <summary>
        /// Saves voice chat configuration to file.
        /// </summary>
        private void SaveConfiguration()
        {
            try
            {
                var json = JsonSerializer.Serialize(_configuration, _jsonOptions);
                File.WriteAllText(_configFilePath, json);
                _logger.LogInformation("Saved voice chat configuration to {ConfigPath}", _configFilePath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving voice chat configuration");
            }
        }

        /// <inheritdoc />
        public async Task<VoiceChatState> JoinVoiceChatAsync(
            Guid groupId,
            string sessionId,
            Guid userId,
            string userName,
            CancellationToken cancellationToken)
        {
            if (!_configuration.Enabled)
            {
                throw new InvalidOperationException("Voice chat is disabled");
            }

            var state = _voiceChatStates.GetOrAdd(groupId, _ => new VoiceChatState
            {
                GroupId = groupId,
                IsActive = true,
                IceServers = _configuration.IceServers
            });

            // Check participant limit
            if (state.Participants.Count >= _configuration.MaxParticipantsPerGroup)
            {
                throw new InvalidOperationException("Maximum number of voice chat participants reached");
            }

            // Check if user already joined
            var existingParticipant = state.Participants.FirstOrDefault(p => p.SessionId == sessionId);
            if (existingParticipant != null)
            {
                _logger.LogWarning("User {UserName} (Session: {SessionId}) already in voice chat for group {GroupId}", userName, sessionId, groupId);
                state.MySessionId = sessionId;
                return state;
            }

            // Add participant
            var participant = new VoiceChatParticipant
            {
                SessionId = sessionId,
                UserId = userId,
                UserName = userName,
                IsMuted = false,
                JoinedAt = DateTime.UtcNow
            };

            state.Participants.Add(participant);

            _logger.LogInformation("User {UserName} (Session: {SessionId}) joined voice chat for group {GroupId}", userName, sessionId, groupId);

            // Broadcast join signal to all participants except the one joining
            var joinSignal = new VoiceChatSignal
            {
                GroupId = groupId,
                FromSessionId = sessionId,
                Type = VoiceChatSignalType.UserJoined,
                Data = System.Text.Json.JsonSerializer.Serialize(participant)
            };

            await BroadcastSignalToGroupAsync(groupId, joinSignal, sessionId, cancellationToken).ConfigureAwait(false);

            // Send updated state to the joining participant
            await SendVoiceChatStateAsync(sessionId, state, cancellationToken).ConfigureAwait(false);

            // Set MySessionId for the response
            state.MySessionId = sessionId;

            return state;
        }

        /// <inheritdoc />
        public async Task LeaveVoiceChatAsync(Guid groupId, string sessionId, CancellationToken cancellationToken)
        {
            if (!_voiceChatStates.TryGetValue(groupId, out var state))
            {
                _logger.LogWarning("Voice chat state not found for group {GroupId}", groupId);
                return;
            }

            var participant = state.Participants.FirstOrDefault(p => p.SessionId == sessionId);
            if (participant == null)
            {
                _logger.LogWarning("Participant with session {SessionId} not found in voice chat for group {GroupId}", sessionId, groupId);
                return;
            }

            state.Participants.Remove(participant);

            _logger.LogInformation("User {UserName} (Session: {SessionId}) left voice chat for group {GroupId}", participant.UserName, sessionId, groupId);

            // If no participants left, remove the state
            if (state.Participants.Count == 0)
            {
                _voiceChatStates.TryRemove(groupId, out _);
                _logger.LogInformation("Voice chat ended for group {GroupId} (no participants)", groupId);
            }
            else
            {
                // Broadcast leave signal to remaining participants
                var leaveSignal = new VoiceChatSignal
                {
                    GroupId = groupId,
                    FromSessionId = sessionId,
                    Type = VoiceChatSignalType.UserLeft,
                    Data = System.Text.Json.JsonSerializer.Serialize(participant)
                };

                await BroadcastSignalToGroupAsync(groupId, leaveSignal, null, cancellationToken).ConfigureAwait(false);
            }
        }

        /// <inheritdoc />
        public async Task SendSignalAsync(VoiceChatSignal signal, CancellationToken cancellationToken)
        {
            if (!_voiceChatStates.TryGetValue(signal.GroupId, out var state))
            {
                _logger.LogWarning("Voice chat state not found for group {GroupId}", signal.GroupId);
                return;
            }

            // Validate sender is in the voice chat
            if (!state.Participants.Any(p => p.SessionId == signal.FromSessionId))
            {
                _logger.LogWarning("Sender {SessionId} not in voice chat for group {GroupId}", signal.FromSessionId, signal.GroupId);
                return;
            }

            // Send signal to target or broadcast
            if (!string.IsNullOrEmpty(signal.ToSessionId))
            {
                // Send to specific participant
                await SendSignalToSessionAsync(signal.ToSessionId, signal, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                // Broadcast to all except sender
                await BroadcastSignalToGroupAsync(signal.GroupId, signal, signal.FromSessionId, cancellationToken).ConfigureAwait(false);
            }
        }

        /// <inheritdoc />
        public async Task UpdateMuteStatusAsync(Guid groupId, string sessionId, bool isMuted, CancellationToken cancellationToken)
        {
            if (!_voiceChatStates.TryGetValue(groupId, out var state))
            {
                _logger.LogWarning("Voice chat state not found for group {GroupId}", groupId);
                return;
            }

            var participant = state.Participants.FirstOrDefault(p => p.SessionId == sessionId);
            if (participant == null)
            {
                _logger.LogWarning("Participant with session {SessionId} not found in voice chat for group {GroupId}", sessionId, groupId);
                return;
            }

            participant.IsMuted = isMuted;

            _logger.LogInformation("User {UserName} (Session: {SessionId}) mute status changed to {IsMuted} in group {GroupId}", participant.UserName, sessionId, isMuted, groupId);

            // Broadcast mute status change
            var muteSignal = new VoiceChatSignal
            {
                GroupId = groupId,
                FromSessionId = sessionId,
                Type = isMuted ? VoiceChatSignalType.UserMuted : VoiceChatSignalType.UserUnmuted,
                Data = System.Text.Json.JsonSerializer.Serialize(participant)
            };

            await BroadcastSignalToGroupAsync(groupId, muteSignal, sessionId, cancellationToken).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public VoiceChatState? GetVoiceChatState(Guid groupId)
        {
            _voiceChatStates.TryGetValue(groupId, out var state);
            return state;
        }

        /// <inheritdoc />
        public VoiceChatConfiguration GetConfiguration()
        {
            return _configuration;
        }

        /// <summary>
        /// Broadcasts a signal to all participants in a group.
        /// </summary>
        /// <param name="groupId">The group identifier.</param>
        /// <param name="signal">The signal to broadcast.</param>
        /// <param name="excludeSessionId">Session ID to exclude from broadcast (optional).</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        private async Task BroadcastSignalToGroupAsync(Guid groupId, VoiceChatSignal signal, string? excludeSessionId, CancellationToken cancellationToken)
        {
            if (!_voiceChatStates.TryGetValue(groupId, out var state))
            {
                return;
            }

            var tasks = state.Participants
                .Where(p => p.SessionId != excludeSessionId)
                .Select(p => SendSignalToSessionAsync(p.SessionId, signal, cancellationToken));

            await Task.WhenAll(tasks).ConfigureAwait(false);
        }

        /// <summary>
        /// Sends a signal to a specific session.
        /// </summary>
        /// <param name="sessionId">The session identifier.</param>
        /// <param name="signal">The signal to send.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        private async Task SendSignalToSessionAsync(string sessionId, VoiceChatSignal signal, CancellationToken cancellationToken)
        {
            try
            {
                await _sessionManager.SendVoiceChatSignal(sessionId, signal, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending voice chat signal to session {SessionId}", sessionId);
            }
        }

        /// <summary>
        /// Sends voice chat state to a specific session.
        /// </summary>
        /// <param name="sessionId">The session identifier.</param>
        /// <param name="state">The voice chat state.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        private async Task SendVoiceChatStateAsync(string sessionId, VoiceChatState state, CancellationToken cancellationToken)
        {
            try
            {
                await _sessionManager.SendVoiceChatState(sessionId, state, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending voice chat state to session {SessionId}", sessionId);
            }
        }
    }
}
