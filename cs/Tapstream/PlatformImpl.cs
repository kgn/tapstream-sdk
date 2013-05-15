﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

#if TEST_WINPHONE || WINDOWS_PHONE
using Microsoft.Phone.Reactive;
using Microsoft.Phone.Info;
using System.Threading;
using System.Net;
using System.IO;
using System.IO.IsolatedStorage;
using System.Reflection;
using System.Windows;
#else
using System.Threading.Tasks;
using System.Net.Http;
using Windows.Globalization;
using Windows.Storage.Streams;
using Windows.System.Profile;
using Windows.ApplicationModel;
#endif

namespace TapstreamMetrics.Sdk
{
    class PlatformImpl : Platform
    {
        private const string FIRED_EVENTS_KEY = "__tapstream_sdk_fired_events";
        private const string UUID_KEY = "__tapstream_sdk_uuid";

        public string LoadUuid()
        {
#if TEST_WINPHONE || WINDOWS_PHONE
            string guid = null;
            if(IsolatedStorageSettings.ApplicationSettings.TryGetValue<string>(UUID_KEY, out guid))
            {
                return guid;
            }
            guid = Guid.NewGuid().ToString();
            IsolatedStorageSettings.ApplicationSettings[UUID_KEY] = guid;
            IsolatedStorageSettings.ApplicationSettings.Save();
            return guid;
#else
            Windows.Storage.ApplicationDataContainer localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
            if(localSettings.Values.ContainsKey(UUID_KEY))
            {
                return (string)localSettings.Values[UUID_KEY];
            }
            string guid = Guid.NewGuid().ToString();
            localSettings.Values[UUID_KEY] = guid;
            return guid;            
#endif
        }

        public HashSet<string> LoadFiredEvents()
        {
#if TEST_WINPHONE || WINDOWS_PHONE
            HashSet<string> firedEvents = new HashSet<string>();
            Dictionary<string, bool> contents;
            if(IsolatedStorageSettings.ApplicationSettings.TryGetValue<Dictionary<string, bool>>(FIRED_EVENTS_KEY, out contents))
            {
                firedEvents.dict = contents;
            }
            return firedEvents;
#else
            Windows.Storage.ApplicationDataContainer localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
            if(!localSettings.Values.ContainsKey(FIRED_EVENTS_KEY))
            {
                return new HashSet<string>();
            }
            return new HashSet<string>((string[])localSettings.Values[FIRED_EVENTS_KEY]);
#endif
        }

        public void SaveFiredEvents(HashSet<string> firedEvents)
        {
#if TEST_WINPHONE || WINDOWS_PHONE
            IsolatedStorageSettings.ApplicationSettings[FIRED_EVENTS_KEY] = firedEvents.dict;
            IsolatedStorageSettings.ApplicationSettings.Save();
#else
            Windows.Storage.ApplicationDataContainer localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
            localSettings.Values[FIRED_EVENTS_KEY] = firedEvents.ToArray();
#endif
        }


        public string GetResolution()
        {
#if TEST_WINPHONE || WINDOWS_PHONE
            int w = (int)System.Windows.Application.Current.Host.Content.ActualWidth;
            int h = (int)System.Windows.Application.Current.Host.Content.ActualHeight;
            return String.Format("{0}x{1}", w, h);
#else
            return null;
#endif
        }

        public string GetManufacturer()
        {
#if TEST_WINPHONE || WINDOWS_PHONE
            return (string)DeviceExtendedProperties.GetValue("DeviceManufacturer");
#else
            return "Microsoft";
#endif
        }

        public string GetModel()
        {
#if TEST_WINPHONE || WINDOWS_PHONE
            return (string)DeviceExtendedProperties.GetValue("DeviceName");
#else
            return null;
#endif
        }

        public string GetOs()
        {
#if TEST_WINPHONE || WINDOWS_PHONE
            return System.Environment.OSVersion.ToString();
#else
            return "Windows 8";
#endif
        }

