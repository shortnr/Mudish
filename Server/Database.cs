using System;
using System.Data;
using MySql.Data.MySqlClient;

namespace Server
{

    /// <summary>
    /// Provides simple MySQL access for the server application.
    /// 
    /// Responsibilities:
    /// - Manage a single MySqlConnection (open/close) using the connection string defined in connStr.
    /// - Execute queries that return result sets via Query(...) and non-query updates via Update(...).
    /// - Log connection/activity errors to the console for diagnostics.
    /// 
    /// Important notes:
    /// - This class is not thread-safe. Concurrent use from multiple threads can result in unpredictable behavior.
    /// - The current implementation constructs SQL using string formatting (String.Format) which is vulnerable
    ///   to SQL injection. Prefer parameterized queries or stored procedures for untrusted input.
    /// - The connection string contains placeholder values and should be secured (e.g., configuration, secrets manager).
    /// </summary>
    public class Database
    {
        // Connection string for MySQL database
        static string connStr = "server=localhost; user=mudish_server; database=mudish; port=3306; password=Bacon!Pancakes!";

        // MySQL connection object
        MySqlConnection connection;

        // Connects to the database
        public void Connect()
        {
            // Attempt to open the database connection
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

        // Disconnects from the database
        public void Disconnect()
        {
            connection.Close();
        }

        // Executes a query and returns the results as a DataTable
        public DataTable Query(string query, params object[] args)
        {
            // Create a MySqlCommand with the formatted query
            MySqlCommand cmd = new MySqlCommand(String.Format(query, args), connection);

            // DataTable to hold the query results
            DataTable data = new DataTable();

            // Execute the query and load results into the DataTable
            try
            {
                data.Load(cmd.ExecuteReader());
            }
            // Catch and log any exceptions that occur during query execution
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                Console.WriteLine(query);
            }

            // Return the populated DataTable
            return data;
        }

        // Executes a non-query update
        public void Update(string query, params object[] args)
        {
            // Create a MySqlCommand with the formatted query
            MySqlCommand cmd = new MySqlCommand(String.Format(query, args), connection);

            // Execute the non-query command
            try
            {
                cmd.ExecuteNonQuery();
            }
            // Catch and log any exceptions that occur during update execution
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                Console.WriteLine(query);
            }
        }
    }

    // Static class containing SQL query strings
    public static class Queries
    {
        // Get character name from socket ID
        public static string NameFromSocket = "select character_name from players where socket_id={0}";

        // Get socket ID from character name
        public static string SocketFromName = "select socket_id from players where character_name='{0}'";

        // Check for existing login credentials
        public static string ExistingLoginQuery = "select socket_id from players where character_name = '{0}' and pwd = '{1}'";

        // Update socket ID for existing login
        public static string ExistingLogin = "update players set socket_id = {0} where character_name = '{1}'";

        // Gets the player id if player exists (if this returns no rows, player does not exist)
        public static string NewLogin = "select BIN_TO_UUID(id) as id from players where character_name = '{0}'";

        // Create a new character
        public static string CreateCharacter = "insert into players (id, character_name, pwd, room_id, socket_id) values (UUID_TO_BIN(UUID()), " +
                                                "'{0}', '{1}', UUID_TO_BIN('abd53eab-d403-11f0-86ce-345a6044ad6a'), {2})";

        // Log out a player
        public static string LogOut = "call LogOut('{0}'";

        // Get current room information
        public static string Room = "select * from rooms where id = (select room_id from players where character_name='{0}')";

        // Get the room ID in a specified direction
        public static string RoomInDirection = "select BIN_TO_UUID(room_exits.destination_id) as new_room from room_exits " +
                                               "inner join players on room_exits.room_id = players.room_id " +
                                               "where direction = '{0}' and players.character_name = '{1}'";

        // Update player's current room
        public static string UpdatePlayerRoom = "update players set room_id = UUID_TO_BIN('{0}') where character_name = '{1}'";

        // Get players in the same room
        public static string PlayersInSameRoom = "call PlayersInSameRoom('{0}')";

        // Get players in the same room excluding a specific player
        public static string RoomPlayers = "select character_name from players where socket_id != -1 and " +
                                           "room_id = (select room_id from players where character_name = '{0}') and character_name not in ('{1}')";

        // Get socket IDs of players in the same room excluding a specific socket
        public static string RoomPlayersBySocket = "select socket_id from players where socket_id != -1 and " +
                                                   "room_id = (select room_id from players where socket_id = '{0}') and socket_id not in ('{1}')";

        // Get players listening to global messages
        public static string PlayersListeningToGlobal = "select socket_id from players where socket_id != -1 and ignoreGlobal == 0";

        public static string RoomMobs = "";

        public static string RoomItems = "";
    }
}
