using ConsoleApp1;
using OfficeOpenXml;
using System.Data;
using System.Net.Sockets;
using System.Net;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Xml.Linq;
using System.Diagnostics;

namespace Package_Generator_Service
{
    public partial class Form1 : Form
    {
        
        private NotifyIcon _notifyIcon;


        private string connectionString = "";
        private DatabaseHelper db;
        private static System.Threading.Timer _timer;
        private static SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);

        private int MediaFILESCounter = 0;

        private string department_num = "";
        public static long imageSize { get; set; } = 0;
        public static string creationFullDateTime { get; } = DateTime.Now.ToString("yyyyMMddHHmmssfff");
        public static string creationDateTime { get; } = DateTime.Now.ToString("yyyyMMddHHmmss");
        public static string creationDate { get; } = DateTime.Now.ToString("yyyyMMdd");
        public static string oCreationDate { get; } = DateTime.Now.ToString("ddMMyy");
        public static string formattedDate { get; } = DateTime.Now.ToString("yyyy-MM-dd");

        private bool GetFolders = true;

        private bool CreateExl = true;

        private string Full_creatation_date, PackageFolder, ResourcesFolder, MetadataFile, FinalFolder, mediapath, temppackagefolder = "1", BatchFolder, packagesFolderwithoutID;

        static string logFolderPath = Path.Combine("..\\..\\..\\", "Logs");

        string logFilePath = Path.Combine(logFolderPath, $"{DateTime.Now:yyyy-MM-dd}_log.txt");

        List<AssetHash> assetHashes = new List<AssetHash>();
        List<AssetHash> CoverHashes = new List<AssetHash>();
        List<AssetHash> XMLHASHES = new List<AssetHash>();
        List<TrackSize> tracksize = new List<TrackSize>();
        public string networkFolderPath = @"\\10.1.1.26\Data";

        public string username = "cms";
        public string password = "cms@Mazzika";
        public string domain = "";

        public Form1()
        {

            InitializeComponent();
            InitializeNotifyIcon();
            progressBar1.Minimum = 0; // Set minimum value
            progressBar1.Maximum = 100;
            this.LoadConfig();
            // Create network credentials

            this.db = new DatabaseHelper(this.connectionString);
            // Set network credentials and map the network folder

            // Your logic to interact with the network folder
            // After finishing work with the network folder, disconnect
            // Only clear credentials for the specific server
        }

