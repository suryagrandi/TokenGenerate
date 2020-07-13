using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TokenGenerate.Models;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Configuration;
using System.Text;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Mail;
using System.Net;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System.IO;
using System.Security.Cryptography;

namespace TokenGenerate.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MailController : ControllerBase
    {
        SuryaContext _context;
        IConfiguration _config;

        public string DecryptPassword(string password)
        {
            string EncryptionKeys = "MAKV2SPBNI99212";
            byte[] cipherBytes = Convert.FromBase64String(password);
            using (Aes encryptor = Aes.Create())
            {
                Rfc2898DeriveBytes pdb = new Rfc2898DeriveBytes(EncryptionKeys, new byte[] { 0x49, 0x76, 0x61, 0x6e, 0x20, 0x4d, 0x65, 0x64, 0x76, 0x65, 0x64, 0x65, 0x76 });
                encryptor.Key = pdb.GetBytes(32);
                encryptor.IV = pdb.GetBytes(16);
                using (MemoryStream ms = new MemoryStream())
                {
                    using (CryptoStream cs = new CryptoStream(ms, encryptor.CreateDecryptor(), CryptoStreamMode.Write))
                    {
                        cs.Write(cipherBytes, 0, cipherBytes.Length);
                        cs.Close();
                    }
                    password = Encoding.Unicode.GetString(ms.ToArray());
                    return password;
                }
            }
        }
        public string EncryptPassword(string password)
        {
            string EncryptionKey = "MAKV2SPBNI99212";
            byte[] clearBytes = Encoding.Unicode.GetBytes(password);
            using (Aes encryptor = Aes.Create())
            {
                Rfc2898DeriveBytes pdb = new Rfc2898DeriveBytes(EncryptionKey, new byte[] { 0x49, 0x76, 0x61, 0x6e, 0x20, 0x4d, 0x65, 0x64, 0x76, 0x65, 0x64, 0x65, 0x76 });
                encryptor.Key = pdb.GetBytes(32);
                encryptor.IV = pdb.GetBytes(16);
                using (MemoryStream ms = new MemoryStream())
                {
                    using (CryptoStream cs = new CryptoStream(ms, encryptor.CreateEncryptor(), CryptoStreamMode.Write))
                    {
                        cs.Write(clearBytes, 0, clearBytes.Length);
                        cs.Close();
                    }
                    password = Convert.ToBase64String(ms.ToArray());
                }
            }
            return password;
        }
        public MailController(SuryaContext context, IConfiguration config)
        {
            _context = context;
            _config = config;
        }

        // GET: api/curd
        [HttpGet]
        public async Task<ActionResult<IEnumerable<User>>> GetUser()
        {
            return await _context.User.ToListAsync();
        }

        // GET: api/curd/5
        [HttpGet("{id}")]
        public async Task<ActionResult<User>> GetUser(int id)
        {
            var user = await _context.User.FindAsync(id);

            if (user == null)
            {
                return NotFound();
            }

            return user;
        }


        [HttpPut("{id}")]
        public async Task<IActionResult> PutUser(int id, User user)
        {
            if (id != user.Id)
            {
                return BadRequest();
            }

            _context.Entry(user).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!UserExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }


        [Route("postuser")]
        [HttpPost]
        public IActionResult PostUser(User user)
        {

            try
            {
                user.Password = EncryptPassword(user.Password);
                user.ConfirmPassword = user.Password;
                user.Status = true;
                user.Count = 0;
                _context.User.Add(user);
                _context.SaveChanges();
                return Ok(
                  new
                  {
                      success = true,
                      status = 200,
                      data = "Registration successful"
                  }); ;
            }
            catch (Exception ex)
            {

                return Ok(new
                {
                    status = false,
                    code = 401,
                    message = "Data not registered."
                });
            }
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult<User>> DeleteUser(int id)
        {
            var user = await _context.User.FindAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            _context.User.Remove(user);
            await _context.SaveChangesAsync();

            return user;
        }

        private bool UserExists(int id)
        {
            return _context.User.Any(e => e.Id == id);
        }

        [Route("Login")] // /login
        [HttpPost]
        public IActionResult Login(User Us)
        {
            try
            {
                User m = _context.User.Where(x => x.EmailId == Us.EmailId).FirstOrDefault();
                if (m != null)
                {
                    
                    var password = DecryptPassword(m.Password);
                    var data = _context.User.Where(x => x.EmailId == Us.EmailId && password == Us.Password).FirstOrDefault();
                    if (data != null)
                    {
                        var accountStatus = _context.User.Where(x => x.EmailId == m.EmailId && x.Count >= 3).FirstOrDefault();
                        if(accountStatus==null)
                        { 
                        var signinKey = new SymmetricSecurityKey(
                          Encoding.UTF8.GetBytes(_config["Jwt:SigningKey"]));
                        int expiryInMinutes = Convert.ToInt32(_config["Jwt:ExpiryInMinutes"]);
                        var token = new JwtSecurityToken(
                          issuer: _config["Jwt:Site"],
                          audience: _config["Jwt:Site"],
                          expires: DateTime.UtcNow.AddMinutes(expiryInMinutes),
                          signingCredentials: new SigningCredentials(signinKey, SecurityAlgorithms.HmacSha256)
                        );

                        var tokenData = new JwtSecurityTokenHandler().WriteToken(token);

                        m.Token = tokenData;
                        m.Date = DateTime.Now;
                        _context.Entry(m).State = EntityState.Modified;
                        _context.SaveChanges();
                        return Ok(
                          new
                          {
                              success = true,
                              status = 200,
                              token = new JwtSecurityTokenHandler().WriteToken(token),
                              expiration = token.ValidTo,
                              EmailIds = m.EmailId
                          });
                    }
                        else
                        {
                            m.Status = false;
                            _context.Entry(m).State = EntityState.Modified;
                            _context.SaveChanges();
                            return Ok(
                                  new
                                  {
                                      status = false,
                                      code = 401,
                                      message = "Account has been locked"
                                  });
                        }
                    }
                    else
                    {
                        m.Count = m.Count + 1;
                        _context.Entry(m).State = EntityState.Modified;
                        _context.SaveChanges();
                        var accountStatus = _context.User.Where(x => x.EmailId == m.EmailId && x.Count >= 3).FirstOrDefault();
                        if (accountStatus == null)
                        {
                            return Ok(
                                new
                                {
                                    status = false,
                                    code = 401,
                                    message = "Invalid credentials"
                                });
                        }
                        else
                        {
                            m.Status = false;
                            _context.Entry(m).State = EntityState.Modified;
                            _context.SaveChanges();
                            return Ok(
                                new
                                {
                                    status = false,
                                    code = 401,
                                    message = "Account has been locked"
                                });
                        }
                    }
                }
                else
                {
                    User models = _context.User.FirstOrDefault(x => x.EmailId == m.EmailId);
                    models.Count = models.Count + 1;
                    _context.Entry(models).State = EntityState.Modified;
                    _context.SaveChanges();
                    return Ok(
                            new
                            {
                                status = false,
                                code = 401,
                                message = "Invalid credentials"
                            });
                }
            }
            catch (Exception ex)
            {
                return Ok(new { status = ex });
            }
        }

        [HttpPost("AccountVerify")]
        public IActionResult AccountVerify(User us)
        {
            var user = _context.User.Where(x => x.EmailId == us.EmailId).FirstOrDefault();
            if (user != null)
            {
                User model = _context.User.FirstOrDefault(x => x.EmailId == us.EmailId);
                var link = "http://localhost:5566/activateaccount";
                var signinKey = new SymmetricSecurityKey(
               Encoding.UTF8.GetBytes(_config["Jwt:SigningKey"]));
                int expiryInMinutes = Convert.ToInt32(_config["Jwt:ExpiryInMinutes"]);
                var token = new JwtSecurityToken(
                  issuer: _config["Jwt:Site"],
                  audience: _config["Jwt:Site"],
                  expires: DateTime.UtcNow.AddMinutes(expiryInMinutes),
                  signingCredentials: new SigningCredentials(signinKey, SecurityAlgorithms.HmacSha256)
                );
                string tokenData = new JwtSecurityTokenHandler().WriteToken(token);
                var fromEmail = new MailAddress("g.sribhaskar5@gmail.com", "Hello");
                var toEmail = new MailAddress("g.sribhaskar5@gmail.com");
                var fromEmailPassword = "Surya8919";
                string subject = "Activate account";
                string body = "Hi,<br/><br/> Kindly click on below link to activate your Account" +
                "<br/><br/><a href=" + link + ">Activate your account</a>" + "<br/><br/>Here is the Token=" + tokenData;

                model.Token = tokenData;
                _context.Entry(model).State = EntityState.Modified;
                _context.SaveChanges();

                var smtp = new SmtpClient
                {
                    Host = "smtp.gmail.com",
                    Port = 587,
                    EnableSsl = true,
                    DeliveryMethod = SmtpDeliveryMethod.Network,
                    UseDefaultCredentials = false,
                    Credentials = new NetworkCredential(fromEmail.Address, fromEmailPassword),
                };
                using (var message = new MailMessage(fromEmail, toEmail)
                {
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = true
                })
                    smtp.Send(message);
                return Ok(new { success = true, status = 200, data = "Kindly check your Email Id to activate account." });
            }
            else
            {
                return Ok(new { success = false, status = 401, data = "Unauthorized user" });
            }
        }



        [HttpPost("ActivateAccount")]
        public IActionResult ActivateAccount(User us)
        {
            var user = _context.User.Where(x => x.EmailId == us.EmailId && x.Token == us.Token && x.Status == false).FirstOrDefault();



            if (user != null)
            {
                User model = _context.User.FirstOrDefault(x => x.EmailId == us.EmailId);
                model.Count = 0;
                model.Status = true;
                _context.Entry(model).State = EntityState.Modified;
                _context.SaveChanges();
                return Ok(new { success = true, status = 200, data = "Account has been activated." });
            }
            else
            {
                return Ok(
                   new
                   {
                       status = false,
                       code = 401,
                       message = "Invalid details"
                   });
            }
        }

        [HttpPost("LogOut")]
        public IActionResult LogOut(User user)
        {
            var users = _context.User.Where(x => x.EmailId == user.EmailId && x.Password == user.Password).FirstOrDefault();

            if (user != null)
            {
                User model = _context.User.FirstOrDefault(x => x.EmailId == user.EmailId);
                model.Token = null;
                model.Date = null;
                _context.Entry(model).State = EntityState.Modified;
                _context.SaveChanges();
                return Ok();
            }
            return Unauthorized();
        }

        [HttpPost("ForgotPassword")]
        public IActionResult ForgotPassword(User user)
        {
            User model = _context.User.FirstOrDefault(x => x.EmailId == user.EmailId);
            if (model != null)
            {
                var signinKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(_config["Jwt:SigningKey"]));
                int expiryInMinutes = Convert.ToInt32(_config["Jwt:ExpiryInMinutes"]);
                var token = new JwtSecurityToken(
                  issuer: _config["Jwt:Site"],
                  audience: _config["Jwt:Site"],
                  expires: DateTime.UtcNow.AddMinutes(expiryInMinutes),
                  signingCredentials: new SigningCredentials(signinKey, SecurityAlgorithms.HmacSha256)
                );
                string tokenData = new JwtSecurityTokenHandler().WriteToken(token);
                DateTime currentTime = DateTime.Now;
                TimeSpan addMinutes = new TimeSpan(0, 0, 5, 0);
                DateTime expiryTime = currentTime.Add(addMinutes);
                model.Date = expiryTime;
                model.Token = tokenData;
                _context.Entry(model).State = EntityState.Modified;
                _context.SaveChanges();



                var link = "http://localhost:5566/resetpassword";
                var fromEmail = new MailAddress("g.sribhaskar5@gmail.com", "Reset Password");
                var toEmail = new MailAddress("g.sribhaskar5@gmail.com");
                var fromEmailPassword = "Surya8919";
                string subject = "Password recovery URL";
                string body = "Hi,<br/><br/>We got request for reset your account password. Please find the token & click on the below link to reset your password" +
                    "<br/><br/> Token :" + tokenData +
                "<br/><br/><a href=" + link + ">Reset Password link</a>";



                var smtp = new SmtpClient
                {
                    Host = "smtp.gmail.com",
                    Port = 587,
                    EnableSsl = true,
                    DeliveryMethod = SmtpDeliveryMethod.Network,
                    UseDefaultCredentials = false,
                    Credentials = new NetworkCredential(fromEmail.Address, fromEmailPassword),



                };
                using (var message = new MailMessage(fromEmail, toEmail)
                {
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = true
                })
                    smtp.Send(message);
                return Ok(new { success = true, status = 200, data = "Kindly check your Email Id to reset password." });
            }
            else
            {
                return Ok(new { success = false, status = 401, data = "Unauthorized user" });
            }
        }

        [HttpPost("ResetPassword")]
        public IActionResult ResetPassword(User user)
        {
            var users = _context.User.Select(x => x.EmailId == user.EmailId && x.Token == user.Token).FirstOrDefault();

            if (user != null)
            {
                User model = _context.User.FirstOrDefault(x => x.EmailId == user.EmailId);
                DateTime date = (DateTime)model.Date;
                TimeSpan duration = new TimeSpan(0, 0, 5, 0);
                DateTime value = date.Add(duration);
                if (DateTime.Now <= value)
                {
                    model.Password = EncryptPassword(user.Password);
                    model.ConfirmPassword = EncryptPassword(user.Password);
                    _context.Entry(model).State = EntityState.Modified;
                    _context.SaveChanges();
                    return Ok(new { success = true, status = 200, data = "Password has been changed. Kindly login with new password" });
                }
                else
                {
                    return Ok(new { success = false, status = 400, data = "Token has been expired. Please try again." });
                }
            }
            return Unauthorized(new { success = false, status = 400, data = "Unauthorizes user" });
        }
    }
}
