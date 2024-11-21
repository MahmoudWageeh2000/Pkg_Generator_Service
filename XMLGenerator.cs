using System;
using System.Collections.Generic;
using System.Data;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Scriban;
using Scriban.Runtime;
using Scriban.Syntax;
using System.Dynamic;
using System.Data;
using static Package_Generator_Service.Program;
using Microsoft.Identity.Client;
using System.Xml;
using Package_Generator_Service;

namespace ConsoleApp1
{
    internal class XMLGenerator
    {
        //private string connectionString = "Data Source=10.1.1.27;Initial Catalog=DPackaging;User ID=cms;Password=D@cms2015;Encrypt=True;TrustServerCertificate=True;";
        private DatabaseHelper db;
        public static TimeSpan album_duration = new TimeSpan(0);
        public bool batch = false;
        public object store_id = "";
        
        public XMLGenerator(string connectionString)
        {
            db = new DatabaseHelper(connectionString);
        }
        public void Generate(string filePath, string outputFile, List<AssetHash> assetHashes, string cover_hash_sum , int album_num , string ID , List<TrackSize> tracksize, string isrc)
        {
            var variables = ParseTemplate(filePath);
            batch = false;
            // Fetch data dynamically based on the parsed variables and structure it accordingly
            dynamic data = FetchDataFromDatabase(variables, assetHashes,cover_hash_sum, album_num, ID , tracksize , isrc);

            // Render template with data
            var xmlOutput = RenderTemplate(filePath, data);

            // Save or use the generated XML
            File.WriteAllText(outputFile, xmlOutput);
        }

        public void GenerateBatch (string filepath,string outputFile, List<AssetHash> assetHashes, string cover_hash_sum, int album_num, string ID)
        {
            List<int> IdsWithBatch = new List<int> { 1, 3, 4, 9, 10, 12, 16, 17, 18, 22, 26, 39, 46,  64 ,27,61};
            List<int> IdsWithoutBatch = new List<int>{2, 6, 7, 8, 11, 13, 14, 15, 19, 20, 21, 23, 24, 25, 27, 28, 29, 30, 31, 32, 33, 34, 35, 37, 38, 40, 41, 42, 43, 44, 45, 47, 48, 49, 50,51, 52, 54, 55, 56, 57, 58, 59, 60, 61, 62
    };
            string store_id_query = $"select store_id from packages where pkg_id = {ID}";
            object store_id = db.ExecuteScalar(store_id_query);

            if (store_id != null && Convert.ToInt32(store_id) == 5)
            {
                var variables = ParseTemplate(filepath);
                batch = true;
                dynamic data = FetchDataFromDatabase(variables, assetHashes, cover_hash_sum, album_num, ID, [] , null);
                var xmlOutput = RenderTemplate(filepath, data);
                File.WriteAllText(outputFile, xmlOutput);
            }
            else if (IdsWithBatch.Contains((int)store_id))
            {
                var  xmlOutput = string.Empty;
                File.WriteAllText(outputFile, xmlOutput);
            }          

            // Save or use the generated XML
            //File.WriteAllText(outputFile, xmlOutput);

        }
        
        public dynamic FetchDataFromDatabase(List<string> variables, List<AssetHash> assetHashes,string cover_hash_sum, int album_num, string ID, List<TrackSize> tracksize , string isrc)
        
