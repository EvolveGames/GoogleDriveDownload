using System;
using System.Net;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;

namespace GoogleDriveDownload
{
    class GDrive
    {
        public static CookieContainer cookie_container = new CookieContainer();

        public static string DirectDownloadLink(string url)
        {
            string direct = CreateDirectLink(url);

            HttpClientHandler handler = new HttpClientHandler
            {
                CookieContainer = cookie_container,
                UseCookies = true,
                AllowAutoRedirect = true,
            };
            
            using (HttpClient client = new HttpClient(handler))
            {
                client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/99.0.9999.999 Safari/537.36");
                client.DefaultRequestHeaders.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3");
                client.DefaultRequestHeaders.Add("Accept-Encoding", "gzip, deflate, br");
                client.DefaultRequestHeaders.Add("Accept-Language", "en-US,en;q=0.9");

                var response = client.GetAsync(direct).Result;

                if (response.IsSuccessStatusCode)
                {
                    string html = response.Content.ReadAsStringAsync().Result;

                    Match
                        uuid_match = Regex.Match(html, @"name=""uuid""\s+value=""([^""]+)"""),
                        id_match = Regex.Match(html, @"name=""id""\s+value=""([^""]+)""");

                    if (uuid_match.Success && id_match.Success)
                    {
                        string uuid = uuid_match.Groups[1].Value;
                        string id = id_match.Groups[1].Value;

                        return $"https://drive.usercontent.google.com/download?id={id}&export=download&authuser=0&confirm=t&uuid={uuid}";
                    }
                }
            }
            return null;
        }

        public static void DownloadFile(string url, string path)
        {
            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/99.0.9999.999 Safari/537.36");

                var response = client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead).Result;

                if (response.IsSuccessStatusCode)
                {
                    using (var fileStream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None))
                    {
                        string bef = "";

                        var content_length = response.Content.Headers.ContentLength ?? -1;
                        var buffer = new byte[8192];
                        var bytes_read = 0L;
                        using (var stream = response.Content.ReadAsStreamAsync().Result)
                        {
                            int read;
                            while ((read = stream.Read(buffer, 0, buffer.Length)) > 0)
                            {
                                fileStream.Write(buffer, 0, read);
                                bytes_read += read;

                                var progress = (int)((bytes_read / content_length) * 100f);
                                if(bef != $"{BytesToString(bytes_read)}/{BytesToString(content_length)}")
                                {
                                    bef = $"{BytesToString(bytes_read)}/{BytesToString(content_length)}";
                                    Console.WriteLine(bef);
                                }


                            }
                        }
                    }
                }
            }
        }

        public static GDriveFileInfo GetFileInfo(string url)
        {
            using (HttpClient client = new HttpClient())
            {
                var response = client.SendAsync(new HttpRequestMessage(HttpMethod.Head, url)).Result;

                if(response.IsSuccessStatusCode)
                {
                    var headers = response.Content.Headers;
                    string name = "data.bin";

                    if (response.Content.Headers.ContentDisposition != null)
                    {
                        ContentDispositionHeaderValue contentDisposition = response.Content.Headers.ContentDisposition;
                        name = (contentDisposition.FileNameStar ?? contentDisposition.FileName).Replace("\"", "");
                    }


                    return new GDriveFileInfo(name, headers.ContentLength.Value);
                }
            }
            return new GDriveFileInfo("", 0);
        }

        static string CreateDirectLink(string url)
        {
            string id = GetID(url);
            return !string.IsNullOrEmpty(id) ? $"https://drive.google.com/uc?export=download&id={id}" : url;
        }

        static string GetID(string url)
        {
            Match match = Regex.Match(url, @"\/d\/([A-Za-z0-9_-]+)\/");
            return match.Success ? match.Groups[1].Value : "";
        }

        public static string BytesToString(long byteCount)
        {
            string[] suffixes = { "B", "KB", "MB", "GB", "TB", "PB", "EB", "ZB", "YB" };
            if (byteCount == 0)
                return "0" + suffixes[0];

            var bytes = Math.Abs(byteCount);
            var place = Convert.ToInt32(Math.Floor(Math.Log(bytes, 1024)));
            var num = Math.Round(bytes / Math.Pow(1024, place), 1);
            return $"{Math.Sign(byteCount) * num} {suffixes[place]}";
        }
    }
    struct GDriveFileInfo
    {
        public string name { get; set; }
        public long length { get; set; }

        public GDriveFileInfo(string name, long length)
        {
            this.name = name;
            this.length = length;
        }
        public override string ToString()
        {
            return $"NAME: {name}, {GDrive.BytesToString(length)}";
        }
    }

}

