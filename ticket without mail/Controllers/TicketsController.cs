﻿using Aspose.Email.Clients;
using Aspose.Email.Clients.Pop3;
using Aspose.Email.Clients.Smtp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Web.Mvc;
using ticket_without_mail.Models;

namespace ticket_without_mail.Controllers
{
    public class TicketsController : Controller
    {
        private TicketContext db = new TicketContext();
        private static List<Ticket> selektirani = new List<Ticket>();
        private static List<resolvedTickets> resolvedSelektirani = new List<resolvedTickets>();
        private ApplicationDbContext applicationDbContext = new ApplicationDbContext();
       
        //promenlivi valuti
        private static bool soMail = true;

        private string POP3host = "outlook.office365.com"; //This should be changed outlook mail
        private string username = "Your email"; //This should be changed
        private string password = "Your password";//This should be changed
        private int POP3port = 995;//This should be changed

        private string smtpFrom = "Your email";//This should be changed
        private string SMTPhost = "smtp.office365.com";//This should be changed
        private int SMTPport = 587;//This should be changed

        //export na otvoreni tiketi
        [HttpPost]
        public FileResult ExportToCSVTicket()
        {
            /*Ticket Model
            * email = ticket.email
            * naslov = problemSubject
            * opis = problemBody
            * ip adresa = ipv4
            * tip na problem = problemType
            * vreme na otvaranje = submitTime
            * vreme na prifakjanje = acceptanceTime
            * koj go prifatil problemot = acceptor
            */
            
            StringBuilder sb = new StringBuilder();
            sb.Append("Ticket Number,Summary,Description,Assigned To,Category,Closed On,Created On,Created By,Due On,Priority,Organization Name,Status,Time Spent,Time To Resolve,Organization Host,Link to Ticket,Потврдете  го Вашиот Е-маил,Име,Презиме,TeamViewer ID,TeamViewer Password");
            sb.Append("\r\n");
            foreach (Ticket item in selektirani)
            {
                sb.Append(item.email + "," + item.problemSubject + "," + item.problemBody + "," + item.submitTime + "," + item.ipv4);
                sb.Append("\r\n");
            }

            return File(Encoding.UTF8.GetBytes(sb.ToString()), "text/csv", "tiketi.csv");
        }

        //export na zatvoreni tiketi
        [HttpPost]
        public FileResult ExportToCSVResolvedTicket()
        {
            /*resolvedTickets Model
         * email = ticket.email
         * naslov = problemSubject
         * opis = problemBody
         * ip adresa = ipv4
         * tip na problem = problemType
         * vreme na otvaranje = submitTime
         * vreme na prifakjanje = acceptanceTime
         * vreme na zatvaranje = resolveTime
         * days, hours, minutes = potrebno vreme (taboteno)
         * koj go prifatil problemot = acceptor
         * zabeleska = note
         */
            StringBuilder sb = new StringBuilder();
            sb.Append("email,naslov,opis,vreme na otvaranje,potrebno vreme,vreme na zatvaranje,prifateno od,ip,tip na problem");
            sb.Append("\r\n");
            foreach (resolvedTickets item in resolvedSelektirani)
            {
                sb.Append(item.email + "," + item.problemSubject + "," + item.problemBody + "," + item.submitTime + "," + item.days + "; "+item.hours+":"+item.minutes+":"+item.seconds + ","+item.resolveTime+"," + item.resolver + "," + item.ipv4 + "," + item.problemType);
                sb.Append("\r\n");
            }

            return File(Encoding.ASCII.GetBytes(sb.ToString()), "text/csv", "reseni tiketi.csv");
        }

        //otvoreni tiketi
        [Authorize]
        public ActionResult Index()
        {
            if(soMail)
            readMails();

            //proverka dali e selektiran nekoj datum
            if (Request.Form["from"] != null && Request.Form["to"] != null && Request.Form["from"] != "" && Request.Form["to"] != "")
            {
                //isprazni ja listata za prikazuvanje na tiketi
                selektirani.Clear();
                DateTime from = DateTime.Parse(Request.Form["from"]);
                DateTime to = DateTime.Parse(Request.Form["to"]);
                //site unresolved tickets
                List<Ticket> site = db.Tickets.ToList();
                
                foreach (Ticket ticket in site)
                {      //ako tiketot e vo soodvetniot datum
                    if (ticket.submitTime >= from && ticket.submitTime <= to)
                    {
                        selektirani.Add(ticket);
                    }
                }
                return View(selektirani);
            }
            else
            {
                selektirani.Clear();
                //procitaj gi site tiketi od bazata
                selektirani = db.Tickets.ToList();
                return View(selektirani);
            }
        }

