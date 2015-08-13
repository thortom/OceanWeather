using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.IO;
using System.Data.SqlClient;
using System.Net;

using System.Diagnostics;

namespace WeatherData
{
    class OceanWeatherToGribNetcdfDatabase
    {
        private static string username, password;
        private static string urlHYCOM, urlNWWIII;
        private static string pathToData, pathToUserData, pathToNWWIII, pathToHYCOM;
        private static int year;
        private static Dictionary<string, string> dataListForHYCOM, dataListForNWWIII;
        private static List<string> HYCOMVariables, NWWIIIVariables;
        private static List<List<string>> glbaFolders;
        private static string glbaFolder, glbaNextFolder;

        public OceanWeatherToGribNetcdfDatabase(string dataPath, int currentYear)
        {
            username = "anonymous";
            password = "anonymous@example.com";
            pathToData = Path.Combine(dataPath, "Data");
            pathToUserData = Path.Combine(pathToData, "OUT");
            year = currentYear;

            glbaFolders = new List<List<string>>();
            glbaFolders.Add(new List<string> {"expt_90.6", "2008"});
            glbaFolders.Add(new List<string> {"expt_90.8", "2010"});
            glbaFolders.Add(new List<string> {"expt_90.9", "2012"});
            glbaFolders.Add(new List<string> {"expt_91.0", "2014"});
            glbaFolders.Add(new List<string> {"expt_91.1", "2015"});

            glbaFolder = "";
            bool nextFolder = false;
            foreach (var list in glbaFolders)
            {
                int glbaYear = Convert.ToInt16(list[1]);
                string folder = list[0];
                if (glbaYear <= currentYear)
                {
                    glbaFolder = folder;
                    nextFolder = false;
                }
                if (nextFolder)
                {
                    glbaNextFolder = folder;
                    break;
                }
                nextFolder = true;
            }

            //urlHYCOM = "ftp://ftp.hycom.org/datasets/GLBa0.08";
            urlHYCOM = "http://ncss.hycom.org/thredds/ncss/grid/GLBa0.08/" + glbaFolder;
            pathToHYCOM = Path.Combine(pathToData, "HYCOM_Current");

            HYCOMVariables = new List<string>();
            HYCOMVariables.Add("salinity");
            HYCOMVariables.Add("temperature");
            HYCOMVariables.Add("u");
            HYCOMVariables.Add("v");

            dataListForHYCOM = new Dictionary<string, string>()
	        {
	            {"s", "Salinity"},
	            {"t", "Temperature"},
	            {"u", "U_Component"},
	            {"v", "V_Component"}
	        };

            urlNWWIII = "ftp://polar.ncep.noaa.gov/pub/history/waves";
            pathToNWWIII = Path.Combine(pathToData, "NWWIII_Wave");

            NWWIIIVariables = new List<string>();
            NWWIIIVariables.Add("dp");
            NWWIIIVariables.Add("hs");
            NWWIIIVariables.Add("tp");
            NWWIIIVariables.Add("wind");

            dataListForNWWIII = new Dictionary<string, string>()
	        {
	            {"dp",   "WaveDirection"},
	            {"hs",   "SignificantHeight"},
	            {"tp",   "MeanPeriod"},
	            {"wind", "Wind_UandV"}
	        };
        }

        public static void usage()
        {
            Console.WriteLine("This program will download all available data from NWWIII and HYCOM.");
            Console.WriteLine("For the following variables:");
            Console.WriteLine("                            NWWIII -> Wave direction, significant wave height, mean period, U and V for wind");
            Console.WriteLine("                            HYCOM  -> Salinity, Temperature, U and V current component");
            Console.WriteLine("The HYCOM HTTP website responces are slow, thus this program might take up to 4hrs to fetch data for one year.");
            Console.WriteLine("************************************************************************\n\n");
            Console.WriteLine("Usage: The input should be like:\n$: InsertToOceanWeatherDataDB.exe 2011");
            Console.WriteLine("       Where year = 2011");
            Console.ReadKey();
            System.Environment.Exit(-1);
        }

        private static MyWebClient anonymousRequest()
        {
            MyWebClient request = new MyWebClient();
            request.Credentials = new NetworkCredential(username, password);
            return request;
        }

        private class MyWebClient : WebClient
        {
            protected override WebRequest GetWebRequest(Uri uri)
            {
                WebRequest w = base.GetWebRequest(uri);
                w.Timeout = 20 * 60 * 1000;
                return w;
            }
        }

