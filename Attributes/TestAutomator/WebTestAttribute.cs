using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace JeremyClifton.Attributes.TestAutomator
{
    [AttributeUsage(AttributeTargets.All)]
    public class WebTestAttribute : Attribute
    {
        public readonly string Name;
        public string ConfigPath
        {
            get;
            set;
        }
        public string Event
        {
            get;
            set;
        }
        public WebTestAttribute(string name)
        {
            this.Name = name;
        }
    }
}