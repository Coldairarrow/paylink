﻿using System.Collections.Generic;
using Essensoft.AspNetCore.Payment.Alipay.Response;

namespace Essensoft.AspNetCore.Payment.Alipay.Request
{
    /// <summary>
    /// alipay.samsung.puc.charge
    /// </summary>
    public class AlipaySamsungPucChargeRequest : IAlipayRequest<AlipaySamsungPucChargeResponse>
    {
        /// <summary>
        /// 设备唯一值
        /// </summary>
        public string ApdId { get; set; }

        /// <summary>
        /// 手机位置信息
        /// </summary>
        public string CellId { get; set; }

        /// <summary>
        /// 商户用户的设备信息
        /// </summary>
        public string DeviceInfoToken { get; set; }

        /// <summary>
        /// 商户输入的扩展信息
        /// </summary>
        public string Exparam { get; set; }

        /// <summary>
        /// 商户用户的无线设备的终端信息
        /// </summary>
        public string Imei { get; set; }

        /// <summary>
        /// 客户端IP
        /// </summary>
        public string Ip { get; set; }

        /// <summary>
        /// 基站LAC
        /// </summary>
        public string LacId { get; set; }

        /// <summary>
        /// 免登业务来源
        /// </summary>
        public string LoginFrom { get; set; }

        /// <summary>
        /// 设备mac信息
        /// </summary>
        public string Mac { get; set; }

        /// <summary>
        /// 设备的安全支付标识
        /// </summary>
        public string Tid { get; set; }

        /// <summary>
        /// 设备umid信息
        /// </summary>
        public string Umid { get; set; }

        /// <summary>
        /// wifi上的mac地址
        /// </summary>
        public string WirelessMac { get; set; }

        #region IAlipayRequest Members

        private bool needEncrypt = false;
        private string apiVersion = "1.0";
        private string terminalType;
        private string terminalInfo;
        private string prodCode;
        private string notifyUrl;
        private string returnUrl;
        private AlipayObject bizModel;

        public void SetNeedEncrypt(bool needEncrypt)
        {
            this.needEncrypt = needEncrypt;
        }

        public bool GetNeedEncrypt()
        {
            return needEncrypt;
        }

        public void SetNotifyUrl(string notifyUrl)
        {
            this.notifyUrl = notifyUrl;
        }

        public string GetNotifyUrl()
        {
            return notifyUrl;
        }

        public void SetReturnUrl(string returnUrl)
        {
            this.returnUrl = returnUrl;
        }

        public string GetReturnUrl()
        {
            return returnUrl;
        }

        public void SetTerminalType(string terminalType)
        {
            this.terminalType = terminalType;
        }

        public string GetTerminalType()
        {
            return terminalType;
        }

        public void SetTerminalInfo(string terminalInfo)
        {
            this.terminalInfo = terminalInfo;
        }

        public string GetTerminalInfo()
        {
            return terminalInfo;
        }

        public void SetProdCode(string prodCode)
        {
            this.prodCode = prodCode;
        }

        public string GetProdCode()
        {
            return prodCode;
        }

        public string GetApiName()
        {
            return "alipay.samsung.puc.charge";
        }

        public void SetApiVersion(string apiVersion)
        {
            this.apiVersion = apiVersion;
        }

        public string GetApiVersion()
        {
            return apiVersion;
        }

        public IDictionary<string, string> GetParameters()
        {
            var parameters = new AlipayDictionary
            {
                { "apd_id", ApdId },
                { "cell_id", CellId },
                { "device_info_token", DeviceInfoToken },
                { "exparam", Exparam },
                { "imei", Imei },
                { "ip", Ip },
                { "lac_id", LacId },
                { "login_from", LoginFrom },
                { "mac", Mac },
                { "tid", Tid },
                { "umid", Umid },
                { "wireless_mac", WirelessMac }
            };
            return parameters;
        }

        public AlipayObject GetBizModel()
        {
            return bizModel;
        }

        public void SetBizModel(AlipayObject bizModel)
        {
            this.bizModel = bizModel;
        }

        #endregion
    }
}
