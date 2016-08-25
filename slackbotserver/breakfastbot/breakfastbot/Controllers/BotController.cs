using System;
using System.Web.Mvc;
using Newtonsoft.Json;
using System.Text;
using System.Net;
using System.Collections.Specialized;
using System.Collections.Generic;
using breakfastbot.Models;
using Google.Apis.Sheets.v4;
using Google.Apis.Auth.OAuth2;
using System.IO;
using System.Threading;
using Google.Apis.Util.Store;
using Google.Apis.Services;
using Google.Apis.Sheets.v4.Data;

namespace AqueductSlackbot.Controllers
{
    public class BotController : Controller
    {
        // GET: Bot
        private static string[] scopes = { SheetsService.Scope.Spreadsheets };
        private static List<developers> breaklist = new List<developers>();
        private static List<developers> temp = new List<developers>();
        private static string applicationName = "breakfastbot";
        private string urlWithAccessToken = "https://hooks.slack.com/services/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";
        private IList<IList<object>> values;
        private string spreadsheetId = "1YMLuQ1tJnTJs1FQN0yruMHAS41nIRm1FHT87pP3GCV0";
        private string range = "Sheet1!A2:D";
        private ValueRange response;
        private SheetsService service;

        public ViewResult Start()
        {
            
            //variables
            var datewords = "";
            string[] done = new string[2];
            string name;
            int i = 0;
            string path = "C:/Users/Hannes Duncan/Documents/Visual Studio 2015/Projects/breakfastbot/breakfastbot/";
            UserCredential credential;
            SlackSend client = new SlackSend(urlWithAccessToken);
            //fetch list of devs
            // look for and read credentials for accessing and updating dev table

            using (var stream =
                new System.IO.FileStream(path + "client_secret.json", FileMode.Open, FileAccess.Read))
            {
                //string credPath = System.Environment.GetFolderPath(
                    //System.Environment.SpecialFolder.Personal);
                //credPath = Path.Combine(credPath, ".credentials/sheets.googleapis.com-dotnet-quickstart.json");
                credential = GoogleWebAuthorizationBroker.AuthorizeAsync(
                    GoogleClientSecrets.Load(stream).Secrets,
                    scopes,
                    "user",
                    CancellationToken.None,
                    new FileDataStore("C:/.credentials/sheets.googleapis.com-dotnet-quickstart.json", true)).Result;
                Console.WriteLine("Credential file saved to: " + "C:/.credentials/sheets.googleapis.com-dotnet-quickstart.json");
            }
            service = new SheetsService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = applicationName,
            });
            // Define request parameters.

            SpreadsheetsResource.ValuesResource.GetRequest request =
                    service.Spreadsheets.Values.Get(spreadsheetId, range);

            // Prints the slacknames and paid dates of devs in a spreadsheet:
            // https://docs.google.com/spreadsheets/d/1BxiMVs0XRA5nFMdKvBdBZjgmUUqptlbs74OgvE2upms/edit
            response = request.Execute();// fetch everything from range(in worksheet : Sheet1, with a range of A2:all of D(should be 14))
            values = response.Values; // put into indexed list of individualised objects
            // if a list of values has been found
            if (values.Count > 0)
            {
                // for each person in the list, create a new developer for them and add them to ateending list
                foreach (var row in values)
                {
                    name = (string)row[1];
                    datewords = (string)row[2];
                    done = datewords.Split('/');
                    temp.Add(new developers
                    {
                        slackname = name,
                        lastpay = new date { day = done[0], month = done[1], year = done[2] }
                    });
                }
            }

            client.PostMessage(text: "Process: Start!",
                       channel: "#breakfastmeet");

            foreach (var dev in temp)
            {
                client.PostMessage(text: "@" + temp[i].slackname + " can you make it for breakfast",
                       channel: "@hannes");
                i++;
            }
           
