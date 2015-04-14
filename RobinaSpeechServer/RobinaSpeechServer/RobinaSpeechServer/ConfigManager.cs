using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace RobinaSpeechServer
{   
    public enum Scope { Grammar, Network, Recognition, TTS };
    class ConfigManager
    {

        Dictionary<Scope, Dictionary<string, string>> options;
        public ConfigManager(string filename)
        {
            options = new Dictionary<Scope, Dictionary<string, string>>();
            loadfile(filename);
            Console.WriteLine("Configurations has been parsed");
            //TEST();

        }
        public string this[Scope scope, string option]
        {
            get { return options[scope][option]; }
            private set { options[scope][option] = value; }
        }
        private void TEST()
        {
            foreach (KeyValuePair<Scope, Dictionary<string, string>> d in options)
            {
                Console.WriteLine(d.Key + ":");
                foreach (KeyValuePair<string, string> dd in d.Value)
                {
                    Console.WriteLine("\t" + dd.Key + ": " + dd.Value);
                }
            }
        }
        private void loadfile(string fn)
        {
            try
            {

                XDocument doc = XDocument.Load(fn);
                XElement options_el = doc.Root;
                foreach (XElement el in options_el.Elements())
                {
                    Scope scope = (Scope)Enum.Parse(typeof(Scope), el.Name.ToString());
                    options.Add(scope, new Dictionary<string, string>());
                    foreach (XElement opt in el.Elements())
                    {
                        foreach (XAttribute att in opt.Attributes())
                        {
                            if (att.Name == "value")
                                options[scope].Add(opt.Name.ToString(), att.Value);
                            else
                                Console.WriteLine("undefined option in " + att.Name + "/" + opt.Name + "/" + el.Name);
                        }

                    }
                    foreach (XAttribute opt in el.Attributes())
                    {
                        options[scope].Add(opt.Name.ToString(), opt.Value);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Console.WriteLine(ex.Message);
                Console.WriteLine("Error in parsing Configuration");
                Console.ReadLine();
                Environment.Exit(1);
            }
        }
    }
}