        //potvrdeno prifakjanje na tiket\
        [HttpPost]
        public ActionResult SiteTiketiConfirmed(int id, FormCollection fc)
        {
            //pronajdi go tiketot koj sto e prifaten
            Ticket ticket = db.Tickets.Find(id);
            //azuriraj go vremeto na prifakjanje
            ticket.acceptanceTime = DateTime.UtcNow;
            
            //azuriraj go selektiraniot acceptor
            ticket.acceptor = fc["res"];

            //zacuvaj gi promenite vo bazata
            db.SaveChanges();
          
            return RedirectToAction("SiteTiketi");
        }
        
        //akcija za povik na funkcija za citanje na mailovi
        public ActionResult ReadMails()
        {
            readMails();
            return RedirectToAction("SiteTiketi");
        }

        //funkcija za prakanje na mail
        private void sendMail(string to, string title, string body)
        {
            //nova poraka
            Aspose.Email.MailMessage EmailMessage = new Aspose.Email.MailMessage();

            //popolnuvanje na porakata
            EmailMessage.Subject = title;
            EmailMessage.To = to;
            EmailMessage.Body = body;
            EmailMessage.From = smtpFrom;

            //Inicijalizacija na smtp klient
            SmtpClient SMTPEmailClient = new SmtpClient();

            //postavuvanje na postavkite na smtp
            SMTPEmailClient.Host = SMTPhost;
            SMTPEmailClient.Username = username;
            SMTPEmailClient.Password = password;
            SMTPEmailClient.Port = SMTPport;
            SMTPEmailClient.SecurityOptions = SecurityOptions.SSLExplicit;

            //prakjanje na mailot
            SMTPEmailClient.Send(EmailMessage);
        }

        //funkcija za citanje na mailovi
        private void readMails()
        {
            //inicijalizacija na pop3 klient
            Pop3Client client = new Pop3Client();
            
            //citanje na mailovite od baza
            List<resolvedTickets> resolvedTickets = db.resolvedTickets.ToList();
            List<Ticket> unresolvedTickets = db.Tickets.ToList();
            
            //postavki za pop3 klient
            client.Host = POP3host;
            client.Username = username;
            client.Password = password;
            client.Port = POP3port;
            client.SecurityOptions = Aspose.Email.Clients.SecurityOptions.Auto;

            try
            {
                Debug.WriteLine("CITAM");
                int messageCount = client.GetMessageCount();
                Debug.WriteLine("Messages count : " + messageCount);

                //poraka
                Aspose.Email.MailMessage msg;

                for (int i = 1; i <= messageCount; i++)
                {
                    Debug.WriteLine("FOR CIKLUS");
                    //procitaj ja porakata
                    msg = client.FetchMessage(i);
                    //brisenje na licencata od aspose
                    string subject = msg.Subject.Replace("(Aspose.Email Evaluation)", "").Trim();
                    string body = msg.Body.Replace("------------------------------", "").Replace("(Aspose.Email", "").Replace("Evaluation)", "").Replace("This", "").Replace("is", "").Replace("an", "").Replace("evaluation", "").Replace("copy", "").Replace("of", "").Replace("Aspose.Email", "").Replace("for", "").Replace(".NET", "").Replace("http://www.aspose.com/corporate/purchase/end-user-license-agreement.aspx:", "").Replace("View", "").Replace("EULA", "").Replace("Online", "").Trim();

                    //proverka dali tiketot e veke vo bazata
                    if ((resolvedTickets.Find(ticket => ((ticket.email == msg.From.ToString()) && (ticket.problemSubject == subject) && (ticket.problemBody == body))) != null) || (unresolvedTickets.Find(ticket => ((ticket.email == msg.From.ToString()) && (ticket.problemSubject == subject) && (ticket.problemBody == body))) != null))
                    {
                        Debug.WriteLine("NE DODAVAM TIKET");
                    }
                    else
                    {
                        Debug.WriteLine("DODAVAM TIKET");
                        Ticket ticket = new Ticket();
                        ticket.email = msg.From.ToString();
                        ticket.submitTime = msg.Date.ToUniversalTime();
                        ticket.problemSubject = subject;
                        ticket.problemBody = body;
                        db.Tickets.Add(ticket);
                        db.SaveChanges();
                    }
                    Debug.WriteLine("Nova poraka " + msg.From.ToString() + " " + msg.Subject.Replace("(Aspose.Email Evaluation)", "").Trim() + " " + msg.Body.Replace("(Aspose.Email", "").Replace("Evaluation)", "").Replace("This", "").Replace("is", "").Replace("an", "").Replace("evaluation", "").Replace("copy", "").Replace("of", "").Replace("Aspose.Email", "").Replace("for", "").Replace(".NET", "").Replace("http://www.aspose.com/corporate/purchase/end-user-license-agreement.aspx:", "").Replace("View", "").Replace("EULA", "").Replace("Online", "").Trim());
                }
                
                client.Dispose();

            }
            catch (Exception ex)
            {
                Console.WriteLine(Environment.NewLine + ex.Message);
            }
            finally
            {
                client.Dispose();
            }
        }