        //Checks the file exists or not: 
        private static bool CheckIfFtpFileExists(string fileUri)
        {
            FtpWebRequest request = (FtpWebRequest)WebRequest.Create(fileUri);
            request.Credentials = new NetworkCredential(username, password);
            request.Method = WebRequestMethods.Ftp.GetFileSize;
            try
            {
                FtpWebResponse response = (FtpWebResponse)request.GetResponse();
                // THE FILE EXISTS
            }
            catch (WebException ex)
            {
                FtpWebResponse response = (FtpWebResponse)ex.Response;
                if (FtpStatusCode.ActionNotTakenFileUnavailable == response.StatusCode)
                {
                    // THE FILE DOES NOT EXIST
                    return false;
                }
            }
            return true;
        }

        //Checks if the file exists or not:
        private static bool CheckIfHttpFileExists(string fileUrl)
        {
            HttpWebResponse response = null;
            var request = (HttpWebRequest)WebRequest.Create(fileUrl);
            request.Timeout = 20 * 60 * 1000;                           //HYCOM HTTP is slow in responding
            request.Method = "HEAD";
            try
            {
                response = (HttpWebResponse)request.GetResponse();
            }
            catch (WebException ex)
            {
                // A WebException will be thrown if the status of the response is not `200 OK`
                return false;
            }
            finally
            {
                if (response != null)
                {
                    response.Close();
                }
            }
            return true;
        }

        private static IEnumerable<string> GetFilesInFtpDirectory(string url)
        {
            // Get the object used to communicate with the server.
            var request = (FtpWebRequest)WebRequest.Create(url);
            request.Method = WebRequestMethods.Ftp.ListDirectoryDetails;
            request.Credentials = new NetworkCredential(username, password);

            using (var response = (FtpWebResponse)request.GetResponse())
            {
                using (var responseStream = response.GetResponseStream())
                {
                    var reader = new StreamReader(responseStream);
                    while (!reader.EndOfStream)
                    {
                        var line = reader.ReadLine();
                        if (string.IsNullOrWhiteSpace(line) == false)
                        {
                            yield return line.Split(new[] { ' ', '\t' }).Last();
                        }
                    }
                }
            }
        }

        // Executes a shell command synchronously.
        public static void executeCommandCMD(object command, string workingDirectory)
        {
            try
            {
                System.Diagnostics.Process process = new System.Diagnostics.Process();
                System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo();
                startInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
                startInfo.WorkingDirectory = workingDirectory;
                startInfo.FileName = "cmd.exe";

                // "/C" ends the process when this argument has been executed
                startInfo.Arguments = "/C" + command;
                process.StartInfo = startInfo;
                process.Start();
                process.WaitForExit();
            }
            catch (Exception objException)
            {
                // Log the exception
            }
        }

        //assumes month on the scale 1 to 12
        private static string fixMonthToString(int numb)
        {
            string month = numb.ToString();
            if (month.Length == 1)
                month = "0" + month;
            return month;
        }

        public static List<string> getPathsForNWWIII()
        {
            List<string> listOfUrls = new List<string>();
            foreach (string variable in NWWIIIVariables)
            {
                //for each month in year
                for (int month = 1; month <= 12; month++)
                {
                    string fileName = urlNWWIII + "/multi_1.glo_30m." + variable + "." + year.ToString() + fixMonthToString(month) + ".grb2";

                    addUrl(ref listOfUrls, fileName);
                }
            }
            return listOfUrls;
        }

        private static string fixDayToString(int numb)
        {
            string day = numb.ToString();
            if (day.Length == 1)
                day = "00" + day;
            if (day.Length == 2)
                day = "0" + day;
            return day;
        }

        private static string fileNameForHYCOM(int year, int day, string dataType)
        {
            return "archv." + year + "_" + fixDayToString(day) + "_00_3z" + dataType[0].ToString() + ".nc";
        }

