using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace CommonUtil.Settings
{
    public class SystemSettings
    {
        private static SystemSettings s_instance = new SystemSettings();
        public static SystemSettings GetInstance()
        {
            return s_instance;
        }

        public ExchangeSettings Debug { get; set; } = new ExchangeSettings();
        public ExchangeSettings Upbit { get; set; } = new ExchangeSettings();
        public ExchangeSettings Binance { get; set; } = new ExchangeSettings();
        public ExchangeSettings BinanceFutures { get; set; } = new ExchangeSettings();

        public void SaveSetting()
        {

            string dataFolder = System.IO.Path.Combine(System.IO.Directory.GetParent(System.IO.Directory.GetCurrentDirectory()).FullName, "Data");
            if(System.IO.Directory.Exists(dataFolder) == false)
            {
                System.IO.Directory.CreateDirectory(dataFolder);
            }
            string fileName = System.IO.Path.Combine(dataFolder, "SystemSettings.xml");

            using(StreamWriter wr = new StreamWriter(fileName))
            {
                XmlSerializer xs = new XmlSerializer(typeof(SystemSettings));
                xs.Serialize(wr, s_instance);
            }

        }

        public void LoadSetting()
        {
            string dataFolder = System.IO.Path.Combine(System.IO.Directory.GetParent(System.IO.Directory.GetCurrentDirectory()).FullName, "Data");
            string fileName = System.IO.Path.Combine(dataFolder, "SystemSettings.xml");
            if(System.IO.Directory.Exists(dataFolder) == false ||
                System.IO.File.Exists(fileName) == false)
            {
                return;
            }

            using (StreamReader rd = new StreamReader(fileName))
            {
                XmlSerializer xs = new XmlSerializer(typeof(SystemSettings));
                s_instance = (SystemSettings)xs.Deserialize(rd);
            }
        }




    }
}