            var model = new BotModels { Message = "Process started successfully" };
            return View(model);
        }

        public ViewResult Msg(HookMessage message)
        {
            if (message.text == "yes")
            {
                breaklist.Add(new developers
                {
                    slackname = message.user_name,
                    lastpay = new date { day = "", month = "", year = "" }
                });
            }
            return null;
        }

        public ViewResult Payer()
        {
            int i = 0;
            int j = 0;
            int attending = 0;
            developers lastpayer = new developers();
            developers lastpayer2 = new developers(); // incase 2 payers are needed
            SlackSend client = new SlackSend(urlWithAccessToken);
            SpreadsheetsResource.ValuesResource.UpdateRequest request =
                service.Spreadsheets.Values.Update(response,spreadsheetId, range);

            foreach (var devs in temp)
            {
                foreach (var dev in breaklist)
                {
                    if (breaklist[i].slackname == temp[j].slackname)
                    {
                        breaklist[i] = temp[j];
                    }
                    i++;
                }
                j++;
            }
            //we now have list of people going

            attending = breaklist.Count;
            i = 0;
            // find last person to pay

            if (attending != 0)
            {
                lastpayer = breaklist[i];
                foreach (var cooldev in breaklist)
                {
                    if (Int32.Parse(lastpayer.lastpay.year) < Int32.Parse(breaklist[i].lastpay.year))
                    {
                        if (Int32.Parse(lastpayer.lastpay.month) < Int32.Parse(breaklist[i].lastpay.month))
                        {
                            if (Int32.Parse(lastpayer.lastpay.day) < Int32.Parse(breaklist[i].lastpay.day))
                            {
                                lastpayer = breaklist[i];
                            }
                        }
                    }
                    i++;
                }// end foreach
                // if there more than 10 people attending find another person to help pay
                // the next person would be the next person who would pay

                if (breaklist.Count > 10)
                {
                    foreach (var otherdev in breaklist)
                    {
                        j = 0;
                        if (Int32.Parse(lastpayer2.lastpay.year) < Int32.Parse(breaklist[j].lastpay.year))
                        {
                            if (Int32.Parse(lastpayer2.lastpay.month) < Int32.Parse(breaklist[j].lastpay.month))
                            {
                                if (Int32.Parse(lastpayer2.lastpay.day) < Int32.Parse(breaklist[j].lastpay.day))
                                {
                                    if (lastpayer.slackname != breaklist[j].slackname)
                                    {
                                        lastpayer2 = breaklist[j];
                                    }
                                }
                            }
                        }
                        j++;
                    }// end foreach otherdev
                }
                // posting messages to channel on results of proccess

                client.PostMessage(text: "It is @" + lastpayer.slackname + " turn to pay",
                    channel: "#breakfastmeet");
                if (!(lastpayer2.slackname == null))
                {
                    client.PostMessage(text: "and @" + lastpayer2.slackname + " has to pay aswell\n because of too many people!",
                        channel: "#breakfastmeet");
                }
                client.PostMessage(text: "there are a total of " + attending + " people attending breakfast!",
                    channel: "#breakfastmeet");
            } else
            {
                client.PostMessage(text: "no breakky :(",
                    channel: "#breakfastmeet");
            }
            //start updating dates for google sheets docs

            for (i = 0; i < response.Values.Count; i++)
            {
                if (response.Values[i][1].ToString() == lastpayer.slackname)
                {
                    response.Values[i][2] = DateTime.Now.ToString("dd/MM/yyyy");
                }
                if (lastpayer2.slackname != null || lastpayer2.slackname != "")
                {
                    if (response.Values[i][1].ToString() == lastpayer2.slackname)
                    {
                        response.Values[i][2] = DateTime.Now.ToString("dd/MM/yyyy");
                    }
                }
            }
            // update table with new last pay date of devs who just payed for breakfast

            string spreadsheetId2 = "1YMLuQ1tJnTJs1FQN0yruMHAS41nIRm1FHT87pP3GCV0";
            string range2 = "Sheet1!A2:D14";
            SpreadsheetsResource.ValuesResource.UpdateRequest request2 =
                service.Spreadsheets.Values.Update(response, spreadsheetId2, range2);
            // execute order 666

            request2.ValueInputOption = SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.RAW;
            request2.Execute();

            client.PostMessage(text: "better luck next time",
                channel: "#breakfastmeet");
            client.PostMessage(text: "Process: Stop",
                channel: "#breakfastmeet");

            return null;
        }

        //currently just post client 
        public class SlackSend
        {
            private readonly Uri _uri;
            private readonly Encoding _encoding = new UTF8Encoding();

            public SlackSend(string urlWithAccessToken)
            {
                _uri = new Uri(urlWithAccessToken);
            }

            //Post a message using simple strings
            public void PostMessage(string text, string channel)
            {
                Payload payload = new Payload()
                {
                    Channel = channel,
                    Text = text
                };
                PostMessage(payload);
            }

            //Post a message using a Payload object
            public void PostMessage(Payload payload)
            {
                string payloadJson = JsonConvert.SerializeObject(payload);

                using (WebClient client = new WebClient())
                {
                    NameValueCollection data = new NameValueCollection();
                    data["payload"] = payloadJson;
                    var response = client.UploadValues(_uri, "POST", data);
                    string responseText = _encoding.GetString(response);
                }
            }
        }

        //This class serializes into the Json payload required by Slack Incoming WebHooks
        public class Payload
        {
            [JsonProperty("channel")]
            public String Channel { get; set; }

            [JsonProperty("text")]
            public String Text { get; set; }
        }
        public class HookMessage
        {
            public string token { get; set; }
            public string team_id { get; set; }
            public string channel_id { get; set; }
            public string channel_name { get; set; }
            public string user_id { get; set; }
            public string user_name { get; set; }
            public string text { get; set; }
            public string trigger_word { get; set; }
        }
        public class date
        {
            public string day { get; set; }
            public string month { get; set; }
            public string year { get; set; }
        }
        public class developers
        {
            public string slackname { get; set; }
            public date lastpay { get; set; }
        }
    }
}