using Nancy;
using Nancy.Hosting.Self;
using Nancy.ModelBinding;
using ServiceStack.Text;
using Slack.Webhooks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
            using (var host = new NancyHost(new Uri("http://localhost:1234"), new DefaultNancyBootstrapper(), hostConfigs))
            {
                host.Start();
                Console.ReadLine();
            }
        }
    }
    //-------
    public class WebhookModule : Nancy.NancyModule
    {
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
                    Console.WriteLine("'" + message + "' sent back to @" + model.user_name);
                }
                if(model.text.ToLower().StartsWith("testbot: no"))
                {
                    message = string.Format("@" + model.user_name + " Recieved! removed from this weeks breky list");
                    Console.WriteLine("'" + message + "' sent back to @" + model.user_name);
                }
                if (!string.IsNullOrWhiteSpace(message))
                    return new SlackMessage { Text = message, Username = "breky bot"};
                return null;
            };
        }
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
}
