using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using MySql.Data;
using MySql.Data.MySqlClient;
using Shared;
using System.Diagnostics;

namespace Server
{
    public class Database
    {
        string dbUser;
        string dbPass;
        static string connStr = "server=10.0.0.71;user={0};database=game;port=3306;password={1}";

        MySqlConnection connection;

        public void Connect()
        {
            try
            {
                Console.WriteLine("Please enter the datbase user: ");
                dbUser = Console.ReadLine();
                Console.WriteLine("Please enter the database password: ");
                dbPass = Console.ReadLine();
                connection = new MySqlConnection(String.Format(connStr, dbUser, dbPass));
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
}
