// Use this code inside a project created with the Visual C# > Windows Desktop > Console Application template.
// Replace the code in Program.cs with this code.

using Newtonsoft.Json;
using SmartUmbrella.Models;
using System;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Net;
using System.Threading;

public class PortChat
{
    static bool _continue;
    static SerialPort _serialPort;
    static bool isRainInForecast = false;

    public static void Main()
    {
        StringComparer stringComparer = StringComparer.OrdinalIgnoreCase;
        Thread readThread = new Thread(Read);

        // Create a new SerialPort object with default settings.
        _serialPort = new SerialPort();

        // Allow the user to set the appropriate properties.
        _serialPort.PortName = "COM1";
        _serialPort.BaudRate = 9600;
        _serialPort.Parity = Parity.None;
        _serialPort.DataBits = 8;
        _serialPort.StopBits = StopBits.One;
        _serialPort.Handshake = Handshake.None;

        // Set the read/write timeouts
        _serialPort.ReadTimeout = 500;
        _serialPort.WriteTimeout = 500;

        _serialPort.Open();
        _continue = true;
        readThread.Start();

        Console.WriteLine("In Weather Service. Stop debugging to exit");

        while (_continue)
        {
            try
            {
                string message = Console.ReadLine();
                if (stringComparer.Equals("quit", message))
                {
                    _continue = false;
                }

            }
            catch (TimeoutException) { }
        }

        readThread.Join();
        _serialPort.Close();
    }

    public static void Read()
    {
        string message;
        while (_continue)
        {
            try
            {
                message = _serialPort.ReadLine();
                Console.WriteLine("Received coordinates {0} from tock application", message);
                if (message != null)
                {
                    string[] coords = message.Split(',');
                    //check decimal
                    decimal latitude;
                    decimal longitude;
                    if (decimal.TryParse(coords[0], out latitude) && decimal.TryParse(coords[1], out longitude))
                    {
                        isRainInForecast = GetForecast(latitude, longitude);
                        Console.WriteLine("Response from Open Weather API - rain: {0}", isRainInForecast);
                        if (isRainInForecast)
                        {
                            _serialPort.WriteLine("1");
                        }
                        else
                        {
                            _serialPort.WriteLine("0");
                        }
                        Console.WriteLine("Sent {0} to tock app", isRainInForecast);
                    }
                    else
                    {
                        Console.WriteLine("Coordinates not in correct format!");
                    }
                    message = null;
                }
            }
            catch (TimeoutException) { }
        }
    }
    public static int[] RainCodes = {200, 201, 202, 210, 211, 212, 221, 230, 231, 232, 300, 301, 302, 310, 311, 312, 313, 314, 321, 500, 501, 502, 503, 504, 511, 520, 521, 522, 531, 611, 612, 613, 615, 616, 620, 621, 622};
    public static bool GetForecast(decimal Latitude, decimal Longitude)
    {
        string urlBase = "http://api.openweathermap.org/data/2.5/onecall?lat={0}&lon={1}&exclude=minutely,daily&APPID=6ce31626181846df8fb15eed32a345b0&units=metric";
        string url = String.Format(urlBase, Latitude, Longitude);
        WeatherForecast weatherForecastViewModel = new WeatherForecast();
        string answer;
        HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
        request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;

        using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
        using (Stream stream = response.GetResponseStream())
        using (StreamReader reader = new StreamReader(stream))
        {
            answer = reader.ReadToEnd();
        }

        weatherForecastViewModel = JsonConvert.DeserializeObject<WeatherForecast>(answer);
        bool isRainInForecast = false;

        isRainInForecast = weatherForecastViewModel.Current.Weather.Any(w => RainCodes.Contains(w.Id)) || weatherForecastViewModel.Hourly.Any(h => h.Weather.Any(w => RainCodes.Contains(w.Id)));

        return isRainInForecast;
    }
}