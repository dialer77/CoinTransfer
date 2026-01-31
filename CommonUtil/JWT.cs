using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace CommonUtil
{
    public static class JWT
    {

        /// <summary>
        /// SHA256 암호화
        /// </summary>
        /// <param name="text">암호화 할 평문</param>
        /// <param name="encoding">System.Text.Encoding</param>
        /// <returns>지정된 인코딩으로 암호화한 문자열</returns>
        //public static string EncryptSHA256(string text, Encoding encoding)
        //{
        //    var sha = new System.Security.Cryptography.SHA256Managed();
        //    byte[] data = sha.ComputeHash(encoding.GetBytes(text));

        //    var sb = new StringBuilder();
        //    foreach (byte b in data)
        //    {
        //        sb.Append(b.ToString("x2"));
        //    }
        //    return sb.ToString();
        //}

        public static StringBuilder GetQueryString(Dictionary<string, string> parameters )
        {
            // Dictionary 형태로 받은 key = value 형태를 
            // ?key1=value1&key2=value2 ... 형태로 만들어줌
            StringBuilder builder = new StringBuilder();
            foreach (KeyValuePair<string, string> pair in parameters)
            {
                builder.Append(pair.Key).Append("=").Append(pair.Value).Append("&");
            }

            if (builder.Length > 0)
            {
                builder.Length = builder.Length - 1; // 마지막 &를 제거하기 위함.
            }
            return builder;
        }

        public static string CreateAuthorizationToken(string accessKey, string secretKey, Dictionary<string, string> parameters)
        {
            string queryString = GetQueryString(parameters).ToString();

            SHA512 sha512 = SHA512.Create();
            byte[] queryHashByteArray = sha512.ComputeHash(Encoding.UTF8.GetBytes(queryString));
            string queryHash = BitConverter.ToString(queryHashByteArray).Replace("-", "").ToLower();


            var payload = new JwtPayload
                    {
                        { "access_key", accessKey},
                        { "nonce", Guid.NewGuid().ToString()  },
                        { "query_hash", queryHash },
                        { "query_hash_alg", "SHA512" }
                    };

            byte[] keyBytes = Encoding.Default.GetBytes(secretKey);
            var securityKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(keyBytes);
            var credentials = new Microsoft.IdentityModel.Tokens.SigningCredentials(securityKey, "HS256");
            var header = new JwtHeader(credentials);
            var secToken = new JwtSecurityToken(header, payload);

            var jwtToken = new JwtSecurityTokenHandler().WriteToken(secToken);

            StringBuilder returnStr = new StringBuilder();
            returnStr.Append("Bearer "); // 띄어쓰기 한칸 있어야함 주의!
            returnStr.Append(jwtToken);

            return returnStr.ToString();
        }

        public static string CreateAuthorizationToken(string accessKey, string secretKey)
        {
            
            var payload = new JwtPayload {
                { "access_key", accessKey },
                { "nonce",  Guid.NewGuid().ToString() }
            };

            byte[] keyBytes = Encoding.Default.GetBytes(secretKey);
            var securityKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(keyBytes);
            var credentials = new Microsoft.IdentityModel.Tokens.SigningCredentials(securityKey, "HS256"); // HMAC SHA256 방식의 줄임말
            var header = new JwtHeader(credentials);
            var secToken = new JwtSecurityToken(header, payload);

            var jwtToken = new JwtSecurityTokenHandler().WriteToken(secToken);

            StringBuilder returnStr = new StringBuilder();
            returnStr.Append("Bearer "); // 띄어쓰기 한칸 있어야함 주의!
            returnStr.Append(jwtToken);

            return returnStr.ToString();
        }
    }
}
