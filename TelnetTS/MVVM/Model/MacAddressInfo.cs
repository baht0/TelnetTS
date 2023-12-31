﻿using HtmlAgilityPack;
using System;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using TelnetInfo.Core;
using TelnetTS.Helper;

namespace TelnetTS.MVVM.Model
{
    public class MacAddressInfo : ObservableObject
    {
        public string Mac1
        {
            get => mac1;
            set
            {
                mac1 = value;
                OnPropertyChanged();
            }
        }
        private string mac1 = "-";
        public string Mac2
        {
            get => mac2;
            set
            {
                mac2 = value;
                OnPropertyChanged();
            }
        }
        private string mac2 = "-";
        public string Vendor
        {
            get => vendor;
            set
            {
                vendor = value;
                OnPropertyChanged();
            }
        }
        private string vendor = "-";
        public string ImgUrl
        {
            get => imgUrl;
            set
            {
                imgUrl = value;
                OnPropertyChanged();
            }
        }
        private string imgUrl = "../../Resources/404.jpg";

        public void Main(string mac)
        {
            Mac1 = string.Join(":", ResponseReader.SplitByLength(mac.Replace(".", string.Empty), 2));
            Mac2 = string.Join("-", ResponseReader.SplitByLength(mac.Replace(".", string.Empty), 4));
            ManufacturerDefinition(mac);
        }
        private async void ManufacturerDefinition(string macAddress)
        {
            try
            {
                var mac = Uri.EscapeDataString(macAddress);
                var url = "https://metroethernet.ru/tools/oui/find?address=" + mac;

                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, url)
                {
                    Content = new StringContent($"address={mac}", Encoding.UTF8, "application/x-www-form-urlencoded")
                };

                var httpClient = new HttpClient();
                var responseMessage = await httpClient.SendAsync(request);
                var result = await responseMessage.Content.ReadAsStringAsync();

                HtmlDocument doc = new HtmlDocument();
                doc.LoadHtml(result);

                var ven = doc.DocumentNode.SelectSingleNode("//td[@class='tools-oui-vendor']").InnerText;
                Vendor = ven != "AO" ? ven : "ZAO NPK RoTeK";

                var img = doc.DocumentNode.SelectSingleNode("//img");
                var scr = img.Attributes["src"].Value;
                ImgUrl = !scr.Contains("nologo.png") ? "https://metroethernet.ru" + scr : "../../Resources/404.jpg";
            }
            catch
            {
                Vendor = "Не определен";
                ImgUrl = "../../Resources/404.jpg";
            }
        }
    }
}
