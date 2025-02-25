﻿using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Essensoft.Paylink.Security;
using Essensoft.Paylink.WeChatPay.V3.Parser;

namespace Essensoft.Paylink.WeChatPay.V3
{
    public class WeChatPayNotifyClient : IWeChatPayNotifyClient
    {
        #region WeChatPayNotifyClient Constructors

        private readonly IWeChatPayClient _client;
        private readonly WeChatPayPlatformCertificateManager _platformCertificateManager;

        public WeChatPayNotifyClient(IWeChatPayClient client, WeChatPayPlatformCertificateManager platformCertificateManager)
        {
            _client = client;
            _platformCertificateManager = platformCertificateManager;
        }

        #endregion

        #region IWeChatPayNotifyClient Members

#if NETCOREAPP3_1_OR_GREATER
        public async Task<T> ExecuteAsync<T>(Microsoft.AspNetCore.Http.HttpRequest request, WeChatPayOptions options) where T : WeChatPayNotify
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            var headers = GetWeChatPayHeadersFromRequest(request);
            using (var reader = new StreamReader(request.Body, Encoding.UTF8, true, 1024, true))
            {
                var body = await reader.ReadToEndAsync();
                return await ExecuteAsync<T>(headers, body, options);
            }
        }

        private static WeChatPayHeaders GetWeChatPayHeadersFromRequest(Microsoft.AspNetCore.Http.HttpRequest request)
        {
            var headers = new WeChatPayHeaders();

            if (request.Headers.TryGetValue(WeChatPayConsts.Wechatpay_Serial, out var serialValues))
            {
                headers.Serial = serialValues.First();
            }

            if (request.Headers.TryGetValue(WeChatPayConsts.Wechatpay_Timestamp, out var timestampValues))
            {
                headers.Timestamp = timestampValues.First();
            }

            if (request.Headers.TryGetValue(WeChatPayConsts.Wechatpay_Nonce, out var nonceValues))
            {
                headers.Nonce = nonceValues.First();
            }

            if (request.Headers.TryGetValue(WeChatPayConsts.Wechatpay_Signature, out var signatureValues))
            {
                headers.Signature = signatureValues.First();
            }

            return headers;
        }
#endif

        #endregion

        #region IWeChatPayNotifyClient Members

        public async Task<T> ExecuteAsync<T>(WeChatPayHeaders headers, string body, WeChatPayOptions options) where T : WeChatPayNotify
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            if (string.IsNullOrEmpty(options.APIv3Key))
            {
                throw new WeChatPayException($"options.{nameof(options.APIv3Key)} is Empty!");
            }

            await CheckNotifySignAsync(headers, body, options);

            var parser = new WeChatPayNotifyJsonParser<T>();
            var notify = parser.Parse(body, options.APIv3Key);

            return notify;
        }

        #endregion

        #region Check Notify Method

        private async Task CheckNotifySignAsync(WeChatPayHeaders headers, string body, WeChatPayOptions options)
        {
            if (string.IsNullOrEmpty(headers.Serial))
            {
                throw new WeChatPayException($"sign check fail: {nameof(headers.Serial)} is empty!");
            }

            if (string.IsNullOrEmpty(headers.Signature))
            {
                throw new WeChatPayException($"sign check fail: {nameof(headers.Signature)} is empty!");
            }

            if (string.IsNullOrEmpty(body))
            {
                throw new WeChatPayException("sign check fail: body is empty!");
            }

            if(headers.Serial.StartsWith(WeChatPayConsts.WeChatPayPublicKeyIdPrefix)) // 微信支付公钥
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
                var cert = await _platformCertificateManager.GetCertificateAsync(_client, options, headers.Serial);
                var signSourceData = WeChatPayUtility.BuildSignatureSourceData(headers.Timestamp, headers.Nonce, body);
                var signCheck = SHA256WithRSA.Verify(signSourceData, headers.Signature, cert.PublicKey);
                if (!signCheck)
                {
                    throw new WeChatPayException("sign check fail: check Sign and Data Fail!");
                }
            }
        }

        #endregion
    }
}
