using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace ConsoleApp1
{
    class Program
    {
        static void Main(string[] args)
        {
            string id = "-";
            string hash = "jwthash";
          
            List<string> passwords = new List<string> { "pw to validate"};
            foreach (var pass in passwords)
            {
                
                    Console.WriteLine(GetHash(pass, id));
 

            }

            Console.WriteLine("Hello World!");


        


        }




        private static string GetHash(string password, string salt)
        {
            string hash = "";
            using (SHA256 sha256Hash = SHA256.Create())
            {
                // ComputeHash - returns byte array  
                byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(password + salt));

                // Convert byte array to a string   
                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
                {
                    builder.Append(bytes[i].ToString("x2"));
                }
                hash = builder.ToString();
            }
            return hash;
        }
    }
}
