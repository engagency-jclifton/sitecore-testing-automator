using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sitecore.Pipelines.HttpRequest;
using System.Web;
using JeremyClifton.Data.TestAutomator;
using Sitecore.SecurityModel;
using JeremyClifton.Util.TestAutomator;
using System.Runtime.Remoting;
using System.Reflection;
using System.Xml.Linq;
using Sitecore.Pipelines.RenderLayout;

namespace JeremyClifton.Pipelines.RenderLayout
{
    public class RunAutomatedTests : RenderLayoutProcessor
    {
        private TestConfiguration _TestConfig;

        public override void Process(RenderLayoutArgs args)
        {
            if (HttpContext.Current.Request["sc_test"] == null)
            {
                return;
            }
            Sitecore.Context.Items["sc_test"] = HttpContext.Current.Request["sc_test"];

            ReadConfiguration();

            // For the purposes of testing, our database should *always* be master.
            Sitecore.Context.Database = Sitecore.Data.Database.GetDatabase("master");

            // Now re-fetch the context item from the master database.
            Sitecore.Context.Item = Sitecore.Context.Database.GetItem(Sitecore.Context.Item.ID.ToString(), Sitecore.Context.Language);

            InitCookies();
            InitContextItem();
            TestRunner runner = new TestRunner();
            runner.RunTest(_TestConfig);
            RevertContextItem();
            WriteOutput(runner);
        }

        protected void InitContextItem()
        {
            using (new SecurityDisabler())
            {
                Sitecore.Context.Item = Sitecore.Context.Item.Versions.AddVersion();
	            if (this._TestConfig.ItemFields.Count > 0)
	            {
                    Sitecore.Context.Item.Editing.BeginEdit();
		            foreach (string i in this._TestConfig.ItemFields.Keys)
		            {
                        Sitecore.Context.Item[i] = this._TestConfig.ItemFields[i];
		            }
                    Sitecore.Context.Item.Editing.EndEdit();
	            }
            }
        }

        protected void InitCookies()
        {
            foreach (string k in _TestConfig.Cookies.Keys)
            {
                HttpContext.Current.Request.Cookies.Add(new HttpCookie(k, _TestConfig.Cookies[k]));
            }
        }

        protected void RevertContextItem()
        {
            using (new SecurityDisabler())
            {
                Sitecore.Context.Item.Versions.RemoveVersion();
            }
        }

        protected void ReadConfiguration()
        {
            _TestConfig = new TestConfiguration(Sitecore.Context.Items["sc_test"].ToString());
        }

        protected void WriteOutput(TestRunner runner)
        {
            XElement root = new XElement("testOutput");
            root.SetAttributeValue("testName", HttpContext.Current.Request["sc_test"]);

            XElement messages = new XElement("messages");
            foreach (string m in runner.Messages)
            {
                XElement message = new XElement("message", m);
                messages.Add(message);
            }
            root.Add(messages);

            XElement output = new XElement("output", runner.Output);
            root.Add(output);

            HttpContext.Current.Response.ContentType = "text/xml";
            HttpContext.Current.Response.Write(root.ToString());
            HttpContext.Current.Response.End();
        }
    }
}