        // Method to map network credentials to a drive
        public static void SetNetworkCredentials(string networkFolder, NetworkCredential credentials)
        {
            string networkDrive = @"X:"; // Specify the drive letter to map (for example Z:)

            // Prepare the net use command
            string netUseCmd = $"net use {networkDrive} {networkFolder} /user:{credentials.UserName} {credentials.Password}";

            // Execute the command and capture output for debugging
            var processStartInfo = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = "/C " + netUseCmd,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            try
            {
                using (var process = Process.Start(processStartInfo))
                {
                    process.WaitForExit();

                    // Check for any errors
                    string output = process.StandardOutput.ReadToEnd();
                    string error = process.StandardError.ReadToEnd();

                  
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("An error occurred while connecting to the network folder: " + ex.Message);
            }
        }

        // Method to clean up (disconnect) from the specific network share after finishing
        public static void CleanupNetworkCredentials(string serverAddress)
        {
            // Command to disconnect the specific network server and remove its credentials
            string netUseDeleteCmd = $"net use X: /delete";
            var processStartInfo = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = "/C " + netUseDeleteCmd,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            try
            {
                using (var process = Process.Start(processStartInfo))
                {
                    process.WaitForExit();

                    // Check for any errors
                    string output = process.StandardOutput.ReadToEnd();
                    string error = process.StandardError.ReadToEnd();

                   
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("An error occurred while disconnecting from the network folder: " + ex.Message);
            }
        }
        public void SetProgress(int percentage)
        {
            if (percentage < 0 || percentage > 100)
            {
                throw new ArgumentOutOfRangeException("Percentage must be between 0 and 100.");
            }

            if (progressBar1.InvokeRequired)
            {
                progressBar1.Invoke(new Action(() => SetProgress(percentage)));
                progressBar1.Refresh();

            }
            else
            {
                progressBar1.Value = percentage;
                progressBar1.Refresh();
            }

        }

        public async void Resetprogress()
        {
            await Task.Delay(2000);
            progressBar1.Value = 0;
            progressBar1.Refresh();
             // Pause for 2 seconds
        }

        public async void resetlabel ()
        {
            await Task.Delay(2000);
            label2.Text = "";
            label2.Refresh();
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void LoadConfig()
        {
            try
            {
                XDocument config = XDocument.Load("..\\..\\..\\config.xml");

                // Read values from the XML configuration
                connectionString = config.Element("configuration")?
                                            .Element("database")?
                                            .Element("connectionString")?.Value;
                department_num = config.Element("configuration")?
                                    .Element("network")?
                                    .Element("department_num")?.Value;

                Console.WriteLine("Configuration loaded successfully.");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error loading configuration: " + ex.Message);
                Environment.Exit(1); // Exit if config loading fails
            }
        }

        private void Getpkgs()
        {
            NetworkCredential networkCredential = new NetworkCredential(username, password, domain);
            SetNetworkCredentials(networkFolderPath, networkCredential);

            var dataTable = new DataTable();

            string selectQuery = $"SELECT * FROM packages where status = 0 and department_num = '{department_num}' ";
            dataTable = db.ExecuteQuery(selectQuery);

            System.Threading.Thread.Sleep(3000);

            foreach (DataRow row in dataTable.Rows)
            {
                string Deactive_PKGS = "SELECT * FROM packages where status = 0";
                string PkgId = row["pkg_id"].ToString();

                string storeID = row["store_id"].ToString();
                string querypkgfile = $"select output_metadata_file_template FROM stores where store_id = {storeID}";
                var pkgfile = db.ExecuteScalar(querypkgfile)?.ToString();
                string companynamequery = $"select store_name from stores where store_id = {storeID}";
                var Company_Name = db.ExecuteScalar(companynamequery)?.ToString();

                if (pkgfile != null)
                {
                    string extension = Path.GetExtension(pkgfile).ToLower();

                    switch (extension)
                    {
                        case ".xml":
                            this.Invoke(new Action(() =>
                            {
                                label2.Text = $"{PkgId}";
                            }));
                            label2.Refresh();
                            Generate_xml(PkgId, pkgfile, Company_Name);
                            AppendLog($"Succsess Generate Package : {PkgId}", Color.DarkGreen);
                            SetProgress(100);
                            SucssesPKG(PkgId);
                            Resetprogress();
                            resetlabel();
                            break;

                        case ".xls":
                        case ".xlsx":
                        case ".xltx":

                            // Handle Excel file
                            GenerateExcel(PkgId, pkgfile, Company_Name);
                            AppendLog($"Succsess Generate Package : {PkgId}", Color.DarkGreen);
                            SucssesPKG(PkgId);
                            break;

                        case ".txt":
                            // Handle text file
                            Console.WriteLine("Processing text file...");
                            break;

                        case ".json":
                            // Handle json file
                            Console.WriteLine("Processing text file...");
                            break;

                        default:
                            Console.WriteLine("Unsupported file type.");
                            break;
                    }
                }

            }

            CleanupNetworkCredentials(@"\\10.1.1.26\Data");
        }

        private void GenerateExcel(string PkgId, string pkgfile, string Company_Name)
        {
            string extensionquery = $"select output_metadata_file_codec from stores where store_name = '{Company_Name}'";
            string extension = db.ExecuteScalar(extensionquery)?.ToString().ToLower();
            string companyFolderPath = Path.Combine("C://Pkg_Output", Company_Name, PkgId);
            if (!Directory.Exists(companyFolderPath))
            {
                Directory.CreateDirectory(companyFolderPath);
            }
            // Set the license context
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            string Pkgtype_ID_query = $"select pkg_type from packages where pkg_id = {PkgId}";
            object PkgType_ID = db.ExecuteScalar(Pkgtype_ID_query);
            string PkgType_Name_query = $"select pkg_type_name from pkg_types_info where pkg_type_id = {PkgType_ID}";
            object PkgType_Name = db.ExecuteScalar(PkgType_Name_query);

            string filePath = $"..\\..\\..\\Templates\\{PkgType_Name}\\{pkgfile}";
            LogMessage(logFilePath, $"PKG ID IS : {PkgId}");
            WriteColoredLine($"PKG ID IS : {PkgId} / Starting now...", ConsoleColor.Green);
            LogMessage(logFilePath, $"Using Excel template: {filePath}");

            Dictionary<string, string> columnMapping = new Dictionary<string, string>{
        { "Label name", "label_name" },
        { "Rights Body", "label_name" },
        { "Alias Promocode", "rbt_code" },
        { "Code", "cd_code" },
        { "Category", "track_genre" },







        { "Album UPC", "album_ubc" },
        { "UPC", "album_ubc" },
        { "Album name A", "album_name_a" },
        { "AlbumName", "album_name_a" },
        { "Album name Arb.", "album_name_a" },
        { "AlbumLanguage(AR)", "album_name_a" },
        { "Album AR", "album_name_a" },
        { "Album name Eng.", "Album_name" },
        { "Album EN", "Album_name" },
        { "AlumLanguage(EN)", "Album_name" },


        { "Album Artist A", "album_artist_a" },
        { "ISRC", "asset_isrc" },
        { "Category ID", "asset_isrc" },
        { "Track ISRC", "asset_isrc" },



        { "Track name A", "track_Name_a" },
        { "اسم النغمة - عربي", "track_Name_a" },
        { "SongName", "track_Name_a" },
        { "SongSubName", "track_Name_a" },
        { "Song Title (AR)", "track_Name_a" },
        { "Title AR", "track_Name_a" },
        { "Short Title AR", "track_Name_a" },
        { "Track name Arb.", "track_name_a" },
        { "Track name Eng.", "track_name" },
        { "Title EN", "track_name" },
        { "Song Title", "track_name" },
        { "Short Title En", "track_name" },
        { "Tone name", "track_name" },


        { "Track Artist A", "track_artist_a" },
        { "اسم المؤدي - عربي", "track_artist_a" },
        { "ArtistName", "track_artist_a" },
        { "Track Artist Arb.", "track_artist_a" },
        { "Track Artist Eng.", "track_artist" },
        { "Actor", "track_artist" },
        { "Author name", "track_artist" },
        { "Singer", "track_artist" },




        { "Composer name Eng.", "composer" },
        { "Composer", "composer" },
        { "Composer name Arb.", "composer_A" },


        { "Writer name Eng.", "lyrics" },
        { "Author", "lyrics" },
        { "Writer name Arb.", "lyrics_a" },



        { "Track#", "track_num" },
        { "Track number", "track_num" },
        { "Track No", "track_num" },
        { "Genre", "track_genre" },

        { "Track Count", "TrackCount" },
        { "Tracks Count", "TrackCount" },


        { "Release Date", "release_date" },
        { "ReleaseDate", "release_date" },
        { "CopyrightStartTime", "start_date" },
        { "CopyrightEndTime", "end_date" },
        { " PreviewStartPoint", "prv_start_point" },
        { "  PreviewEndPoint", "prv_end_point" },
        { "  TerritoryCode", "country_iso_code" },


        { "Track Duration", "track_duration" },
        { "Track Genre", "track_genre" },
        { "File Path", "External_FilePath" }, // External variable
        { "File name", "External_FilePath" }, // External variable
        { "SongFileName", "External_SongFileName" }, // External variable
        { "Language", "External_Language" }, // External variable
        { "Movie/Album", "External_Movie/Album" }, // External variable
        { "Content Type name", "External_ContentTypeName" }, // External variable
    };

            string querygetpkginfo = $"select * from t_packages_info where pkg_id = {PkgId} Order by album_num ";
            var dataTable = db.ExecuteQuery(querygetpkginfo);
            var groupedRows = dataTable.AsEnumerable()
                                        .GroupBy(row => row.Field<int>("album_num"))
                                        .ToList();
            int totalGroups = groupedRows.Count;

            int currentGroupIndex = 0;
            int Groupcounter = 1;
            string store_id_query = $"select store_id from packages where pkg_id = {PkgId}";
            object store_id = db.ExecuteScalar(store_id_query);



            foreach (var group in groupedRows)
            {
                int excelMedia = 0;
                int ISRC_COUNTER = 0;
                string albumUPC = group.First().Field<string>("album_ubc");
                string RBTCODE = group.First().Field<string>("rbt_code");
                string album_artist = group.First().Field<string>("album_artist");
                string album_Title = group.First().Field<string>("album_name");
                string asset_ISRC = group.Skip(ISRC_COUNTER).FirstOrDefault()?.Field<string>("asset_isrc");

              


                LogMessage(logFilePath, $"Processing album UPC: {albumUPC}");
                WriteColoredLine($"Processing album UPC: {albumUPC}", ConsoleColor.White);

                try
                {
                    GetFolderPaths(Company_Name, albumUPC, album_artist, album_Title, group, PkgId, null);
                    LogMessage(logFilePath, $"Sucsses:Folder paths generated for album {albumUPC}.");
                    if (store_id.ToString() == "25")
                    {
                        foreach (var groupp in groupedRows)
                        {
                            GetFolderPaths(Company_Name, albumUPC, album_artist, album_Title, groupp, PkgId, null);
                            GetMediaData(PkgId, groupedRows, groupp);

                        }
                    }
                    GetMediaPath(asset_ISRC, Company_Name, group);
                    LogMessage(logFilePath, $"Sucsses:Media path generated for ISRC: {asset_ISRC}");

                }
                catch (Exception ex)
                {
                    string cleanedMessage = ex.Message.Replace("'", "");
                    LogErrorToDatabase(cleanedMessage, PkgId);
                    LogMessage(logFilePath, $"Error: generating folder or media paths: {cleanedMessage}");

                    continue;
                }


                FileInfo fileInfo = new FileInfo(filePath);
                try
                {
                    using (ExcelPackage package = new ExcelPackage(fileInfo))
                    {
                        // Get the first worksheet
                        foreach (var worksheet in package.Workbook.Worksheets)
                        {
                            var excelColumnNames = worksheet.Cells[1, 1, 1, worksheet.Dimension.End.Column]
                                               .Select(cell => cell.Text)
                                               .ToList();

                            foreach (var map in columnMapping)
                            {
                                string excelColumnName = map.Key;
                                string dataTableColumnName = map.Value;

                                if (excelColumnNames.Contains(excelColumnName))
                                {
                                    int columnIndex = excelColumnNames.IndexOf(excelColumnName) + 1;

                                    for (int i = 0; i < dataTable.Rows.Count; i++)
                                    {
                                        // Check for external variables

                                        if (dataTableColumnName.StartsWith("External_"))
                                        {
                                            string externalValue = dataTableColumnName switch
                                            {
                                                "External_FilePath" => $"{mediapath}",
                                                "External_Language" => "Arabic",
                                                "External_Movie/Album" => "NA",
                                                "External_ContentTypeName" => "RBT",
                                                _ => string.Empty
                                            };
                                            if (ISRC_COUNTER < group.Count() && dataTableColumnName == "External_FilePath")
                                            {
                                                externalValue = $"{mediapath}";
                                                worksheet.Cells[i + 2, columnIndex].Value = externalValue;
                                                ISRC_COUNTER++;
                                                asset_ISRC = group.Skip(ISRC_COUNTER).FirstOrDefault()?.Field<string>("asset_isrc");
                                                GetMediaPath(asset_ISRC, Company_Name, group);


                                            }
                                            else
                                            {
                                                if (dataTableColumnName == "External_FilePath")
                                                {

                                                    if (Groupcounter < totalGroups)
                                                    {
                                                        var secondGroup = groupedRows[Groupcounter];
                                                        // Get the second group
                                                        asset_ISRC = secondGroup.Skip(currentGroupIndex).FirstOrDefault()?.Field<string>("asset_isrc");
                                                        if (currentGroupIndex > secondGroup.Count())
                                                        {
                                                            Groupcounter++;
                                                            currentGroupIndex = 0;

                                                        }
                                                        if (asset_ISRC is null)
                                                        {
                                                            Groupcounter++;
                                                            currentGroupIndex = 0;
                                                            secondGroup = groupedRows[Groupcounter];
                                                            // Get the second group
                                                            asset_ISRC = secondGroup.Skip(currentGroupIndex).FirstOrDefault()?.Field<string>("asset_isrc");
                                                            GetMediaPath(asset_ISRC, Company_Name, secondGroup);
                                                            currentGroupIndex++;
                                                            externalValue = $"{mediapath}";
                                                            currentGroupIndex--;

                                                            worksheet.Cells[i + 2, columnIndex].Value = externalValue;
                                                        }

                                                        GetMediaPath(asset_ISRC, Company_Name, secondGroup);
                                                        currentGroupIndex++;
                                                        externalValue = $"{mediapath}";

                                                        worksheet.Cells[i + 2, columnIndex].Value = externalValue;


                                                    }

                                                }
                                                else
                                                {
                                                    worksheet.Cells[i + 2, columnIndex].Value = externalValue;

                                                }




                                            }
                                        }
                                        else
                                        {
                                            // Set the value from the DataTable
                                            worksheet.Cells[i + 2, columnIndex].Value = dataTable.Rows[i][dataTableColumnName];
                                        }


                                    }

                                    LogMessage(logFilePath, $"Sucsses: Filled data for column: {excelColumnName}");

                                }
                                // GetMediaData(PkgId, group);


                            }
                            try
                            {
                                if (excelMedia == 0)
                                {
                                    if (store_id.ToString() != "25")
                                    {
                                        GetMediaData(PkgId, groupedRows, group);
                                    }
                                 
                                    excelMedia = 1;
                                    if (MediaFILESCounter == 0)
                                    {
                                        throw new FileNotFoundException($"No MP3 files found");
                                    }
                                    else
                                    {
                                        LogMessage(logFilePath, "Sucsses: Media data generated successfully.");
                                    }
                                }

                            }
                            catch (Exception ex)
                            {
                                string cleanedMessage = ex.Message.Replace("'", "");
                                LogErrorToDatabase(cleanedMessage, PkgId);
                                LogMessage(logFilePath, $"Error: processing Excel file: {cleanedMessage}");

                                break;
                            }

                            string filePathh = Path.Combine(companyFolderPath, $"{MetadataFile}");

                            // Ensure the directory exists before trying to save the file
                            string directoryPathh = Path.GetDirectoryName(filePathh);
                            if (!Directory.Exists(directoryPathh))
                            {
                                Directory.CreateDirectory(directoryPathh);
                            }
                            package.SaveAs(filePathh);
                            LogMessage(logFilePath, $"Sucsses: Excel file saved to: {filePathh}");

                            //SucssesPKG(PkgId);
                            LogMessage(logFilePath, $"Sucsses: GenerateExcel completed for Package ID: {PkgId}");

                            WriteColoredLine($"Modified Excel file saved to: {filePathh}", ConsoleColor.Green);

                            CreateExl = false;



                        }

                        break;



                    }

                }

                catch (Exception ex)
                {
                    string cleanedMessage = ex.Message.Replace("'", "");
                    LogErrorToDatabase(cleanedMessage, PkgId);
                    LogMessage(logFilePath, $"Error: processing Excel file: {cleanedMessage}");

                    continue;


                }


            }


        }

        private void ComputeMD5(string filePath, string isrc, int operation)
        {
            // Compute MF5 for asset
            if (operation == 1)
            {
                using (var md5 = MD5.Create())
                {
                    using (var stream = File.OpenRead(filePath))
                    {
                        byte[] hashBytes = md5.ComputeHash(stream);

                        StringBuilder sb = new StringBuilder();
                        for (int i = 0; i < hashBytes.Length; i++)
                        {
                            sb.Append(hashBytes[i].ToString("X2"));
                        }


                        AssetHash assetHash = new AssetHash
                        {
                            ISRC = isrc,
                            MD5Hash = sb.ToString()
                        };


                        assetHashes.Add(assetHash);
                    }
                }
            }
            // Compute MD5 for Cover image
            else if (operation == 2)
            {
                using (var md5 = MD5.Create())
                {
                    using (var stream = File.OpenRead(filePath))
                    {
                        byte[] hashBytes = md5.ComputeHash(stream);

                        StringBuilder sb = new StringBuilder();
                        for (int i = 0; i < hashBytes.Length; i++)
                        {
                            sb.Append(hashBytes[i].ToString("X2"));
                        }


                        AssetHash assetHash = new AssetHash
                        {
                            ISRC = isrc,
                            MD5Hash = sb.ToString()
                        };


                        CoverHashes.Add(assetHash);
                    }
                }

            }
            //Compute MD5 for XML file
            else if (operation == 3)
            {

                using (var md5 = MD5.Create())
                {
                    using (var stream = File.OpenRead(filePath))
                    {
                        byte[] hashBytes = md5.ComputeHash(stream);

                        StringBuilder sb = new StringBuilder();
                        for (int i = 0; i < hashBytes.Length; i++)
                        {
                            sb.Append(hashBytes[i].ToString("X2"));
                        }


                        AssetHash assetHash = new AssetHash
                        {
                            ISRC = isrc,
                            MD5Hash = sb.ToString()
                        };


                        XMLHASHES.Add(assetHash);
                    }
                }
            }
        }

        private async void Generate_xml(string ID, string filename, string Company_Name)
        {

            try
            {
                // Create log folder if it doesn't exist
                if (!Directory.Exists(logFolderPath))
                {
                    Directory.CreateDirectory(logFolderPath);
                }

                string Pkgtype_ID_query = $"select pkg_type from packages where pkg_id = {ID}";
                object PkgType_ID = db.ExecuteScalar(Pkgtype_ID_query);
                string PkgType_Name_query = $"select pkg_type_name from pkg_types_info where pkg_type_id = {PkgType_ID}";
                object PkgType_Name = db.ExecuteScalar(PkgType_Name_query);
                string store_id_query = $"select store_id from packages where pkg_id = {ID}";
                object store_id = db.ExecuteScalar(store_id_query);

             
                LogMessage(logFilePath, $"Starting XML generation for package ID: {ID}, Filename: {filename}, Company: {Company_Name}");
                AppendLog($"Starting XML generation for package ID: {ID}, Filename: {filename}, Company: {Company_Name}", Color.Green);
                
                SetProgress(20);

                WriteColoredLine($"Starting XML generation for package ID: {ID}, Filename: {filename}, Company: {Company_Name}", ConsoleColor.White);

                if (string.IsNullOrWhiteSpace(ID) || string.IsNullOrWhiteSpace(filename) || string.IsNullOrWhiteSpace(Company_Name))
                {
                    LogErrorToDatabase("ID, filename, and Company_Name cannot be null or empty.", ID);
                    LogMessage(logFilePath, "Error: ID, filename, and Company_Name cannot be null or empty.");

                }
                // Fetch package information
                string querygetpkginfo = $"SELECT * FROM t_packages_info WHERE pkg_id = {ID} ORDER BY album_num";
                DataTable dataTable;
                dataTable = new DataTable();

                try
                {
                    dataTable = db.ExecuteQuery(querygetpkginfo);
                    LogMessage(logFilePath, $"Success: fetched package info for ID: {ID}");
                    SetProgress(30);

                    AppendLog($"Success: fetched package info for ID: {ID}", Color.Green);

                    WriteColoredLine($"Success: fetched package info for ID: {ID}", ConsoleColor.Green);

                }
                catch (Exception ex)
                {
                    string cleanedMessage = ex.Message.Replace("'", "");
                    LogErrorToDatabase(cleanedMessage, ID);
                    AppendLog(cleanedMessage, Color.Red);

                    LogMessage(logFilePath, $"Error: fetching package info: {cleanedMessage}");

                }

                var groupedRows = dataTable.AsEnumerable()
                                           .GroupBy(row => row.Field<int>("album_num"))
                                           .ToList();

                if (groupedRows.Count == 0)
                    throw new Exception("No rows returned from the query.");

                // Create company folder
                string companyFolderPath = Path.Combine("C:\\Pkg_Output", Company_Name, ID);
                try
                {
                    if (!Directory.Exists(companyFolderPath))
                    {
                        Directory.CreateDirectory(companyFolderPath);
                    }
                }
                catch (Exception ex)
                {
                    string cleanedMessage = ex.Message.Replace("'", "");
                    LogErrorToDatabase(cleanedMessage, ID);
                    LogMessage(logFilePath, $"Error:creating company folder: {cleanedMessage}");

                }

                // Process each group
                foreach (var group in groupedRows)
                {
                    try
                    {
                        string albumUPC = group.First().Field<string>("album_ubc");
                        string RBTCODE = group.First().Field<string>("rbt_code");
                        string album_artist = group.First().Field<string>("album_artist");
                        string album_Title = group.First().Field<string>("album_name");
                        int album_num = group.First().Field<int>("album_num");
                        int group_len = group.Count();
                        int ISRC_COUNTER = 0;
                        string asset_ISRC = "";

                        LogMessage(logFilePath, $"Processing group with Album UPC: {albumUPC}");

                        AppendLog($"Processing group with Album UPC: {albumUPC}", Color.Green);
                        SetProgress(50);



                        if (PackageFolder == null)
                        {
                            if (group_len == ISRC_COUNTER)
                            {
                                ISRC_COUNTER = 0;
                                GetFolderPaths(Company_Name, albumUPC, album_artist, album_Title, group, ID, asset_ISRC);
                                LogMessage(logFilePath, $"Sucsses: Retrieved folder paths for package: {ID}");

                                SetProgress(10);


                            }
                            else
                            {
                                asset_ISRC = group.Skip(ISRC_COUNTER).FirstOrDefault()?.Field<string>("asset_isrc");
                                GetFolderPaths(Company_Name, albumUPC, album_artist, album_Title, group, ID, asset_ISRC);
                                ISRC_COUNTER++;
                                LogMessage(logFilePath, $"Sucsses: Retrieved folder paths for package: {ID}");
                                SetProgress(60);


                            }


                        }
                        else
                        {
                            temppackagefolder = PackageFolder;
                            if (group_len == ISRC_COUNTER)
                            {
                                ISRC_COUNTER = 0;
                                GetFolderPaths(Company_Name, albumUPC, album_artist, album_Title, group, ID, asset_ISRC);
                                LogMessage(logFilePath, $"Sucsses: Retrieved folder paths for package: {ID}");
                                SetProgress(70);



                            }
                            else
                            {
                                asset_ISRC = group.Skip(ISRC_COUNTER).FirstOrDefault()?.Field<string>("asset_isrc");
                                GetFolderPaths(Company_Name, albumUPC, album_artist, album_Title, group, ID, asset_ISRC);
                                ISRC_COUNTER++;
                                LogMessage(logFilePath, $"Sucsses: Retrieved folder paths for package: {ID}");


                            }
                            //GetFolderPaths(Company_Name, albumUPC, album_artist, album_Title, group, ID, asset_ISRC);
                            // LogMessage(logFilePath, $"Sucsses: Retrieved folder paths for package: {ID}");

                            temppackagefolder = "";
                        }

                        try
                        {

                            await GetMediaData(ID, groupedRows, group);
                            string Checkerror = $"Select error_notes from packages where pkg_id = {ID}";
                            object error_notes = db.ExecuteScalar(Checkerror);
                            SetProgress(80);

                            if (error_notes != "")
                            {
                                throw new FileNotFoundException($"No Media Found");
                            }
                            else
                            {
                                LogMessage(logFilePath, "Media data generated successfully.");
                                WriteColoredLine("Media data generated successfully.", ConsoleColor.Green);
                                AppendLog("Media data generated successfully.", Color.Green);


                            }


                        }
                        catch (Exception ex)
                        {
                            string cleanedMessage = ex.Message.Replace("'", "");
                            LogErrorToDatabase(cleanedMessage, ID);
                            AppendLog(cleanedMessage, Color.Red);

                            LogMessage(logFilePath, $"Error: processing Excel file: {cleanedMessage}");

                            continue;
                        }
                        AssetHash matchingAsset = CoverHashes.FirstOrDefault(hash => hash.ISRC == albumUPC);
                        string coverHashImage = matchingAsset?.MD5Hash;

                        // XML generation
                        try
                        {
                            if (store_id.ToString() == "40")
                            {
                                foreach (var item in group)
                                {

                                    GetFolderPaths(Company_Name, albumUPC, album_artist, album_Title, group, ID, item.Field<string>("asset_isrc"));
                                    var xmlGenerator = new XMLGenerator(connectionString);
                                    AppendLog($"Start Generating in ISRC {item.Field<string>("asset_isrc")}", Color.Green);
                                    SetProgress(90);

                                    xmlGenerator.Generate($"..\\..\\..\\Templates\\{PkgType_Name}\\" + filename, $"{companyFolderPath}\\" + $"{MetadataFile}", assetHashes, coverHashImage, album_num, ID, tracksize, item.Field<string>("asset_isrc"));
                                    AppendLog($"Success: Generating in ISRC {item.Field<string>("asset_isrc")}", Color.Green);

                                }

                            }
                            else
                            {
                                var xmlGenerator = new XMLGenerator(connectionString);
                                xmlGenerator.Generate($"..\\..\\..\\Templates\\{PkgType_Name}\\" + filename, $"{companyFolderPath}\\" + $"{MetadataFile}", assetHashes, coverHashImage, album_num, ID, tracksize, null);
                                ComputeMD5($"{companyFolderPath}\\" + $"{MetadataFile}", albumUPC, 3);

                                LogMessage(logFilePath, $"Success: generated XML for package ID: {ID}");
                                SetProgress(90);
                                AppendLog($"Success: generated XML for package ID: {ID}", Color.Green);

                                WriteColoredLine($"Success: generated XML for package ID: {ID}", ConsoleColor.Green);
                            }



                        }
                        catch (Exception ex)
                        {
                            string cleanedMessage = ex.Message.Replace("'", "");
                            LogErrorToDatabase(cleanedMessage, ID);
                            LogMessage(logFilePath, $"Error: generating XML for package: {cleanedMessage}");
                            AppendLog(cleanedMessage, Color.Red);

                            Console.WriteLine($"Error: generating XML for package: {cleanedMessage}");
                            break;

                        }
                    }
                    catch (Exception ex)
                    {
                        string cleanedMessage = ex.Message.Replace("'", "");
                        LogErrorToDatabase(cleanedMessage, ID);
                        AppendLog(cleanedMessage, Color.Red);

                        LogMessage(logFilePath, $"Error: processing group: {cleanedMessage}");

                        continue; // Optionally continue with the next group if one fails
                    }
                }



                var xmlGeneratorBatch = new XMLGenerator(connectionString);
                int firstSlashIndex = PackageFolder.IndexOf('\\');

                // Check if a '/' was found
                if (firstSlashIndex >= 0)
                {
                    // Remove the first part including the '/'
                    packagesFolderwithoutID = PackageFolder.Substring(firstSlashIndex + 1);
                }

                xmlGeneratorBatch.GenerateBatch($"..\\..\\..\\Templates\\Batches\\" + filename, $"{companyFolderPath}\\" + $"{BatchFolder}", XMLHASHES, "0", 1, ID);
                temppackagefolder = "1";
            }
            catch (Exception ex)
            {
                string cleanedMessage = ex.Message.Replace("'", "");
                LogErrorToDatabase(cleanedMessage, ID);
                AppendLog(cleanedMessage, Color.Red);

                LogMessage(logFilePath, $"Critical error: {cleanedMessage}");
                throw; // Optionally rethrow the exception or handle it as needed
            }
        }


        private void GetMediaPath(string ISRC, string Company_Name, IGrouping<int, DataRow> group)
        {

            string get_media_path = $"select output_media_file_path from stores where store_name = '{Company_Name}'";
            string media_path = db.ExecuteScalar(get_media_path)?.ToString();
            var matchingRow = group.FirstOrDefault(row => row.Field<string>("asset_isrc") == ISRC);
            if (matchingRow != null)
            {
                var values_Media_Folder = new Dictionary<string, string>
                    {
                        { "CREATION_FULL_DATE_TIME", DateTime.Now.ToString("yyyyMMddHHmmssfff") },
                        { "CREATAION_DATE", DateTime.Now.ToString("yyyyMMddHHmmss") },
                        { "O_CREATION_DATE", DateTime.Now.ToString("ddMMyy") },
                        { "PackageFolder", PackageFolder },
                        { "ALBUM_UPC", matchingRow.Field<string>("album_ubc") },
                        { "DATE", DateTime.Now.ToString("yyyy-MM-dd") },
                        { "ALBUM_ARTIST", matchingRow.Field<string>("album_artist") },
                        { "ALBUM_TITLE", matchingRow.Field<string>("album_name") },
                        { "ResourcesFolder", ResourcesFolder },
                        { "DISK_NUM", matchingRow.Field<int>("disc_num").ToString() },
                        { "DISK_NO", matchingRow.Field<int>("disc_num").ToString() },

                        { "TRACK_FILE_NUM", matchingRow.Field<string>("track_num") },
                        { "ISRC", matchingRow.Field<string>("asset_isrc") },
                        { "TRACK_TITLE", matchingRow.Field<string>("track_name") },
                        { "TRACK_NUMBER", matchingRow.Field<string>("track_num") },
                        { "TRACK_NUM", matchingRow.Field<string>("track_num") },
                        { "TRACK_ARTIST",matchingRow.Field<string>("track_artist") },
                   
                        //Need To revise Those Varaibles Take from what ???
                        { "RBT_CODE", matchingRow.Field<string>("rbt_code") },
                        { "IVR_CODE", matchingRow.Field<string>("cd_code") },

                    };

                media_path = ReplacePlaceholders(media_path, values_Media_Folder);

                mediapath = media_path;

            }


        }
        private void GetFolderPaths(string Company_Name, string albumUPC, string album_artist, string album_Title, IGrouping<int, DataRow> group, string ID, string isrc)
        {
           
            try
            {
                LogMessage(logFilePath, "Starting GetFolderPaths...");

                if (PackageFolder == temppackagefolder)
                {
                    int index = temppackagefolder.IndexOf('\\');

                    // If a backslash is found and there is more content after it
                    if (index >= 0 && index + 1 < temppackagefolder.Length)
                    {
                        // Extract the substring starting from the second backslash
                        string trimmedValue = temppackagefolder.Substring(index + 1);

                        // Find the second backslash if it exists
                        int secondIndex = trimmedValue.IndexOf('\\');
                        if (secondIndex >= 0)
                        {
                            // Trim the string to include everything from the second backslash onward
                            trimmedValue = trimmedValue.Substring(secondIndex);
                        }

                        // Assign the trimmed value to PackageFolder
                        PackageFolder = trimmedValue;
                        LogMessage(logFilePath, $"Sucsses: PackageFolder trimmed to: {PackageFolder}");

                    }

                }
                else
                {

                    LogMessage(logFilePath, $"Fetching package folder for company: {Company_Name}");

                    string getPKGFolderQuery = $"select package_folder from stores where store_name = '{Company_Name}'";
                    PackageFolder = db.ExecuteScalar(getPKGFolderQuery)?.ToString();

                    var values_PKG_Folder = new Dictionary<string, string>
    {
        { "CREATION_FULL_DATE_TIME",creationFullDateTime },
        { "CREATION_DATE_TIME",creationDateTime },
        { "CREATION_DATE",creationDate },
        { "O_CREATION_DATE",oCreationDate },
        { "ALBUM_UPC", group.First().Field<string>("album_ubc") },
        { "PackageFolder", PackageFolder },
    };

                    PackageFolder = ReplacePlaceholders(PackageFolder, values_PKG_Folder);
                    GetFolders = false;
                    LogMessage(logFilePath, $"Sucsses: PackageFolder set to: {PackageFolder}");

                }
                LogMessage(logFilePath, $"Sucsses: Fetching resources folder for company: {Company_Name}");

                // Query for resources folder
                string get_Resources_Folder_Query = $"select resources_folder from stores where store_name = '{Company_Name}'";
                ResourcesFolder = db.ExecuteScalar(get_Resources_Folder_Query)?.ToString();

                var values_Resources_Folder = new Dictionary<string, string>
    {
        { "ALBUM_UPC", group.First().Field<string>("album_ubc") },
        { "PackageFolder", PackageFolder },
        { "DATE",formattedDate },
        { "ALBUM_ARTIST", group.First().Field<string>("album_artist") },
        { "ALBUM_TITLE",  group.First().Field<string>("album_name") },
    };

                ResourcesFolder = ReplacePlaceholders(ResourcesFolder, values_Resources_Folder);
                LogMessage(logFilePath, $"Sucsses: ResourcesFolder set to: {ResourcesFolder}");


                LogMessage(logFilePath, $"Sucsses: Fetching metadata file path for company: {Company_Name}");

                // Query for metadata file path
                string get_metadata_Folder_path = $"select output_metadata_file_path from stores where store_name = '{Company_Name}'";
                MetadataFile = db.ExecuteScalar(get_metadata_Folder_path)?.ToString();

                var values_Metadata_Folder = new Dictionary<string, string>
    {
        { "CREATION_FULL_DATE_TIME",creationFullDateTime },
        { "CREATION_DATE", creationDate },
        { "O_CREATION_DATE",oCreationDate },
        { "PackageFolder", PackageFolder },
        { "ALBUM_UPC", group.First().Field<string>("album_ubc") },
        { "DATE", formattedDate },
        { "ISRC", group.First().Field<string>("asset_isrc") },
         { "ALBUM_ARTIST", group.First().Field<string>("album_artist") },
        { "ALBUM_TITLE",  group.First().Field<string>("album_name") },
        { "ResourcesFolder", ResourcesFolder },
    };

                MetadataFile = ReplacePlaceholders(MetadataFile, values_Metadata_Folder);

                // Combine paths to form the full path to save the metadata file
                LogMessage(logFilePath, $"Sucsses: MetadataFile path set to: {MetadataFile}");


                string get_Batch_Folder_path = $"select output_batch_file_path from stores where store_name = '{Company_Name}'";
                BatchFolder = db.ExecuteScalar(get_Batch_Folder_path)?.ToString();

                var values_Batch_Folder = new Dictionary<string, string>
                       {
                           { "CREATION_FULL_DATE_TIME", creationFullDateTime },
                           { "CREATION_DATE",creationDate},
                           { "CREATION_DATE_TIME",creationDateTime },
                           { "O_CREATION_DATE",oCreationDate },
                           { "PackageFolder", PackageFolder },
        { "ALBUM_UPC", group.First().Field<string>("album_ubc") },
                           { "DATE",formattedDate},
         { "ALBUM_ARTIST", group.First().Field<string>("album_artist") },
        { "ALBUM_TITLE",  group.First().Field<string>("album_name") },
                           { "ResourcesFolder", ResourcesFolder },
                       };

                BatchFolder = ReplacePlaceholders(BatchFolder, values_Batch_Folder);



                //MetadataFile = MetadataFile.Replace("\\", "//");

                PackageFolder = Path.Combine(ID, PackageFolder);
                ResourcesFolder = Path.Combine(ID, ResourcesFolder);

                LogMessage(logFilePath, $"Sucsses: Final PackageFolder path: {PackageFolder}");
                LogMessage(logFilePath, $"Sucsses: Final ResourcesFolder path: {ResourcesFolder}");

                //if (!Directory.Exists(PackageFolder))
                //{
                //    Directory.CreateDirectory(PackageFolder);
                //}

                Console.WriteLine($"Final MetadataFile path: {MetadataFile}");





            }

            catch (Exception ex)
            {
                LogMessage(logFilePath, $"Error: in GetFolderPaths: {ex.Message}");
                throw;
            }



        }

        string ReplacePlaceholders(string input, Dictionary<string, string> values)
        {
            foreach (var key in values.Keys)
            {
                string placeholder = $"@[{key}]@";
                if (input.Contains(placeholder))
                {
                    input = input.Replace(placeholder, values[key]);
                }
            }
            return input;
        }

        private void GetCover(string path, string UPC, string extension, string Company_path, string pkg_type)
        {
            try
            {
                LogMessage(logFilePath, "Starting GetCover...");

                if (string.IsNullOrEmpty(path))
                {
                    LogMessage(logFilePath, "Error: Provided path is empty. Exiting GetCover.");
                    return;
                }

                // Define the source path for the cover file
                string sourceCoverDirectory = @"\\10.1.1.26\Data\FTP_Data\CMSDocs\AlbumsCovers";
                string coverSearchPattern = $"{UPC}.jpg";
                string[] coverFiles = Directory.GetFiles(sourceCoverDirectory, coverSearchPattern);
                string coverFilePath = Path.Combine(sourceCoverDirectory, coverSearchPattern);

                LogMessage(logFilePath, $"Sucsses: Searching for cover file: {coverFilePath}");

                // If a cover file is found
                if (coverFiles.Length > 0)
                {
                    string selectedCoverFile = coverFiles[0];
                    LogMessage(logFilePath, $"Sucsses: Found cover file: {selectedCoverFile}");

                    // Separate the directory path and the file name from the media path
                    string destinationDirectory = Path.GetDirectoryName(Path.Combine(Company_path, path));
                    string destinationFileName = Path.GetFileName(path);

                    // Ensure the destination directory exists

                    if (!Directory.Exists(destinationDirectory))
                    {
                        Directory.CreateDirectory(destinationDirectory);
                        LogMessage(logFilePath, $"Sucsses: Created directory: {destinationDirectory}");
                    }

                    // Form the full destination path
                    string destinationFilePath = Path.Combine(destinationDirectory, destinationFileName);

                    // Compute MD5 hash for the original cover file
                    ComputeMD5(selectedCoverFile, UPC, 2);
                    GetFileSize(selectedCoverFile, UPC, 2);

                    LogMessage(logFilePath, $"Sucsses: Computed MD5 hash for: {selectedCoverFile}");
                    if (pkg_type != "4" && pkg_type != "5")
                    {
                        File.Copy(selectedCoverFile, destinationFilePath, overwrite: true);
                    }
                    // Copy the cover file to the destination
                    LogMessage(logFilePath, $"Sucsses: Copied cover file to: {destinationFilePath}");
                }
                else
                {
                    // Log the event if no cover file is found
                    LogMessage(logFilePath, $"Error: No cover file found for UPC: {UPC} in {sourceCoverDirectory}");
                }
            }
            catch (Exception ex)
            {
                LogMessage(logFilePath, $"Error: in GetCover: {ex.Message}");
                throw;
            }
        }

        private async Task GetMediaData(string PkgID, List<IGrouping<int, DataRow>> groupedRows, IGrouping<int, DataRow> group)
        {
            try
            {
                string basePath = @"\\10.1.1.26\Data\FTP_Data\AudioLibrary";
                string[] RBT_Companies = { "24",  "26", "28", "29", "30", "S31", "32", "33", "34", "35", "47", "49", "50" };
                string[] XML_companies = { "1", "2", "25", "3", "4", "5", "9", "10", "11", "12", "13", "14", "15", "16", "17", "18", "19", "21", "22", "27", "35", "37", "38", "40", "41", "42", "43", "44", "45", "46", "47", "52", "54", "55", "56", "57", "58", "59", "60", "61", "62", "63", "64" };

                string getstore_query = $"select store_id from packages where pkg_id = {PkgID}";
                string storeID = db.ExecuteScalar(getstore_query)?.ToString().ToLower();

                string getstore_Name_query = $"select store_name from stores where store_id = {storeID}";
                string Company_Name = db.ExecuteScalar(getstore_Name_query)?.ToString().ToLower();
                string companyFolderPath = Path.Combine("C://Pkg_Output", Company_Name);

                string pkg_type_query = $" select pkg_type from packages where pkg_id = {PkgID}";
                string pkg_type = db.ExecuteScalar(pkg_type_query)?.ToString();





                string get_media_extension_query = $"select output_media_file_codec from stores where store_id = {storeID}";
                string Media_extension = db.ExecuteScalar(get_media_extension_query)?.ToString();

                string get_cover_extention = $"select output_cover_file_codec from stores where store_id = {storeID}";
                string Cover_extension = db.ExecuteScalar(get_cover_extention)?.ToString();

                string get_media_path = $"select output_media_file_path from stores where store_id = {storeID}";
                string media_path = db.ExecuteScalar(get_media_path)?.ToString();

                string get_cover_path = $"select output_cover_file_path from stores where store_id = {storeID}";
                string cover_path = db.ExecuteScalar(get_cover_path)?.ToString();
                int image_num = 0;
                DataTable albumTable = new DataTable();

                if (XML_companies.Contains(storeID))
                {
                    foreach (var item in group)
                    {
                        var values_Media_Folder = new Dictionary<string, string> { };
                        if (storeID == "5" || storeID == "58")
                        {
                            string queryAlbum = $"SELECT * FROM t_packages_info WHERE pkg_id = {PkgID} AND album_num={item.Field<int>("album_num")}  order by album_num";
                            albumTable = db.ExecuteQuery(queryAlbum);
                            image_num = albumTable.Rows.Count + 1;


                            values_Media_Folder = new Dictionary<string, string>
                                      {
                                          { "CREATION_FULL_DATE_TIME", DateTime.Now.ToString("yyyyMMddHHmmssfff") },
                                          { "CREATAION_DATE", DateTime.Now.ToString("yyyyMMddHHmmss") },
                                          { "O_CREATION_DATE", DateTime.Now.ToString("ddMMyy") },
                                          { "PackageFolder", PackageFolder },
                                          { "ALBUM_UPC", item.Field<string>("album_ubc") },
                                          { "DATE", DateTime.Now.ToString("yyyy-MM-dd") },
                                          { "ALBUM_ARTIST", item.Field<string>("album_artist") },
                                          { "ALBUM_TITLE", item.Field<string>("album_name") },
                                          { "ResourcesFolder", ResourcesFolder },
                                          { "DISK_NUM", item.Field<int>("disc_num").ToString("D2") },
                                          { "DISK_NO", item.Field<int>("disc_num").ToString("D2") },
                                          { "TRACK_FILE_NO",  int.Parse(item.Field<string>("track_num")).ToString("D3") },

                                          { "TRACK_FILE_NUM",int.Parse(item.Field<string>("track_num")).ToString("D3")  },
                                          { "ISRC", item.Field<string>("asset_isrc") },
                                          { "TRACK_TITLE", item.Field<string>("track_name") },
                                          { "TRACK_NUMBER",int.Parse(item.Field<string>("track_num")).ToString("D3")  },
                                          { "TRACK_NUM", int.Parse(item.Field<string>("track_num")).ToString("D3")  },
                                          { "TRACK_NO", int.Parse(item.Field<string>("track_num")).ToString("D3")  },
                                          {"IMAGE_NO" , image_num.ToString() },

                                          { "TRACK_ARTIST",item.Field<string>("track_artist") },
                                     
                                          //Need To revise Those Varaibles Take from what ???
                                          { "RBT_CODE", item.Field<string>("rbt_code") },
                                          { "IVR_CODE", item.Field<string>("cd_code") },

                                      };

                        }
                        else if (storeID == "27" || storeID == "17")
                        {
                            string queryAlbum = $"SELECT * FROM t_packages_info WHERE pkg_id = {PkgID} AND album_num={item.Field<int>("album_num")}  order by album_num";
                            albumTable = db.ExecuteQuery(queryAlbum);
                            image_num = albumTable.Rows.Count + 1;
                            values_Media_Folder = new Dictionary<string, string>
                                      {
                                          { "CREATION_FULL_DATE_TIME", DateTime.Now.ToString("yyyyMMddHHmmssfff") },
                                          { "CREATAION_DATE", DateTime.Now.ToString("yyyyMMddHHmmss") },
                                          { "O_CREATION_DATE", DateTime.Now.ToString("ddMMyy") },
                                          { "PackageFolder", PackageFolder },
                                          { "ALBUM_UPC", item.Field<string>("album_ubc") },
                                          { "DATE", DateTime.Now.ToString("yyyy-MM-dd") },
                                          { "ALBUM_ARTIST", item.Field<string>("album_artist") },
                                          { "ALBUM_TITLE", item.Field<string>("album_name") },
                                          { "ResourcesFolder", ResourcesFolder },
                                          { "DISK_NUM", item.Field<int>("disc_num").ToString("D1") },
                                          { "DISK_NO", item.Field<int>("disc_num").ToString("D1") },
                                          { "TRACK_FILE_NO",  int.Parse(item.Field<string>("track_num")).ToString("D1") },

                                          { "TRACK_FILE_NUM",int.Parse(item.Field<string>("track_num")).ToString("D1")  },
                                          { "ISRC", item.Field<string>("asset_isrc") },
                                          { "TRACK_TITLE", item.Field<string>("track_name") },
                                          { "TRACK_NUMBER",int.Parse(item.Field<string>("track_num")).ToString("D1")  },
                                          { "TRACK_NUM", int.Parse(item.Field<string>("track_num")).ToString("D1")  },
                                          { "TRACK_NO", int.Parse(item.Field<string>("track_num")).ToString("D1")  },
                                          { "TRACK_ARTIST",item.Field<string>("track_artist") },
                                          {"IMAGE_NO" , image_num.ToString() },                                     
                                          //Need To revise Those Varaibles Take from what ???
                                          { "RBT_CODE", item.Field<string>("rbt_code") },
                                          { "IVR_CODE", item.Field<string>("cd_code") },

                                      };
                        }
                        else if (storeID == "16")
                        {
                            string queryAlbum = $"SELECT * FROM t_packages_info WHERE pkg_id = {PkgID} AND album_num={item.Field<int>("album_num")}  order by album_num";
                            albumTable = db.ExecuteQuery(queryAlbum);
                            image_num = albumTable.Rows.Count + 1;
                            values_Media_Folder = new Dictionary<string, string>
                                      {
                                          { "CREATION_FULL_DATE_TIME", DateTime.Now.ToString("yyyyMMddHHmmssfff") },
                                          { "CREATAION_DATE", DateTime.Now.ToString("yyyyMMddHHmmss") },
                                          { "O_CREATION_DATE", DateTime.Now.ToString("ddMMyy") },
                                          { "PackageFolder", PackageFolder },
                                          { "ALBUM_UPC", item.Field<string>("album_ubc") },
                                          { "DATE", DateTime.Now.ToString("yyyy-MM-dd") },
                                          { "ALBUM_ARTIST", item.Field<string>("album_artist") },
                                          { "ALBUM_TITLE", item.Field<string>("album_name") },
                                          { "ResourcesFolder", ResourcesFolder },
                                          { "DISK_NUM", item.Field<int>("disc_num").ToString("D2") },
                                          { "DISK_NO", item.Field<int>("disc_num").ToString("D2") },
                                          { "TRACK_FILE_NO",  int.Parse(item.Field<string>("track_num")).ToString("D2") },
                                          {"IMAGE_NO" , image_num.ToString() },


                                          { "TRACK_FILE_NUM",int.Parse(item.Field<string>("track_num")).ToString("D3")  },
                                          { "ISRC", item.Field<string>("asset_isrc") },
                                          { "TRACK_TITLE", item.Field<string>("track_name") },
                                          { "TRACK_NUMBER",int.Parse(item.Field<string>("track_num")).ToString("D2")  },
                                          { "TRACK_NUM", int.Parse(item.Field<string>("track_num")).ToString("D2")  },
                                          { "TRACK_NO", int.Parse(item.Field<string>("track_num")).ToString("D2")  },
                                          { "TRACK_ARTIST",item.Field<string>("track_artist") },
                                     
                                          //Need To revise Those Varaibles Take from what ???
                                          { "RBT_CODE", item.Field<string>("rbt_code") },
                                          { "IVR_CODE", item.Field<string>("cd_code") },

                                      };

                        }
                        else
                        {
                            string queryAlbum = $"SELECT * FROM t_packages_info WHERE pkg_id = {PkgID} AND album_num={item.Field<int>("album_num")}  order by album_num";
                            albumTable = db.ExecuteQuery(queryAlbum);
                            image_num = albumTable.Rows.Count + 1;
                            values_Media_Folder = new Dictionary<string, string>
                                      {
                                          { "CREATION_FULL_DATE_TIME", DateTime.Now.ToString("yyyyMMddHHmmssfff") },
                                          { "CREATAION_DATE", DateTime.Now.ToString("yyyyMMddHHmmss") },
                                          { "O_CREATION_DATE", DateTime.Now.ToString("ddMMyy") },
                                          { "PackageFolder", PackageFolder },
                                          { "ALBUM_UPC", item.Field<string>("album_ubc") },
                                          { "DATE", DateTime.Now.ToString("yyyy-MM-dd") },
                                          { "ALBUM_ARTIST", item.Field<string>("album_artist") },
                                          { "ALBUM_TITLE", item.Field<string>("album_name") },
                                          { "ResourcesFolder", ResourcesFolder },
                                          { "DISK_NUM", item.Field<int>("disc_num").ToString("D1") },
                                          { "DISK_NO", item.Field<int>("disc_num").ToString("D1") },
                                          { "TRACK_FILE_NO",  int.Parse(item.Field<string>("track_num")).ToString("D1") },

                                          { "TRACK_FILE_NUM",int.Parse(item.Field<string>("track_num")).ToString("D1")  },
                                          { "ISRC", item.Field<string>("asset_isrc") },
                                          { "TRACK_TITLE", item.Field<string>("track_name") },
                                          { "TRACK_NUMBER",int.Parse(item.Field<string>("track_num")).ToString("D1")  },
                                          { "TRACK_NUM", int.Parse(item.Field<string>("track_num")).ToString("D1")  },
                                          { "TRACK_NO", int.Parse(item.Field<string>("track_num")).ToString("D1")  },
                                          { "TRACK_ARTIST",item.Field<string>("track_artist") },
                                          {"IMAGE_NO" , image_num.ToString() },                                     
                                          //Need To revise Those Varaibles Take from what ???
                                          { "RBT_CODE", item.Field<string>("rbt_code") },
                                          { "IVR_CODE", item.Field<string>("cd_code") },

                                      };

                        }


                        media_path = db.ExecuteScalar(get_media_path)?.ToString();
                        media_path = ReplacePlaceholders(media_path, values_Media_Folder);
                        cover_path = ReplacePlaceholders(cover_path, values_Media_Folder);
                        mediapath = media_path;
                        GetCover(cover_path, item.Field<string>("album_ubc"), Cover_extension, companyFolderPath, pkg_type);

                        LogMessage(logFilePath, $"Start Getting Media For package info: {PkgID}");

                        switch (Media_extension)
                        {
                            case "MP3":
                                if (!RBT_Companies.Contains(storeID))
                                {

                                    string mp3FolderPath = Path.Combine(basePath, "MP3");
                                    string[] mp3Files = Directory.GetFiles(mp3FolderPath, $"{item.Field<string>("asset_isrc")}.mp3");
                                    if (mp3Files.Length == 0)
                                    {
                                        // Throw an exception and exit the function
                                        LogMessage(logFilePath, $"No MP3 files found for asset ISRC: {item.Field<string>("asset_isrc")}");
                                        LogErrorToDatabase($"No MP3 files found for asset ISRC: {item.Field<string>("asset_isrc")}", PkgID);
                                        MediaFILESCounter = 0;

                                        continue;

                                    }
                                    string filepathMP31 = Path.Combine(mp3FolderPath, $"{item.Field<string>("asset_isrc")}.mp3");
                                    ComputeMD5(filepathMP31, item.Field<string>("asset_isrc"), 1);
                                    GetFileSize(filepathMP31, item.Field<string>("asset_isrc"), 1);

                                    if (pkg_type != "4" && pkg_type != "5")
                                    {
                                        if (mp3Files.Length > 0)
                                        {
                                            MediaFILESCounter = 1;
                                            string selectedMp3File = mp3Files[0];

                                            // Separate the directory path and the file name from media_path
                                            string destinationDirectory = Path.GetDirectoryName(Path.Combine(companyFolderPath, media_path));
                                            string destinationFileName = Path.GetFileName(media_path);

                                            // Ensure the directory exists
                                            if (!Directory.Exists(destinationDirectory))
                                            {
                                                Directory.CreateDirectory(destinationDirectory);
                                            }

                                            // Combine the directory and the new file name
                                            string destinationFilePath = Path.Combine(destinationDirectory, destinationFileName);

                                            // Copy the file to the new location with the correct name
                                            File.Copy(selectedMp3File, destinationFilePath, overwrite: true);
                                        }
                                    }
                                }
                                else
                                {
                                    basePath = @"\\10.1.1.26\Data\FTP_Data\AudioLibrary\IVR_Cuts\mp3\45";

                                    string mp3FolderPath = Path.Combine(basePath);
                                   
                                    string[] mp3Files = Directory.GetFiles(mp3FolderPath, $"{item.Field<string>("rbt_code")}.mp3");
                                    if (mp3Files.Length == 0)
                                    {
                                        LogMessage(logFilePath, $"No MP3 files found for asset ISRC: {item.Field<string>("asset_isrc")}");
                                        LogErrorToDatabase($"No MP3 files found for asset ISRC: {item.Field<string>("asset_isrc")}", PkgID);
                                        continue;
                                    }
                                    string filepathMP32 = Path.Combine(mp3FolderPath, $"{item.Field<string>("asset_isrc")}.mp3");
                                    //ComputeMD5(filepathMP32, item.Field<string>("asset_isrc"));

                                    if (mp3Files.Length > 0)
                                    {
                                        string selectedMp3File = mp3Files[0];

                                        // Separate the directory path and the file name from media_path
                                        string destinationDirectory = Path.GetDirectoryName(Path.Combine(companyFolderPath, media_path));
                                        string destinationFileName = Path.GetFileName(media_path);

                                        // Ensure the directory exists
                                        if (!Directory.Exists(destinationDirectory))
                                        {
                                            Directory.CreateDirectory(destinationDirectory);
                                        }

                                        // Combine the directory and the new file name
                                        string destinationFilePath = Path.Combine(destinationDirectory, destinationFileName);

                                        // Copy the file to the new location with the correct name
                                        File.Copy(selectedMp3File, destinationFilePath, overwrite: true);
                                    }


                                }



                                break;

                            case "WAVE":
                                if (!RBT_Companies.Contains(storeID))
                                {
                                    string WAVFolderPath = Path.Combine(basePath, "WAV");
                                    string[] WAVFiles = Directory.GetFiles(WAVFolderPath, $"{item.Field<string>("asset_isrc")}.WAV");
                                    if (WAVFiles.Length == 0)
                                    {
                                        // Throw an exception and exit the function
                                        LogMessage(logFilePath, $"No WAV files found for asset ISRC: {item.Field<string>("asset_isrc")}");
                                        LogErrorToDatabase($"No WAV files found for asset ISRC: {item.Field<string>("asset_isrc")}", PkgID);
                                        continue;
                                    }
                                    string filepathWAV1 = Path.Combine(WAVFolderPath, $"{item.Field<string>("asset_isrc")}.WAV");
                                    ComputeMD5(filepathWAV1, item.Field<string>("asset_isrc"), 1);
                                    GetFileSize(filepathWAV1, item.Field<string>("asset_isrc"), 1);

                                    if (pkg_type != "4" && pkg_type != "5")
                                    {
                                        if (WAVFiles.Length > 0)
                                        {
                                            string selectedWAVFile = WAVFiles[0];

                                            // Separate the directory path and the file name from media_path
                                            string destinationDirectory = Path.GetDirectoryName(Path.Combine(companyFolderPath, media_path));
                                            string destinationFileName = Path.GetFileName(media_path);

                                            // Ensure the directory exists
                                            if (!Directory.Exists(destinationDirectory))
                                            {
                                                Directory.CreateDirectory(destinationDirectory);
                                            }

                                            // Combine the directory and the new file name
                                            string destinationFilePath = Path.Combine(destinationDirectory, destinationFileName);

                                            // Copy the file to the new location with the correct name
                                            File.Copy(selectedWAVFile, destinationFilePath, overwrite: true);
                                        }
                                    }
                                }
                                else
                                {
                                    basePath = @"\\10.1.1.26\Data\FTP_Data\AudioLibrary\IVR_Cuts\wav\45";

                                    string WAVFolderPath = Path.Combine(basePath);
                                    string[] WAVFiles = Directory.GetFiles(WAVFolderPath, $"{item.Field<string>("rbt_code")}.WAV");
                                    if (WAVFiles.Length == 0)
                                    {
                                        LogMessage(logFilePath, $"No WAV files found for asset ISRC: {item.Field<string>("asset_isrc")}");
                                        LogErrorToDatabase($"No WAV files found for asset ISRC: {item.Field<string>("asset_isrc")}", PkgID);
                                        continue;
                                    }

                                    string filepathWAV2 = Path.Combine(WAVFolderPath, $"{item.Field<string>("asset_isrc")}.WAV");
                                    // ComputeMD5(filepathWAV2, item.Field<string>("asset_isrc"));

                                    if (WAVFiles.Length > 0)
                                    {
                                        string selectedWAVFile = WAVFiles[0];

                                        // Separate the directory path and the file name from media_path
                                        string destinationDirectory = Path.GetDirectoryName(Path.Combine(companyFolderPath, media_path));
                                        string destinationFileName = Path.GetFileName(media_path);

                                        // Ensure the directory exists
                                        if (!Directory.Exists(destinationDirectory))
                                        {
                                            Directory.CreateDirectory(destinationDirectory);
                                        }

                                        // Combine the directory and the new file name
                                        string destinationFilePath = Path.Combine(destinationDirectory, destinationFileName);

                                        // Copy the file to the new location with the correct name
                                        File.Copy(selectedWAVFile, destinationFilePath, overwrite: true);
                                    }
                                }


                                break;

                            case "FLAC":
                                string FLACFolderPath = Path.Combine(basePath, "FLAC");
                                string[] FLACFiles = Directory.GetFiles(FLACFolderPath, $"{item.Field<string>("asset_isrc")}.FLAC");
                                if (FLACFiles.Length == 0)
                                {
                                    LogMessage(logFilePath, $"No FLAC files found for asset ISRC: {item.Field<string>("asset_isrc")}");
                                    LogErrorToDatabase($"No FLAC files found for asset ISRC: {item.Field<string>("asset_isrc")}", PkgID);
                                    continue;
                                }
                                string filepath = Path.Combine(FLACFolderPath, $"{item.Field<string>("asset_isrc")}.FLAC");
                                ComputeMD5(filepath, item.Field<string>("asset_isrc"), 1);
                                GetFileSize(filepath, item.Field<string>("asset_isrc"), 1);

                                if (pkg_type != "4" && pkg_type != "5")
                                {
                                    if (FLACFiles.Length > 0)
                                    {
                                        string selectedFLACFile = FLACFiles[0];

                                        // Separate the directory path and the file name from media_path
                                        string destinationDirectory = Path.GetDirectoryName(Path.Combine(companyFolderPath, media_path));
                                        string destinationFileName = Path.GetFileName(media_path);

                                        // Ensure the directory exists
                                        if (!Directory.Exists(destinationDirectory))
                                        {
                                            Directory.CreateDirectory(destinationDirectory);
                                        }

                                        // Combine the directory and the new file name
                                        string destinationFilePath = Path.Combine(destinationDirectory, destinationFileName);

                                        // Copy the file to the new location with the correct name
                                        File.Copy(selectedFLACFile, destinationFilePath, overwrite: true);
                                    }
                                }
                                break;

                            case "MP4":

                                break;

                            case "M4A":
                                string M4AFolderPath = Path.Combine(basePath, "M4A");
                                string[] M4AFiles = Directory.GetFiles(M4AFolderPath, $"{item.Field<string>("asset_isrc")}.M4A");
                                if (M4AFiles.Length == 0)
                                {
                                    LogMessage(logFilePath, $"No M4A files found for asset ISRC: {item.Field<string>("asset_isrc")}");
                                    LogErrorToDatabase($"No M4A files found for asset ISRC: {item.Field<string>("asset_isrc")}", PkgID);
                                    continue;
                                }
                                string filepathM4A = Path.Combine(M4AFolderPath, $"{item.Field<string>("asset_isrc")}.M4A");
                                ComputeMD5(filepathM4A, item.Field<string>("asset_isrc"), 1);
                                GetFileSize(filepathM4A, item.Field<string>("asset_isrc"), 1);

                                if (pkg_type != "4" && pkg_type != "5")
                                {
                                    if (M4AFiles.Length > 0)
                                    {
                                        string selectedM4AFile = M4AFiles[0];

                                        // Separate the directory path and the file name from media_path
                                        string destinationDirectory = Path.GetDirectoryName(Path.Combine(companyFolderPath, media_path));
                                        string destinationFileName = Path.GetFileName(media_path);

                                        // Ensure the directory exists
                                        if (!Directory.Exists(destinationDirectory))
                                        {
                                            Directory.CreateDirectory(destinationDirectory);
                                        }

                                        // Combine the directory and the new file name
                                        string destinationFilePath = Path.Combine(destinationDirectory, destinationFileName);

                                        // Copy the file to the new location with the correct name
                                        File.Copy(selectedM4AFile, destinationFilePath, overwrite: true);
                                    }
                                }
                                break;
                        }

                    }
                }
                else
                {



                    foreach (var groupp in groupedRows)
                    {

                        foreach (var item in groupp)
                        {
                            var values_Media_Folder = new Dictionary<string, string>
                    {
                        { "CREATION_FULL_DATE_TIME", DateTime.Now.ToString("yyyyMMddHHmmssfff") },
                        { "CREATAION_DATE", DateTime.Now.ToString("yyyyMMddHHmmss") },
                        { "O_CREATION_DATE", DateTime.Now.ToString("ddMMyy") },
                        { "PackageFolder", PackageFolder },
                        { "ALBUM_UPC", item.Field<string>("album_ubc") },
                        { "DATE", DateTime.Now.ToString("yyyy-MM-dd") },
                        { "ALBUM_ARTIST", item.Field<string>("album_artist") },
                        { "ALBUM_TITLE", item.Field<string>("album_name") },
                        { "ResourcesFolder", ResourcesFolder },
                        { "DISK_NUM", item.Field<int>("disc_num").ToString() },
                        { "DISK_NO", item.Field<int>("disc_num").ToString() },
                        { "TRACK_FILE_NO", item.Field<string>("track_num").ToString() },

                        { "TRACK_FILE_NUM", item.Field<string>("track_num") },
                        { "ISRC", item.Field<string>("asset_isrc") },
                        { "TRACK_TITLE", item.Field<string>("track_name") },
                        { "TRACK_NUMBER", item.Field<string>("track_num") },
                        { "TRACK_NUM", item.Field<string>("track_num") },
                        { "TRACK_NO", item.Field<string>("track_num") },

                        { "TRACK_ARTIST",item.Field<string>("track_artist") },
                   
                        //Need To revise Those Varaibles Take from what ???
                        { "RBT_CODE", item.Field<string>("rbt_code") },
                        { "IVR_CODE", item.Field<string>("cd_code") },

                    };

                            media_path = db.ExecuteScalar(get_media_path)?.ToString();
                            media_path = ReplacePlaceholders(media_path, values_Media_Folder);
                            cover_path = ReplacePlaceholders(cover_path, values_Media_Folder);
                            mediapath = media_path;
                            GetCover(cover_path, item.Field<string>("album_ubc"), Cover_extension, companyFolderPath, pkg_type);


                            switch (Media_extension)
                            {
                                case "MP3":
                                    if (!RBT_Companies.Contains(storeID))
                                    {

                                        string mp3FolderPath = Path.Combine(basePath, "MP3");
                                        string[] mp3Files = Directory.GetFiles(mp3FolderPath, $"{item.Field<string>("asset_isrc")}.mp3");
                                        if (mp3Files.Length == 0)
                                        {
                                            LogMessage(logFilePath, $"No MP3 files found for asset ISRC: {item.Field<string>("asset_isrc")}");
                                            LogErrorToDatabase($"No MP3 files found for asset ISRC: {item.Field<string>("asset_isrc")}", PkgID);
                                            continue;
                                        }
                                        string filepathMP31 = Path.Combine(mp3FolderPath, $"{item.Field<string>("asset_isrc")}.mp3");
                                        ComputeMD5(filepathMP31, item.Field<string>("asset_isrc"), 1);

                                        if (mp3Files.Length > 0)
                                        {
                                            string selectedMp3File = mp3Files[0];

                                            // Separate the directory path and the file name from media_path
                                            string destinationDirectory = Path.GetDirectoryName(Path.Combine(companyFolderPath, media_path));
                                            string destinationFileName = Path.GetFileName(media_path);

                                            // Ensure the directory exists
                                            if (!Directory.Exists(destinationDirectory))
                                            {
                                                Directory.CreateDirectory(destinationDirectory);
                                            }

                                            // Combine the directory and the new file name
                                            string destinationFilePath = Path.Combine(destinationDirectory, destinationFileName);

                                            // Copy the file to the new location with the correct name
                                            File.Copy(selectedMp3File, destinationFilePath, overwrite: true);
                                        }
                                    }
                                    else
                                    {
                                        basePath = @"\\10.1.1.26\Data\FTP_Data\AudioLibrary\IVR_Cuts\mp3\45";
                                        string[] mp3Files = [];
                                        string mp3FolderPath = Path.Combine(basePath);
                                        if (storeID == "25")
                                        {
                                            basePath = @"\\10.1.1.26\Data\FTP_Data\AudioLibrary";
                                            mp3FolderPath = Path.Combine(basePath, "MP3");
                                            mp3Files = Directory.GetFiles(mp3FolderPath, $"{item.Field<string>("asset_isrc")}.mp3");

                                        }
                                        else
                                        {
                                            mp3Files = Directory.GetFiles(mp3FolderPath, $"{item.Field<string>("rbt_code")}.mp3");
                                        }
                                        if (mp3Files.Length == 0)
                                        {
                                            LogMessage(logFilePath, $"No MP3 files found for asset ISRC: {item.Field<string>("asset_isrc")}");
                                            LogErrorToDatabase($"No MP3 files found for asset ISRC: {item.Field<string>("asset_isrc")}", PkgID);
                                            MediaFILESCounter = 0;
                                            continue;
                                        }

                                        MediaFILESCounter = 1;
                                        string filepathMP32 = Path.Combine(mp3FolderPath, $"{item.Field<string>("asset_isrc")}.mp3");
                                        // ComputeMD5(filepathMP32, item.Field<string>("asset_isrc"));

                                        if (mp3Files.Length > 0)
                                        {
                                            string selectedMp3File = mp3Files[0];

                                            // Separate the directory path and the file name from media_path
                                            string destinationDirectory = Path.GetDirectoryName(Path.Combine(companyFolderPath, media_path));
                                            string destinationFileName = Path.GetFileName(media_path);

                                            // Ensure the directory exists
                                            if (!Directory.Exists(destinationDirectory))
                                            {
                                                Directory.CreateDirectory(destinationDirectory);
                                            }

                                            // Combine the directory and the new file name
                                            string destinationFilePath = Path.Combine(destinationDirectory, destinationFileName);

                                            // Copy the file to the new location with the correct name
                                            File.Copy(selectedMp3File, destinationFilePath, overwrite: true);
                                        }


                                    }



                                    break;

                                case "WAVE":
                                    if (!RBT_Companies.Contains(storeID))
                                    {
                                        string WAVFolderPath = Path.Combine(basePath, "WAV");
                                        string[] WAVFiles = Directory.GetFiles(WAVFolderPath, $"{item.Field<string>("asset_isrc")}.WAV");
                                        if (WAVFiles.Length == 0)
                                        {
                                            // Throw an exception and exit the function
                                            LogMessage(logFilePath, $"No WAV files found for asset ISRC: {item.Field<string>("asset_isrc")}");
                                            LogErrorToDatabase($"No WAV files found for asset ISRC: {item.Field<string>("asset_isrc")}", PkgID);
                                            MediaFILESCounter = 0;
                                            continue;
                                        }
                                        MediaFILESCounter = 1;
                                        string filepathWAV1 = Path.Combine(WAVFolderPath, $"{item.Field<string>("asset_isrc")}.WAV");
                                        ComputeMD5(filepathWAV1, item.Field<string>("asset_isrc"), 1);

                                        if (WAVFiles.Length > 0)
                                        {
                                            string selectedWAVFile = WAVFiles[0];

                                            // Separate the directory path and the file name from media_path
                                            string destinationDirectory = Path.GetDirectoryName(Path.Combine(companyFolderPath, media_path));
                                            string destinationFileName = Path.GetFileName(media_path);

                                            // Ensure the directory exists
                                            if (!Directory.Exists(destinationDirectory))
                                            {
                                                Directory.CreateDirectory(destinationDirectory);
                                            }

                                            // Combine the directory and the new file name
                                            string destinationFilePath = Path.Combine(destinationDirectory, destinationFileName);

                                            // Copy the file to the new location with the correct name
                                            File.Copy(selectedWAVFile, destinationFilePath, overwrite: true);
                                        }
                                    }
                                    else
                                    {
                                        basePath = @"\\10.1.1.26\Data\FTP_Data\AudioLibrary\IVR_Cuts\wav\45";

                                        string WAVFolderPath = Path.Combine(basePath);
                                        string[] WAVFiles = Directory.GetFiles(WAVFolderPath, $"{item.Field<string>("rbt_code")}.WAV");
                                        if (WAVFiles.Length == 0)
                                        {
                                            LogMessage(logFilePath, $"No WAV files found for asset ISRC: {item.Field<string>("asset_isrc")}");
                                            LogErrorToDatabase($"No WAV files found for asset ISRC: {item.Field<string>("asset_isrc")}", PkgID);
                                            MediaFILESCounter = 0;
                                            continue;
                                        }
                                        MediaFILESCounter = 1;

                                        string filepathWAV2 = Path.Combine(WAVFolderPath, $"{item.Field<string>("asset_isrc")}.WAV");
                                        // ComputeMD5(filepathWAV2, item.Field<string>("asset_isrc"));

                                        if (WAVFiles.Length > 0)
                                        {
                                            string selectedWAVFile = WAVFiles[0];

                                            // Separate the directory path and the file name from media_path
                                            string destinationDirectory = Path.GetDirectoryName(Path.Combine(companyFolderPath, media_path));
                                            string destinationFileName = Path.GetFileName(media_path);

                                            // Ensure the directory exists
                                            if (!Directory.Exists(destinationDirectory))
                                            {
                                                Directory.CreateDirectory(destinationDirectory);
                                            }

                                            // Combine the directory and the new file name
                                            string destinationFilePath = Path.Combine(destinationDirectory, destinationFileName);

                                            // Copy the file to the new location with the correct name
                                            File.Copy(selectedWAVFile, destinationFilePath, overwrite: true);
                                        }
                                    }


                                    break;

                                case "FLAC":
                                    string FLACFolderPath = Path.Combine(basePath, "FLAC");
                                    string[] FLACFiles = Directory.GetFiles(FLACFolderPath, $"{item.Field<string>("asset_isrc")}.FLAC");
                                    if (FLACFiles.Length == 0)
                                    {
                                        LogMessage(logFilePath, $"No FLAC files found for asset ISRC: {item.Field<string>("asset_isrc")}");
                                        LogErrorToDatabase($"No FLAC files found for asset ISRC: {item.Field<string>("asset_isrc")}", PkgID);
                                        continue;
                                    }
                                    string filepath = Path.Combine(FLACFolderPath, $"{item.Field<string>("asset_isrc")}.FLAC");
                                    // ComputeMD5(filepath, item.Field<string>("asset_isrc"));

                                    if (FLACFiles.Length > 0)
                                    {
                                        string selectedFLACFile = FLACFiles[0];

                                        // Separate the directory path and the file name from media_path
                                        string destinationDirectory = Path.GetDirectoryName(Path.Combine(companyFolderPath, media_path));
                                        string destinationFileName = Path.GetFileName(media_path);

                                        // Ensure the directory exists
                                        if (!Directory.Exists(destinationDirectory))
                                        {
                                            Directory.CreateDirectory(destinationDirectory);
                                        }

                                        // Combine the directory and the new file name
                                        string destinationFilePath = Path.Combine(destinationDirectory, destinationFileName);

                                        // Copy the file to the new location with the correct name
                                        File.Copy(selectedFLACFile, destinationFilePath, overwrite: true);
                                    }

                                    break;

                                case "MP4":

                                    break;

                                case "M4A":
                                    string M4AFolderPath = Path.Combine(basePath, "M4A");
                                    string[] M4AFiles = Directory.GetFiles(M4AFolderPath, $"{item.Field<string>("asset_isrc")}.M4A");
                                    if (M4AFiles.Length == 0)
                                    {
                                        LogMessage(logFilePath, $"No M4A files found for asset ISRC: {item.Field<string>("asset_isrc")}");
                                        LogErrorToDatabase($"No M4A files found for asset ISRC: {item.Field<string>("asset_isrc")}", PkgID);
                                        continue;
                                    }
                                    string filepathM4A = Path.Combine(M4AFolderPath, $"{item.Field<string>("asset_isrc")}.M4A");
                                    // ComputeMD5(filepathM4A, item.Field<string>("asset_isrc"));

                                    if (M4AFiles.Length > 0)
                                    {
                                        string selectedM4AFile = M4AFiles[0];

                                        // Separate the directory path and the file name from media_path
                                        string destinationDirectory = Path.GetDirectoryName(Path.Combine(companyFolderPath, media_path));
                                        string destinationFileName = Path.GetFileName(media_path);

                                        // Ensure the directory exists
                                        if (!Directory.Exists(destinationDirectory))
                                        {
                                            Directory.CreateDirectory(destinationDirectory);
                                        }

                                        // Combine the directory and the new file name
                                        string destinationFilePath = Path.Combine(destinationDirectory, destinationFileName);

                                        // Copy the file to the new location with the correct name
                                        File.Copy(selectedM4AFile, destinationFilePath, overwrite: true);
                                    }

                                    break;
                            }

                        }
                    }

                }

            }
            catch (Exception ex)
            {
                string cleanedMessage = ex.Message.Replace("'", "");
                LogErrorToDatabase(cleanedMessage, PkgID);
                LogMessage(logFilePath, $"Error fetching package info: {cleanedMessage}");
            }



        }


        private void LogErrorToDatabase(string errorMessage, string pkgid)
        {

            string updatequery = $"UPDATE packages set status = 2 , error_notes = error_notes + ' Err: ' + '{errorMessage}' WHERE pkg_id = {pkgid}";
            db.ExecuteNonQuery(updatequery);



        }

        private void SucssesPKG(string pkgid)
        {
            string updatequery = $"UPDATE packages set status = 1  WHERE pkg_id = {pkgid}";
            db.ExecuteNonQuery(updatequery);

        }

        private void LogMessage(string logFilePath, string message)
        {
            using (StreamWriter writer = new StreamWriter(logFilePath, true))
            {
                writer.WriteLine($"{DateTime.Now}: {message}");
            }
        }

        public void GetFileSize(string filePath, string isrc, int type)
        {   //Get Size of asset
            if (File.Exists(filePath) && type == 1)
            {
                FileInfo fileInfo = new FileInfo(filePath);

                TrackSize track = new TrackSize
                {
                    isrc = isrc,
                    size = fileInfo.Length
                };

                tracksize.Add(track);

            }
            //Get size of Cover
            else if (File.Exists(filePath) && type == 2)
            {
                FileInfo fileInfo = new FileInfo(filePath);

                imageSize = fileInfo.Length;

            }
            else
            {
                throw new FileNotFoundException("File not found: " + filePath);
            }
        }
        public static string GetLocalIPAddress()
        {
            try
            {
                // Get the host name of the local machine
                var host = Dns.GetHostEntry(Dns.GetHostName());

                // Loop through each address and find the first IPv4 address
                foreach (var ip in host.AddressList)
                {
                    if (ip.AddressFamily == AddressFamily.InterNetwork)
                    {
                        return ip.ToString();
                    }
                }
                throw new Exception("No IPv4 address found for the local machine.");
            }
            catch (Exception ex)
            {
                return $"Error retrieving IP address: {ex.Message}";
            }
        }

        private void WriteColoredLine(string message, ConsoleColor color)
        {
            // Save the current console color
            var previousColor = Console.ForegroundColor;

            // Set the console color to the specified color
            Console.ForegroundColor = color;

            // Write the message
            Console.WriteLine(message);

            // Restore the original console color
            Console.ForegroundColor = previousColor;
        }

        /*private static void DisableMouseInput()
        {
            IntPtr consoleHandle = GetStdHandle(STD_INPUT_HANDLE);

            if (GetConsoleMode(consoleHandle, out uint consoleMode))
            {
                // Disable mouse input and quick edit mode
                uint newConsoleMode = consoleMode & ~(ENABLE_MOUSE_INPUT | ENABLE_QUICK_EDIT_MODE);
                newConsoleMode |= ENABLE_EXTENDED_FLAGS;

                SetConsoleMode(consoleHandle, newConsoleMode);
            }
        }*/

        private async Task RunTask()
        {
            // Only allow one execution of Getpkgs at a time
            if (await _semaphore.WaitAsync(0))
            {
                try
                {
                    Getpkgs();
                }
                finally
                {
                    if (checkBox1.Checked == false)
                    {
                    }
                    else
                    {
                        button1.Enabled = false;
                    }
                    _semaphore.Release();
                   
                 

                }
            }
            else
            {
                WriteColoredLine("Task is still running. Skipping this interval...", ConsoleColor.Yellow);
            }
        }


        private void button1_Click(object sender, EventArgs e)
        {
            Getpkgs();
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox1.Checked)
            {
                // Start the timer
                _timer = new System.Threading.Timer(async state => await RunTask(), null, 0, 30000);
                button1.Enabled = false;
            }
            else
            {
                // Stop the timer
                _timer?.Change(Timeout.Infinite, 0); // Stop the timer
                _timer?.Dispose(); // Dispose of the timer if no longer needed
                _timer = null;
                button1.Enabled = true;
                // Clear the reference

            }
        }



        private void InitializeNotifyIcon()
        {
            _notifyIcon = new NotifyIcon
            {
                Icon = new Icon(Path.Combine("..\\..\\..\\", "app.ico")), // Set your icon here
                Visible = true,
                Text = "Package-Generator" // Tooltip text
            };

            // Create a context menu
            var contextMenu = new ContextMenuStrip();
            contextMenu.Items.Add("Show", null, Restore_Click);
            contextMenu.Items.Add("Exit", null, Exit_Click);
            _notifyIcon.ContextMenuStrip = contextMenu;

            // Handle double-click to restore
            _notifyIcon.DoubleClick += (s, e) => Restore();
        }
        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            if (WindowState == FormWindowState.Minimized)
            {
                Hide(); // Hide the form
            }
        }
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            e.Cancel = true; // Cancel the close event
            Hide(); // Hide the form instead
        }
        private void Restore_Click(object sender, EventArgs e)
        {
            Restore();
        }
        private void Restore()
        {
            Show(); // Show the form
            WindowState = FormWindowState.Normal; // Restore window state
        }

        private void Exit_Click(object sender, EventArgs e)
        {
            Environment.Exit(0);

        }
        private void AppendLog(string message, Color color)
        {
            if (richTextBox1.InvokeRequired)
            {
                richTextBox1.Invoke(new Action(() => AppendLog(message, color)));

            }
            else
            {
                richTextBox1.SelectionStart = richTextBox1.TextLength;
                richTextBox1.SelectionLength = 0;
                richTextBox1.SelectionColor = color;
                richTextBox1.AppendText(message + Environment.NewLine);
                richTextBox1.SelectionColor = richTextBox1.ForeColor; // Reset to default color
                richTextBox1.ScrollToCaret(); // Scroll to the end
                richTextBox1.Refresh(); // Forces the control to redraw itself

            }
        }

    }


    public class AssetHash
    {
        public string ISRC { get; set; }
        public string MD5Hash { get; set; }
    }

    public class XMLHASH
    {
        public string upc { get; set; }
        public string md5 { get; set; }

    }

    public class TrackSize
    {
        public string isrc { get; set; }
        public long size { get; set; }
    }
}