        //site tiketi
        [Authorize]
        public ActionResult SiteTiketi(int id = -1)
        {
            SiteModel selektiranii = new SiteModel();

            //registrirani korisnici
            List<ApplicationUser> users = applicationDbContext.Users.ToList();
            List<string> userMails = new List<string>();
            
            if (soMail)
                readMails();

                if (id != -1)
                {
                    Ticket ticket = db.Tickets.Find(id);
                    ticket.acceptanceTime = DateTime.UtcNow;

                    Debug.WriteLine(Request.Form["testtt"]);
                    Debug.WriteLine(id + " " + Request.Form["res"]);
                    ticket.acceptor = Request.Form["res"];
                    db.SaveChanges();
                    return RedirectToAction("SiteTiketi");
                }
                else
                {
                    //dodavanje na adminite
                    foreach (ApplicationUser applicationUser in users)
                    {
                        userMails.Add(applicationUser.Email);
                    }
                    selektiranii.emails = userMails;

                    //ako ima selektrirano datum od-do
                    if (Request.Form["from"] != null && Request.Form["to"] != null && Request.Form["from"] != "" && Request.Form["to"] != "")
                    {
                        Debug.WriteLine("SO DATUM");

                        //citanje na datumot koj e selektiran
                        DateTime from = DateTime.Parse(Request.Form["from"]);
                        DateTime to = DateTime.Parse(Request.Form["to"]);

                        //pomosen model
                        SiteModel siteModel = new SiteModel();

                        //polnenje na pomosen model so podatoci od baza
                        siteModel.resolvedTickets = db.resolvedTickets.ToList();
                        siteModel.unresolvedTickets = db.Tickets.ToList();

                        foreach (Ticket ticket in siteModel.unresolvedTickets)
                        {
                            if (ticket.submitTime >= from && ticket.submitTime <= to)
                            {
                                Debug.WriteLine("DA");
                                selektiranii.unresolvedTickets.Add(ticket);
                            }
                        }
                        foreach (resolvedTickets ticket in siteModel.resolvedTickets)
                        {
                            if (ticket.submitTime >= from && ticket.submitTime <= to)
                            {
                                Debug.WriteLine("DA");
                                selektiranii.resolvedTickets.Add(ticket);
                            }
                        }
                        return View(selektiranii);
                    }
                    //ako nema selektirano datum
                    else
                    {
                        Debug.WriteLine("BEZ DATUM");

                        if(soMail)
                        readMails();

                        //polnenje od baza
                        selektiranii.unresolvedTickets = db.Tickets.ToList();
                        selektiranii.resolvedTickets = db.resolvedTickets.ToList();

                        return View(selektiranii);
                    }
            }
        }

        //dodavanje na nov tip problem
        public ActionResult AddSelection(string id)
        {
            ProblemType newProblem = new ProblemType();
            newProblem.problemName = id;
            db.ProblemTypes.Add(newProblem);
            Debug.WriteLine(id);
            Debug.WriteLine("OK");
            db.SaveChanges();
            return RedirectToAction("Index");
        }

