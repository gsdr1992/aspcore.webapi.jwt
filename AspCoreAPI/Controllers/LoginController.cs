﻿using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Principal;
using System.Threading.Tasks;

namespace AspCoreAPI.Controllers
{
    [Route("api/[controller]")]
    public class LoginController :Controller
    {
        [AllowAnonymous]
        [HttpPost]
        public object Post([FromBody] User usuario,
                            [FromServices]UserDAO userDAO,
                            [FromServices]SigningConfigurations signingConfigurations,
                            [FromServices]TokenConfiguration tokenConfigurations)
        {
            bool credentialisValid = false;

            if(usuario != null && !string.IsNullOrWhiteSpace(usuario.UserID))
            {
                var usuarioBase = userDAO.Find(usuario.UserID);
                credentialisValid = (usuarioBase != null
                                        && usuario.UserID == usuarioBase.UserID
                                        && usuario.AccessKey == usuarioBase.AccessKey);
            }

            if(credentialisValid)
            {
                ClaimsIdentity identity = new ClaimsIdentity(
                        new GenericIdentity(usuario.UserID,"Login"),
                        new[] {
                            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N")),
                            new Claim(JwtRegisteredClaimNames.UniqueName, usuario.UserID)
                        }
                    );


                DateTime dataCriacao = DateTime.Now;
                DateTime dataExpiracao = dataCriacao +
                    TimeSpan.FromSeconds(tokenConfigurations.Seconds);

                var handler = new JwtSecurityTokenHandler();
                var securityToken = handler.CreateToken(new SecurityTokenDescriptor
                {
                    Issuer = tokenConfigurations.Issuer,
                    Audience = tokenConfigurations.Audience,
                    SigningCredentials = signingConfigurations.SigningCredentials,
                    Subject = identity,
                    NotBefore = dataCriacao,
                    Expires = dataExpiracao
                });
                var token = handler.WriteToken(securityToken);

                return new
                {
                    authenticated = true,
                    created = dataCriacao.ToString("yyyy-MM-dd HH:mm:ss"),
                    expiration = dataExpiracao.ToString("yyyy-MM-dd HH:mm:ss"),
                    accessToken = token,
                    message = "OK"
                };
            }
            else
            {
                return new
                {
                    authenticated = false,
                    message = "Falha ao autenticar"
                };
            }
        }
    }
}
