﻿namespace FinalProjApi.Dto
{
    public class UserDtoLogin
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;

        public bool RememberMe { get; set; } = false;
    }
}
