using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

using System.IO;

public partial class WeatherService : System.Web.UI.Page
{
    private static Dictionary<string, bool> variables;
    private static double north, south, west, east;
    private static DateTime timeStart, timeEnd;
    private static Dictionary<string, string> dataListForHYCOM, dataListForNWWIII;
    private static string pathToData, pathToNWWIII, pathToHYCOM, pathToUserData;
    private static string drivePath;

    protected void Page_Load(object sender, EventArgs e)
    {
        variables = new Dictionary<string, bool>()
	    {
	        {"dp", false},
	        {"hs", false},
	        {"tp", false},
	        {"wind", false}
	    };

        // TODO: NWWIII and HYCOM should be in the same dataList
        dataListForHYCOM = new Dictionary<string, string>()
	    {
	        {"s", "Salinity"},
	        {"t", "Temperature"},
	        {"u", "U_Component"},
	        {"v", "V_Component"}
	    };

        dataListForNWWIII = new Dictionary<string, string>()
	    {
	        {"dp",   "WaveDirection"},
	        {"hs",   "SignificantHeight"},
	        {"tp",   "MeanPeriod"},
	        {"wind", "Wind_UandV"}
	    };

        //TODO: this should not be hardcoded
        drivePath = @"E:\";
        pathToData = Path.Combine(drivePath, "Data");
        pathToHYCOM = Path.Combine(pathToData, "HYCOM_Current");
        pathToNWWIII = Path.Combine(pathToData, "NWWIII_Wave");
        pathToUserData = Path.Combine(pathToData, "OUT");

    }

    private static DateTime fixInputDate(string date)
    {
        string[] startDate = date.Split('-');
        string[] startDateTime = startDate[3].Split(':');

        System.Diagnostics.Debug.WriteLine(startDate);

        // The time resolution is in minutes
        DateTime time = new DateTime(Convert.ToInt16(startDate[0]), Convert.ToInt16(startDate[1]),
                                        Convert.ToInt16(startDate[2]), Convert.ToInt16(startDateTime[0]),
                                        Convert.ToInt16(startDateTime[1]), 0);

        return time;
    }

    private void getParametersFromUser()
    {
        // TODO: Finish this one. Get all the required data from the user site.
        if (varCheck_dp.Checked)
            variables["dp"] = true;
        else
            variables["dp"] = false;
        if (varCheck_hs.Checked)
            variables["hs"] = true;
        else
            variables["hs"] = false;
        if (varCheck_tp.Checked)
            variables["tp"] = true;
        else
            variables["tp"] = false;
        if (varCheck_wind.Checked)
            variables["wind"] = true;
        else
            variables["wind"] = false;

        north = Convert.ToDouble(textBox_north.Text);
        south = Convert.ToDouble(textBox_south.Text);
        west = Convert.ToDouble(textBox_west.Text);
        east = Convert.ToDouble(textBox_east.Text);

        timeStart = fixInputDate(textBox_timeStart.Text);
        timeEnd = fixInputDate(textBox_timeEnd.Text);

    }

