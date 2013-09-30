using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using System.Xml.XPath;

namespace JeremyClifton.Data.TestAutomator
{
    public class TestResult
    {
        private XElement _data;
        public XElement Data
        {
            get { return _data; }
        }

        private List<string> _messages = new List<string>();
        public List<string> Messages
        {
            get { return _messages; }
        }

        private string _output;
        public string Output
        {
            get { return _output; }
        }

        public string RawXml
        {
            get { return _data.ToString(); }
        }

        public TestResult()
        {
            throw new Exception("Not implemented!");
        }

        public TestResult(XElement xml)
        {
            _data = xml;
            InitMessages();
            InitOutput();
        }

        private void InitMessages()
        {
            foreach (XElement el in _data.XPathSelectElements("//messages/message"))
            {
                _messages.Add(el.Value);
            }
        }

        private void InitOutput()
        {
            XElement el = _data.XPathSelectElement("//output");
            if (el != null)
            {
                _output = el.Value;
            }
        }
    }
}
