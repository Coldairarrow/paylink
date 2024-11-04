﻿using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using Essensoft.Paylink.Security;
using Essensoft.Paylink.WeChatPay.V3.Extensions;
using Essensoft.Paylink.WeChatPay.V3.Parser;

namespace Essensoft.Paylink.WeChatPay.V3
{
    public class WeChatPayClient : IWeChatPayClient
    {
        public const string Name = "WeChatPay.V3.Client";

        private readonly IHttpClientFactory _httpClientFactory;
        private readonly WeChatPayPlatformCertificateManager _platformCertificateManager;

        #region WeChatPayClient Constructors

        public WeChatPayClient(IHttpClientFactory httpClientFactory, WeChatPayPlatformCertificateManager platformCertificateManager)
        {
            _httpClientFactory = httpClientFactory;
            _platformCertificateManager = platformCertificateManager;
        }

        #endregion

        #region IWeChatPayClient Members

        public Task<WeChatPayDictionary> ExecuteAsync(IWeChatPaySdkRequest request, WeChatPayOptions options)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            if (string.IsNullOrEmpty(options.AppId))
            {
                throw new WeChatPayException($"options.{nameof(WeChatPayOptions.AppId)} is Empty!");
            }

            if (string.IsNullOrEmpty(options.MchId))
            {
                throw new WeChatPayException($"options.{nameof(WeChatPayOptions.MchId)} is Empty!");
            }

            if (string.IsNullOrEmpty(options.APIv3Key))
            {
                throw new WeChatPayException($"options.{nameof(WeChatPayOptions.APIv3Key)} is Empty!");
            }

            var sortedTxtParams = new WeChatPayDictionary(request.GetParameters());

            request.PrimaryHandler(sortedTxtParams, options);

            return Task.FromResult(sortedTxtParams);
        }

        #endregion

        #region IWeChatPayClient Members

        public async Task<T> ExecuteAsync<T>(IWeChatPayGetRequest<T> request, WeChatPayOptions options) where T : WeChatPayResponse
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            if (string.IsNullOrEmpty(options.MchId))
            {
                throw new WeChatPayException($"options.{nameof(WeChatPayOptions.MchId)} is Empty!");
            }

            if (string.IsNullOrEmpty(options.Certificate))
            {
                throw new WeChatPayException($"options.{nameof(WeChatPayOptions.Certificate)} is Empty!");
            }

            if (string.IsNullOrEmpty(options.APIv3Key))
            {
                throw new WeChatPayException($"options.{nameof(WeChatPayOptions.APIv3Key)} is Empty!");
            }

            var client = _httpClientFactory.CreateClient(Name);
            var (headers, body, statusCode) = await client.GetAsync(request, options);
            var parser = new WeChatPayResponseJsonParser<T>();
            var response = parser.Parse(body, statusCode);

            if (request.GetNeedCheckSign())
            {
                if (!response.IsError)
                {
                    await CheckResponseSignAsync(headers, body, options);
                }
            }

