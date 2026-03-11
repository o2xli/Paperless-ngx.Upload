using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http.Json;
using System.Xml.Linq;
using System.Threading;

namespace PaperLess.Upload
{
    public class PaperlessNgxClient
    {
        private string target;
        private string token;

        public PaperlessNgxClient(string target, string token)
        {
            this.target = target;
            this.token = token;
        }

        public bool UploadDocument(string file, int dateTagId)
        {
            using HttpClient client = new();
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Token", token);

            client.Timeout = TimeSpan.FromSeconds(30);

            using var content = new MultipartFormDataContent();
            
            var values = new[]
            {
                new KeyValuePair<string, string>("tags", dateTagId.ToString()),
            };

            foreach (var keyValuePair in values)
            {
                content.Add(new StringContent(keyValuePair.Value), keyValuePair.Key);
            }
            var fi = new System.IO.FileInfo(file);
            if (fi.Length <= 0)
                return false;

            content.Add(new ByteArrayContent(System.IO.File.ReadAllBytes(file)), "document", fi.Name);

            var requestUri = $"{target}/api/documents/post_document/";
            var result = client.PostAsync(requestUri, content).GetAwaiter().GetResult();
            if (result.StatusCode == System.Net.HttpStatusCode.OK)
            {
                return true;
            }
            else
            {
                var errorContent = result.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                Console.Write($" Error: {result.StatusCode}");
                Console.WriteLine($" {errorContent}");
                return false;
            }
        
        }

        public int CreateTag(string tag)
        {
            using HttpClient client = new();
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Token", token);

            var formContent = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("name", tag),
            });


            var response = client.PostAsync($"{target}/api/tags/", formContent).GetAwaiter().GetResult();
            if (response.StatusCode != System.Net.HttpStatusCode.OK)
            {
                var errorContent = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                Console.Write($" Error: {response.StatusCode}");
                Console.WriteLine($" {errorContent}");
            }
            var tagResult = response.Content.ReadFromJsonAsync<Tags.Result>();
            return tagResult.Result.id;
        }

        public Dictionary<string, int> GetTags()
        {
            Dictionary<string, int> result = new();
            using HttpClient client = new();
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Token", token);
            var url = $"{target}/api/tags/";

            while (!string.IsNullOrWhiteSpace(url))
            {
                var response = client.GetFromJsonAsync<Tags>(url).GetAwaiter().GetResult();
                url = response.next?.ToString();
                var list = response.results.Select(t => new { t.name, t.id });
                foreach (var kvp in list)
                {
                    result[kvp.name] = kvp.id;
                }
            }
            return result;
           
        }


        public class Tags
        {
            public int count { get; set; }
            public object next { get; set; }
            public object previous { get; set; }
            public Result[] results { get; set; }

            public class Result
            {
                public int id { get; set; }
                public string slug { get; set; }
                public string name { get; set; }
                public int colour { get; set; }
                public string match { get; set; }
                public int matching_algorithm { get; set; }
                public bool is_insensitive { get; set; }
                public bool is_inbox_tag { get; set; }
                public int document_count { get; set; }
            }
        }

        
    }
}
