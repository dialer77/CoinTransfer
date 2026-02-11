using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;

namespace CoinTransfer
{
    /// <summary>
    /// 출금 주소를 Key-Value 형태로 파일에 저장/불러오기
    /// Key: 저장 이름(라벨), Value: 주소 정보 (Address, Tag)
    /// </summary>
    public static class AddressStorage
    {
        private const string FILE_PATH = "addresses.json";

        /// <summary>
        /// 주소 값 (Value 부분)
        /// </summary>
        public class AddressValue
        {
            public string Address { get; set; } = "";
            public string Tag { get; set; } = "";
        }

        /// <summary>
        /// ComboBox 등에서 사용하는 Key + Value 묶음
        /// </summary>
        public class SavedAddress
        {
            public string Key { get; set; } = "";
            public string Address { get; set; } = "";
            public string Tag { get; set; } = "";

            public string DisplayText => string.IsNullOrEmpty(Key) ? Address : $"{Key} - {Address}";

            public override string ToString() => DisplayText;
        }

        /// <summary>
        /// Key-Value 형태로 저장된 데이터 로드 (기존 리스트 형식도 호환)
        /// </summary>
        public static List<SavedAddress> Load()
        {
            try
            {
                if (!File.Exists(FILE_PATH)) return new List<SavedAddress>();
                var json = File.ReadAllText(FILE_PATH);
                var token = JToken.Parse(json);

                if (token is JObject obj)
                {
                    // Key-Value 형식: { "키1": { "Address": "...", "Tag": "..." }, ... }
                    var list = new List<SavedAddress>();
                    foreach (var prop in obj.Properties())
                    {
                        var val = prop.Value as JObject;
                        if (val == null) continue;
                        list.Add(new SavedAddress
                        {
                            Key = prop.Name,
                            Address = val["Address"]?.ToString() ?? "",
                            Tag = val["Tag"]?.ToString() ?? ""
                        });
                    }
                    return list;
                }

                if (token is JArray arr)
                {
                    // 기존 리스트 형식 호환: [{ "Address": "...", "Tag": "...", "Memo": "..." }, ...]
                    var list = new List<SavedAddress>();
                    int idx = 0;
                    foreach (var item in arr)
                    {
                        var j = item as JObject;
                        if (j == null) continue;
                        var memo = j["Memo"]?.ToString()?.Trim();
                        var key = !string.IsNullOrEmpty(memo) ? memo : ($"주소_{idx + 1}");
                        list.Add(new SavedAddress
                        {
                            Key = key,
                            Address = j["Address"]?.ToString() ?? "",
                            Tag = j["Tag"]?.ToString() ?? ""
                        });
                        idx++;
                    }
                    return list;
                }

                return new List<SavedAddress>();
            }
            catch
            {
                return new List<SavedAddress>();
            }
        }

        /// <summary>
        /// Key-Value Dictionary 형태로 저장
        /// </summary>
        public static void Save(List<SavedAddress> addresses)
        {
            try
            {
                var dict = new Dictionary<string, AddressValue>(StringComparer.OrdinalIgnoreCase);
                foreach (var a in addresses)
                {
                    if (string.IsNullOrWhiteSpace(a.Key)) continue;
                    dict[a.Key.Trim()] = new AddressValue { Address = a.Address ?? "", Tag = a.Tag ?? "" };
                }
                var json = JsonConvert.SerializeObject(dict, Formatting.Indented);
                File.WriteAllText(FILE_PATH, json);
            }
            catch (Exception ex)
            {
                throw new Exception($"주소 저장 실패: {ex.Message}");
            }
        }

        public static void Add(string key, string address, string tag)
        {
            var keyTrim = key?.Trim() ?? "";
            if (string.IsNullOrEmpty(keyTrim))
                throw new ArgumentException("저장 이름(Key)을 입력하세요.");
            var list = Load();
            var existing = list.FindIndex(a => string.Equals(a.Key, keyTrim, StringComparison.OrdinalIgnoreCase));
            if (existing >= 0)
                list[existing] = new SavedAddress { Key = keyTrim, Address = address ?? "", Tag = tag ?? "" };
            else
                list.Add(new SavedAddress { Key = keyTrim, Address = address ?? "", Tag = tag ?? "" });
            Save(list);
        }

        public static void Remove(string key)
        {
            var list = Load();
            list.RemoveAll(a => string.Equals(a.Key, key?.Trim(), StringComparison.OrdinalIgnoreCase));
            Save(list);
        }
    }
}