        public string GetLocale()
        {
#if TEST_WINPHONE || WINDOWS_PHONE
            return String.Format("{0}_{1}", System.Globalization.CultureInfo.CurrentCulture.TwoLetterISOLanguageName, System.Globalization.RegionInfo.CurrentRegion.TwoLetterISORegionName);
#else
            try
            {
                string locale = ApplicationLanguages.Languages.ElementAt(0);
                return locale.Replace("-", "_");
            }
            catch (Exception)
            {
                return null;
            }
#endif
        }

#if TEST_WINPHONE || WINDOWS_PHONE
        public string GetDeviceUniqueId()
        {
            byte[] bytes = (byte[])DeviceExtendedProperties.GetValue("DeviceUniqueId");
            string hex = BitConverter.ToString(bytes);
            return hex.Replace("-", "").ToLower();
        }
#else
        public string GetAppSpecificHardwareId()
        {
            // http://msdn.microsoft.com/en-us/library/windows/apps/jj553431
            string deviceId = "";
            HardwareToken hardwareToken = HardwareIdentification.GetPackageSpecificToken(null);
            using (DataReader dataReader = DataReader.FromBuffer(hardwareToken.Id))
            {
                int offset = 0;
                while (offset < hardwareToken.Id.Length)
                {
                    byte[] hardwareEntry = new byte[4];
                    dataReader.ReadBytes(hardwareEntry);

                    // CPU ID of the processor || Size of the memory || Serial number of the disk device || BIOS
                    if ((hardwareEntry[0] == 1 || hardwareEntry[0] == 2 || hardwareEntry[0] == 3 || hardwareEntry[0] == 9) && hardwareEntry[1] == 0)
                    {
                        deviceId += string.Format("{0}.{1}", hardwareEntry[2], hardwareEntry[3]);
                    }
                    offset += 4;
                }
            }
            return deviceId;
        }
#endif

        public string GetAppName()
        {
#if TEST_WINPHONE || WINDOWS_PHONE
            AssemblyName nameHelper = new AssemblyName(Application.Current.GetType().Assembly.FullName);
            return nameHelper.Name;
#else
            return Package.Current.Id.Name;
#endif
        }

        public string GetPackageName()
        {
#if TEST_WINPHONE || WINDOWS_PHONE
            AssemblyName nameHelper = new AssemblyName(Application.Current.GetType().Assembly.FullName);
            return nameHelper.FullName;
#else
            return Package.Current.Id.FullName;
#endif
        }

        public Response Request(string url, string data)
        {
#if TEST_WINPHONE || WINDOWS_PHONE
            int status = -1;
            string message = null;
            AutoResetEvent signal = new AutoResetEvent(false);
            
            HttpWebRequest req = (HttpWebRequest)HttpWebRequest.CreateHttp(url);
            req.Method = "POST";
            req.ContentType = "application/x-www-form-urlencoded;charset=UTF-8";
            try
            {
                req.BeginGetRequestStream(reqState =>
                {
                    Stream postStream = req.EndGetRequestStream(reqState);

                    Encoding enc = new System.Text.UTF8Encoding();
                    postStream.Write(enc.GetBytes(data), 0, data.Length);
                    postStream.Close();

                    req.BeginGetResponse(respState =>
                    {
                        try
                        {
                            HttpWebResponse response = (HttpWebResponse)req.EndGetResponse(respState);
                            status = (int)response.StatusCode;
                            message = response.StatusDescription;
                        }
                        catch (WebException we)
                        {
                            var resp = we.Response as HttpWebResponse;
                            if (resp == null)
                            {
                                throw;
                            }
                            status = (int)resp.StatusCode;
                            message = we.ToString();
                        }
                        catch (Exception ex)
                        {
                            status = -1;
                            message = ex.ToString();
                        }

                        signal.Set();

                    }, null);

                }, null);
            }
            catch (Exception ex)
            {
                status = -1;
                message = ex.ToString();
            }

            signal.WaitOne();
            return new Response(status, message);
#else

            int status = -1;
            string message = null;
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    HttpResponseMessage response = client.PostAsync(url, new StringContent(data, Encoding.UTF8, "application/x-www-form-urlencoded")).Result;
                    status = (int)response.StatusCode;
                    if(!response.IsSuccessStatusCode)
                    {
                        message = response.ReasonPhrase;
                    }
                }
            }
            catch (Exception ex)
            {
                status = -1;
                while (ex.InnerException != null)
                {
                    ex = ex.InnerException;
                }
                message = ex.Message;
            }
            return new Response(status, message);
#endif
        }
    }
}
