using System;

namespace JiraTicketManager.Models
{
    public class AuthenticationResult
    {
        public bool IsSuccess { get; set; }
        public string UserEmail { get; set; } = string.Empty;
        public string ErrorMessage { get; set; } = string.Empty;
        public AuthenticationMethod Method { get; set; }
        public DateTime AuthenticatedAt { get; set; }

        public static AuthenticationResult Success(string email, AuthenticationMethod method)
        {
            return new AuthenticationResult
            {
                IsSuccess = true,
                UserEmail = email,
                Method = method,
                AuthenticatedAt = DateTime.Now
            };
        }

        public static AuthenticationResult Failure(string errorMessage, AuthenticationMethod method)
        {
            return new AuthenticationResult
            {
                IsSuccess = false,
                ErrorMessage = errorMessage,
                Method = method,
                AuthenticatedAt = DateTime.Now
            };
        }
    }

    public enum AuthenticationMethod
    {
        MicrosoftSSO,
        JiraAPI
 
    }




}