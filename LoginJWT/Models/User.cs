using System;
using System.Collections.Generic;

namespace TokenGenerate.Models
{
    public partial class User
    {
        public int Id { get; set; }
        public string FirstName { get; set; }
        public string MiddleName { get; set; }
        public string LastName { get; set; }
        public string EmailId { get; set; }
        public long? MobileNo { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
        public string ConfirmPassword { get; set; }
        public string Token { get; set; }
        public DateTime? Date { get; set; }
        public int? Count { get; set; }
        public bool? Status { get; set; }
    }
}
