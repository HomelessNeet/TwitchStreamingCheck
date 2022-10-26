using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;

namespace TwitchStreamingCheck
{
    class Program
    {
        static string token;                        //Twitch API所使用的OAuth與Client_ID
        static string client_id;
        static string id;
        static void Main(string[] args)
        {            
            Init();
            Run();
            Console.ReadKey();
        }
        static void Init()                          //讀取Token
        {
            try
            {
                var s = File.ReadAllText("Token.json");
                Info info = JsonConvert.DeserializeObject<Info>(s);
                token = info.Token;
                client_id = info.Client_Id;
                id = info.Id;
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.Message);
                Console.WriteLine("無法取得Token,按任意鍵結束");
                Console.ReadKey();
                Environment.Exit(0);
            }            
        }
        static async void Run()
        {
            List<string> followed = new List<string>();
            string query = "";
            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Clear();
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
                client.DefaultRequestHeaders.Add("Accept", "application/json");
                client.DefaultRequestHeaders.Add("Client-Id", client_id);

                #region 取得追隨頻道前100
                using (HttpResponseMessage response = await client.GetAsync($"https://api.twitch.tv/helix/users/follows?from_id={id}&first=100"))
                {
                    string s = await response.Content.ReadAsStringAsync();
                    dynamic jo = JObject.Parse(s);
                    int total = jo.total;
                    if (total > 0)                  //將追隨清單轉換成QueryString
                    {
                        var list = ((JArray)jo.data).Cast<dynamic>().ToList();
                        query += $"user_login={(string)list[0].to_login}";
                        for (int i = 1; i < total; i++)
                        {
                            query += $"&user_login={(string)list[i].to_login}";
                        }
                    }                    
                }
                #endregion

                #region 取得直播中的追隨頻道 無追隨則取得Top20
                using (HttpResponseMessage response=await client.GetAsync($"https://api.twitch.tv/helix/streams?{query}"))
                {
                    string s = await response.Content.ReadAsStringAsync();
                    dynamic jo = JObject.Parse(s);
                    var list = ((JArray)jo.data).Cast<dynamic>().ToList();
                    if (list.Any())
                    {
                        foreach (dynamic dyna in list)
                        {
                            Console.OutputEncoding = Encoding.UTF8;
                            Console.WriteLine($"User_Name = {dyna.user_name}");
                            Console.WriteLine($"StreamUrl = https://www.twitch.tv/{dyna.user_login}");
                            Console.WriteLine($"Game_Name = {dyna.game_name}\n");
                        }
                    }
                    else
                    {
                        Console.WriteLine("現在沒直播");
                    }
                }
                #endregion
            }
        }
    }
    class Info
    {
        public string Token { get; set; }
        public string Client_Id { get; set; }
        public string Id { get; set; }
    }
}
