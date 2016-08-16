using Nancy;
using Nancy.Hosting.Self;
using Nancy.ModelBinding;
using Newtonsoft.Json;
using ServiceStack.Text;
using Slack.Webhooks;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Net;
using System.Text;

namespace TestListener
{
    //----
    class Program
    {
        static void Main(string[] args)
        {
            JsConfig.EmitLowercaseUnderscoreNames = true;
            JsConfig.IncludeNullValues = false;
            JsConfig.PropertyConvention = JsonPropertyConvention.Lenient;
            HostConfiguration hostConfigs = new HostConfiguration()
            {
                UrlReservations = new UrlReservations() { CreateAutomatically = true }
            };
            sendClient slackout = new sendClient();
            slackout.process(1);
            int i = 0;
            var test = new WebhookModule();
            var buuuuList = new List<developers>();
            var breakfastList = new List<developers>();
            breakfastList.Add(new developers {
                slackname = "slash",
                date = "incomplete"
            });
            using (var host = new NancyHost(new Uri("http://localhost:1234"), new DefaultNancyBootstrapper(), hostConfigs))
            {
                host.Start();
                slackout.send(ref breakfastList);
                System.Threading.Thread.Sleep(20000);
                //System.Threading.Thread.Sleep(3600000);//1 hour
                test.update(ref buuuuList,ref breakfastList);

            }
            //exit message
            slackout.process(0);
            //end console result
            foreach(var dev in buuuuList)
            {
                Console.WriteLine(buuuuList[i]);
                i++;
            }
            i = 0;
            foreach(var dev in breakfastList)
            {
                Console.WriteLine(breakfastList[i]);
                i++;
            }
            Console.ReadLine();
        }
    }
    //-------
    //------ sending mesasages
    public class sendClient
    {
        string urlWithAccessToken;
        SlackSend client;
        int i;

        public sendClient()
        {
            urlWithAccessToken = "https://hooks.slack.com/services/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";
            client = new SlackSend(urlWithAccessToken);
        }
        public string step(int step)
        {
            String message;
            if (step == 1)
                message = "start";
             else
                message = "stop";
            return "Process " + message + "!";
        }
        public int send (ref List<developers> breakfastList)
        {
            i = 0;
            foreach (var dev in breakfastList)
            {
                client.PostMessage(text: "@" + breakfastList[i].slackname + " can you make it for breakfast",
                       channel: "#general");
            }

            return 0;
        }
        public void process(int part)
        {
            String a = step(part);
            client.PostMessage(text: a,
                channel: "#general");
        }
    }
    //---receiving messages module
    public class WebhookModule : Nancy.NancyModule
    {
        public static List<developers> nolist = new List<developers>();
        public static List<developers> yeslist = new List<developers>();
        public WebhookModule()
        {
            Post["/"] = _ =>
            {
                var model = this.Bind<HookMessage>();
                var message = string.Empty;
                Console.WriteLine(model.text.ToLower());
                if (model.token != "5jZxYUF2PKJgh9zDnaomRJ0V") { 

                    Console.WriteLine("Invalid Token\n Ignored!");
                    return null;
                }
                if (model.text.ToLower().StartsWith("testbot: yes"))
                {
                    message = string.Format("@" + model.user_name + " Recieved!");
                    yeslist.Add(new developers
                    {
                        slackname = model.user_name,
                        date = "inclomplete"
                    });
                    Console.WriteLine("'" + message + "' sent back to @" + model.user_name);
                }
                if(model.text.ToLower().StartsWith("testbot: no"))
                {
                    message = string.Format("@" + model.user_name + " Recieved! removed from this weeks breky list");
                    String name = model.user_name;
                    nolist.Add(new developers
                    {
                        slackname = model.user_name,
                        date = "incomplete"
                        
                    });
                    Console.WriteLine("'" + message + "' sent back to @" + model.user_name);
                }
                if (!string.IsNullOrWhiteSpace(message))
                    return new SlackMessage { Text = message, Username = "breky bot"};
                return null;
            };
        }
        public void update(ref List<developers> buuuuList, ref List<developers> breakfastList)
        {
            buuuuList = nolist;
            breakfastList = yeslist;
        }
    }
    //------
    public class developers
    {
        public string slackname { get; set; }
        public string date { get; set; }
    }
    //------
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
    //-------
    public class TitleCaseFieldNameConverter : IFieldNameConverter
    {
        public string Convert(string fieldName)
        {
            return fieldName.ToTitleCase();
        }
    }
    //------
    public class Bootstrapper : Nancy.DefaultNancyBootstrapper
    {
        protected override void ApplicationStartup(Nancy.TinyIoc.TinyIoCContainer container, Nancy.Bootstrapper.IPipelines pipelines)
        {
            container.Register<IFieldNameConverter, TitleCaseFieldNameConverter>();
            base.ApplicationStartup(container, pipelines);
        }
    }
    //-----sending stuff
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

                //The response text is usually "ok"
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
}
