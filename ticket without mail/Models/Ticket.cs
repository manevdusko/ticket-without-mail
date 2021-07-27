﻿using DocumentFormat.OpenXml.Bibliography;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;
using System.Xml.Linq;

namespace ticket_without_mail.Models
{
    public class Ticket
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Полето за емаил е задолжително")]
        [DisplayName("Вашата e-mail адреса")]
        public string email { get; set; }

        [Required(ErrorMessage = "Полето за наслов на проблемот е задолжително")]
        [DisplayName("Наслов на проблемот")]
        public string problemSubject { get; set; }

        [Required(ErrorMessage = "Внесете опис на проблемот")]
        [DisplayName("Опис на проблемот")]
        public string problemBody { get; set; }

        [DisplayName("Забелешка")]
        public string note { get; set; }

        [DisplayName("Тип на проблем")]
        public List<String> problemType{ get; set; }

        [DisplayName("Време на прифаќање")]
        public DateTime? acceptanceTime { get; set; }
            
        public string ipv4 { get; set; }

        [DisplayName("Време на отварање")]
        public DateTime submitTime { get; set; }

        public Ticket() {
            problemType = new List<string>();

        }
        public Ticket(DateTime time)
        {
            problemType = new List<string>();
            submitTime = time;
        }
    }
}