using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using System.Reflection;
using JeremyClifton.Data.TestAutomator;
using System.Net;
using System.Xml.Linq;
using System.Xml.XPath;
using System.Collections.Specialized;

namespace JeremyClifton.Util.TestAutomator
{
    public class TestRunner
    {
        private List<string> _messages = new List<string>();
        public List<string> Messages
        {
            get { return _messages; }
            set { _messages = value; }
        }

        private string _output = string.Empty;
        public string Output
        {
            get { return _output; }
            set { _output = value; }
        }

        public void Log(string message)
        {
            Messages.Add(message);
        }

        public void RunTest(TestConfiguration testConfig)
        {
            string assemblyPath = string.Format(@"{0}\{1}.dll", HttpContext.Current.Server.MapPath("~/bin"), testConfig.Assembly);
            Assembly dll = Assembly.LoadFrom(assemblyPath);
            Type type = dll.GetType(testConfig.Class);
            object o = Activator.CreateInstance(type);

            SetupTest(testConfig, type, o);
            if (typeof(System.Web.UI.Control).IsAssignableFrom(type))
            {
                SetupTestControl(testConfig, type, o as System.Web.UI.Control);
                RunTestEvents(testConfig, type, o);
            }
            else if (typeof(System.Web.UI.Page).IsAssignableFrom(type))
            {
                SetupTestPage(testConfig, type, o as System.Web.UI.Page);
                RunTestEvents(testConfig, type, o);
            }

            RunTestMethod(testConfig, type, o);
        }

        private static void SetupTest(TestConfiguration testConfig, Type type, object o)
        {
            FieldInfo _request = type.GetField("_request", BindingFlags.GetField | BindingFlags.NonPublic | BindingFlags.Instance);
            if (_request != null)
            {
                _request.SetValue(o, HttpContext.Current.Request);
            }
        }

        private void SetupTestControl(TestConfiguration testConfig, Type type, System.Web.UI.Control o)
        {

        }

        private void SetupTestPage(TestConfiguration testConfig, Type type, System.Web.UI.Page o)
        {
            
        }

        private void RunTestEvents(TestConfiguration testConfig, Type type, object o)
        {
            foreach (string evt in testConfig.EventsToFire)
            {
                List<object> eventArgs = new List<object>();
                if (evt.StartsWith("Page_"))
                {
                    eventArgs.Add(new object());
                    eventArgs.Add(EventArgs.Empty);
                }
                else
                {
                    eventArgs.Add(EventArgs.Empty);
                }
                MethodInfo eventMethod = type.GetMethod(evt, BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.InvokeMethod);
                if (eventMethod == null)
                {
                    Output = string.Format("Event handler {0} in class {1} (assembly {2}) does not exist!", evt, testConfig.Class, testConfig.Assembly);
                    return;
                }
                eventMethod.Invoke(o, eventArgs.ToArray<object>());
            }
        }

        private void RunTestMethod(TestConfiguration testConfig, Type type, object o)
        {
            MethodInfo method = type.GetMethod(testConfig.Method, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.InvokeMethod);
            if (method == null)
            {
                Output = string.Format("Method {0} in class {1} (assembly {2}) does not exist!", testConfig.Method, testConfig.Class, testConfig.Assembly);
                return;
            }
            object[] parameters = new object[] { this };
            method.Invoke(o, parameters);
        }

        public TestResult RunTestRemotely(string hostName, string testName)
        {
            return new TestResult(RunTestRemotelyAsXElement(hostName, testName));
        }

        public XElement RunTestRemotelyAsXElement(string hostName, string testName)
        {
            return XElement.Parse(RunTestRemotelyAsString(hostName, testName));
        }

        public string RunTestRemotelyAsString(string hostName, string testName)
        {
            TestConfiguration testConfiguration = new TestConfiguration(testName, hostName);
            string url = string.Format(@"http://{0}/?sc_test={1}", hostName, testName);
            string results = string.Empty;
            using (WebClient client = new WebClient())
            {
                if (testConfiguration.IsPostBack)
                {
                    results = RunTestRemotelyAsPost(client, testConfiguration, url + "&method=post");
                }
                else
                {
                    results = RunTestRemotelyAsGet(client, testConfiguration, url + "&method=get");
                }
            }
            return results;
        }

        private static string RunTestRemotelyAsGet(WebClient client, TestConfiguration testConfiguration, string url)
        {
            if (testConfiguration.Vars.Count > 0)
            {
                url += "&" + GetQueryString(testConfiguration.Vars);
            }
            return client.DownloadString(new Uri(url));
        }

        private static string RunTestRemotelyAsPost(WebClient client, TestConfiguration testConfiguration, string url)
        {
            client.Headers[HttpRequestHeader.ContentType] = "application/x-www-form-urlencoded";
            return client.UploadString(new Uri(url), GetQueryString(testConfiguration.Vars));
        }

        private static string GetQueryString(NameValueCollection nvc)
        {
            List<string> pairs = new List<string>();
            foreach (string k in nvc.AllKeys)
            {
                pairs.Add(string.Format("{0}={1}", HttpUtility.UrlEncode(k), HttpUtility.UrlEncode(nvc[k])));
            }
            return string.Join("&", pairs);
        }
    }
}