        {
            
            var variablesData = variables.AsEnumerable().Where(x => x.StartsWith("album.")).Select(x => x.Replace("album.", "")).ToList();
            dynamic data = new ExpandoObject();
            var dataDict = (IDictionary<string, object>)data;
            DataTable albumTable = default!;
            object track_count = default!;
            object album_count = default!;int image_num = 0;
            string store_id_query2 = $"select store_id from packages where pkg_id = {ID}";
            store_id = db.ExecuteScalar(store_id_query2);


            if (batch)
            {
                string queryAlbum = $"select album_ubc from t_packages_info where pkg_id = {ID} group by album_ubc ";
                albumTable = db.ExecuteQuery(queryAlbum);
                string album_count_query = $"select MAX(album_num) from t_packages_info where pkg_id={ID}";
                album_count = db.ExecuteScalar(album_count_query);
            }
            else if (!batch && store_id.ToString() != "40") 
            {
                string queryAlbum = $"SELECT * FROM t_packages_info WHERE pkg_id = {ID} AND album_num={album_num}  order by album_num";
                albumTable = db.ExecuteQuery(queryAlbum);
                image_num = albumTable.Rows.Count + 1;
                string Track_count_query = $"select COUNT(asset_isrc) from t_packages_info where pkg_id={ID}";
                track_count = db.ExecuteScalar(Track_count_query);
                string store_id_query = $"select store_id from packages where pkg_id = {ID}";
                store_id = db.ExecuteScalar(store_id_query);

            }
            else if (store_id.ToString() =="40")
            {
                string queryAlbum = $"SELECT * FROM t_packages_info WHERE pkg_id = {ID} AND album_num={album_num} AND asset_isrc ='{isrc}'  order by album_num";
                albumTable = db.ExecuteQuery(queryAlbum);
                image_num = albumTable.Rows.Count + 1;
                string Track_count_query = $"select COUNT(asset_isrc) from t_packages_info where pkg_id={ID}";
                track_count = db.ExecuteScalar(Track_count_query);
            }
            
            
            

            if (albumTable.Rows.Count > 0)
            {
                dataDict["creation_full_date_time"] = Form1.creationFullDateTime;
                dataDict["creation_date"] = Form1.creationDate;
                dataDict["o_creation_date"] = Form1.oCreationDate;
                dataDict["date"] = Form1.formattedDate;
                dataDict["creation_date_time"] = Form1.creationDateTime;
                dataDict["full_date_time"] = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.fff");
                dataDict["date_time"] = DateTime.Now.ToString("s");
                dataDict["album_count"] = album_count;
                dataDict["track_count"] = track_count;
                dataDict["image_size"] = Form1.imageSize;
                
                //string release_date = albumTable.Rows[0]["release_date"]?.ToString() ?? DateTime.Now.ToString("yyyyMMdd");
                //dataDict["release_year"] = new string(release_date.Take(4).ToArray());
                //relase_date => yyyyMMdd
                //dataDict["release_date"] = DateTime.Now.ToString("yyyy-mm-dd");
                foreach (var variable in variablesData)
                {

                    if (variable=="release"||variable=="release_year"||variable=="release_date"|| variable == "start_date" || variable == "end_date" || variable == "takedown_date")
                    {
                        string dateString = null;

                        if (variable == "release" || variable == "release_year")
                        {
                            dateString = string.IsNullOrEmpty(albumTable.Rows[0]["release_date"].ToString()) ? DateTime.Now.ToString("yyyyMMdd") : albumTable.Rows[0]["release_date"].ToString();
                        }
                        else
                        {
                            dateString = string.IsNullOrEmpty(albumTable.Rows[0][variable].ToString()) ? DateTime.Now.ToString("yyyyMMdd") : albumTable.Rows[0][variable].ToString();
                        }
                        // Extract year, month, and day from the string and convert them to integers
                        int year = int.Parse(dateString.Substring(0, 4));
                        int month = int.Parse(dateString.Substring(4, 2));
                        int day = int.Parse(dateString.Substring(6, 2));
                        DateTime date = new DateTime(year, month, day);
                        if (variable == "release")
                            dataDict[variable] = date.ToString("ddMMyyyy");
                        else if (variable == "release_date" || variable == "start_date" || variable == "end_date" || variable == "takedown_date")
                            dataDict[variable] = date.ToString("yyyy-MM-dd");
                        else if (variable == "release_year")
                            dataDict[variable] = date.ToString("yyyy");
                        else
                            dataDict[variable] = date;
                    }
                    else if (variable.Contains("youtube_track_duration"))
                    {
                        string[] duration = albumTable.Rows[0]["track_duration"].ToString().Split(":");
                        TimeSpan time = TrackDurationFormat(duration);
                        album_duration += time;
                        dataDict[variable] = $"PT{time.Hours.ToString("D1")}H{time.Minutes.ToString("D2")}M{time.Seconds.ToString("D2")}S";
                    }
                    else if (albumTable.Columns.Contains(variable) && !variable.Contains("_list"))
                    {
                        
                        /*if (variable.Contains("track_duration"))
                        {
                            string[] duration = albumTable.Rows[0][variable].ToString().Split(":");
                            dataDict[variable] = $"{duration[0]}H{duration[1]}M{duration[2]}S";
                        }
                        else
                        {*/
                        //}
                        dataDict[variable] = albumTable.Rows[0][variable];

                    }
                    else if (variable.Contains("_list_string"))
                    {
                        var list_name = variable.Split("_list_string")[0];
                        string[] strings = albumTable.Rows[0][list_name].ToString().Split(",");
                        /*if (strings.Length >= 1 && strings[0] != "")
                            dataDict[variable] = strings.ToList();*/
                       
                        if (variable.Contains("artist"))
                        {
                            string[] stringsAr = albumTable.Rows[0][list_name + "_a"].ToString().Split(",");
                            string[] stringID = albumTable.Rows[0][list_name + "_id"].ToString().Split(",");
                            string[] stringappleID = albumTable.Rows[0][list_name + "_apple_id"].ToString().Split(",");
                            List<Artist> artists = new List<Artist>();
                            int seq = 1;
                            if (strings.Length > 0 && strings[0] != "")
                            {
                                if (store_id.ToString() == "58")
                                {
                                    for (int i = 0; i < strings.Length; i++)
                                    {
                                        Artist artist = new Artist
                                        {
                                            name = strings[i],
                                            name_ar = stringsAr[i] is not null ? stringsAr[i] : "",
                                            seq = seq,
                                            id = stringID[i],
                                            apple_id = stringappleID[i]
                                        };
                                        artists.Add(artist);
                                        seq++;
                                    }
                                    dataDict[variable] = artists;
                                }
                                else
                                {
                                    for (int i = 0; i < strings.Length; i++)
                                    {
                                        Artist artist = new Artist
                                        {
                                            name = strings[i],
                                            name_ar = stringsAr[i] is not null ? stringsAr[i] : "",
                                            seq = seq,
                                            id = stringID[i],
                                            apple_id = ""
                                        };
                                        artists.Add(artist);
                                        seq++;
                                    }
                                    dataDict[variable] = artists;

                                }
                            }
                            else
                            {
                                dataDict[variable] = null;
                            }
                        }
                        else
                        {

                            if (strings.Length >= 1 && strings[0] != "")
                                dataDict[variable] = strings.ToList();
                        }
                    }
                    else if (variable.Contains("_list"))
                    {
                        var list_name = variable.Split("_list")[0];
                        dataDict[variable] = List_Recursion(list_name, variables, albumTable, assetHashes,cover_hash_sum ,tracksize);
                        if(album_duration.TotalSeconds > 0)
                        {
                            dataDict["album_duration"] = $"PT{album_duration.Hours:D2}H{album_duration.Minutes:D2}M{album_duration.Seconds:D2}S";
                        }
                    }

                    else
                    {
                        if (variable.Contains("hash_sum_image"))
                        {
                            dataDict[variable] = cover_hash_sum;
                        }
                        if (variable.Contains("image_num"))
                        {
                            dataDict[variable] = image_num;
                        }
                    }
                    
                }
                // data.album_upc = albumTable.Rows[0]["album_upc"];
                // data.creation_date_time = albumTable.Rows[0]["creation_date_time"];
            }
            return data;
        }
        public List<object> List_Recursion(string list_name, List<string> variables, DataTable dataTable, List<AssetHash> assetHashes,string cover_hash_sum, List<TrackSize> tracksize)
        {
            var variablesList = variables.AsEnumerable().Where(x => x.StartsWith(list_name + ".")).Select(x => x.Replace(list_name + ".", "")).ToList();
            /*dynamic datalist = new ExpandoObject();
            var listDict = (IDictionary<string, object>)datalist;*/
            //TimeSpan total_duration = new TimeSpan(0);

            List<object> list = new List<object>();
            foreach (DataRow row in dataTable.Rows)
            {
                var listDict = new ExpandoObject();
                var listDictDict = (IDictionary<string, object>)listDict;
                ///Fixed Objects
                listDictDict["creation_full_date_time"] = Form1.creationFullDateTime;
                listDictDict["creation_date"] = Form1.creationDate;
                listDictDict["o_creation_date"] = Form1.oCreationDate;
                listDictDict["date"] = Form1.formattedDate;
                listDictDict["creation_date_time"] = Form1.creationDateTime;
                listDictDict["full_date_time"] = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.fff");
                listDictDict["date_time"] = DateTime.Now.ToString("s");
                listDictDict["image_size"] = Form1.imageSize;
                foreach (var variable in variablesList)
                {
                    if (variable.Contains("track_file_size"))
                    {
                        listDictDict[variable] = tracksize.Find(x => x.isrc == row["asset_isrc"].ToString()).size;

                    }                    
                    if (variable.Contains("HASH_SUM"))
                    {
                        listDictDict[variable] = assetHashes.Find(x => x.ISRC == row["asset_isrc"].ToString()).MD5Hash;
                    }
                    if (variable.Contains("hash_sum_xml"))
                    {
                        listDictDict[variable] = assetHashes.Find(x => x.ISRC == row["album_ubc"].ToString()).MD5Hash;
                    }
                    if (variable.Contains("hash_sum_image"))
                    {
                        listDictDict[variable] = cover_hash_sum;
                    }
                    if (variable == "release" || variable == "release_year" || variable == "release_date" || variable == "start_date" || variable == "end_date" || variable == "takedown_date")
                    {
                        string dateString = null;

                        if (variable == "release" || variable == "release_year")
                        {
                            dateString = string.IsNullOrEmpty(row["release_date"].ToString()) ? DateTime.Now.ToString("yyyyMMdd") : row["release_date"].ToString();
                        }
                        else
                        {
                            dateString = string.IsNullOrEmpty(row[variable].ToString()) ? DateTime.Now.ToString("yyyyMMdd") : row[variable].ToString();
                        }

                        // Extract year, month, and day from the string and convert them to integers
                        int year = int.Parse(dateString.Substring(0, 4));
                        int month = int.Parse(dateString.Substring(4, 2));
                        int day = int.Parse(dateString.Substring(6, 2));
                        DateTime date = new DateTime(year, month, day);
                        if (variable == "release")
                            listDictDict[variable] = date.ToString("ddMMyyyy");
                        else if (variable == "release_date" || variable == "start_date" || variable == "end_date" || variable == "takedown_date")
                            listDictDict[variable] = date.ToString("yyyy-MM-dd");
                        else if (variable == "release_year")
                            listDictDict[variable] = date.ToString("yyyy");
                        else
                            listDictDict[variable] = date;
                    }
                    else if (variable.Contains("fb_track_duration"))
                    {
                        string[] duration = row["track_duration"].ToString().Split(":");
                        TimeSpan time = TrackDurationFormat(duration);
                        album_duration += time;
                       listDictDict[variable] = $"PT{time.Hours.ToString("D2")}H{time.Minutes.ToString("D2")}M{time.Seconds.ToString("D2")}S";
                    }
                    else if (variable.Contains("mondia_track_duration"))
                    {
                        string[] duration = row["track_duration"].ToString().Split(":");
                        TimeSpan time = TrackDurationFormat(duration);
                        album_duration += time;
                        listDictDict[variable] = $"{time.TotalSeconds.ToString()}";
                    }
                    else if (variable.Contains("youtube_track_duration"))
                    {
                        string[] duration = row["track_duration"].ToString().Split(":");
                        TimeSpan time = TrackDurationFormat(duration);
                        album_duration += time;
                        listDictDict[variable] = $"PT{time.Hours.ToString("D1")}H{time.Minutes.ToString("D2")}M{time.Seconds.ToString("D2")}S";
                    }
                    else if (dataTable.Columns.Contains(variable) && !variable.Contains("_list"))
                    {
                        if (variable.Contains("track_duration"))
                        {
                            if (row["store_id"].ToString() == "5")
                            {
                                string[] duration = row[variable].ToString().Split(":");
                                album_duration += TrackDurationFormat(duration);
                                listDictDict[variable] = $"{duration[1]}M{duration[2]}S";
                            }
                            else { 
                            string[] duration = row[variable].ToString().Split(":");
                            album_duration += TrackDurationFormat(duration);
                            listDictDict[variable] = $"{duration[0]}H{duration[1]}M{duration[2]}S";
                            }
                        }
                        else if (variable.Contains("fb_track_duration"))
                        {
                            string[] duration = row["track_duration"].ToString().Split(":");
                            TimeSpan time = TrackDurationFormat(duration);
                            album_duration += time;
                            listDictDict[variable] = $"PT{time.Hours.ToString("D2")}H{time.Minutes.ToString("D2")}M{time.Seconds.ToString("D2")}S";
                        }
                        else if (variable.Contains("youtube_track_duration"))
                        {
                            string[] duration = row["track_duration"].ToString().Split(":");
                            TimeSpan time = TrackDurationFormat(duration);
                            album_duration += time;
                            listDictDict[variable] = $"PT{time.Hours.ToString("D1")}H{time.Minutes.ToString("D2")}M{time.Seconds.ToString("D2")}S";
                        }
                        else { 
                        listDictDict[variable] = row[variable];
                        }
                    }
                    else if (variable.Contains("_list_string"))
                    {
                        string list_string_name= variable.Split("_list_string")[0];
                        string[] strings = row[list_string_name].ToString().Split(",");

                        if (variable.Contains("_artist_"))
                        {
                            string[] stringsAr = row[list_string_name+"_a"].ToString().Split(",");
                            string[] stringID = row[list_string_name + "_id"].ToString().Split(",");
                            string[] stringappleID = row[list_string_name + "_apple_id"].ToString().Split(",");
                            List<Artist> artists = new List<Artist>();
                            int seq = 1;
                            if (strings.Length > 0 && strings[0]!="")
                            {
                                if (store_id.ToString() == "58")
                                {
                                    for (int i = 0; i < strings.Length; i++)
                                    {

                                        Artist artist = new Artist
                                        {
                                            name = strings[i],
                                            name_ar = stringsAr[i] is not null ? stringsAr[i] : "",
                                            seq = seq,
                                            id = stringID[i],
                                            apple_id = stringappleID[i]
                                        };
                                        artists.Add(artist);
                                        seq++;
                                    }
                                    listDictDict[variable] = artists;
                                }
                                else
                                {
                                    for (int i = 0; i < strings.Length; i++)
                                    {

                                        Artist artist = new Artist
                                        {
                                            name = strings[i],
                                            name_ar = stringsAr[i] is not null ? stringsAr[i] : "",
                                            seq = seq,
                                            id = stringID[i],
                                            apple_id = ""
                                        };
                                        artists.Add(artist);
                                        seq++;
                                    }
                                    listDictDict[variable] = artists;
                                }
                            }
                            else
                            {
                                listDictDict[variable] = null;
                            }
                        }
                        else
                        {
                            
                            if (strings.Length >= 1 && strings[0] != "")
                                listDictDict[variable] = strings.ToList();
                        }
                        
                       /* else
                        {
                            listDictDict[list_string_name] = null;
                        }*/
                    }
                    else if (variable.Contains("_list"))
                    {
                        var nested_list_name = variable.Split("_list")[0];
                        if (nested_list_name.Contains("artist"))
                        {

                        }
                        listDictDict[variable] = List_Recursion(nested_list_name, variables, dataTable, assetHashes,cover_hash_sum ,tracksize);
                    }
                }
                
                list.Add(listDict);
                
            }
            return list;
        }
        public List<string> ParseTemplate(string filePath)
        {
            // Load the template from the file
            var templateContent = File.ReadAllText(filePath);

            // Parse the template
            var template = Template.Parse(templateContent);

            // Check for errors during parsing
            if (template.HasErrors)
            {
                foreach (var message in template.Messages)
                {
                    // Handle errors (e.g., log them or throw an exception)
                    Console.WriteLine($"Template Error: {message}");
                }
                return null;
            }
            var variables = new HashSet<string>();
            Traverse(template.Page.Body, variables);

            return new List<string>(variables);
        }
        private void Traverse(ScriptNode node, HashSet<string> variables)
        {
            // Check if the node is null before proceeding
            if (node == null) return;
            if (node is ScriptMemberExpression memberExpr)
            {
                // Extract the target (e.g., "data") and the member (e.g., "album_isrc")
                if (memberExpr.Target is ScriptVariableGlobal target)
                {
                    var expression = $"{target.Name}.{memberExpr.Member.Name}";
                    variables.Add(expression);
                }
            }
            else
            {
                // If it's another kind of node, traverse its children (if any)
                foreach (var child in node.Children)
                {
                    Traverse(child, variables);
                }
            }
        }

