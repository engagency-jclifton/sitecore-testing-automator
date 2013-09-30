using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using System.Xml.XPath;
using System.Web;
using System.Collections.Specialized;
using System.Configuration;
using System.Net;

namespace JeremyClifton.Data.TestAutomator
{
    public class TestConfiguration
    {
        private string _RawData;
        private XElement _XmlData;
        private List<string> _EventsToFire;
        private Dictionary<string, string> _Cookies;
        private NameValueCollection _Vars;
        private Dictionary<string, string> _ItemFields;

        private XElement _meta;

        private string _testGroup;
        private string _testName;

        public Dictionary<string, string> Cookies
        {
            get { return _Cookies; }
        }

        public NameValueCollection Vars
        {
            get { return _Vars; }
        }

        public List<string> EventsToFire
        {
            get {  return _EventsToFire; }
        }

        public Dictionary<string, string> ItemFields
        {
            get { return _ItemFields; }
        }

        private bool _isPostBack = false;
        public bool IsPostBack
        {
            get { return _isPostBack; }
        }

        public string Assembly
        {
            get
            {
                return _meta.Attribute("assembly").Value;
            }
        }

        public string Class
        {
            get
            {
                return _meta.Attribute("class").Value;
            }
        }

        public string Method
        {
            get
            {
                return _meta.Attribute("method").Value;
            }
        }

        public TestConfiguration()
        {
            throw new Exception("Not implemented!");
        }

        public TestConfiguration(string testName)
        {
            _Init(testName, Sitecore.Configuration.Settings.GetFileSetting("TestAutomator.TestConfigurationDirectory"));
        }

        public TestConfiguration(string testName, string hostName)
        {
            _Init(testName, string.Format("http://{0}/{1}", hostName, ConfigurationManager.AppSettings["configpath"]));
        }

        private void _Init(string testName, string configPath)
        {
            string[] parts = testName.Split('.');
            _testGroup = parts[0];
            _testName = parts[1];
            
            if (configPath.StartsWith("http://"))
            {
                configPath = string.Format(@"{0}/{1}.xml", configPath, _testGroup);
                //Console.WriteLine("Config Path: " + configPath);
                using (WebClient client = new WebClient())
                {
                    _RawData = client.DownloadString(configPath);
                }
            }
            else
            {
                configPath = string.Format(@"{0}\{1}.xml", configPath, _testGroup);
                //Console.WriteLine("Config Path: " + configPath);
                _RawData = System.IO.File.ReadAllText(configPath);
            }

            if (_RawData == string.Empty)
            {
                throw new Exception("Could not read test configuration!");
            }

            _XmlData = XElement.Parse(_RawData);
            if (_XmlData == null)
            {
                throw new Exception("Could not read test configuration!");
            }

            _InitCookies();
            _InitVars();
            _InitEventsToFire();
            _InitItem();
            _InitMeta();

            XElement test = _XmlData.XPathSelectElement(string.Format("/test[@id='{0}']", _testName));
            _isPostBack = (test != null && test.Attribute("ispostback") != null && test.Attribute("ispostback").Value.ToLower() == "true");
        }

        private void _InitCookies()
        {
            _Cookies = new Dictionary<string, string>();
            foreach (XElement xe in _XmlData.XPathSelectElements(string.Format("/test[@id='{0}']/cookies/cookie", _testName)))
            {
                if (xe.Attribute("name") != null && xe.Attribute("value") != null)
                {
                    _Cookies.Add(xe.Attribute("name").Value, xe.Attribute("value").Value);
                }
            }
        }

        private void _InitVars()
        {
            _Vars = new NameValueCollection();
            foreach (XElement xe in _XmlData.XPathSelectElements(string.Format("/test[@id='{0}']/vars/var", _testName)))
            {
                if (xe.Attribute("name") != null && xe.Attribute("value") != null)
                {
                    _Vars.Add(xe.Attribute("name").Value, xe.Attribute("value").Value);
                }
            }
        }

        private void _InitEventsToFire()
        {
            _EventsToFire = new List<string>();
            foreach (XElement xe in _XmlData.XPathSelectElements(string.Format("/test[@id='{0}']/fire-events", _testName)))
            {
                _EventsToFire.Add(xe.Value);
            }
        }

        private void _InitItem()
        {
            _ItemFields = new Dictionary<string, string>();
            foreach (XElement xe in _XmlData.XPathSelectElements(string.Format("/test[@id='{0}']/item/field", _testName)))
            {
                _ItemFields.Add(xe.Attribute("name").Value, xe.Value);
            }
        }

        private void _InitMeta()
        {
            _meta = _XmlData.XPathSelectElement(string.Format("/test[@id='{0}']/meta", _testName));
        }
    }
}
