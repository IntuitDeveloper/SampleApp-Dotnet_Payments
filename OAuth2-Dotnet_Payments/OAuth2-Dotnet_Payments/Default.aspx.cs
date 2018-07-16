

/******************************************************
 * Intuit sample app for Oauth2 using Intuit .Net SDK
 * RFC docs- https://tools.ietf.org/html/rfc6749
 * ****************************************************/

//https://stackoverflow.com/questions/23562044/window-opener-is-undefined-on-internet-explorer/26359243#26359243
//IE issue- https://stackoverflow.com/questions/7648231/javascript-issue-in-ie-with-window-opener

using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Net;
using System.Web.UI;
using System.Configuration;
using System.Web;
using Intuit.Ipp.OAuth2PlatformClient;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace OAuth2_Dotnet_UsingSDK
{
    public partial class Default : System.Web.UI.Page
    {
        // OAuth2 client configuration
        static string redirectURI = ConfigurationManager.AppSettings["redirectURI"];
        static string clientID = ConfigurationManager.AppSettings["clientID"];
        static string clientSecret = ConfigurationManager.AppSettings["clientSecret"];
        static string logPath = ConfigurationManager.AppSettings["logPath"];
        static string appEnvironment = ConfigurationManager.AppSettings["appEnvironment"];
        static string paymentsBaseUrl = ConfigurationManager.AppSettings["paymentsBaseUrl"];

        static OAuth2Client oauthClient = new OAuth2Client(clientID, clientSecret, redirectURI, appEnvironment);
        static string authCode;
        static string idToken;
        public static Dictionary<string, string> dictionary = new Dictionary<string, string>();

        protected void Page_PreInit(object sender, EventArgs e)
        {
            if (!dictionary.ContainsKey("accessToken"))
            {
                homeButtons.Visible = true;
                connected.Visible = false;
            }
            else
            {
                homeButtons.Visible = false;
                connected.Visible = true;
            }
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            {
                AsyncMode = true;
                if (!dictionary.ContainsKey("accessToken"))
                {
                    if (Request.QueryString.Count > 0)
                    {
                        var response = new AuthorizeResponse(Request.QueryString.ToString());
                        if (response.State != null)
                        {
                            if (oauthClient.CSRFToken == response.State)
                            {
                                if (response.RealmId != null)
                                {
                                    if (!dictionary.ContainsKey("realmId"))
                                    {
                                        dictionary.Add("realmId", response.RealmId);
                                    }
                                }

                                if (response.Code != null)
                                {
                                    authCode = response.Code;
                                    output("Authorization code obtained.");
                                    PageAsyncTask t = new PageAsyncTask(performCodeExchange);
                                    Page.RegisterAsyncTask(t);
                                    Page.ExecuteRegisteredAsyncTasks();
                                }
                            }
                            else
                            {
                                output("Invalid State");
                                dictionary.Clear();
                            }
                        }
                    }
                }
                else
                {
                    homeButtons.Visible = false;
                    connected.Visible = true;
                }
            }
        }

        #region button click events

        protected void ImgOpenId_Click(object sender, ImageClickEventArgs e)
        {
            output("Intiating OpenId call.");
            try
            {
                if (!dictionary.ContainsKey("accessToken"))
                {
                    List<OidcScopes> scopes = new List<OidcScopes>();
                    scopes.Add(OidcScopes.OpenId);
                    scopes.Add(OidcScopes.Phone);
                    scopes.Add(OidcScopes.Profile);
                    scopes.Add(OidcScopes.Address);
                    scopes.Add(OidcScopes.Email);

                    var authorizationRequest = oauthClient.GetAuthorizationURL(scopes);
                    Response.Redirect(authorizationRequest, "_blank", "menubar=0,scrollbars=1,width=780,height=900,top=10");
                }
            }
            catch (Exception ex)
            {
                output(ex.Message);
            }
        }

        protected void ImgC2QB_Click(object sender, ImageClickEventArgs e)
        {
            output("Intiating OAuth2 call.");
            try
            {
                if (!dictionary.ContainsKey("accessToken"))
                {
                    List<OidcScopes> scopes = new List<OidcScopes>();
                    scopes.Add(OidcScopes.Payment);
                    var authorizationRequest = oauthClient.GetAuthorizationURL(scopes);
                    Response.Redirect(authorizationRequest, "_blank", "menubar=0,scrollbars=1,width=780,height=900,top=10");
                }
            }
            catch (Exception ex)
            {
                output(ex.Message);
            }
        }

        protected void ImgGetAppNow_Click(object sender, ImageClickEventArgs e)
        {
            output("Intiating Get App Now call.");
            try
            {
                if (!dictionary.ContainsKey("accessToken"))
                {
                    List<OidcScopes> scopes = new List<OidcScopes>();
                    scopes.Add(OidcScopes.Payment);
                    scopes.Add(OidcScopes.OpenId);
                    scopes.Add(OidcScopes.Phone);
                    scopes.Add(OidcScopes.Profile);
                    scopes.Add(OidcScopes.Address);
                    scopes.Add(OidcScopes.Email);

                    var authorizationRequest = oauthClient.GetAuthorizationURL(scopes);
                    Response.Redirect(authorizationRequest, "_blank", "menubar=0,scrollbars=1,width=780,height=900,top=10");
                }
            }
            catch (Exception ex)
            {
                output(ex.Message);
            }
        }

        protected async void btnRevoke_Click(object sender, EventArgs e)
        {
            output("Performing Revoke tokens.");
            if ((dictionary.ContainsKey("accessToken")) && (dictionary.ContainsKey("refreshToken")))
            {
                var revokeTokenResp = await oauthClient.RevokeTokenAsync(dictionary["refreshToken"]);
                if (revokeTokenResp.HttpStatusCode == HttpStatusCode.OK)
                {
                    dictionary.Clear();
                    if (Request.Url.Query == "")
                        Response.Redirect(Request.RawUrl);
                    else
                        Response.Redirect(Request.RawUrl.Replace(Request.Url.Query, ""));
                }
                output("Token revoked.");
            }

        }

        protected async void btnPaymentsAPICall_Click(object sender, EventArgs e)
        {
            if (dictionary.ContainsKey("realmId"))
            {
                if (dictionary.ContainsKey("accessToken"))
                {
                    await paymentsApiCall();
                }
            }
            else
            {
                output("SIWI call does not returns realm for Payments api call.");
                lblPaymentsCall.Visible = true;
                lblPaymentsCall.Text = "SIWI call does not returns realm for Payments api call";
            }
        }
        #endregion

        /// <summary>
        /// Start code exchange to get the Access Token and Refresh Token
        /// </summary>
        public async Task performCodeExchange()
        {
            output("Exchanging code for tokens.");
            try
            {
                var tokenResp = await oauthClient.GetBearerTokenAsync(authCode);
                if (!dictionary.ContainsKey("accessToken"))
                    dictionary.Add("accessToken", tokenResp.AccessToken);
                else
                    dictionary["accessToken"] = tokenResp.AccessToken;

                if (!dictionary.ContainsKey("refreshToken"))
                    dictionary.Add("refreshToken", tokenResp.RefreshToken);
                else
                    dictionary["refreshToken"] = tokenResp.RefreshToken;

                if (tokenResp.IdentityToken != null)
                    idToken = tokenResp.IdentityToken;
                if (Request.Url.Query == "")
                {
                    Response.Redirect(Request.RawUrl);
                }
                else
                {
                    Response.Redirect(Request.RawUrl.Replace(Request.Url.Query, ""));
                }
            }
            catch (Exception ex)
            {
                output("Problem while getting bearer tokens.");
            }
        }

        #region payments calls
        /// <summary>
        /// Test QBO api call
        /// </summary>
        public async Task paymentsApiCall()
        {
            try
            {
                if ((dictionary.ContainsKey("accessToken")) && (dictionary.ContainsKey("realmId")))
                {
                    output("Making QBO API Call.");
                    string cardToken = getCardToken();
                    JObject cardChargeResponse = createPaymentsCharge(cardToken);

                    output("QBO call successful.");
                    lblPaymentsCall.Visible = true;
                    lblPaymentsCall.Text = "QBO Call successful";
                }
            }
            catch (Exception ex)
            {
                if (ex.Message == "Unauthorized-401")
                {
                    output("Invalid/Expired Access Token.");

                    var tokenResp = await oauthClient.RefreshTokenAsync(dictionary["refreshToken"]);
                    if (tokenResp.AccessToken != null && tokenResp.RefreshToken != null)
                    {
                        dictionary["accessToken"] = tokenResp.AccessToken;
                        dictionary["refreshToken"] = tokenResp.RefreshToken;
                        await paymentsApiCall();
                    }
                    else
                    {
                        output("Error while refreshing tokens: " + tokenResp.Raw);
                    }
                }
                else
                {
                    output(ex.Message);
                }
            }
        }

        /// <summary>
        /// Get card token
        /// </summary>
        /// <returns>string</returns>
        public string getCardToken()
        {
            string cardToken="";
            JObject jsonDecodedResponse;
            string cardTokenJson = "";
            string cardTokenEndpoint = "quickbooks/v4/payments/tokens";
            string uri= paymentsBaseUrl + cardTokenEndpoint;

            string cardTokenRequestBody = "{\"card\":{\"expYear\":\"2020\",\"expMonth\":\"02\",\"address\":{\"region\":\"CA\",\"postalCode\":\"94086\",\"streetAddress\":\"1130 Kifer Rd\",\"country\":\"US\",\"city\":\"Sunnyvale\"},\"name\":\"emulate=0\",\"cvc\":\"123\",\"number\":\"4111111111111111\"}}";
           
            // send the request (token api call does not requires Authorization header, rest all payments call do)
            HttpWebRequest cardTokenRequest = (HttpWebRequest)WebRequest.Create(uri);
            cardTokenRequest.Method = "POST";           
            cardTokenRequest.ContentType = "application/json";
            cardTokenRequest.Headers.Add("Request-Id", Guid.NewGuid().ToString());//assign guid

            byte[] _byteVersion = Encoding.ASCII.GetBytes(cardTokenRequestBody);
            cardTokenRequest.ContentLength = _byteVersion.Length;
            Stream stream = cardTokenRequest.GetRequestStream();
            stream.Write(_byteVersion, 0, _byteVersion.Length);
            stream.Close();

            // get the response
            HttpWebResponse cardTokenResponse = (HttpWebResponse)cardTokenRequest.GetResponse();
            using (Stream data = cardTokenResponse.GetResponseStream())
            {
                cardTokenJson= new StreamReader(data).ReadToEnd();
                jsonDecodedResponse = JObject.Parse(cardTokenJson);
                if (!string.IsNullOrEmpty(jsonDecodedResponse.TryGetString("value")))
                {
                    cardToken = jsonDecodedResponse["value"].ToString();
                }
            }
            return cardToken;
        }

        /// <summary>
        /// Execute Charge on the card
        /// </summary>
        /// <param name="cardToken"></param>
        /// <param name="realmId"></param>
        /// <param name="access_token"></param>
        /// <param name="refresh_token"></param>
        /// <returns>JObject</returns>
        public JObject createPaymentsCharge(string cardToken)
        {
            string cardChargeJson = "";
            JObject jsonDecodedResponse;
            string cardChargeEndpoint = "quickbooks/v4/payments/charges";
            string uri = paymentsBaseUrl + cardChargeEndpoint;

            string cardChargeRequestBody= "{\"amount\":\"10.55\",\"token\":\""+ cardToken + "\",\"currency\":\"USD\",\"context\":{\"mobile\":\"false\",\"isEcommerce\":\"true\"}}";
            
            // send the request
            HttpWebRequest cardChargeRequest = (HttpWebRequest)WebRequest.Create(uri);
            cardChargeRequest.Method = "POST";
            if (dictionary["access_token"] != null)
                cardChargeRequest.Headers.Add(string.Format("Authorization: Bearer {0}", dictionary["access_token"]));
            else
                output("Access token not found.");
            cardChargeRequest.ContentType = "application/json";
            cardChargeRequest.Accept = "application/json";
            cardChargeRequest.Headers.Add("Request-Id", Guid.NewGuid().ToString());//assign unique guid everytime

            byte[] _byteVersion = Encoding.ASCII.GetBytes(cardChargeRequestBody);
            cardChargeRequest.ContentLength = _byteVersion.Length;
            Stream stream = cardChargeRequest.GetRequestStream();
            stream.Write(_byteVersion, 0, _byteVersion.Length);
            stream.Close();

            // get the response
            HttpWebResponse cardChargeResponse = (HttpWebResponse)cardChargeRequest.GetResponse();

            using (Stream data = cardChargeResponse.GetResponseStream())
            {
                //return XML response
                cardChargeJson = new StreamReader(data).ReadToEnd();
                jsonDecodedResponse = JObject.Parse(cardChargeJson);
            }
            return jsonDecodedResponse;
        }
        #endregion

        #region Helper methods for logging
        /// <summary>
        /// Gets log path
        /// </summary>
        public string GetLogPath()
        {
            try
            {
                if (logPath == "")
                {
                    logPath = Environment.GetEnvironmentVariable("TEMP");
                    if (!logPath.EndsWith("\\")) logPath += "\\";
                }
            }
            catch
            {
                output("Log error path not found.");
            }
            return logPath;
        }

        /// <summary>
        /// Appends the given string to the on-screen log, and the debug console.
        /// </summary>
        /// <param name="logMsg">string to be appended</param>
        public void output(string logMsg)
        {
            StreamWriter sw = File.AppendText(GetLogPath() + "OAuth2SampleAppLogs.txt");
            try
            {
                string logLine = System.String.Format(
                    "{0:G}: {1}.", System.DateTime.Now, logMsg);
                sw.WriteLine(logLine);
            }
            finally
            {
                sw.Close();
            }
        }
        #endregion
    }

    /// <summary>
    /// Helper for calling self
    /// </summary>
    public static class ResponseHelper
    {
        public static void Redirect(this HttpResponse response, string url, string target, string windowFeatures)
        {
            if ((String.IsNullOrEmpty(target) || target.Equals("_self", StringComparison.OrdinalIgnoreCase)) && String.IsNullOrEmpty(windowFeatures))
            {
                response.Redirect(url);
            }
            else
            {
                Page page = (Page)HttpContext.Current.Handler;
                if (page == null)
                {
                    throw new InvalidOperationException("Cannot redirect to new window outside Page context.");
                }
                url = page.ResolveClientUrl(url);
                string script;
                if (!String.IsNullOrEmpty(windowFeatures))
                {
                    script = @"window.open(""{0}"", ""{1}"", ""{2}"");";
                }
                else
                {
                    script = @"window.open(""{0}"", ""{1}"");";
                }
                script = String.Format(script, url, target, windowFeatures);
                ScriptManager.RegisterStartupScript(page, typeof(Page), "Redirect", script, true);
            }
        }
    }
}