        //forma za kreiranje na problem
        public ActionResult Create()
        {
            return View();
        }

        //Efikasnost
        [Authorize]
        public ActionResult Performancecalc()
        {
            int brojNaPodneseniTiketi = 0;
            int brojNaReseniTiketi = 0;
            int days=0, hours=0, minutes=0, seconds=0;
            List<Resolver> resolvers = new List<Resolver>();

            if (Request.Form["from"] != null && Request.Form["to"] != null && Request.Form["from"] != "" && Request.Form["to"] != "")
            {
                DateTime from = DateTime.Parse(Request.Form["from"]);
                DateTime to = DateTime.Parse(Request.Form["to"]);

                foreach (resolvedTickets ticket in db.resolvedTickets.ToList())
                {
                    if (ticket.submitTime >= from && ticket.submitTime <= to)
                    {
                        brojNaPodneseniTiketi++;
                    }
                    if (ticket.resolveTime >= from && ticket.resolveTime <= to)
                    {
                        days += ticket.days;
                        hours += ticket.hours;
                        minutes += ticket.minutes;
                        seconds += ticket.seconds;
                        if (resolvers.Find(x => x.ime.Equals(ticket.resolver)) != null)
                        {
                            Resolver resolver = resolvers.Find(x => x.ime.Equals(ticket.resolver));
                            resolvers.Remove(resolver);
                            resolver.brojNaReseniTiketi++;
                            resolver.days += ticket.days;
                            resolver.hours += ticket.hours;
                            resolver.minutes += ticket.minutes;
                            resolver.seconds += ticket.seconds;
                            resolvers.Add(resolver);

                        }
                        else
                        {
                            Resolver resolver = new Resolver();
                            resolver.brojNaReseniTiketi = 1;
                            resolver.ime = ticket.resolver;
                            resolver.days += ticket.days;
                            resolver.hours += ticket.hours;
                            resolver.minutes += ticket.minutes;
                            resolver.seconds += ticket.seconds;
                            resolvers.Add(resolver);
                        }
                        brojNaReseniTiketi++;
                    }
                }
                foreach (Ticket ticket in db.Tickets.ToList())
                {
                    if (ticket.submitTime >= from && ticket.submitTime <= to)
                    {
                        brojNaPodneseniTiketi++;
                    }
                }
            }
            PerformanceModel performanceModel = new PerformanceModel();
            performanceModel.brojNaPodneseniTiketi = brojNaPodneseniTiketi;
            performanceModel.brojNaReseniTiketi = brojNaReseniTiketi;
            performanceModel.efikasnostVoProcenti = (float)brojNaReseniTiketi / brojNaPodneseniTiketi * (float)100.0;
            performanceModel.days = days;
            performanceModel.hours = hours;
            performanceModel.minutes = minutes;
            performanceModel.seconds = seconds;
            performanceModel.resolvers = resolvers;

            return View(performanceModel);
        }