        // TODO: use the NetcdfSubset for the download path at: 
        //              http://tds.hycom.org/thredds/GLBa0.08/expt_90.9.html?dataset=GLBa0.08/expt_90.9
        // example download links:
        //              http://ncss.hycom.org/thredds/ncss/grid/GLBa0.08/expt_90.9?var=u&north=89.9777&west=74.1199&east=1019.1199&south=-78.6399&horizStride=1&time_start=2011-01-01T00%3A00%3A00Z&time_end=2011-02-01T00%3A00%3A00Z&timeStride=1&vertCoord=0&accept=netcdf
        //              http://ncss.hycom.org/thredds/ncss/grid/GLBa0.08/expt_90.9?var=salinity&north=89.9777&west=74.1199&east=1019.1199&south=-78.6399&horizStride=1&time_start=2011-01-03T00%3A00%3A00Z&time_end=2011-08-20T00%3A00%3A00Z&timeStride=1&vertCoord=0&accept=netcdf
        //              http://ncss.hycom.org/thredds/ncss/grid/GLBa0.08/expt_90.9?var=temperature&north=89.9777&west=74.1199&east=1019.1199&south=-78.6399&horizStride=1&time_start=2011-01-03T00%3A00%3A00Z&time_end=2011-08-20T00%3A00%3A00Z&timeStride=1&vertCoord=0&accept=netcdf
        // With the NetcdfSubset it is posible to choose what vertical layers to download
        public static List<string> getPathsForHYCOM()
        {
            List<string> listOfUrls = new List<string>();
            string fileName;
            foreach (string variable in HYCOMVariables)
            {
                bool done = false;
                while (!done)
                {
                    Console.WriteLine("searching for HYCOM files in glbaFolder: " + glbaFolder);
                    //for each month in year
                    for (int month = 1; month < 12; month++)
                    {
                        fileName = urlHYCOM + "?var=" + variable + "&north=89.9777&west=74.1199&east=1019.1199&south=-78.6399&horizStride=1&time_start=" + year.ToString() + "-" + fixMonthToString(month) + "-01T00%3A00%3A00Z&time_end=" + year.ToString() + "-" + fixMonthToString(month + 1) + "-01T00%3A00%3A00Z&timeStride=1&vertCoord=0&accept=netcdf";

                        addUrl(ref listOfUrls, fileName);
                    }
                    //The url for the 12 month is different
                    string request = "?var=" + variable + "&north=89.9777&west=74.1199&east=1019.1199&south=-78.6399&horizStride=1&time_start=" + year.ToString() + "-" + fixMonthToString(12) + "-01T00%3A00%3A00Z&time_end=" + (year + 1).ToString() + "-" + fixMonthToString(1) + "-01T00%3A00%3A00Z&timeStride=1&vertCoord=0&accept=netcdf";
                    fileName = urlHYCOM + request;

                    addUrl(ref listOfUrls, fileName);

                    //if the 12 month was added then we are done
                    if (listOfUrls.Count() > 0 && listOfUrls.Last() == fileName)
                    {
                        done = true;
                        Console.WriteLine("Got all HYCOM links for variable: " + variable);
                    }
                    else
                    {
                        Console.WriteLine("searching for HYCOM files in glbaFolder: " + glbaNextFolder);
                        urlHYCOM = urlHYCOM.Substring(0, urlHYCOM.Length - glbaFolder.Length);
                        urlHYCOM = urlHYCOM + glbaNextFolder;
                    }
                }
            }
            return listOfUrls;
        }

        private static void addUrl(ref List<string> listOfUrls, string url)
        {
            string protocol = url.Substring(0, 3).ToLower();
            if (protocol == "ftp")
            {
                if (CheckIfFtpFileExists(url))
                {
                    listOfUrls.Add(url);
                    Console.WriteLine("Adding: " + url);
                }
            }
            else //http
            {
                if (CheckIfHttpFileExists(url))
                {
                    listOfUrls.Add(url);
                    Console.WriteLine("Adding: " + url);
                }
            }
            return;
        }

        private static string fixFileName(string fileName)
        {
            //the HYCOM request strings are longer than NWWIII
            if (fileName.Length > 100)                       //TODO: make more robust than => > 100
            {
                int i;
                string[] variables = fileName.Split('&');
                i = variables[0].IndexOf("?var=") + "?var=".Length;
                string variable = variables[0].Substring(i);

                i = variables[6].IndexOf("time_start=") + "time_start=".Length;
                string time = variables[6].Substring(i, 7);

                fileName = time + "_" + variable + ".nc";
            }
            return fileName;
        }

        private static string downloadFile(string urlRequest, string saveLocation)
        {
            // TODO: maybe use SmartFTP for faster download rates
            //initialize web request
            MyWebClient request = anonymousRequest();          // Use MyWebClient to extend the timeout time

            //Debug text
            Console.WriteLine(urlRequest);
            Console.WriteLine("Starting the download...");            

            string fileName = urlRequest.Split('/').Last();
            fileName = fixFileName(fileName);
            string filePath = Path.Combine(saveLocation, fileName);

            // Download and save the file to disk
            request.DownloadFile(urlRequest, filePath);

            Console.WriteLine("Download complete");
            Console.WriteLine("File saved to " + filePath);

            return fileName;
        }

