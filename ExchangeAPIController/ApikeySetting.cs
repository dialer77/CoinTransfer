using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExchangeAPIController
{
    public class ApikeySetting
    {
        private static ApikeySetting s_instance = null;

        public static ApikeySetting GetInstance()
        {
            if(s_instance == null)
            {
                s_instance = new ApikeySetting();
            }

            return s_instance;
        }

        public ApikeySetting()
        {
            foreach(EnumExchange exchange in Enum.GetValues(typeof(EnumExchange)))
            {
                m_dicExchangeAccessKey.Add(exchange, "");
                m_dicExchangeSecretKey.Add(exchange, "");
                m_dicExchangePassphrase.Add(exchange, "");
                m_dicDepositAddress.Add(exchange, new Dictionary<string, (string, string)>());
            }
        }


        private Dictionary<EnumExchange, string> m_dicExchangeAccessKey = new Dictionary<EnumExchange, string>();
        private Dictionary<EnumExchange, string> m_dicExchangeSecretKey = new Dictionary<EnumExchange, string>();
        private Dictionary<EnumExchange, string> m_dicExchangePassphrase = new Dictionary<EnumExchange, string>();

        private Dictionary<EnumExchange, Dictionary<string, (string, string)>> m_dicDepositAddress = new Dictionary<EnumExchange, Dictionary<string, (string, string)>>();

        public string GetExchangeAccessKey(EnumExchange exchange)
        {

            return m_dicExchangeAccessKey[exchange];
        }

        public void SetExchangeAccessKey(EnumExchange exchange, string accessKey)
        {

            if (m_dicExchangeAccessKey.ContainsKey(exchange) == false)
            {
                m_dicExchangeAccessKey.Add(exchange, accessKey);
            }
            else
            {
                m_dicExchangeAccessKey[exchange] = accessKey;
            }
        }

        public string GetExchangeSecretKey(EnumExchange exchange)
        {
            return m_dicExchangeSecretKey[exchange];
        }

        public void SetExchangeSecretKey(EnumExchange exchange, string secretKey)
        {

            if (m_dicExchangeSecretKey.ContainsKey(exchange) == false)
            {
                m_dicExchangeSecretKey.Add(exchange, secretKey);
            }
            else
            {
                m_dicExchangeSecretKey[exchange] = secretKey;
            }
        }

        /// <summary>OKX 등 Passphrase가 필요한 거래소용. config.ini [거래소] Passphrase= 값.</summary>
        public string GetExchangePassphrase(EnumExchange exchange)
        {
            return m_dicExchangePassphrase.TryGetValue(exchange, out string v) ? v : "";
        }

        public void SetExchangePassphrase(EnumExchange exchange, string passphrase)
        {
            if (m_dicExchangePassphrase.ContainsKey(exchange) == false)
                m_dicExchangePassphrase.Add(exchange, passphrase ?? "");
            else
                m_dicExchangePassphrase[exchange] = passphrase ?? "";
        }

        public List<string> GetDepositCoinList(EnumExchange exchange)
        {


            return m_dicDepositAddress[exchange].Keys.ToList();
        }

        public (string, string) GetDepositAddress(EnumExchange exchange, string coinName)
        {
            if (m_dicDepositAddress[exchange].ContainsKey(coinName) == false)
            {
                return ("", "");
            }

            return m_dicDepositAddress[exchange][coinName];
        }

        public void SetDepositAddress(EnumExchange exchange, string coinName, string address, string destinationTag)
        {

            if (address == "" && destinationTag == "")
            {
                return;
            }
            m_dicDepositAddress[exchange][coinName] = (address, destinationTag);
        }

        public void SaveSetting()
        {
            IniFile ini = new IniFile();

            foreach(EnumExchange exchange in Enum.GetValues(typeof(EnumExchange)))
            {
                ini[exchange.ToString()].Add("AccessKey", m_dicExchangeAccessKey[exchange]);
                ini[exchange.ToString()].Add("SecretKey", m_dicExchangeSecretKey[exchange]);
                ini[exchange.ToString()].Add("Passphrase", m_dicExchangePassphrase[exchange]);

                List<string> depositCoinList = m_dicDepositAddress[exchange].Keys.ToList();
                ini[exchange.ToString()].Add("DepositCoinList", string.Join(",", depositCoinList));

                foreach (string coinName in depositCoinList)
                {
                    string coinDepositAddressKey = $"{coinName}_DepositAddress";
                    string coinDestinationTagKey = $"{coinName}_DestinationTag";
                    ini[exchange.ToString()].Add(coinDepositAddressKey, m_dicDepositAddress[exchange][coinName].Item1);
                    ini[exchange.ToString()].Add(coinDestinationTagKey, m_dicDepositAddress[exchange][coinName].Item2);
                }

                ini.Save("config.ini", System.IO.FileMode.Create);
            }
        }

        public void LoadSetting()
        {
            IniFile ini = new IniFile();
            if(System.IO.File.Exists("config.ini") == false)
            {
                return;
            }

            ini.Load("config.ini");

            foreach (string section in ini.Keys)
            {
                if(Enum.TryParse(section, out EnumExchange exchange) == false)
                {
                    continue;
                }



                SetExchangeAccessKey(exchange, ini[exchange.ToString()]["AccessKey"].ToString());
                SetExchangeSecretKey(exchange, ini[exchange.ToString()]["SecretKey"].ToString());
                if (ini[exchange.ToString()].ContainsKey("Passphrase"))
                    SetExchangePassphrase(exchange, ini[exchange.ToString()]["Passphrase"].ToString());
                List<string> coinList = ini[exchange.ToString()]["DepositCoinList"].ToString().Split(',').ToList();
                foreach(string coinName in coinList)
                {
                    if (coinName == null || coinName == "")
                    {
                        continue;
                    }

                    string coinDepositAddressKey = $"{coinName}_DepositAddress";
                    string coinDestinationTagKey = $"{coinName}_DestinationTag";

                    SetDepositAddress(exchange, coinName, ini[exchange.ToString()][coinDepositAddressKey].ToString(), ini[exchange.ToString()][coinDestinationTagKey].ToString());
                }
            }

        }
    }
}