            return response;
        }

        #endregion

        #region IWeChatPayClient Members

        public async Task<T> ExecuteAsync<T>(IWeChatPayPostRequest<T> request, WeChatPayOptions options) where T : WeChatPayResponse
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            if (string.IsNullOrEmpty(options.MchId))
            {
                throw new WeChatPayException($"options.{nameof(WeChatPayOptions.MchId)} is Empty!");
            }

            if (string.IsNullOrEmpty(options.Certificate))
            {
                throw new WeChatPayException($"options.{nameof(WeChatPayOptions.Certificate)} is Empty!");
            }

            var client = _httpClientFactory.CreateClient(Name);
            var (headers, body, statusCode) = await client.PostAsync(request, options);
            var parser = new WeChatPayResponseJsonParser<T>();
            var response = parser.Parse(body, statusCode);

            if (!response.IsError)
            {
                await CheckResponseSignAsync(headers, body, options);
            }

            return response;
        }

        #endregion

        #region IWeChatPayClient Members

        public async Task<T> ExecuteAsync<T>(IWeChatPayPrivacyGetRequest<T> request, WeChatPayOptions options) where T : WeChatPayResponse
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            if (string.IsNullOrEmpty(options.MchId))
            {
                throw new WeChatPayException($"options.{nameof(WeChatPayOptions.MchId)} is Empty!");
            }

            if (string.IsNullOrEmpty(options.Certificate))
            {
                throw new WeChatPayException($"options.{nameof(WeChatPayOptions.Certificate)} is Empty!");
            }

            if (string.IsNullOrEmpty(options.APIv3Key))
            {
                throw new WeChatPayException($"options.{nameof(WeChatPayOptions.APIv3Key)} is Empty!");
            }

            string certSerialNo, certPublicKey;

            if (string.IsNullOrEmpty(options.WeChatPayPublicKeyId) || string.IsNullOrEmpty(options.WeChatPayPublicKey))
            {
                var cert = await _platformCertificateManager.GetCertificateAsync(this, options);
                certSerialNo = cert.SerialNo;
                certPublicKey = cert.PublicKey;
            }
            else
            {
                certSerialNo = options.WeChatPayPublicKeyId;
                certPublicKey = options.WeChatPayPublicKey;
            }

            // 加密敏感信息
            EncryptPrivacyProperty(request.GetQueryModel(), certPublicKey);

            var client = _httpClientFactory.CreateClient(Name);
            var (headers, body, statusCode) = await client.GetAsync(request, options, certSerialNo);
            var parser = new WeChatPayResponseJsonParser<T>();
            var response = parser.Parse(body, statusCode);

            if (request.GetNeedCheckSign())
            {
                if (!response.IsError)
                {
                    await CheckResponseSignAsync(headers, body, options);
                }
            }

            // 解密敏感信息
            DecryptPrivacyProperty(response, options.RSAPrivateKey);

            return response;
        }

        #endregion

        #region IWeChatPayClient Members

        public async Task<T> ExecuteAsync<T>(IWeChatPayPrivacyPostRequest<T> request, WeChatPayOptions options) where T : WeChatPayResponse
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            if (string.IsNullOrEmpty(options.MchId))
            {
                throw new WeChatPayException($"options.{nameof(WeChatPayOptions.MchId)} is Empty!");
            }

            if (string.IsNullOrEmpty(options.Certificate))
            {
                throw new WeChatPayException($"options.{nameof(WeChatPayOptions.Certificate)} is Empty!");
            }

            string certSerialNo, certPublicKey;

            if (string.IsNullOrEmpty(options.WeChatPayPublicKeyId) || string.IsNullOrEmpty(options.WeChatPayPublicKey))
            {
                var cert = await _platformCertificateManager.GetCertificateAsync(this, options);
                certSerialNo = cert.SerialNo;
                certPublicKey = cert.PublicKey;
            }
            else
            {
                certSerialNo = options.WeChatPayPublicKeyId;
                certPublicKey = options.WeChatPayPublicKey;
            }

            // 加密敏感信息
            EncryptPrivacyProperty(request.GetBodyModel(), certPublicKey);

            var client = _httpClientFactory.CreateClient(Name);
            var (headers, body, statusCode) = await client.PostAsync(request, options, certSerialNo);
            var parser = new WeChatPayResponseJsonParser<T>();
            var response = parser.Parse(body, statusCode);

            if (!response.IsError)
            {
                await CheckResponseSignAsync(headers, body, options);
            }

            // 解密敏感信息
            DecryptPrivacyProperty(response, options.RSAPrivateKey);

            return response;
        }

        #endregion

        #region Check Response Method

        private async Task CheckResponseSignAsync(WeChatPayHeaders headers, string body, WeChatPayOptions options)
        {
            if (string.IsNullOrEmpty(headers.Serial))
            {
                throw new WeChatPayException($"sign check fail: {nameof(headers.Serial)} is empty!");
            }

            if (string.IsNullOrEmpty(headers.Signature))
            {
                throw new WeChatPayException($"sign check fail: {nameof(headers.Signature)} is empty!");
            }

            if (headers.Serial.StartsWith("PUB_KEY_ID_")) // 微信支付公钥
            {
                if (!string.IsNullOrEmpty(options.WeChatPayPublicKeyId) && headers.Serial == options.WeChatPayPublicKeyId)
                {
                    var signSourceData = WeChatPayUtility.BuildSignatureSourceData(headers.Timestamp, headers.Nonce, body);
                    var signCheck = SHA256WithRSA.Verify(signSourceData, headers.Signature, options.WeChatPayPublicKey);
                    if (!signCheck)
                    {
                        throw new WeChatPayException("sign check fail: check Sign and Data Fail!");
                    }
                }
                else
                {
                    throw new WeChatPayException("sign check fail: WeChatPay Public Key Id Fail!");
                }
            }
            else
            {
                var cert = await _platformCertificateManager.GetCertificateAsync(this, options, headers.Serial);
                var signSourceData = WeChatPayUtility.BuildSignatureSourceData(headers.Timestamp, headers.Nonce, body);
                var signCheck = SHA256WithRSA.Verify(signSourceData, headers.Signature, cert.PublicKey);
                if (!signCheck)
                {
                    throw new WeChatPayException("sign check fail: check Sign and Data Fail!");
                }
            }
        }

        #endregion

        #region Helper

        /// <summary>
        /// 加密敏感信息字段
        /// </summary>
        /// <remarks>
        /// <para><a href="https://pay.weixin.qq.com/wiki/doc/apiv3/wechatpay/wechatpay4_3.shtml">敏感信息加解密</a></para>
        /// </remarks>
        private static void EncryptPrivacyProperty(WeChatPayObject obj, string publicKey)
        {
            if(obj == null)
            {
                return;
            }

            foreach (var propertyInfo in obj.GetType().GetProperties())
            {
                if (propertyInfo.PropertyType == typeof(string)) // 加密字符串
                {
                    if (!propertyInfo.IsDefined(typeof(WeChatPayPrivacyPropertyAttribute)))
                    {
                        continue;
                    }

                    var value = propertyInfo.GetValue(obj); // 获取值
                    var strValue = value as string;
                    if (string.IsNullOrEmpty(strValue))
                    {
                        propertyInfo.SetValue(obj, null);
                        continue;
                    }

                    // 加密并将密文设置回对象
                    var ciphertext = OaepSHA1WithRSA.Encrypt(strValue, publicKey);
                    propertyInfo.SetValue(obj, ciphertext);
                }
                else if (propertyInfo.PropertyType.IsClass) // 加密子对象
                {
                    if (propertyInfo.PropertyType.IsArray)
                    {
                        if (propertyInfo.GetValue(obj) is WeChatPayObject[] array)
                        {
                            foreach (var item in array)
                            {
                                EncryptPrivacyProperty(item, publicKey);
                            }
                        }
                    }
                    if (propertyInfo.PropertyType.IsGenericType && typeof(IEnumerable<WeChatPayObject>).IsAssignableFrom(propertyInfo.PropertyType))
                    {
                        if (propertyInfo.GetValue(obj) is IEnumerable<WeChatPayObject> enumerable)
                        {
                            foreach (var item in enumerable)
                            {
                                EncryptPrivacyProperty(item, publicKey);
                            }
                        }
                    }
                    else if(typeof(WeChatPayObject).IsAssignableFrom(propertyInfo.PropertyType))
                    {
                        if (propertyInfo.GetValue(obj) is WeChatPayObject wcpObj)
                        {
                            EncryptPrivacyProperty(wcpObj, publicKey);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 解密敏感信息字段
        /// </summary>
        /// <remarks>
        /// <para><a href="https://pay.weixin.qq.com/wiki/doc/apiv3/wechatpay/wechatpay4_3.shtml">敏感信息加解密</a></para>
        /// </remarks>
        private static void DecryptPrivacyProperty(WeChatPayObject obj, string privateKey)
        {
            if(obj == null)
            {
                return;
            }

            foreach (var propertyInfo in obj.GetType().GetProperties())
            {
                if (propertyInfo.PropertyType == typeof(string)) // 加密字符串
                {
                    if (!propertyInfo.IsDefined(typeof(WeChatPayPrivacyPropertyAttribute)))
                    {
                        continue;
                    }

                    var value = propertyInfo.GetValue(obj); // 获取值
                    var strValue = value as string;
                    if (string.IsNullOrEmpty(strValue))
                    {
                        propertyInfo.SetValue(obj, null);
                        continue;
                    }

                    // 解密并将明文设置回对象
                    var ciphertext = OaepSHA1WithRSA.Decrypt(strValue, privateKey);
                    propertyInfo.SetValue(obj, ciphertext);
                }
                else if (propertyInfo.PropertyType.IsClass) // 解密子对象
                {
                    if (propertyInfo.PropertyType.IsArray)
                    {
                        if (propertyInfo.GetValue(obj) is WeChatPayObject[] array)
                        {
                            foreach (var item in array)
                            {
                                DecryptPrivacyProperty(item, privateKey);
                            }
                        }
                    }
                    if (propertyInfo.PropertyType.IsGenericType && typeof(IEnumerable<WeChatPayObject>).IsAssignableFrom(propertyInfo.PropertyType))
                    {
                        if (propertyInfo.GetValue(obj) is IEnumerable<WeChatPayObject> enumerable)
                        {
                            foreach (var item in enumerable)
                            {
                                DecryptPrivacyProperty(item, privateKey);
                            }
                        }
                    }
                    else if(typeof(WeChatPayObject).IsAssignableFrom(propertyInfo.PropertyType))
                    {
                        if (propertyInfo.GetValue(obj) is WeChatPayObject wcpObj)
                        {
                            DecryptPrivacyProperty(wcpObj, privateKey);
                        }
                    }
                }
            }
        }

        #endregion
    }
}