        public string RenderTemplate(string templatePath, ExpandoObject data)
        {
            // Load the template from the file
            var templateContent = File.ReadAllText(templatePath).Replace("album.", "");

            // Parse the template
            var template = Template.Parse(templateContent);

            // Check for errors during parsing
            if (template.HasErrors)
            {
                foreach (var message in template.Messages)
                {
                    // Handle errors (e.g., log them or throw an exception)
                    Console.WriteLine($"Render Error: {message}");
                }
                return null;
            }
            var sObject = BuildScriptObject(data);
            //var scriptObject = new ScriptObject();
            var context = new TemplateContext();
            context.PushGlobal(sObject);
            return template.Render(context);
        }
        private static ScriptObject BuildScriptObject(ExpandoObject expando)
        {
            var dict = (IDictionary<string, object>)expando;
            var scriptObject = new ScriptObject();

            foreach (var kv in dict)
            {
                var renamedKey = StandardMemberRenamer.Rename(kv.Key);

                if (kv.Value is ExpandoObject expandoValue)
                {
                    scriptObject.Add(renamedKey, BuildScriptObject(expandoValue));
                }
                else
                {
                    scriptObject.Add(renamedKey, kv.Value);
                }
            }

            return scriptObject;
        }
        protected TimeSpan TrackDurationFormat(string[] duration)
        {
            int hour = int.Parse(duration[0]);
            int minutes = int.Parse(duration[1]);
            int seconds = int.Parse(duration[2]);
            TimeSpan time = new TimeSpan(hour, minutes, seconds);
            return time;
        }
        public class Artist
        {
            public string name { get; set; } = default!;
            public string name_ar { get; set; } = default!;
            public int seq { get; set; } = 1;
            public string id { get; set; } = default;
            public string apple_id { get; set; } = default!;
        }
    }
}
