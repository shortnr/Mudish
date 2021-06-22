namespace Shared
{
    public class Types
    {
        // Message types.
        public enum MessageType : ushort
        {
            HEARBEAT,
            ACK,
            ERROR,
            SERVERMESSAGE,
            LOGIN,
            COMMAND,
            ROOM,
            WHO,
            SCORE,
            TELL
        }

        // Command types.
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

        // Client state types. I thought this might be usable, but
        // I never used it.
        public enum ClientState : byte
        {
            DISCONNECTED,
            LOGIN,
            NEWCHAR,
            PLAYING,
            LOGOUT
        }

        public enum AckType : byte
        {
            LOGIN
        }

        public enum LoginType : byte
        {
            EXISTING,
            NEW
        }

        // Server message types.
        public enum ServerMessageType : byte
        {
            CONSOLE,
            POPUP
        }

        // Chat message types. Depreciated.
        public enum ChatMessageType : byte
        {
            TELL,
            SAY,
            SHOUT,
            OOC
        }

        public enum QueryType : byte
        {
            SING,
            MULT
        }

        // Static query strings, for use with String.Format
        public static class Queries
        {
            public static string NameFromSocket = "call NameFromSocket({0})";

            public static string SocketFromName = "call SocketFromName(\"{0}\")";

            public static string ExistingLoginQuery = "select socket_id from players where character_name = \"{0}\" and pwd = \"{1}\"";

            public static string ExistingLogin = "update players set socket_id = {0} where character_name = \"{1}\"";
            
            public static string NewLogin = "select BIN_TO_UUID(id) as id from players where character_name = \"{0}\"";

            public static string CreateCharacter = "insert into players (id, character_name, pwd, room_id, socket_id) values (UUID_TO_BIN(UUID()), " +
                                                    "\"{0}\", \"{1}\", UUID_TO_BIN(\"620bab47-ca98-11eb-bd40-2cf05ddda1bf\"), {2})";

            public static string LogOut = "call Logout('{0}'";

            /// <summary>
            /// This query gets information about a room.
            /// <para>
            public static string Room = "call GetRoom(\"{0}\")";
            
            /// <include file='docs.xml' path='docs/members[@name="queries"]/RoomInDirection/*'/>
            public static string RoomInDirection = "select BIN_TO_UUID(exits.to_room) as new_room from exits " +
                                                   "inner join players on exits.from_room = players.room_id " +
                                                   "where direction = \"{0}\" and players.character_name = \"{1}\"";

            public static string UpdatePlayerRoom = "update players set room_id = UUID_TO_BIN(\"{0}\") where character_name = \"{1}\"";

            /// <summary>
            /// This query returns the other active players in the room.
            /// </summary>
            public static string PlayersInSameRoom = "call PlayersInSameRoom(\"{0}\")";

            public static string RoomPlayers = "select character_name from players where socket_id != -1 and " +
                                               "room_id = (select room_id from players where character_name = \"{0}\") and character_name not in (\"{1}\")";

            public static string RoomPlayersBySocket = "select socket_id from players where socket_id != -1 and " +
                                                       "room_id = (select room_id from players where socket_id = \"{0}\") and socket_id not in (\"{1}\")";

            public static string PlayersListeningToGlobal = "select socket_id from players where socket_id != -1 and ignoreGlobal == 0";

            public static string RoomMobs = "";

            public static string RoomItems = "";
        }
    }
}