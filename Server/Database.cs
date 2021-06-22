using System;
using System.Data;
using MySql.Data.MySqlClient;

namespace Server
{
    public class Database
    {
        static string connStr = "server=db_ip; user=username; database=game; port=3306; password=password";

        MySqlConnection connection;

        public void Connect()
        {
            try
            {
                connection = new MySqlConnection(connStr);
                Console.WriteLine("Connecting...");
                connection.Open();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

        public void Disconnect()
        {
            connection.Close();
        }

        public DataTable Query(string query, params object[] args)
        {
            MySqlCommand cmd = new MySqlCommand(String.Format(query, args), connection);
            
            DataTable data = new DataTable();

            try
            {
                data.Load(cmd.ExecuteReader());
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                Console.WriteLine(query);
            }

            return data;
        }

        public void Update(string query, params object[] args)
        {

            MySqlCommand cmd = new MySqlCommand(String.Format(query, args), connection);
            try
            {
                cmd.ExecuteNonQuery();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                Console.WriteLine(query);
            }
            
        }
    }

    public static class Queries
    {
        public static string NameFromSocket = "call NameFromSocket({0})";

        public static string SocketFromName = "call SocketFromName('{0}')";

        public static string ExistingLoginQuery = "select socket_id from players where character_name = '{0}' and pwd = '{1}'";

        public static string ExistingLogin = "update players set socket_id = {0} where character_name = '{1}'";

        public static string NewLogin = "select BIN_TO_UUID(id) as id from players where character_name = '{0}'";

        public static string CreateCharacter = "insert into players (id, character_name, pwd, room_id, socket_id) values (UUID_TO_BIN(UUID()), " +
                                                "'{0}', '{1}', UUID_TO_BIN('620bab47-ca98-11eb-bd40-2cf05ddda1bf'), {2})";

        public static string LogOut = "call LogOut('{0}'";

        /// <summary>
        /// This query gets information about a room.
        /// <para>
        public static string Room = "call GetRoom('{0}')";

        /// <include file='docs.xml' path='docs/members[@name="queries"]/RoomInDirection/*'/>
        public static string RoomInDirection = "select BIN_TO_UUID(exits.to_room) as new_room from exits " +
                                               "inner join players on exits.from_room = players.room_id " +
                                               "where direction = '{0}' and players.character_name = '{1}'";

        public static string UpdatePlayerRoom = "update players set room_id = UUID_TO_BIN('{0}') where character_name = '{1}'";

        /// <summary>
        /// This query returns the other active players in the room.
        /// </summary>
        public static string PlayersInSameRoom = "call PlayersInSameRoom('{0}')";

        public static string RoomPlayers = "select character_name from players where socket_id != -1 and " +
                                           "room_id = (select room_id from players where character_name = '{0}') and character_name not in ('{1}')";

        public static string RoomPlayersBySocket = "select socket_id from players where socket_id != -1 and " +
                                                   "room_id = (select room_id from players where socket_id = '{0}') and socket_id not in ('{1}')";

        public static string PlayersListeningToGlobal = "select socket_id from players where socket_id != -1 and ignoreGlobal == 0";

        public static string RoomMobs = "";

        public static string RoomItems = "";
    }
}