        private static bool createFolder(string path)
        {
            bool retValue = false;
            DirectoryInfo di = new DirectoryInfo(path);
            try
            {
                // Determine whether the directory exists. 
                if (di.Exists)
                {
                    // Indicate that the directory already exists.
                    Console.WriteLine("The path exists already.");
                }
                else
                {
                    // Create the directory.
                    di.Create();
                    Console.WriteLine("The directory was created successfully at: " + path);
                    retValue = true;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("The process failed: {0}", e.ToString());
            }

            return retValue;
        }

        private static void createFoldersFromDict(string parentFolder, Dictionary<string, string> dict)
        {
            createFolder(parentFolder);
            foreach (var folder in dict)
            {
                createFolder(Path.Combine(parentFolder, folder.Value));
            }
        }

        public static void checkForDataFolders()
        {
            //Create all data folders if dataPath does not exist before
            if (createFolder(pathToData))
            {
                //Create NWWIII data tree
                createFoldersFromDict(pathToNWWIII, dataListForNWWIII);

                //Create HYCOM data tree
                createFoldersFromDict(pathToHYCOM, dataListForHYCOM);

                //Create Users output data folder
                createFolder(pathToUserData);
            }
        }

        private static string getNWWIIIKeyFromPath(string path)
        {
            string[] splitPath = path.Split('.');
            string key = splitPath[splitPath.Length - 3];
            return key;
        }

        private static string getHYCOMKeyFromPath(string path)
        {
            string prefix = "var=";
            int idx = path.IndexOf(prefix);
            string key = path.Substring(idx + prefix.Length, 1);
            return key;
        }

        public static void setNWWIIIFilesToFolders(List<string> downloadPaths)
        {
            foreach (string path in downloadPaths)
            {
                string key = getNWWIIIKeyFromPath(path);
                string saveLocation = Path.Combine(pathToNWWIII, dataListForNWWIII[key]);
                string fileName = downloadFile(path, saveLocation);
                //TODO: apply fix file with cdo use .bash for that?
            }
        }

        private static string fixHYCOMFileName(string oldFileName)
        {
            string[] splitFile = oldFileName.Split('_');
            string year = splitFile[0].Split('.').Last();
            string day = splitFile[1];
            string type = splitFile.Last();

            return year + "_" + day + "_" + type;
        }

        public static void setHYCOMFilesToFolders(List<string> downloadPaths)
        {
            foreach (string path in downloadPaths)
            {
                string key = getHYCOMKeyFromPath(path);
                string workingDirectory = Path.Combine(pathToHYCOM, dataListForHYCOM[key]);

                //The NetCDF files are rather big. This download may take long time if the site becomes slow.
                string fileName = downloadFile(path, workingDirectory);

                ////use the cdo tool to retrieve only the surface measurements 
                //string command = String.Format("cdo select,level=0 {0} {1}", fileName, fixHYCOMFileName(fileName));
                //executeCommandCMD(command, workingDirectory);

                // Should not be needed if the correct file was downloaded
                ////remove the old file with all the unnecessary vertical mesurements
                //command = String.Format("rm {0}", fileName);
                //executeCommandCMD(command, workingDirectory);
            }
        }

        static void Main(string[] args)
        {
            string year = null;
            if (args.Length == 1)
            {
                year = args[0];
                Console.WriteLine("Collecting data for year: " + year);
            }
            else
            {
                usage();
                //year = "2011";
            }

            OceanWeatherToGribNetcdfDatabase dataHandler = new OceanWeatherToGribNetcdfDatabase(@"\", Convert.ToInt32(year));

            //Check if data folders exist if not then create them
            checkForDataFolders();

            //Get download paths for NWWIII
            Console.WriteLine("\nGetting download paths for NWWIII");
            List<string> NWWIIIpaths = getPathsForNWWIII();

            //Get download paths for HYCOM
            Console.WriteLine("\nGetting download paths for HYCOM....\nThis might take awhile");
            List<string> HYCOMpaths = getPathsForHYCOM();

            //Download and add each NWWIII file to database folders
            setNWWIIIFilesToFolders(NWWIIIpaths);

            //Download and add each HYCOM file to database folders
            setHYCOMFilesToFolders(HYCOMpaths);

            Console.ReadKey();
        }
    }
}
