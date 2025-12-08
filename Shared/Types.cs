namespace Shared
{
    /// <summary>
    /// Container for shared enum types used by both client and server for
    /// message framing, command identification and related protocol values.
    /// </summary>
    public class Types
    {
        /// <summary>
        /// Network message types used in the 4-byte header. The numeric value
        /// is placed in the header's "Type" field and indicates how the body
        /// (if any) should be interpreted.
        /// </summary>
        public enum MessageType : ushort
        {
            /// <summary>
            /// Heartbeat message. Header-only (no JSON body).
            /// </summary>
            HEARBEAT,
            /// <summary>
            /// Acknowledgement message (contains an Ack payload).
            /// </summary>
            ACK,
            /// <summary>
            /// Error indicator.
            /// </summary>
            ERROR,
            /// <summary>
            /// Server-side textual message intended for console or popup display.
            /// </summary>
            SERVERMESSAGE,
            /// <summary>
            /// Login request/response containing Login payload.
            /// </summary>
            LOGIN,
            /// <summary>
            /// Generic command from client to server (Command payload).
            /// </summary>
            COMMAND,
            /// <summary>
            /// Room description payload.
            /// </summary>
            ROOM,
            /// <summary>
            /// Who list payload containing active player names.
            /// </summary>
            WHO,
            /// <summary>
            /// Score information (unused).
            /// </summary>
            SCORE,
            /// <summary>
            /// Message (TELL) payload. Generally used for all chat messages.
            /// </summary>
            TELL
        }

        /// <summary>
        /// Gameplay command identifiers sent from client to server. Each command
        /// may require specific arguments (for example, MOVE expects a direction).
        /// Many of these are placeholders for future implementation.
        /// </summary>
        public enum Commands : ushort
        {
            LOOK,
            MOVE,
            SCORE,
            INV,
            TAKE,
            PUT,
            DROP,
            EQUIP,
            WHO,
            SAY,
            SHOUT,
            TELL,
            OOC,
            IGNORE,
            QUIT
        }

        /// <summary>
        /// Client lifecycle states. These values describe the high-level state of
        /// a client connection during authentication and gameplay.
        /// </summary>
        public enum ClientState : byte
        {
            DISCONNECTED,
            LOGIN,
            NEWCHAR,
            PLAYING,
            LOGOUT
        }

        /// <summary>
        /// Acknowledgement subtypes used in Ack messages (for example, LOGIN ack).
        /// </summary>
        public enum AckType : byte
        {
            LOGIN
        }

        /// <summary>
        /// Login action subtypes: whether the client is attempting to log into an
        /// existing character or create a new one.
        /// </summary>
        public enum LoginType : byte
        {
            EXISTING,
            NEW
        }

        /// <summary>
        /// Server message presentation types. CONSOLE messages are intended for the
        /// in-game console area, while POPUP messages should be shown as modal alerts.
        /// </summary>
        public enum ServerMessageType : byte
        {
            CONSOLE,
            POPUP
        }

        /// <summary>
        /// Deprecated chat message type enum. Historically used to categorize
        /// chat variants (tell/say/shout/ooc). This enum is retained for
        /// compatibility but is not used in the current message framing.
        /// </summary>
        public enum ChatMessageType : byte
        {
            /// <summary>
            /// Private message between two players.
            /// </summary>
            TELL,
            /// <summary>
            /// Local speech heard by players in the same room.
            /// </summary>
            SAY,
            /// <summary>
            /// Loud message intended to propagate beyond the current room.
            /// </summary>
            SHOUT,
            /// <summary>
            /// Out-of-character global chat.
            /// </summary>
            OOC
        }

        /// <summary>
        /// Query result cardinality hints used by the Database helper (SING = single-row,
        /// MULT = multiple rows). These help callers indicate expected result shape.
        /// </summary>
        public enum QueryType : byte
        {
            SING,
            MULT
        }
    }
}