    // TODO: This function is a copy from OceanWeatherToGribDatabase this should be avoided
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
            System.Diagnostics.Debug.WriteLine("Error in executeCommandCDM " + objException.ToString());
        }
    }

    private static int getFileMonth(string fileName)
    {
        string[] split = fileName.Split('.');
        string date = split[split.Length - 2];
        string strMonth = date[date.Length - 2].ToString() + date[date.Length - 1].ToString();
        int month = Convert.ToInt16(strMonth);

        return month;
    }

    //TODO: fix this one
    //This should span all years and months. Not just months
    private static bool fileContainsTimeSpan(string fileName)
    {
        bool contains = false;
        //this function should be something like the following
        //IEnumerable<int> yearRange = Enumerable.Range(timeStart.Year, timeEnd.Year);
        //if (timeStart.Month < timeEnd.Year)
        //{
        //    IEnumerable<int> monthRange = Enumerable.Range(timeStart.Month, timeEnd.Month);
        //
        //}
        // foreach in time span range check if the file belongs there

        IEnumerable<int> monthRange = Enumerable.Range(timeStart.Month, timeEnd.Month);
        int month = getFileMonth(fileName);
        contains = monthRange.Contains<int>(month);

        return contains;
    }

    private static List<string> getFilesWithDesiredTimeSpan(string targetDirectory)
    {
        List<string> files = new List<string>();
        // Process the list of files found in the directory. 
        string[] fileEntries = Directory.GetFiles(targetDirectory);
        foreach (string filePath in fileEntries)
        {
            string fileName = filePath.Split('\\').Last();
            if (fileContainsTimeSpan(fileName))
            {
                files.Add(filePath);
                System.Diagnostics.Debug.WriteLine(filePath);
            }
        }
        return files;
    }

    //TODO: finish this function
    private static string combineFilesOnParameters()
    {
        //TODO: check for correctness in -sellonlatbox for these coordinates
        //TODO: fix for startMonth > endMonth
        string command;
        string outFile = "out.grb2";
        string tempFile = "";
        List<string> tempFiles = new List<string>();
        foreach (var checkedVariable in variables)
        {
            command = "cdo mergetime";
            string subCommand = String.Format(" -sellonlatbox,{0},{1},{2},{3} ", west, east, south, north);
            System.Diagnostics.Debug.WriteLine(subCommand);
            if (checkedVariable.Value)
            {
                string folder = dataListForNWWIII[checkedVariable.Key];
                System.Diagnostics.Debug.WriteLine("folder: " + folder);
                List<string> files = getFilesWithDesiredTimeSpan(Path.Combine(pathToNWWIII, folder));
                foreach (string file in files)
                {
                    command += subCommand + file;
                }
                tempFile = "out" + checkedVariable.Key + ".grb2";
                tempFiles.Add(tempFile);
                command += " " + tempFile;

                System.Diagnostics.Debug.WriteLine(command);
                executeCommandCMD(command, pathToUserData);
            }
        }

        //Assumes that each dataset contains same number of timesteps and different variables in each
        outFile = tempFile; //TODO:
        //command = "cdo merge ";
        //foreach (string file in tempFiles)
        //{
        //    command += file + " ";
        //}
        //command += outFile;
        //System.Diagnostics.Debug.WriteLine(command);
        //executeCommandCMD(command, pathToUserData);

        //Delete all tempfiles
        foreach (string file in tempFiles)
        {
            System.Diagnostics.Debug.WriteLine("rm " + file);
            //executeCommandCMD("rm " + file, pathToUserData);
        }

        //Filter for correct time span
        command = "cdo selday";
        IEnumerable<int> dayRange = Enumerable.Range(timeStart.Day, timeEnd.Day - (timeStart.Day - 1));

        System.Diagnostics.Debug.WriteLine("time End day:" + timeEnd.Day);

        foreach (int day in dayRange)
        {
            command += "," + day.ToString();
        }
        string oldFile = outFile;
        outFile = "OUT.grb2";
        command += " " + oldFile + " " + outFile;

        System.Diagnostics.Debug.WriteLine(command);
        executeCommandCMD(command, pathToUserData);
        //executeCommandCMD("rm " + oldFile, pathToUserData);

        System.Diagnostics.Debug.WriteLine("Writing to CSV");
        command = String.Format("degrib {0} -C -msg all -nMet -Csv", outFile);
        System.Diagnostics.Debug.WriteLine(command);
        executeCommandCMD(command, pathToUserData);

        return outFile;
    }

    //System.Diagnostics.Debug.WriteLine();
    protected void buttonSubmit_Click(object sender, EventArgs e)
    {
        //TODO: collect the desired data and return to user csv-file

        //Get all the parameters from the user
        try
        {
            getParametersFromUser();
        }
        catch (Exception objException)
        {
            // Throw error:
            System.Diagnostics.Debug.WriteLine("Error in users parameters" + objException.ToString());
        }

        //Get the user requested data file from database
        string tempGribFile = combineFilesOnParameters();

        System.Diagnostics.Debug.WriteLine("final File: " + tempGribFile);


        System.Diagnostics.Debug.WriteLine("Done");
    }

    //System.Diagnostics.Debug.WriteLine();
    protected void buttonReset_Click(object sender, EventArgs e)
    {
        textBox_north.Text = "90";
        textBox_south.Text = "-90";
        textBox_west.Text = "-180";
        textBox_east.Text = "180";

        textBox_timeStart.Text = "2011-01-04-12:04:00";
        textBox_timeEnd.Text = "2011-01-06-15:07:00";
    }
}