        //prijavi problem
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "Id,email,problemSubject,problemBody")] Ticket ticket)
        {
            
            string ipv4 = "";
            try
            {
                var host = Dns.GetHostEntry(Dns.GetHostName());
                foreach (var ip in host.AddressList)
                {
                    if (ip.AddressFamily == AddressFamily.InterNetwork)
                    {
                        ipv4 = ip.ToString();
                    }
                }
            }
            catch
            {
                Debug.WriteLine("No ip");
            }
            if (ModelState.IsValid)
            {
                
                ticket.ipv4 = ipv4;
                ticket.submitTime = (DateTime)DateTime.UtcNow;
                db.Tickets.Add(ticket); 
                db.SaveChanges();
                return RedirectToAction("success");
            }

            return View(ticket);
        }

        //potvrdi zatvaranje na tiket
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            AdditionalModel additionalModel = new AdditionalModel();
            additionalModel.ticket = db.Tickets.Find(id);
            additionalModel.problemTypes = db.ProblemTypes.ToList();
            if (additionalModel.ticket == null)
            {
                return HttpNotFound();
            }
            return View(additionalModel);
        }

        //zatvori tiket
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            /*resolvedTickets Model
             * email = ticket.email
             * naslov = problemSubject
             * opis = problemBody
             * ip adresa = ipv4
             * tip na problem = problemType
             * vreme na otvaranje = submitTime
             * vreme na prifakjanje = acceptanceTime
             * vreme na zatvaranje = resolveTime
             * days, hours, minutes = potrebno vreme (taboteno)
             * koj go prifatil problemot = acceptor
             * zabeleska = note
             */
            Ticket ticket = db.Tickets.Find(id);
            resolvedTickets resolvedTickets = new resolvedTickets(ticket.Id, ticket.email, ticket.problemSubject, ticket.problemBody, ticket.ipv4, Request.Form["tip"], ticket.submitTime, DateTime.UtcNow, ticket.acceptor, Request.Form["qty"]);
            if(ticket.acceptanceTime != null)
            resolvedTickets.acceptanceTime = ticket.acceptanceTime;
            Debug.WriteLine(Request.Form["tip"]);

            int rabotniMinuti = 0;

            if (Int32.TryParse(Request.Form["raboteno"], out rabotniMinuti))
            {
                if (rabotniMinuti >= 1440)
                {
                    resolvedTickets.days = Int32.Parse(Request.Form["raboteno"]) / 1440;
                    rabotniMinuti = resolvedTickets.days % 1440;

                    resolvedTickets.hours = Int32.Parse(Request.Form["raboteno"]) / 60;
                    rabotniMinuti = resolvedTickets.days % 60;

                    resolvedTickets.minutes = rabotniMinuti;
                }

                else if (Int32.Parse(Request.Form["raboteno"]) >= 60)
                {
                    resolvedTickets.hours = Int32.Parse(Request.Form["raboteno"]) / 60;
                    rabotniMinuti = resolvedTickets.days % 60;

                    resolvedTickets.minutes = rabotniMinuti;
                }
                else
                {
                    resolvedTickets.minutes = rabotniMinuti;
                }
            }
            db.Tickets.Remove(ticket);
            sendMail(resolvedTickets.email, "Проблемот е решен", "Проблемот кој го имавте пратено е успешно решен.");
            db.resolvedTickets.Add(resolvedTickets);
            db.SaveChanges();
            return RedirectToAction("Index");
        }

        //potvrdi prifakjanje na tiket
        public ActionResult Accept(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Ticket ticket = db.Tickets.Find(id);
            AcceptModel acceptModel = new AcceptModel();
            acceptModel.ticket = ticket;
            List<ApplicationUser> users = applicationDbContext.Users.ToList();
            List<string> userMails = new List<string>();

            foreach (ApplicationUser applicationUser in users)
            {
                userMails.Add(applicationUser.Email);
            }
            acceptModel.emails = userMails;
            if (ticket == null)
            {
                return HttpNotFound();
            }
            return View(acceptModel);
        }


       

        //potvrdeno prifakjanje na tiket
        [HttpPost, ActionName("Accept")]
        [ValidateAntiForgeryToken]
        public ActionResult AcceptConfirmed(int id)
        {
            Ticket ticket = db.Tickets.Find(id);
            ticket.acceptanceTime = DateTime.UtcNow;
            //ticket.acceptor = User.Identity.Name;
            ticket.acceptor = Request.Form["res"];
            db.SaveChanges();
            return RedirectToAction("Index");
        }

        //prikaz na Затворени тикети:
        [Authorize]
        public ActionResult ResolvedTickets()
        {
            if (Request.Form["from"] != null && Request.Form["to"] != null && Request.Form["from"] != "" && Request.Form["to"] != "")
            {
                DateTime from = DateTime.Parse(Request.Form["from"]);
                DateTime to = DateTime.Parse(Request.Form["to"]);
                List<resolvedTickets> site = db.resolvedTickets.ToList();
                resolvedSelektirani = new List<resolvedTickets>();
                foreach (resolvedTickets ticket in site)
                {
                    if (ticket.submitTime >= from && ticket.submitTime <= to)
                    {
                        resolvedSelektirani.Add(ticket);
                    }
                }
                return View(resolvedSelektirani);
            }
            resolvedSelektirani = db.resolvedTickets.ToList();
            return View(resolvedSelektirani);
        }

        //Успешно го пријавивте проблемот. Ви благодариме.
        public ActionResult success()
        {
            return View();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
